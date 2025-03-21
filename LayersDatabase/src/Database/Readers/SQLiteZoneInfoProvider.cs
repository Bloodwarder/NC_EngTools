using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using LoaderCore.SharedData;
using Microsoft.EntityFrameworkCore;

namespace LayersIO.Database.Readers
{
    public class SQLiteZoneInfoProvider : SQLiteDataProvider<string, ZoneInfo[]>
    {
        public SQLiteZoneInfoProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }

        public override Dictionary<string, ZoneInfo[]> GetData()
        {
            using (var db = _contextFactory.CreateDbContext())
            {

                try
                {
                    var layers = db.Layers.AsNoTracking()
                                  .Where(l => l.Zones != null && l.Zones.Any())
                                  .Where(l => l.LayerGroup != null && !string.IsNullOrEmpty(l.StatusName))
                                  .Include(l => l.Zones)
                                  .ThenInclude(z => z.ZoneLayer)
                                  .ThenInclude(zl => zl.LayerGroup)
                                  .Include(l => l.LayerGroup)
                                  .AsEnumerable();

                    var kvpCollection = layers.Select(l => new KeyValuePair<string, ZoneInfo[]>
                                                    (l.Name, l.Zones.Select(z => z.ToZoneInfo()).ToArray()));
                    return new Dictionary<string, ZoneInfo[]>(kvpCollection!);

                }
                catch (Exception)
                {
                    // TODO: Log
                    throw;
                }
            }
        }

        public override ZoneInfo[]? GetItem(string key)
        {
            using (var db = _contextFactory.CreateDbContext())
            {
                var layer = db.Set<LayerData>().AsNoTracking()
                                               .Where(l => l.Name == key && l.Zones.Any())
                                               .Include(l => l.Zones)
                                               .ThenInclude(z => z.ZoneLayer)
                                               .SingleOrDefault();
                var zones = layer?.Zones.Select(z => z.ToZoneInfo()).ToArray();
                return zones;
            }
        }
    }
}
