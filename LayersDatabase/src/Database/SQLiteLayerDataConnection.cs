using LayersIO.Connection;
using LayersIO.DataTransfer;

namespace LayersIO.Database
{
    internal abstract class SQLiteLayerDataConnection
    {
        protected FileInfo _database { get; init; }
        static SQLiteLayerDataConnection()
        {
            TinyMapperConfigurer.Configure();
        }
        internal SQLiteLayerDataConnection(string path)
        {
            _database = new FileInfo(path);
            if (!_database.Exists)
                throw new FileNotFoundException("Файл базы данных не найден");
        }
        private protected LayersDatabaseContextSqlite GetNewContext() => new(_database.FullName);
    }
}