using System.Windows;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserNumericInput.xaml
    /// </summary>
    public partial class UserNumericInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty InputValueProperty;
        static UserNumericInput()
        {
            InputValueProperty = DependencyProperty.Register(nameof(InputValue), typeof(double), typeof(UserNumericInput), new(0d));
        }

        public double InputValue
        {
            get => (double)GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);
        }

        public UserNumericInput()
        {
            InitializeComponent();
        }
    }
}
