using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Writers
{
    public class SQLiteLayerLegendDataWriter : SQLiteDataWriter<string, LegendData>
    {
        public SQLiteLayerLegendDataWriter(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }

        public override void OverwriteItem(string key, LegendData item)
        {
            OverwriteItemInContext(key, item, _context!);
            _context!.SaveChanges();
        }

        public override void OverwriteSource(Dictionary<string, LegendData> dictionary)
        {
            foreach (var kvp in dictionary)
                OverwriteItemInContext(kvp.Key, kvp.Value, _context!);
            _context!.SaveChanges();
        }

        protected override void OverwriteItemInContext(string key, LegendData item, LayersDatabaseContextSqlite db)
        {
            var query = db.Set<LayerGroupData>().Include(l => l.LayerLegendData).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, LegendData item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            IQueryable<LayerGroupData> layers = (IQueryable<LayerGroupData>)querable;
            LayerGroupData? layer = layers.Where(l => l.MainName == key)
                                         .SingleOrDefault();
            layer ??= db.Set<LayerGroupData>().Add(new(key)).Entity;
            LayerLegendData overwriteData = TinyMapper.Map<LayerLegendData>(item);
            layer.LayerLegendData = overwriteData;
        }
    }


}
