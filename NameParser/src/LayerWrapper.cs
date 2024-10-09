
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
        static LayerWrapper()
        {
            StandartPrefix ??= NameParser.LoadedParsers.FirstOrDefault().Key;
        }
        public LayerWrapper(string layerName)
        {
            // Поиск префикса по любому возможному разделителю
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            if (!NameParser.LoadedParsers.ContainsKey(prefix))
                throw new WrongLayerException($"Нет данных для интерпретации слоя с префиксом {prefix}");
            var layerInfoResult = NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);
            if (layerInfoResult.Status == LayerInfoParseStatus.Success)
            {
                LayerInfo = layerInfoResult.Value;
            }
            else
            {
                throw layerInfoResult.Exceptions.First();
            }
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

        public static LayerInfoResult GetInfoFromString(string layerName)
        {
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            return NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);
        }
    }
}


