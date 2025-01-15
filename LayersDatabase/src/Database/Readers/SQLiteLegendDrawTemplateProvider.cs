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
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData).Include(l => l.LayerGroup);
                if (layers.Any())
                {
                    var kvpCollection = layers.AsNoTracking()
                                              .Where(l => l.LayerGroup != null && !string.IsNullOrEmpty(l.StatusName) && l.LayerDrawTemplateData != null)
                                              .Select(l => new KeyValuePair<string, LegendDrawTemplate>
                                                    (l.Name, TinyMapper.Map<LegendDrawTemplate>(l.LayerDrawTemplateData)));
                    try
                    {
                        var dictionary = new Dictionary<string, LegendDrawTemplate>(kvpCollection!);
                        return dictionary;
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
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
