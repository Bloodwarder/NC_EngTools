using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LayersIO.Connection
{
    public sealed class PrototypeLayersDatabaseContextSqlite : LayersDatabaseContextSqlite
    {

        public PrototypeLayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base(dataSource, logger)
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dataSource}");
            //base.OnConfiguring(optionsBuilder);
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.ApplyConfiguration(new LayerDataConfiguration());
        //    modelBuilder.ApplyConfiguration(new LayerGroupDataConfiguration());
        //    //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        //    base.OnModelCreating(modelBuilder);
        //}
    }
}