using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;

namespace LayersIO.Database
{
    internal class SQLiteLayerDataProvider<TKey, TValue> : LayerDataProvider<TKey, TValue> where TKey : class where TValue : class
    {
        FileInfo _database { get; init; }

        SQLiteLayerDataProvider(string path)
        {
            _database = new FileInfo(path);
            if (!_database.Exists)
                throw new Exception("Файл базы данных не найден");
        }

        public override Dictionary<TKey, TValue> GetData()
        {
            if (typeof(TKey) == typeof(string))
            {
                if (typeof(TValue) == typeof(LayerProps))
                    return GetLayerProps();
                else if (typeof(TValue) == typeof(LayerLegendData))
                    return GetLegendData();
                else if (typeof(TValue) == typeof(LegendDrawTemplate))
                    return GetLegendDrawTemplate();
                else if (typeof(TValue) == typeof(string))
                    return GetAlterLayers();
                else
                    throw new Exception();
            }
            else
            {
                throw new Exception();
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            throw new NotImplementedException();
        }

        private Dictionary<TKey, TValue> GetLayerProps()
        {
            TinyMapperConfigurer.Configure();
            using (LayersDatabaseContextSqlite db = new(_database.FullName))
            {
                var layers = db.Layers.Include(l => l.LayerPropertiesData);
                var kvpCollection = layers.Select(l => new KeyValuePair<TKey?, TValue>(l.Name as TKey, TinyMapper.Map<TValue>(l.LayerPropertiesData)));
                return new Dictionary<TKey, TValue>(kvpCollection!);
            }
        }

        private Dictionary<TKey, TValue> GetLegendDrawTemplate()
        {
            TinyMapperConfigurer.Configure();
            using (LayersDatabaseContextSqlite db = new(_database.FullName))
            {
                var layers = db.Layers.Include(l => l.LayerDrawTemplateData);
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<TKey?, TValue>(l.Name as TKey, TinyMapper.Map<TValue>(l.LayerDrawTemplateData)));
                    return new Dictionary<TKey, TValue>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<TKey, TValue>();
                }
            }
        }
        private Dictionary<TKey, TValue> GetLegendData()
        {
            TinyMapperConfigurer.Configure();
            using (LayersDatabaseContextSqlite db = new(_database.FullName))
            {

                var layers = db.LayerGroups.Include(l => l.LayerLegendData);
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<TKey?, TValue>(l.MainName as TKey, TinyMapper.Map<TValue>(l.LayerLegendData)));
                    return new Dictionary<TKey, TValue>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<TKey, TValue>();
                }
            }
        }

        private Dictionary<TKey, TValue> GetAlterLayers()
        {
            using (LayersDatabaseContextSqlite db = new(_database.FullName))
            {
                if (typeof(TValue) != typeof(string))
                    throw new Exception();
                var layers = db.LayerGroups;
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<TKey?, TValue?>(l.MainName as TKey, l.AlternateLayer as TValue));
                    return new Dictionary<TKey, TValue>(kvpCollection!);
                }
                else
                {
                    return new Dictionary<TKey, TValue>();
                }
            }
        }

        private void OverwriteLayerProps()
        {

        }
        private void OverwriteLegendData()
        {

        }
        private void OverwriteLegendDrawTemplate()
        {

        }
        private void OverwriteAlternateLayers()
        {

        }

    }
}
