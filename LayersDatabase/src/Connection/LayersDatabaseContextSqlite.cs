using LayersIO.Model;
using LoaderCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace LayersIO.Connection
{
    public class LayersDatabaseContextSqlite : DbContext
    {
        public ILogger? _logger = LoaderCore.NcetCore.ServiceProvider.GetService<ILogger>();
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
            _logger?.LogInformation("Сохранение данных");
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