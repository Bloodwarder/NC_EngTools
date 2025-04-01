using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database.Writers
{
    public class SQLiteLayerPropsWriter : SQLiteDataWriter<string, LayerProps>
    {
        public SQLiteLayerPropsWriter(IDbContextFactory<LayersDatabaseContextSqlite> factory) : base(factory) { }



        public override void OverwriteSource(Dictionary<string, LayerProps> dictionary)
        {
                var query = _context!.Set<LayerData>().Where(l => dictionary.ContainsKey(l.Name)).AsQueryable();
                foreach (var kvp in dictionary)
                    OverwriteItemInContext(kvp.Key, kvp.Value, _context, query);
                _context!.SaveChanges();
        }
        public override void OverwriteItem(string key, LayerProps item)
        {
                OverwriteItemInContext(key, item, _context!);
                _context!.SaveChanges();
        }
        protected override void OverwriteItemInContext(string key, LayerProps item, LayersDatabaseContextSqlite db)
        {
            var query = db.Set<LayerData>().Include(l => l.LayerPropertiesData).AsQueryable();
            OverwriteItemInContext(key, item, db, query);
        }

        protected override void OverwriteItemInContext(string key, LayerProps item, LayersDatabaseContextSqlite db, IQueryable querable)
        {
            //throw new NotImplementedException();
            var layers = (IQueryable<LayerData>)querable;
            LayerData? layer = layers.SingleOrDefault(l => l.Name == key);
            //layer ??= db.Layers.Add(new(key)).Entity;  // здесь раньше было полное имя, теперь нужен только статус
            LayerPropertiesData overwriteData = TinyMapper.Map<LayerPropertiesData>(item);
            layer.LayerPropertiesData = overwriteData;
        }
    }


}
