using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Writers
{
    public class SQLiteLayerPropsWriter : SQLiteDataWriter<string, LayerProps>
    {
        public SQLiteLayerPropsWriter(string path) : base(path) { }



        public override void OverwriteSource(Dictionary<string, LayerProps> dictionary)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var query = db.Layers.Where(l => dictionary.ContainsKey(l.Name)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, db, query);
                db.SaveChanges();
            }
        }
        public override void OverwriteItem(string key, LayerProps item)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                OverwriteItemInContext(key, item, db);
                db.SaveChanges();
            }
        }
        protected override void OverwriteItemInContext(string key, LayerProps item, LayersDatabaseContextSqlite db)
        {
            var query = db.Layers.Include(l => l.LayerPropertiesData).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, LayerProps item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            var layers = (IQueryable<LayerData>)querable;
            LayerData? layer = layers.SingleOrDefault(l => l.Name == key);
            layer ??= db.Layers.Add(new(key)).Entity;
            LayerPropertiesData overwriteData = TinyMapper.Map<LayerPropertiesData>(item);
            layer.LayerPropertiesData = overwriteData;
        }
    }


}
