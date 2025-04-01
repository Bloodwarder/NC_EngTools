using LayersIO.Connection;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Readers
{
    public abstract class SQLiteDataProvider<TKey, TValue> : ILayerDataProvider<TKey, TValue> where TKey : notnull
    {
        //private protected readonly IDbContextFactory<LayersDatabaseContextSqlite> _contextFactory;
        private protected readonly LayersDatabaseContextSqlite _context;

        static SQLiteDataProvider()
        {

        }
        public SQLiteDataProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) 
        {
            //_contextFactory = factory;
            _context = factory.CreateDbContext();
        }
        public abstract Dictionary<TKey, TValue> GetData();
        public abstract TValue? GetItem(TKey key);
    }
}
