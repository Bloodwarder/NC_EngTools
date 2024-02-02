using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public abstract class ExternalDictionary<TKey, TValue> where TKey : class
    {
        private protected Dictionary<TKey, TValue> InstanceDictionary { get; set; }

        private protected TValue GetInstanceValue(TKey key, out bool success)
        {
            success = InstanceDictionary.TryGetValue(key, out TValue? value);
            return value;
            // Выдаёт ошибку, когда возвращает value=null. Поправить после перехода на 6.0
        }

        private protected void ReloadInstance(LayerDataProvider<TKey, TValue> primary, LayerDataProvider<TKey, TValue> secondary)
        {
            InstanceDictionary = secondary.GetData();
            primary.OverwriteSource(InstanceDictionary);
        }

        private protected bool CheckInstanceKey(TKey key)
        { return InstanceDictionary.ContainsKey(key); }
    }
}