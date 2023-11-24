using Microsoft.EntityFrameworkCore;

using LayersDatabase.Model;

namespace LayersDatabase.Connection
{
    public class LayersDatabaseContextSqlite : DbContext
    {
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;

        private string _dataSource;
        public LayersDatabaseContextSqlite(string dataSource)
        {
            Database.EnsureCreated();
            _dataSource = dataSource;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dataSource}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LayerDataConfiguration());
            modelBuilder.ApplyConfiguration(new LayerGroupDataConfiguration());
        }
    }
}