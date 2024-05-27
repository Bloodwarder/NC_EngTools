namespace LayersIO.Database.Readers
{
    internal class SQLiteAlterLayersProvider : SQLiteLayerDataProvider<string, string?>
    {
        public SQLiteAlterLayersProvider(string path) : base(path) { }

        public override Dictionary<string, string?> GetData()
        {
            using (var db = GetNewContext())
            {
                var layers = db.LayerGroups;
                if (layers.Any())
                {
                    var kvpCollection = layers.Select(l => new KeyValuePair<string, string?>(l.MainName, l.AlternateLayer));
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
            using (var db = GetNewContext())
            {
                var layers = db.LayerGroups;
                return layers.Where(l => l.MainName == key).Select(l => l.AlternateLayer).FirstOrDefault();
            }
        }
    }
}
