using LayersIO.Connection;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Writers
{
    public abstract class SQLiteDataWriter<TKey, TValue> : ILayerDataWriter<TKey, TValue> where TKey : notnull
    {
        private protected readonly string _path;
        private protected readonly static SQLiteLayerDataContextFactory _contextFactory;

        static SQLiteDataWriter()
        {
            _contextFactory = NcetCore.ServiceProvider.GetRequiredService<SQLiteLayerDataContextFactory>();
        }
        public SQLiteDataWriter(string path)
        {
            _path = path;
        }

        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);
        public abstract void OverwriteItem(TKey key, TValue item);
        protected abstract void OverwriteItemInContext(TKey key, TValue item, LayersDatabaseContextSqlite context);
        protected abstract void OverwriteItemInContext(TKey key, TValue item, LayersDatabaseContextSqlite db, IQueryable querable);

    }
}
