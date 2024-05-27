using LayersIO.DataTransfer;
using LayersIO.Database.Readers;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    internal class SQLiteLayerLegendDataProvider : SQLiteLayerDataProvider<string, LegendData>
    {
        internal SQLiteLayerLegendDataProvider(string path) : base(path) { }
        public override Dictionary<string, LegendData> GetData()
        {
            using (var db = GetNewContext())
            {

                var layers = db.LayerGroups.Include(l => l.LayerLegendData);
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<string, LegendData>(l.MainName, TinyMapper.Map<LegendData>(l.LayerLegendData)));
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
            using (var db = GetNewContext())
            {
                var layers = db.LayerGroups.Include(l => l.LayerLegendData);
                return layers.Where(l => l.MainName == key).Select(l => TinyMapper.Map<LegendData>(l.LayerLegendData)).FirstOrDefault();
            }
        }
    }
}
