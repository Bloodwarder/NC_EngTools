using LayersIO.DataTransfer;
using Nelibur.ObjectMapper;

namespace LayerWorks.LayerProcessing
{
    public class StatedLayerProps : LayerProps, ICloneable
    {
        public StatedLayerProps() { }
        public bool? IsOff { get; set; }
        public bool? IsFrozen { get; set; }
        public bool? IsLocked { get; set; }

        public object Clone() => this.MemberwiseClone();
    }

    public static class LayerPropsExtension
    {
        static LayerPropsExtension()
        {
            TinyMapper.Bind<LayerProps, StatedLayerProps>();
        }

        public static StatedLayerProps ToStatedLayerProps(this LayerProps props)
        {
            return TinyMapper.Map<StatedLayerProps>(props);
        }

        public static Teigha.Colors.Color? GetColor(this LayerProps props)
        {
            if (props.Red != null && props.Green != null && props.Green != null)
                return Teigha.Colors.Color.FromRgb(props.Red!.Value, props.Green!.Value, props.Blue!.Value);
            else
                return null;
        }
    }

}


