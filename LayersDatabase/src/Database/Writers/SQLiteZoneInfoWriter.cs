using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;

namespace LayersIO.Database.Writers
{
    public class SQLiteZoneInfoWriter : SQLiteDataWriter<string, ZoneInfo[]>
    {
        public SQLiteZoneInfoWriter(string path) : base(path) { }



        public override void OverwriteSource(Dictionary<string, ZoneInfo[]> dictionary)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                var query = db.Layers.Where(l => dictionary.ContainsKey(l.Name)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, db, query);
                db.SaveChanges();
            }
        }
        public override void OverwriteItem(string key, ZoneInfo[] item)
        {
            using (var db = _contextFactory.CreateDbContext(_path))
            {
                OverwriteItemInContext(key, item, db);
                db.SaveChanges();
            }
        }
        protected override void OverwriteItemInContext(string key, ZoneInfo[] item, LayersDatabaseContextSqlite db)
        {
            var query = db.Layers.Include(l => l.Zones).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, ZoneInfo[] item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            var layers = (IQueryable<LayerData>)querable;
            LayerData? layer = layers.SingleOrDefault(l => l.Name == key);
            layer ??= db.Layers.Add(new(key)).Entity;
            db.Zones.RemoveRange(layer.Zones);
            foreach (var zoneInfo in item)
            {
                ZoneInfoData zid = new()
                {
                    SourceLayer = layer,
                    ZoneLayer = db.Layers.Where(l => l.Name == zoneInfo.ZoneLayer).Single(),
                    Value = zoneInfo.Value,
                    DefaultConstructionWidth = zoneInfo.DefaultConstructionWidth
                };
                db.Zones.Add(zid);
            };
        }
    }


}
