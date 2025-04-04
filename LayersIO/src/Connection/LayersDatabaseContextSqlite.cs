﻿using LayersIO.Model;
using Microsoft.Data.Sqlite;
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
        public DbSet<ZoneMapping> ZoneMappings { get; set; } = null!;
        public DbSet<OldLayerReference> OldLayers { get; set; } = null!;
        public DbSet<DrawOrderGroup> DrawOrderGroups { get; set; } = null!;

        internal protected readonly string _connectionString;
        public LayersDatabaseContextSqlite(string dataSource, ILogger? logger) : base()
        {
            _logger = logger;
            _connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = dataSource,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

            _logger?.LogDebug("Подключение к {DataSource}", dataSource);
        }

        public override int SaveChanges()
        {
            _logger?.LogInformation("Сохранение данных");
            return base.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString, b => b.MigrationsAssembly(MigrationsAssemblyName));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LayerDataConfiguration())
                        .ApplyConfiguration(new LayerGroupDataConfiguration())
                        .ApplyConfiguration(new ZoneInfoDataConfiguration())
                        .ApplyConfiguration(new ZoneMappingConfiguration())
                        .ApplyConfiguration(new OldLayerReferenceConfiguration())
                        .ApplyConfiguration(new DrawOrderGroupConfiguration());
            //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}