using LayersIO.DataTransfer;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    internal class SQLiteLayerPropsProvider : SQLiteLayerDataProvider<string, LayerProps>
    {

        internal SQLiteLayerPropsProvider(string path) : base(path) { }
        public override Dictionary<string, LayerProps> GetData()
        {
            using (var db = GetNewContext())
            {
                var layers = db.Layers.Include(l => l.LayerPropertiesData);
                var kvpCollection = layers.Select(l => new KeyValuePair<string, LayerProps>(l.Name, TinyMapper.Map<LayerProps>(l.LayerPropertiesData)));
                return new Dictionary<string, LayerProps>(kvpCollection!);
            }
        }

        public override LayerProps? GetItem(string key)
        {
            using (var db = GetNewContext())
            {
                var layers = db.Layers.Include(l => l.LayerPropertiesData);
                return layers.Where(l => l.Name == key).Select(l => TinyMapper.Map<LayerProps>(l.LayerPropertiesData)).FirstOrDefault();
            }
        }
    }
}
