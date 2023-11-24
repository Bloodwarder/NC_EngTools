using Microsoft.EntityFrameworkCore;

using LayersDatabase.Model;

namespace LayersDatabase.Connection
{
    public class LayersDatabaseContext : DbContext
    {
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;

        public LayersDatabaseContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=LayersDBtest.db");
        }
    }
}