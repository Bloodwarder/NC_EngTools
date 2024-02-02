﻿using LayersIO.ExternalData;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LayersIO.Xml
{
    public class XmlLayerDataProvider<TKey, TValue> : LayerDataProvider<TKey, TValue> 
        where TKey : class
        where TValue : class
    {
        private string FilePath { get; set; }
        private readonly FileInfo _fileInfo;

        public XmlLayerDataProvider(string path)
        {
            FilePath = path;
            _fileInfo = new FileInfo(FilePath);
        }

        public override Dictionary<TKey, TValue> GetData()
        {
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new(FilePath, FileMode.Open))
            {
                XmlSerializableDictionary<TKey, TValue> dct = xs.Deserialize(fs) as XmlSerializableDictionary<TKey, TValue>;
                return dct!;
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new(FilePath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }
    }
}