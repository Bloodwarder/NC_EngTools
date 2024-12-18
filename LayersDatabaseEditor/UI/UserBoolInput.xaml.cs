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
