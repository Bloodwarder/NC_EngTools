using LayersIO.Connection;
using LoaderCore.Configuration;
using LoaderCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LayersIO.Database
{
    public class SQLiteFallbackContextFactory : IDbContextFactory<LayersDatabaseContextSqlite>
    {
        readonly ILogger _logger;
        readonly Fallback<LayersDatabaseContextSqlite, string> _fallback;

        public SQLiteFallbackContextFactory(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            var paths = configuration.GetRequiredSection("LayerWorksConfiguration:LayerStandardPaths:LayerWorksPath")
                                     .Get<LayerWorksPath[]>()!;
            if (!paths.Any())
                throw new IOException("В файле конфигурации нет путей к файлам БД");
            var sortedPaths = paths.OrderBy(p => p.Type).Select(p => p.Path ?? string.Empty).Where(s => !string.IsNullOrEmpty(s));
            _fallback = new(sortedPaths, p => new(p, _logger), ErrorCallback);
        }

        ///<inheritdoc/>
        public LayersDatabaseContextSqlite CreateDbContext()
        {
            return _fallback.GetResult();
        }

        private void ErrorCallback(string path, Exception ex) => 
            _logger.LogWarning(ex, "Не удалось подключиться к БД по пути \"{Path}\". Ошибка:\t{Error}", path, ex.Message);
    }
}