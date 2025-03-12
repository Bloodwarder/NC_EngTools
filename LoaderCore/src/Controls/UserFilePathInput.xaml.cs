using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace LoaderCore.Controls
{
    /// <summary>
    /// Логика взаимодействия для UserStringInput.xaml
    /// </summary>
    public partial class UserFilePathInput : LabeledHorizontalInput
    {
        public static readonly DependencyProperty FilePathProperty;
        static UserFilePathInput()
        {
            FilePathProperty = DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(UserFilePathInput));
        }

        public string FilePath
        {
            get => (string)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public UserFilePathInput()
        {
            InitializeComponent();
        }

        private void bOpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileInfo? fi = null;
            if (!string.IsNullOrEmpty(inputFilePath.Text))
                fi = new(inputFilePath.Text);

            
            OpenFileDialog ofd = new OpenFileDialog()
            {
                DefaultExt = ".dwg",
                Filter = "DWG files|*.dwg",
                CheckFileExists = true,
                Multiselect = false
            };
            if (fi?.Directory?.Exists ?? false)
            {
                ofd.InitialDirectory = fi.DirectoryName;
            }
            var result = ofd.ShowDialog();
            if (result == true)
                inputFilePath.Text = ofd.FileName;
        }
    }
}
