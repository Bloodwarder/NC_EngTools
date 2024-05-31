using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Writers
{
    public class SQLiteLayerLegendDrawTemplateWriter : SQLiteDataWriter<string, LegendDrawTemplate>
    {
        public SQLiteLayerLegendDrawTemplateWriter(string path) : base(path) { }

        public override void OverwriteItem(string key, LegendDrawTemplate item)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                OverwriteItemInContext(key, item, db);
                db.SaveChanges();
            }
        }

        public override void OverwriteSource(Dictionary<string, LegendDrawTemplate> dictionary)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var query = db.Layers.Include(l => l.LayerDrawTemplateData).Where(l => dictionary.ContainsKey(l.Name)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, db, query);
                db.SaveChanges();
            }
        }

        protected override void OverwriteItemInContext(string key, LegendDrawTemplate item, LayersDatabaseContextSqlite db)
        {
            var query = db.Layers.Include(l => l.LayerDrawTemplateData).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, LegendDrawTemplate item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            var layers = (IQueryable<LayerData>)querable;
            LayerData? layer = layers.SingleOrDefault(l => l.Name == key);
            layer ??= db.Layers.Add(new(key)).Entity;
            LayerDrawTemplateData overwriteData = TinyMapper.Map<LayerDrawTemplateData>(item);
            layer.LayerDrawTemplateData = overwriteData;
        }
    }


}
