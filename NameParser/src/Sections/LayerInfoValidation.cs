using System.Xml.Linq;

namespace NameClassifiers.Sections
{

    internal class LayerInfoValidation
    {
        private const string ValidStatusesElementName = "ValidStatuses";
        private const string ValidPrimaryElementName = "ValidPrimary";
        private const string ValidAuxilaryElementName = "ValidAuxilary";
        private const string ValidSuffixElementName = "ValidSuffix";

        internal LayerInfoValidation(XElement validationElement)
        {
            InitializeStatusValidation(validationElement);
            InitializePrimaryValidation(validationElement);
            InitializeAuxilaryClassifierValidation(validationElement);
            InitializeSuffixValidation(validationElement);
        }

        private void InitializePrimaryValidation(XElement validationElement)
        {
            // Получить элемент валидации основного классификатора
            XElement? primaryElement = validationElement.Element(ValidPrimaryElementName);
            if (primaryElement != null)
            {
                ValidPrimary = primaryElement?.Elements("ChapterReference")
                                              .Select(e => e.Attribute("Value")!.Value)
                                              .ToHashSet();
                // При необходимости добавить трансформации для основного и дополнительных классификаторов. В исходном варианте - не нужны
            }
        }

        private void InitializeAuxilaryClassifierValidation(XElement validationElement)
        {
            XElement? auxilaryElement = validationElement.Element(ValidAuxilaryElementName);
            if (auxilaryElement != null)
            {
                XElement[] references = auxilaryElement.Elements("ClassifierReference").ToArray();
                string[] classifierKeys = references.Select(e => e.Attribute("Name")!.Value).Distinct().ToArray();
                if (references.Length > 0)
                    ValidAuxilary ??= new();
                else
                    return;
                foreach (XElement reference in references)
                {
                    foreach (string classifierKey in classifierKeys)
                    {
                        HashSet<string> validSet = reference.Elements()
                                                            .Where(e => e.Attribute("Name")!.Value == classifierKey)
                                                            .Select(e => e.Attribute("Value")!.Value)
                                                            .ToHashSet();
                        if (validSet.Any())
                        {
                            ValidAuxilary.Add(reference.Attribute("Name")!.Value, validSet);
                        }
                    }
                }
                // Добавить трансформации при необходимости
            }
        }

        private void InitializeStatusValidation(XElement validationElement)
        {
            // Получить элемент валидации статуса
            XElement? statusElement = validationElement.Element(ValidStatusesElementName);
            // если нет - все статусы допустимы
            if (statusElement != null)
            {
                // если да - получить сет с допустимыми статусами
                ValidStatus = statusElement?.Elements("StatusReference")
                                            .Select(e => e.Attribute("Value")!.Value)
                                            .ToHashSet();
                // если указано - получить элемент с трансформациями - какой статус должен получить объект с недопустимым статусом
                XElement? transformations = statusElement!.Element("Transformations");
                if (transformations != null)
                {
                    Dictionary<string, string> dict = new();
                    foreach (XElement transformation in transformations.Elements())
                    {
                        string? key = transformation.Element("Source")?.Elements().First().Attribute("Value")?.Value;
                        string? value = transformation.Element("Output")?.Elements().First().Attribute("Value")?.Value;
                        if (key != null && value != null)
                            dict.Add(key, value);
                    }
                    StatusTransformations = dict;
                }
            }
        }

        private static void InitializeSuffixValidation(XElement validationElement)
        {
            XElement? suffixElement = validationElement.Element(ValidSuffixElementName);
            if (suffixElement != null)
            {

            }
        }

        internal HashSet<string>? ValidPrimary { get; private set; }
        internal Dictionary<string, HashSet<string>>? ValidAuxilary { get; private set; }
        internal HashSet<string>? ValidStatus { get; private set; }
        internal Dictionary<string, HashSet<bool>>? ValidSuffix { get; private set; }
        internal Dictionary<string, string>? StatusTransformations { get; private set; }
        //internal Dictionary<string, string> PrimaryTransformations { get; private set; } = new();
        //internal Dictionary<string, Dictionary<string, string>> AuxilaryTransformations { get; private set; } = new();
        // TODO: Реализовать валидацию и трансформацию суффиксов
        //internal Dictionary<string, bool> SuffixTransformations { get; private set; } = new();



        internal bool TryValidateAndTransform(LayerInfo layerInfo)
        {
            if (!ValidateStatus(layerInfo))
                return false;
            if (!ValidatePrimary(layerInfo))
                return false;
            if (!ValidateAuxilary(layerInfo))
                return false;
            if (!TransoformStatus(layerInfo))
                return false;
            // остальные трансформации
            return true;
        }

        private bool ValidateStatus(LayerInfo layerInfo) =>
            (ValidStatus?.Contains(layerInfo.Status!) ?? true || !ValidStatus.Any()) || (StatusTransformations?.ContainsKey(layerInfo.Status!) ?? false);
        private bool TransoformStatus(LayerInfo layerInfo)
        {
            bool validStatus = ValidStatus?.Contains(layerInfo.Status!) ?? true;
            if (validStatus)
                return true;
            if (StatusTransformations == null)
                return false;
            bool success = StatusTransformations.TryGetValue(layerInfo.Status!, out string? newStatus);
            layerInfo.Status = newStatus ?? layerInfo.Status;
            return success;
        }

        private bool ValidatePrimary(LayerInfo layerInfo) =>
            ValidPrimary?.Contains(layerInfo.PrimaryClassifier!) ?? true;

        private bool ValidateAuxilary(LayerInfo layerInfo)
        {
            foreach (var c in layerInfo.ParentParser.AuxilaryClassifiers)
            {
                if (ValidAuxilary?[c.Key].Contains(layerInfo.AuxilaryClassifiers[c.Key]) ?? true)
                    continue;
                return false;
            }
            return true;
        }
    }
}
