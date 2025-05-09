﻿using LayersIO.Connection;
using LayersIO.DataTransfer;
using LoaderCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LayersIO.Database
{
    public class SQLiteLayerDataContextFactory : IDbContextFactory<LayersDatabaseContextSqlite>
    {
        private const string DatabaseFileName = "LayerData.db";
        readonly ILogger? _logger;
        readonly string? _localPath;
        readonly string? _sharedPath;
        static SQLiteLayerDataContextFactory() { }
        public SQLiteLayerDataContextFactory(ILogger? logger, IConfiguration configuration)
        {
            _logger = logger;
            var paths = configuration.GetRequiredSection("LayerWorksConfiguration:LayerStandardPaths:LayerWorksPath")
                                     .Get<LayerWorksPath[]>()!;

            var localPath = paths.FirstOrDefault(p => p.Type == PathRoute.Local)?.Path;
            if (localPath != null)
                _localPath = Path.Combine(localPath, DatabaseFileName);

            var sharedPath = paths.FirstOrDefault(p => p.Type == PathRoute.Shared)?.Path;
            if (sharedPath != null)
                _sharedPath = Path.Combine(sharedPath, DatabaseFileName);
        }
        public LayersDatabaseContextSqlite CreateDbContext(string path)
        {
            return File.Exists(path) ? new(path, _logger) : throw new FileNotFoundException("Файл базы данных не найден");
        }

        public LayersDatabaseContextSqlite CreateDbContext(PathRoute pathRoute)
        {
            string? path = null;
            switch (pathRoute)
            {
                case PathRoute.Local:
                    path = _localPath;
                    break;
                case PathRoute.Shared:
                    path = _sharedPath;
                    break;
            }
            if (path == null || !File.Exists(path))
            {
                Exception ex = new FileNotFoundException($"Файл базы данных по пути \"{pathRoute}\" не найден");
                _logger?.LogError(ex, "{Message}", ex.Message);
                throw ex;
            }
            return new(path, _logger);
        }

        public LayersDatabaseContextSqlite CreateDbContext()
        {
            if (_localPath == null || !File.Exists(_localPath))
            {
                Exception ex = new FileNotFoundException($"Файл базы данных по пути \"{PathRoute.Local}\" не найден");
                _logger?.LogError(ex, "{Message}", ex.Message);
                throw ex;
            }
            return new(_localPath, _logger);
        }
    }
}