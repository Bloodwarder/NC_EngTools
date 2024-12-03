using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LayersIO.Connection
{
    public class LayersDatabaseContextSqlite : DbContext
    {
        private readonly ILogger? _logger;
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;

        private readonly string _dataSource;
        public LayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base()
        {
            _logger = logger;
            _dataSource = dataSource;
            Database.EnsureCreated();
            _logger?.LogDebug("Подключение к {DataSource}", dataSource);
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
            modelBuilder.ApplyConfiguration(new LayerDataConfiguration());
            modelBuilder.ApplyConfiguration(new LayerGroupDataConfiguration());
            //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }

    public class DesignTimeLayerDatabaseContextFactory : IDesignTimeDbContextFactory<LayersDatabaseContextSqlite>
    {
        private const string SourceDatabasePath = @"C:\Users\konovalove\source\repos\Bloodwarder\NC_EngTools\LayersDatabase\Data\LayerData_ИС.db";
        public LayersDatabaseContextSqlite CreateDbContext(string[] args)
        {
            return new(SourceDatabasePath, null);
        }
    }
}