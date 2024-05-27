using Microsoft.EntityFrameworkCore;

using LayersIO.Model;
using System.Reflection;
using LoaderCore.Utilities;

namespace LayersIO.Connection
{
    public class LayersDatabaseContextSqlite : DbContext
    {
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;

        private readonly string _dataSource;
        public LayersDatabaseContextSqlite(string dataSource)
        {
            Database.EnsureCreated();
            _dataSource = dataSource;
        }

        public override int SaveChanges()
        {
            Logger.WriteLog?.Invoke("Сохранение данных");
            return base.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dataSource}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}