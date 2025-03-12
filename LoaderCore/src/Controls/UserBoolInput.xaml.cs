using System.Windows;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserStringInput.xaml
    /// </summary>
    public partial class UserBoolInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty StateProperty;
        static UserBoolInput()
        {
            StateProperty = DependencyProperty.Register(nameof(State), typeof(bool), typeof(UserBoolInput));
        }

        public bool State
        {
            get => (bool)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        public UserBoolInput()
        {
            InitializeComponent();
        }
    }
}
