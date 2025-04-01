using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLayerLegendDataProvider : SQLiteDataProvider<string, LegendData>
    {
        public SQLiteLayerLegendDataProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }
        public override Dictionary<string, LegendData> GetData()
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{

                var layers = _context.Set<LayerGroupData>().Include(l => l.LayerLegendData);
                if (layers.Any())
                {
                    var kvpCollection = layers.AsNoTracking()
                                              .Where(l => l.LayerLegendData != null)
                                              .Select(l => new KeyValuePair<string, LegendData>(l.MainName, TinyMapper.Map<LegendData>(l.LayerLegendData)));
                    return new Dictionary<string, LegendData>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<string, LegendData>();
                }
            //}
        }

        public override LegendData? GetItem(string key)
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{
                var layers = _context.LayerGroups.AsNoTracking().Include(l => l.LayerLegendData);
                return layers.Where(l => l.MainName == key).Select(l => TinyMapper.Map<LegendData>(l.LayerLegendData)).FirstOrDefault();
            //}
        }
    }
}
