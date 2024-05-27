﻿namespace LayersIO.ExternalData
{
    public interface ILayerDataWriter<TKey, TValue> where TKey : notnull
    {
        public void OverwriteSource(Dictionary<TKey, TValue> dictionary);
        public void OverwriteItem(Dictionary<TKey, TValue> dictionary);
    }
}