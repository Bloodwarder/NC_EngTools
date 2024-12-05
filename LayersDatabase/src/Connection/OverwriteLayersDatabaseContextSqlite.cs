using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LayersIO.Connection
{
    [Obsolete("Class is not using migrations and may cause errors")]
    public sealed class OverwriteLayersDatabaseContextSqlite : LayersDatabaseContextSqlite
    {

        public OverwriteLayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base(dataSource, logger)
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