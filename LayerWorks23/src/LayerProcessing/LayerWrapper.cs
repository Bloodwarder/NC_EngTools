using LayerWorks23.LayerProcessing;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace LayerWorks.LayerProcessing
{

    public abstract class LayerWrapper
    {
        public LayerInfo LayerInfo { get; private set; }
        public static string StandartPrefix { get; set; }
        public LayerWrapper(string layerName)
        {
            // Поиск префикса по любому возможному разделителю
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            LayerInfo = NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);
        }

        public void AlterLayerInfo(Action<LayerInfo, string> action, string value)
        {
            action(LayerInfo, value);
        }
        public void AlterLayerInfo(Action<LayerInfo, string, string> action, string key, string value)
        {
            action(LayerInfo, key, value);
        }
        public void AlterLayerInfo(Action<LayerInfo> action)
        {
            action(LayerInfo);
        }

        public abstract void Push();
    }
}


