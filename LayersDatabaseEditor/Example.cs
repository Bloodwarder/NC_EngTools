using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace LayersDatabaseEditor.Example
{
    // POCO class
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        // Add more properties as needed
    }

    // WPF User Control code-behind
    public partial class PersonEditorControl : UserControl
    {
        private readonly Person _person;

        public PersonEditorControl(Person person)
        {
            InitializeComponent();
            _person = person;
            GenerateControls();
        }

        private void GenerateControls()
        {
            foreach (var property in typeof(Person).GetProperties())
            {
                StackPanel stackPanel = new StackPanel();
                Label label = new Label();
                label.Content = property.Name;
                stackPanel.Children.Add(label);

                if (property.PropertyType == typeof(string))
                {
                    TextBox textBox = new TextBox();
                    textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { Source = _person });
                    stackPanel.Children.Add(textBox);
                }
                else if (property.PropertyType == typeof(int))
                {
                    TextBox textBox = new TextBox();
                    textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { Source = _person });
                    stackPanel.Children.Add(textBox);
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    DatePicker datePicker = new DatePicker();
                    datePicker.SetBinding(DatePicker.SelectedDateProperty, new Binding(property.Name) { Source = _person });
                    stackPanel.Children.Add(datePicker);
                }

                // Add more cases for different property types

                MainStackPanel.Children.Add(stackPanel);
            }
        }

        private void ConfirmChangesButton_Click(object sender, RoutedEventArgs e)
        {
            // Apply changes to the _person object after confirmation
            // Implement validation logic before applying changes
        }
    }
}