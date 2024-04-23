namespace LayersIO.ExternalData
{
    public interface ILayerDataWriter<TKey, TValue> where TKey:class
    {
        public void OverwriteSource(Dictionary<TKey, TValue> dictionary);
    }
}