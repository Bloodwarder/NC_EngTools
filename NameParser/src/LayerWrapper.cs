
using System.Text.RegularExpressions;


namespace NameClassifiers
{

    public abstract class LayerWrapper
    {
        private static string? _standartPrefix;

        public LayerInfo LayerInfo { get; private set; }
        public static string? StandartPrefix
        {
            get => _standartPrefix;
            set
            {
                if (NameParser.LoadedParsers.ContainsKey(value!))
                    _standartPrefix = value;
                else
                    throw new Exception($"Не загружен интерпретатор для префикса {value}");
            }
        }
        public LayerWrapper(string layerName)
        {
            // Поиск префикса по любому возможному разделителю
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            StandartPrefix ??= prefix;
            if (!NameParser.LoadedParsers.ContainsKey(prefix))
                throw new WrongLayerException($"Нет данных для интерпретации слоя с префиксом {prefix}");
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

        public static LayerInfo? GetInfoFromString(string layerName, out string? exceptionMessage)
        {
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            LayerInfo? info = null;
            try
            {
                info = NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);
            }
            catch (WrongLayerException e)
            {
                exceptionMessage = e.Message;
                return info;
            }
            exceptionMessage = null;
            return info;
        }
    }
}


