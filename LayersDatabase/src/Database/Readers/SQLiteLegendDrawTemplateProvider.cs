using LayersIO.DataTransfer;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    internal class SQLiteLegendDrawTemplateProvider : SQLiteLayerDataProvider<string, LegendDrawTemplate>
    {
        internal SQLiteLegendDrawTemplateProvider(string path) : base(path) { }

        public override Dictionary<string, LegendDrawTemplate> GetData()
        {
            using (var db = GetNewContext())
            {
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData);
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<string, LegendDrawTemplate>(l.Name, TinyMapper.Map<LegendDrawTemplate>(l.LayerDrawTemplateData)));
                    return new Dictionary<string, LegendDrawTemplate>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<string, LegendDrawTemplate>();
                }
            }
        }

        public override LegendDrawTemplate? GetItem(string key)
        {
            using (var db = GetNewContext())
            {
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData);
                return layers.Where(l => l.Name == key).Select(l => TinyMapper.Map<LegendDrawTemplate>(l.LayerDrawTemplateData)).FirstOrDefault();
            }
        }
    }
}
