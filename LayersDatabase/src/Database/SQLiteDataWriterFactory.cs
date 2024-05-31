using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Database
{
    public class SQLiteDataWriterFactory<TKey, TValue> where TKey : notnull
    {
        private Func<string, ILayerDataWriter<TKey, TValue>> _writerFactioryMethod =
            LoaderCore.LoaderExtension.ServiceProvider.GetRequiredService<Func<string, ILayerDataWriter<TKey, TValue>>>();

        public ILayerDataWriter<TKey, TValue> CreateWriter(string path)
        {
            return _writerFactioryMethod(path);
        }
    }
}
