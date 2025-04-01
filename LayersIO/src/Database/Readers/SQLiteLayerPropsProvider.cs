using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Readers
{
    public class SQLiteLayerPropsProvider : SQLiteDataProvider<string, LayerProps>
    {

        public SQLiteLayerPropsProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }
        public override Dictionary<string, LayerProps> GetData()
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{
            var layers = _context.Set<LayerData>().Include(l => l.LayerPropertiesData)
                                            .Include(l => l.LayerGroup);
            var kvpCollection = layers.AsNoTracking()
                                      .Where(l => l.LayerGroup != null && !string.IsNullOrEmpty(l.StatusName))
                                      .Select(l => new KeyValuePair<string, LayerProps>
                                            (l.Name, TinyMapper.Map<LayerProps>(l.LayerPropertiesData)));
            try
            {
                var dictionary = new Dictionary<string, LayerProps>(kvpCollection!);
                return dictionary;
            }
            catch (Exception ex)
            {
                throw;
            }
            //}
        }

        public override LayerProps? GetItem(string key)
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{
            var layers = _context.Layers.AsNoTracking().Include(l => l.LayerPropertiesData);
            return layers.Where(l => l.Name == key).Select(l => TinyMapper.Map<LayerProps>(l.LayerPropertiesData)).FirstOrDefault();
            //}
        }
    }
}
