using LayersIO.Connection;
using LayersIO.Model;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Writers
{
    public class SQLiteAlterLayerWriter : SQLiteDataWriter<string, string>
    {
        public SQLiteAlterLayerWriter(string path) : base(path) { }

        public override void OverwriteItem(string key, string item)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                OverwriteItemInContext(key, item, db);
                db.SaveChanges();
            }
        }

        public override void OverwriteSource(Dictionary<string, string> dictionary)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var query = db.LayerGroups.Where(l => dictionary.ContainsKey(l.MainName)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, db, query);
                db.SaveChanges();
            }
        }

        protected override void OverwriteItemInContext(string key, string item, LayersDatabaseContextSqlite db)
        {
            LayerGroupData? layer = db.LayerGroups.SingleOrDefault(l => l.MainName == key);
            if (layer != null && db.LayerGroups.Any(l => l.MainName == item))
                layer.AlternateLayer = item;
        }

        protected override void OverwriteItemInContext(string key, string item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            IQueryable<LayerGroupData> layers = (IQueryable<LayerGroupData>)querable;
            LayerGroupData? layer = layers.Where(l => l.MainName == key)
                                          .SingleOrDefault();
            layer ??= db.LayerGroups.Add(new(key)).Entity;
            layer.AlternateLayer = item;
        }
    }


}
