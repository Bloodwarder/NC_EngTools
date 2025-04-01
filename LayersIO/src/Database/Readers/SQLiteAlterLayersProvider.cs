using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;

namespace LayersIO.Database.Readers
{
    public class SQLiteAlterLayersProvider : SQLiteDataProvider<string, string?>
    {
        public SQLiteAlterLayersProvider(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }

        public override Dictionary<string, string?> GetData()
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{
            var layers = _context.Set<LayerGroupData>();
            if (layers.Any())
            {
                var kvpCollection = layers.AsNoTracking()
                                          .Select(l => new KeyValuePair<string, string?>(l.MainName, l.AlternateLayer));
                return new Dictionary<string, string?>(kvpCollection!);
            }
            else
            {
                return new Dictionary<string, string?>();
            }
            //}
        }

        public override string? GetItem(string key)
        {
            //using (var db = _contextFactory.CreateDbContext())
            //{
            var layers = _context.LayerGroups;
            return layers.AsNoTracking()
                         .Where(l => l.MainName == key)
                         .Select(l => l.AlternateLayer)
                         .FirstOrDefault();
            //}
        }
    }
}
