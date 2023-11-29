using LayerWorks.Dictionaries;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LayerWorks.ExternalData
{
    internal class XmlDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue>
    {
        private string FilePath { get; set; }
        private readonly FileInfo _fileInfo;

        internal XmlDictionaryDataProvider(string path)
        {
            FilePath = path;
            _fileInfo = new FileInfo(FilePath);
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(FilePath, FileMode.Open))
            {
                XmlSerializableDictionary<TKey, TValue> dct = xs.Deserialize(fs) as XmlSerializableDictionary<TKey, TValue>;
                return dct;
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(FilePath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }
    }
}