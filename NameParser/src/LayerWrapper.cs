
using System.Text.RegularExpressions;


namespace NameClassifiers
{

    public abstract class LayerWrapper
    {
        static LayerWrapper() { }

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

        public LayerInfo LayerInfo { get; private set; }

        /// <summary>
        /// Изменяет привязанный объект в переопределённом классе в соответствии с текущими данными LayerInfo
        /// </summary>
        public abstract void Push();


        /// <summary>
        /// Распознаёт префикс и запрашивает результат метода GetLayerInfo соответствующего парсера, если он загружен
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        /// <returns></returns>
        public static LayerInfoResult GetInfoFromString(string layerName)
        {
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            return NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);
        }
    }
}


