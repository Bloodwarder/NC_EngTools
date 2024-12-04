using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LayersIO.Connection
{
    public class OverwriteLayersDatabaseContextSqlite : DbContext
    {
        public ILogger? _logger;
        public DbSet<LayerData> LayerData { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroupData { get; set; } = null!;

        private readonly string _dataSource;

        public OverwriteLayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base()
        {
            _logger = logger;
            _dataSource = dataSource;
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public override int SaveChanges()
        {
            _logger?.LogInformation("Сохранение данных");
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