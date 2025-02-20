using System.Windows;
using System.Windows.Controls;

namespace LoaderCore.Controls
{
    public partial class LabeledHorizontalInput : UserControl
    {
        public static readonly DependencyProperty LabelTextProperty;
        static LabeledHorizontalInput()
        {
            LabelTextProperty = DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(LabeledHorizontalInput));
        }

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }
    }
}
