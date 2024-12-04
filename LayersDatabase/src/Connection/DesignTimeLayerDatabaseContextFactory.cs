using Microsoft.EntityFrameworkCore.Design;

namespace LayersIO.Connection
{
    public class DesignTimeLayerDatabaseContextFactory : IDesignTimeDbContextFactory<LayersDatabaseContextSqlite>
    {
        private const string SourceDatabasePath = @"C:\Users\konovalove\source\repos\Bloodwarder\NC_EngTools\LayersDatabase\Data\LayerData_Testing.db";
        public LayersDatabaseContextSqlite CreateDbContext(string[] args)
        {
            return new(SourceDatabasePath, null);
        }
    }
}