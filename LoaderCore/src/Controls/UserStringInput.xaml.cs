using System.Windows;
using System.Windows.Controls;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserStringInput.xaml
    /// </summary>
    public partial class UserStringInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty InputValueProperty;
        static UserStringInput()
        {
            InputValueProperty = DependencyProperty.Register(nameof(InputValue), typeof(string), typeof(UserStringInput));
        }

        public string InputValue
        {
            get => (string)GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);

            //get => (string)GetValue(InputValueProperty);
            //set => SetValue(InputValueProperty, value);
        }

        public UserStringInput()
        {
            InitializeComponent();
        }

        public event TextChangedEventHandler? TextChanged
        {
            add => inputText.TextChanged += value;
            remove => inputText.TextChanged -= value;
        }

    }
}
