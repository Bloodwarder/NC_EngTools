using LayersIO.Connection;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Readers
{
    public abstract class SQLiteDataProvider<TKey, TValue> : ILayerDataProvider<TKey, TValue> where TKey : notnull
    {
        private protected readonly string _path;
        private protected readonly static SQLiteLayerDataContextFactory _contextFactory;

        static SQLiteDataProvider()
        {
            _contextFactory = LoaderExtension.ServiceProvider.GetRequiredService<SQLiteLayerDataContextFactory>();
        }
        public SQLiteDataProvider(string path) 
        {
            _path = path; 
        }
        public abstract Dictionary<TKey, TValue> GetData();
        public abstract TValue? GetItem(TKey key);
    }
}
