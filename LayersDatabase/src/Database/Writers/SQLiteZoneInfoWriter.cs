using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using LoaderCore.SharedData;
using Microsoft.EntityFrameworkCore;

namespace LayersIO.Database.Writers
{
    public class SQLiteZoneInfoWriter : SQLiteDataWriter<string, ZoneInfo[]>
    {
        public SQLiteZoneInfoWriter(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }



        public override void OverwriteSource(Dictionary<string, ZoneInfo[]> dictionary)
        {
                var query = _context!.Set<LayerData>().Where(l => dictionary.ContainsKey(l.Name)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, _context!, query);
                _context!.SaveChanges();
        }
        public override void OverwriteItem(string key, ZoneInfo[] item)
        {
                OverwriteItemInContext(key, item, _context!);
                _context!.SaveChanges();
        }
        protected override void OverwriteItemInContext(string key, ZoneInfo[] item, LayersDatabaseContextSqlite db)
        {
            var query = db.Set<LayerData>().Include(l => l.Zones).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, ZoneInfo[] item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            var layers = (IQueryable<LayerData>)querable;
            LayerData? layer = layers.SingleOrDefault(l => l.Name == key);
            //layer ??= db.Layers.Add(new(key)).Entity;  // здесь раньше было полное имя, теперь нужен только статус
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
