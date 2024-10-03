using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLegendDrawTemplateProvider : SQLiteDataProvider<string, LegendDrawTemplate>
    {
        public SQLiteLegendDrawTemplateProvider(string path) : base(path) { }

        public override Dictionary<string, LegendDrawTemplate> GetData()
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData);
                if (layers.Any())
                {
                    var kvpCollection = layers.AsNoTracking()
                                              .Where(l => !string.IsNullOrEmpty(l.MainName) && !string.IsNullOrEmpty(l.StatusName))
                                              .Select(l => new KeyValuePair<string, LegendDrawTemplate>
                                                    (l.Name, TinyMapper.Map<LegendDrawTemplate>(l.LayerDrawTemplateData)));

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
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData);
                var result = layers.AsNoTracking()
                                   .Where(l => l.Name == key)
                                   .Select(l => TinyMapper.Map<LegendDrawTemplate>(l.LayerDrawTemplateData))
                                   .FirstOrDefault();
                return result;
            }
        }
    }
}
