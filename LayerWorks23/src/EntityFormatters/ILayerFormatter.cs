using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public interface ILayerFormatter
    {
        public void FormatLayer(LayerTableRecord record);
    }
}
