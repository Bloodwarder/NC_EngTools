using System.Windows;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserNumericInput.xaml
    /// </summary>
    public partial class UserPseudoNumericInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty InputValueProperty;
        static UserPseudoNumericInput()
        {
            InputValueProperty = DependencyProperty.Register(nameof(InputValue), typeof(string), typeof(UserPseudoNumericInput), new("0"));
        }

        public string InputValue
        {
            get => (string)GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);
        }

        public UserPseudoNumericInput()
        {
            InitializeComponent();
        }
    }
}
