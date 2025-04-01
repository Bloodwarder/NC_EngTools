using LoaderCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Xml
{
    public class XmlDataWriterFactory<TKey, TValue> : IDataWriterFactory<TKey, TValue> where TKey : notnull
    {
        public ILayerDataWriter<TKey, TValue> CreateWriter(string path)
        {
            return new XmlLayerDataWriter<TKey, TValue>(path);
        }
    }
}
