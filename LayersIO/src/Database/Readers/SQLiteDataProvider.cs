using LayersIO.Connection;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LayersIO.Database.Readers
{
    public abstract class SQLiteDataProvider<TKey, TValue> : ILayerDataProvider<TKey, TValue> where TKey : notnull
    {
        private protected readonly LayersDatabaseContextSqlite _context;
        private protected readonly ILogger? _logger;

        static SQLiteDataProvider() { }

        public SQLiteDataProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory)
        {
            _context = factory.CreateDbContext();
        }

        public SQLiteDataProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory, ILogger? logger) : this(factory)
        {
            _logger = logger;
        }

        public abstract Dictionary<TKey, TValue> GetData();
        public abstract TValue? GetItem(TKey key);
    }
}
