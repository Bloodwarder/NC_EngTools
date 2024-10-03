using LayersIO.DataTransfer;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLayerPropsProvider : SQLiteDataProvider<string, LayerProps>
    {

        public SQLiteLayerPropsProvider(string path) : base(path) { }
        public override Dictionary<string, LayerProps> GetData()
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.Layers.Include(l => l.LayerPropertiesData);
                var kvpCollection = layers.AsNoTracking()
                                          .Where(l => !string.IsNullOrEmpty(l.MainName) && !string.IsNullOrEmpty(l.StatusName))
                                          .Select(l => new KeyValuePair<string, LayerProps>
                                                (l.Name, TinyMapper.Map<LayerProps>(l.LayerPropertiesData)));
                return new Dictionary<string, LayerProps>(kvpCollection!);
            }
        }

        public override LayerProps? GetItem(string key)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.Layers.AsNoTracking().Include(l => l.LayerPropertiesData);
                return layers.Where(l => l.Name == key).Select(l => TinyMapper.Map<LayerProps>(l.LayerPropertiesData)).FirstOrDefault();
            }
        }
    }
}
