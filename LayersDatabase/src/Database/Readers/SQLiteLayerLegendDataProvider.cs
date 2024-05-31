using LayersIO.DataTransfer;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLayerLegendDataProvider : SQLiteDataProvider<string, LegendData>
    {
        public SQLiteLayerLegendDataProvider(string path) : base(path) { }
        public override Dictionary<string, LegendData> GetData()
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {

                var layers = db.LayerGroups.Include(l => l.LayerLegendData);
                if (layers.Any())
                {
                    var kvpCollection = layers.AsNoTracking()
                                              .Select(l => new KeyValuePair<string, LegendData>(l.MainName, TinyMapper.Map<LegendData>(l.LayerLegendData)));
                    return new Dictionary<string, LegendData>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<string, LegendData>();
                }
            }
        }

        public override LegendData? GetItem(string key)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.LayerGroups.AsNoTracking().Include(l => l.LayerLegendData);
                return layers.Where(l => l.MainName == key).Select(l => TinyMapper.Map<LegendData>(l.LayerLegendData)).FirstOrDefault();
            }
        }
    }
}
