using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
