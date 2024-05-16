using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("Color")]
    public class Color
    {
        public Color() { }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public System.Drawing.Color GetColor()
        {
            return System.Drawing.Color.FromArgb(Red, Green, Blue);
        }
    }
}
