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
