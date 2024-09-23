
using System.Diagnostics.CodeAnalysis;

namespace LoaderCore.Interfaces
{
    public interface IStandardReader<T>
    {
        public T GetStandard(string layerName);
        //public T GetStandard(LayerInfo layerInfo);
        public bool TryGetStandard([MaybeNullWhen(false)]string layerName, out T? standard);
        //public bool TryGetStandard(LayerInfo layerInfo, out T? standard);

    }

}
