﻿using System.Xml.Serialization;

namespace LayersIO.Xml
{

    /// <summary>
    /// Сериализуемый словарь
    /// </summary>
    /// <typeparam name="TKey">Ключ</typeparam>
    /// <typeparam name="TValue">Значение</typeparam>
    [XmlRoot("Dictionary")]
    public class XmlSerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable where TKey : notnull
    {
        /// <inheritdoc/>
        public System.Xml.Schema.XmlSchema? GetSchema()
        {
            return null;
        }

        /// <inheritdoc/>
        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new(typeof(TKey));
            XmlSerializer valueSerializer = new(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }
        /// <inheritdoc/>
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new(typeof(TKey));
            XmlSerializer valueSerializer = new(typeof(TValue));
            foreach (TKey key in Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}