using NameClassifiers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LayerWorks.UI
{
    /// <summary>
    /// Логика взаимодействия для NewStandardLayerWindow.xaml
    /// </summary>
    public partial class NewStandardLayerWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(NewStandardLayerWindowVm), typeof(NewStandardLayerWindow), new PropertyMetadata());
        public NewStandardLayerWindow(IEnumerable<string> layers)
        {
            InitializeComponent();
            ViewModel = new(layers);
            ViewModel.InputCompleted += (s, e) => this.Close();
            lvNodes.SelectedIndex = 0;
            Dispatcher.Invoke(() => lvNodes.Focus());
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NewStandardLayerWindowVm.SelectedNode) && ViewModel.SelectedNode != null)
                {
                    lvNodes.ScrollIntoView(ViewModel.SelectedNode);
                    var container = lvNodes.ItemContainerGenerator.ContainerFromItem(ViewModel.SelectedNode) as ListViewItem;
                    container?.Focus();
                }
            };
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        public NewStandardLayerWindowVm ViewModel
        {
            get => (NewStandardLayerWindowVm)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public IEnumerable<string> GetResultLayers() => ViewModel.GetResultLayers();

        private void HandleGlobalTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 1)
            {
                char inputChar = e.Text[0];
                if (IsValidUnicodeLetter(inputChar))
                {
                    ViewModel.AppendSearch(e.Text);
                }
            }
        }

        private void HandleSpecialCases(object sender, KeyEventArgs e)
        {
            // Handle keyboard combinations that might not trigger TextInput
            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
            {
                ViewModel.AppendSearch(" ");
            }
        }

        private bool IsValidUnicodeLetter(char c)
        {
            // Check if character is a letter from any alphabet (including Cyrillic)
            return char.IsLetter(c) ||
                   (c >= '\u0400' && c <= '\u04FF'); // Explicit Cyrillic range check
        }

        private void UpdateButtonState(Key key, bool isPressed)
        {
            switch (key)
            {
                case Key.Up:
                    bUp.Tag = isPressed ? "Pressed" : null;
                    break;
                case Key.Down:
                    bDown.Tag = isPressed ? "Pressed" : null;
                    break;
                case Key.Left:
                    bLeft.Tag = isPressed ? "Pressed" : null;
                    break;
                case Key.Right:
                    bRight.Tag = isPressed ? "Pressed" : null;
                    break;
            }
        }



#pragma warning disable IDE1006 // Стили именования
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                UpdateButtonState(e.Key, true);
            }
            if (e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                if (Keyboard.Modifiers != ModifierKeys.Shift)
                    ReturnFocusToNodes();
            }
            if ((e.Key == Key.Up || e.Key == Key.Down) &&
                Keyboard.Modifiers == ModifierKeys.Shift && expFilter.IsExpanded == false)
            {
                expFilter.IsExpanded = true;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                UpdateButtonState(e.Key, false);
            }
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var tp = border.TemplatedParent;
        }

        private void expFilter_Expanded(object sender, RoutedEventArgs e)
        {
            var expander = (Expander)sender;
            expander.Height += 50;
            this.Height += 50;

        }

        private void expFilter_Collapsed(object sender, RoutedEventArgs e)
        {
            var expander = (Expander)sender;
            expander.Height -= 50;
            this.Height -= 50;
        }

        private void cbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReturnFocusToNodes();
        }
#pragma warning restore IDE1006 // Стили именования

        private void ReturnFocusToNodes()
        {
            var selected = lvNodes.SelectedItem ?? lvNodes.Items.Cast<object>().First();
            var container = lvNodes.ItemContainerGenerator.ContainerFromItem(selected) as ListViewItem;
            container?.Focus();
        }
    }
}
