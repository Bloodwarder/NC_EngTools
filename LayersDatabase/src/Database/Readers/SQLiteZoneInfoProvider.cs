using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteZoneInfoProvider : SQLiteDataProvider<string, ZoneInfo[]>
    {
        public SQLiteZoneInfoProvider(string path) : base(path) { }

        public override Dictionary<string, ZoneInfo[]> GetData()
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.Layers.Include(l => l.Zones);
                var kvpCollection = layers.AsNoTracking()
                                          .Where(l => !string.IsNullOrEmpty(l.MainName) && !string.IsNullOrEmpty(l.StatusName))
                                          .Where(l => l.Zones.Any())
                                          .Select(l => new KeyValuePair<string, ZoneInfo[]>
                                                (l.Name, l.Zones.Select(z => new ZoneInfo(z)).ToArray()));
                return new Dictionary<string, ZoneInfo[]>(kvpCollection!);
            }
        }

        public override ZoneInfo[]? GetItem(string key)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layer = db.Layers.Where(l => l.Name == key).Include(l => l.Zones).Single();
                var zones = layer.Zones.Select(z => new ZoneInfo(z)).ToArray();
                return zones;
            }
        }
    }
}
