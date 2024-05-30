using Teigha.DatabaseServices;

namespace LoaderCore.Interfaces
{
    public interface ILayerFormatter
    {
        public void FormatLayer(LayerTableRecord record);
    }
}
