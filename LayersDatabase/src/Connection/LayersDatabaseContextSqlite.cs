using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LayersIO.Connection
{
    public class LayersDatabaseContextSqlite : DbContext
    {
        private const string MigrationsAssemblyName = "LayerDbMigrations";

        private readonly ILogger? _logger;
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;
        public DbSet<ZoneInfoData> Zones { get; set; } = null!;

        internal protected readonly string _dataSource;
        public LayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base()
        {
            _logger = logger;
            _dataSource = dataSource;
            _logger?.LogDebug("Подключение к {DataSource}", dataSource);
        }

        public override int SaveChanges()
        {
            _logger?.LogInformation("Сохранение данных");
            return base.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dataSource}", b => b.MigrationsAssembly(MigrationsAssemblyName));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LayerDataConfiguration())
                        .ApplyConfiguration(new LayerGroupDataConfiguration())
                        .ApplyConfiguration(new ZoneInfoDataConfiguration());
            //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}