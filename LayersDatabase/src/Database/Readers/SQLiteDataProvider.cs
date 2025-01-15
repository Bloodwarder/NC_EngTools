using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Readers
{
    public abstract class SQLiteDataProvider<TKey, TValue> : ILayerDataProvider<TKey, TValue> where TKey : notnull
    {
        private protected readonly string _path;
        private protected readonly static SQLiteLayerDataContextFactory _contextFactory;

        static SQLiteDataProvider()
        {
            _contextFactory = NcetCore.ServiceProvider.GetRequiredService<SQLiteLayerDataContextFactory>();
            // TODO: заменить на инъекцию, и FallbackPath
        }
        public SQLiteDataProvider(string path) 
        {
            _path = path; 
        }
        public abstract Dictionary<TKey, TValue> GetData();
        public abstract TValue? GetItem(TKey key);
    }
}
