using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
