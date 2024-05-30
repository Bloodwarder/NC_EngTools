using LoaderCore.Interfaces;
using System.Xml.Serialization;

namespace LayersIO.Xml
{
    public class XmlLayerDataWriter<TKey, TValue> : ILayerDataWriter<TKey, TValue>
    where TKey : notnull
    {
        private string FilePath { get; set; }
        private readonly FileInfo _fileInfo;

        public XmlLayerDataWriter(string path)
        {
            FilePath = path;
            _fileInfo = new FileInfo(FilePath);
        }
        public void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new(FilePath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }

        public void OverwriteItem(Dictionary<TKey, TValue> dictionary)
        {
            throw new NotImplementedException();
        }
    }
}