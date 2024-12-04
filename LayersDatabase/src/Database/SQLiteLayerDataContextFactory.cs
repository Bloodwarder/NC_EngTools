using LayersIO.Connection;
using LayersIO.DataTransfer;
using Microsoft.Extensions.Logging;

namespace LayersIO.Database
{
    public class SQLiteLayerDataContextFactory //: IDbContextFactory<LayersDatabaseContextSqlite>
    {
        readonly ILogger _logger;
        static SQLiteLayerDataContextFactory()
        {
            TinyMapperConfigurer.Configure();
        }
        public SQLiteLayerDataContextFactory(ILogger logger) 
        {
            _logger = logger;            
        }
        public LayersDatabaseContextSqlite CreateDbContext(string path)
        {
            return File.Exists(path) ? new(path, _logger) : throw new FileNotFoundException("Файл базы данных не найден");
        }


    }
}