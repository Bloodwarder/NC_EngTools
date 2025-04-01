using LayersIO.Connection;
using LoaderCore.Configuration;
using LoaderCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LayersIO.Database
{
    public class SQLiteFallbackContextFactory : IDbContextFactory<LayersDatabaseContextSqlite>, IDisposable
    {
        private const string DatabaseFileName = "LayerData.db";

        readonly ILogger _logger;
        readonly Fallback<LayersDatabaseContextSqlite, string> _fallback;
        LayersDatabaseContextSqlite? _context;

        public SQLiteFallbackContextFactory(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            var paths = configuration.GetRequiredSection("LayerWorksConfiguration:LayerStandardPaths:LayerWorksPath")
                                     .Get<LayerWorksPath[]>()!;
            if (!paths.Any())
                throw new IOException("В файле конфигурации нет путей к файлам БД");
            var sortedPaths = paths.OrderBy(p => p.Type)
                                   .Select(p => p.Path)
                                   .Where(s => !string.IsNullOrEmpty(s))
                                   .Select(s => Path.Combine(s!, DatabaseFileName));
            _fallback = new(sortedPaths, p => new(p, _logger), ErrorCallback, SuccessCallback);
        }

        ///<inheritdoc/>
        public LayersDatabaseContextSqlite CreateDbContext()
        {
            _context ??= _fallback.GetResult();
            return _context;
        }

        public void Dispose()
        {
            _context?.Dispose();
            _context = null;
            GC.SuppressFinalize(this);
        }

        private void ErrorCallback(string path, Exception ex) =>
            _logger.LogWarning(ex, "Не удалось подключиться к БД по пути \"{Path}\". Ошибка:\t{Error}", path, ex.Message);
        private void SuccessCallback(string path) =>
            _logger.LogInformation("Соединение с БД по пути \"{Path}\" установлено", path);
    }
}