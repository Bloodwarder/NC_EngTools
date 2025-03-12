using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserStringInput.xaml
    /// </summary>
    public partial class UserComboBoxInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty ItemsSourceProperty;
        public static readonly DependencyProperty SelectedItemProperty;

        //public static readonly RoutedEvent SelectionChangedEvent;

        static UserComboBoxInput()
        {
            ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(UserComboBoxInput));
            SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(object), typeof(UserComboBoxInput));
            //UserComboBoxInput.SelectionChangedEvent = EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(UserComboBoxInput));
        }
        public UserComboBoxInput()
        {
            InitializeComponent();
            //Binding selectedBinding = new();
            //selectedBinding.Source = cbInput;
            //selectedBinding.Path = new("SelectedItem");
            //selectedBinding.Mode = BindingMode.TwoWay;
            //this.SetBinding(SelectedItemProperty, selectedBinding);

            //Binding sourceBinding = new();
            //sourceBinding.Source = cbInput;
            //sourceBinding.Path = new("ItemsSource");
            //sourceBinding.Mode = BindingMode.TwoWay;
            //this.SetBinding(ItemsSourceProperty, sourceBinding);
        }


        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set
            {
                cbInput.ItemsSource = value;
                SetValue(ItemsSourceProperty, value);
            }
        }

        public object SelectedItem
        {
            get => (object)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public event SelectionChangedEventHandler? SelectionChanged
        {
            add => cbInput.SelectionChanged += value;
            remove => cbInput.SelectionChanged -= value;
        }




    }
}
