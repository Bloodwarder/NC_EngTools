using LayersIO.Connection;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Writers
{
    public abstract class SQLiteDataWriter<TKey, TValue> : ILayerDataWriter<TKey, TValue> where TKey : notnull
    {
        private protected readonly IDbContextFactory<LayersDatabaseContextSqlite> _contextFactory;

        static SQLiteDataWriter() { }
        public SQLiteDataWriter(IDbContextFactory<LayersDatabaseContextSqlite> factory)
        {
            _contextFactory = factory;
        }

        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);
        public abstract void OverwriteItem(TKey key, TValue item);
        protected abstract void OverwriteItemInContext(TKey key, TValue item, LayersDatabaseContextSqlite context);
        protected abstract void OverwriteItemInContext(TKey key, TValue item, LayersDatabaseContextSqlite db, IQueryable querable);

    }
}
