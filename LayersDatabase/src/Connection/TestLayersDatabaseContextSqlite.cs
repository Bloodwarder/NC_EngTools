using Microsoft.EntityFrameworkCore;
using LoaderCore.Utilities;
using LayersIO.Model;
using System.Reflection;

namespace LayersIO.Connection
{
    public class TestLayersDatabaseContextSqlite : DbContext
    {
        public DbSet<LayerData> LayerData { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroupData { get; set; } = null!;

        private string _dataSource;
        
        public TestLayersDatabaseContextSqlite(string dataSource)
        {
            _dataSource = dataSource;
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public override int SaveChanges()
        {
            Logger.WriteLog("Сохранение данных");
            return base.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dataSource}");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LayerDataConfiguration());
            modelBuilder.ApplyConfiguration(new LayerGroupDataConfiguration());
            //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}