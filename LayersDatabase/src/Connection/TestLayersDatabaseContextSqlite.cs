﻿using Microsoft.EntityFrameworkCore;

using LayersIO.Model;
using System.Reflection;

namespace LayersIO.Connection
{
    public class TestLayersDatabaseContextSqlite : DbContext
    {
        public DbSet<LayerData> Layers { get; set; } = null!;
        public DbSet<LayerGroupData> LayerGroups { get; set; } = null!;

        private string _dataSource;
        public TestLayersDatabaseContextSqlite(string dataSource)
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
            _dataSource = dataSource;
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