using LayersIO.ExternalData;

namespace LayersIO.Database
{
    internal class SQLiteLayerDataWriter<TKey, TValue> : ILayerDataWriter<TKey, TValue> where TKey : class where TValue : class
    {
        FileInfo _database { get; init; }

        SQLiteLayerDataWriter(string path)
        {
            _database = new FileInfo(path);
            if (!_database.Exists)
                throw new Exception("Файл базы данных не найден");
        }

        public void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            throw new NotImplementedException();
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
