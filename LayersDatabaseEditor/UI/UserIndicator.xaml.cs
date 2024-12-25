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

namespace LayersDatabaseEditor.UI
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
