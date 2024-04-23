using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public abstract class ExternalDictionary<TKey, TValue> where TKey : class
    {
        private protected Dictionary<TKey, TValue> InstanceDictionary { get; set; }

        private protected bool TryGetInstanceValue(TKey key, out TValue? value)
        {
            bool success = InstanceDictionary.TryGetValue(key, out value);
            return success;
            // Выдаёт ошибку, когда возвращает value=null. Поправить после перехода на 6.0
        }

        private protected void ReloadInstance(ILayerDataWriter<TKey, TValue> primary, ILayerDataProvider<TKey, TValue> secondary)
        {
            InstanceDictionary = secondary.GetData();
            primary.OverwriteSource(InstanceDictionary);
        }

        private protected bool CheckInstanceKey(TKey key)
        { return InstanceDictionary.ContainsKey(key); }
    }
}