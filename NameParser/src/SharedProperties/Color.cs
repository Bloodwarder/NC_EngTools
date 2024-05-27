using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("Color")]
    public struct Color
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public static explicit operator System.Drawing.Color(Color color)
        {
            return System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue);
        }
    }
}
