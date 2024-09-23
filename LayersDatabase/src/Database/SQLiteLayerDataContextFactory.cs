using LayersIO.Connection;
using LayersIO.DataTransfer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database
{
    public class SQLiteLayerDataContextFactory
    {
        static SQLiteLayerDataContextFactory()
        {
            TinyMapperConfigurer.Configure();
        }
        public SQLiteLayerDataContextFactory() { }
        public LayersDatabaseContextSqlite CreateDbContext(string path)
        {
            return File.Exists(path) ? new(path) : throw new FileNotFoundException("Файл базы данных не найден");
        }


    }
}