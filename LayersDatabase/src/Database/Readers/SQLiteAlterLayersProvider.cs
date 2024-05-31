using LayersIO.Connection;
using LoaderCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.Database.Readers
{
    public class SQLiteAlterLayersProvider : SQLiteDataProvider<string, string?>
    {
        public SQLiteAlterLayersProvider(string path) : base(path) { }

        public override Dictionary<string, string?> GetData()
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.LayerGroups;
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
            }
        }

        public override string? GetItem(string key)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var layers = db.LayerGroups;
                return layers.AsNoTracking()
                             .Where(l => l.MainName == key)
                             .Select(l => l.AlternateLayer)
                             .FirstOrDefault();
            }
        }
    }
}
