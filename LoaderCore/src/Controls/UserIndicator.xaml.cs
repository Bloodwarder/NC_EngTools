using System.Windows;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserIndicator.xaml
    /// </summary>
    public partial class UserIndicator : LabeledHorizontalInput
    {
        public static readonly DependencyProperty InputValueProperty;
        static UserIndicator()
        {
            InputValueProperty = DependencyProperty.Register(nameof(InputValue), typeof(double), typeof(UserIndicator), new(0d));
        }

        public double InputValue
        {
            get => (double)GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);
        }

        public UserIndicator()
        {
            InitializeComponent();
        }
    }
}
