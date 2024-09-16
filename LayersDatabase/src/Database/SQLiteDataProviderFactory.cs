using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Database
{
    public class SQLiteDataProviderFactory<TKey, TValue> : IDataProviderFactory<TKey,TValue> where TKey : notnull
    {
        private Func<string, ILayerDataProvider<TKey, TValue>> _providerFactioryMethod =
            LoaderCore.LoaderExtension.ServiceProvider.GetRequiredService<Func<string, ILayerDataProvider<TKey, TValue>>>();

        public ILayerDataProvider<TKey, TValue> CreateProvider(string path)
        {
            return _providerFactioryMethod(path);
        }
    }
}
