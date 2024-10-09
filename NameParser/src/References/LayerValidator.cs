using System.Xml.Serialization;

namespace NameClassifiers.References
{
    [XmlRoot(ElementName ="ValidSet")]
    public class LayerValidator
    {
        //[XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        [XmlElement(Type = typeof(BoolReference))]
        public SectionReference[] ValidReferences { get; set; }

        [XmlElement(ElementName = nameof(Transformations) ,Type = typeof(Transformation[]))]
        public Transformation[] Transformations { get; set; }

        public bool IsValid()
        {
            bool isNull = ValidReferences != null;
            if (isNull)
                return false;
            bool isEmpty = ValidReferences!.Any();
            if (isEmpty)
                return false;

            Type type = ValidReferences!.FirstOrDefault()!.GetType();
            bool isSameTyped = ValidReferences!.All(r => r.GetType() == type);
            if (!isSameTyped)
                return false;
            if (type == typeof(NamedSectionReference))
            {
                bool isSameNamed = ValidReferences!.Select(r => (NamedSectionReference)r).Select(nr => nr.Name).Distinct().Count() == 1;
                if (!isSameNamed)
                    return false;
            }
            if (Transformations == null)
                return true;
            return Transformations.All(t => t.Source.GetType() == type && t.Output.GetType() == type);
        }

        // TODO: сделать систему проверки или транзакций для трансформации. Необходимо, чтобы финальное значение с учётом трансформаций также было валидным.
        public bool ValidateLayerInfo(LayerInfo layerInfo) => ValidReferences.Any(r => r.Match(layerInfo));
        public bool Transform(LayerInfo layerInfo)
        {
            if (Transformations.Any(t => t.Source.Any(r => r.Match(layerInfo))))
            {
                var transformation = Transformations.Where(t => t.Source.Any(r => r.Match(layerInfo))).Single();
                var reference = transformation.Output.Single();
                layerInfo.AssignReferenceValue(reference);
                return true;
            }
            return false;
        }
    }
}
