using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLayerPropsProvider : SQLiteDataProvider<string, LayerProps>
    {

        public SQLiteLayerPropsProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }
        public SQLiteLayerPropsProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory, ILogger? logger) : base(factory, logger) { }
        public override Dictionary<string, LayerProps> GetData()
        {
            Func<LayerData, KeyValuePair<string, LayerProps>> entityToKvp = l =>
            {
                LayerProps props = TinyMapper.Map<LayerProps>(l.LayerPropertiesData);
                props.DrawOrderIndex = l.DrawOrderGroup?.Index ?? 0;
                return new(l.Name, props);
            };

            var layers = _context.Set<LayerData>()
                                 .AsNoTracking()
                                 .Include(l => l.LayerPropertiesData)
                                 .Include(l => l.LayerGroup)
                                 .Include(l => l.DrawOrderGroup);

            var kvpCollection = layers.Where(l => l.LayerGroup != null && !string.IsNullOrEmpty(l.StatusName))
                                      .Select(entityToKvp);
            try
            {
                var dictionary = new Dictionary<string, LayerProps>(kvpCollection!);
                return dictionary;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка получения данных:\t{Message}", ex.Message);
                throw;
            }
        }

        public override LayerProps? GetItem(string key)
        {
            var layers = _context.Set<LayerData>()
                                 .AsNoTracking()
                                 .Include(l => l.LayerPropertiesData)
                                 .Include(l => l.DrawOrderGroup)
                                 .Include(l => l.LayerGroup);

            var layer = layers.Where(l => l.Name == key).FirstOrDefault();

            if (layer == null)
                return null;

            var props = TinyMapper.Map<LayerProps>(layer.LayerPropertiesData);
            props.DrawOrderIndex = layer.DrawOrderGroup?.Index ?? 0;

            return props;
        }
    }
}
