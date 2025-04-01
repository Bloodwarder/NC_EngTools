using LoaderCore.Integrity;
using LoaderCore.Utilities;
using Markdig;
using Ookii.Dialogs.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xml;
using System.Xml.Linq;

namespace LoaderCore.UI
{
    /// <summary>
    /// Логика взаимодействия для StartUpWindow.xaml
    /// </summary>
    public partial class StartUpWindow : Window
    {
        private readonly XDocument _xmlConfig;
        private readonly string _xmlConfigPath;
        //XmlDataProvider _xmlDataProvider;

        public StartUpWindow(string xmlConfigPath)
        {
            _xmlConfigPath = xmlConfigPath;
            _xmlConfig = XDocument.Load(xmlConfigPath);
            InitializeComponent();
#if DEBUG
            this.Title = $"{this.Title} (отладочная сборка)";
#endif
            //_xmlDataProvider = (XmlDataProvider)this.Resources["configurationProvider"];
            //_xmlDataProvider.Document = new XmlDocument();
            //_xmlDataProvider.Document.Load(xmlConfigPath);
            //_xmlDataProvider.Refresh();
            //XmlDataProvider provider = new()
            //{
            //    Source = new Uri(xmlConfigPath),
            //    XPath = "configuration"
            //};

            // TODO: заменить на XmlDataProvider и привязки
#nullable disable warnings
            chbShowOnStartUp.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("ShowStartup").Value);

            //chbIncludeLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("LayerWorksConfiguration").Element("Enabled").Value);
            //chbAutoUpdateLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("LayerWorksConfiguration").Element("UpdateEnabled").Value);
            //chbIncludeUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("UtilitiesConfiguration").Element("Enabled").Value);
            //chbAutoUpdateUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("UtilitiesConfiguration").Element("UpdateEnabled").Value);
            //chbIncludeGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("GeoModConfiguration").Element("Enabled").Value);
            //chbAutoUpdateGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("GeoModConfiguration").Element("UpdateEnabled").Value);

            tbSourcePath.Text = _xmlConfig.Root.Element("Directories").Element("UpdateDirectory").Value;
#nullable restore
            // Вывод данных о последнем обновлении
            DisplayPatchNotes();
        }

        private void DisplayPatchNotes()
        {
            var patchNotesPath = Path.Combine(new FileInfo(_xmlConfigPath).DirectoryName!, "ExtensionLibraries", "LoaderCore", "Список изменений.md");
            var stylesPath = Path.Combine(new FileInfo(_xmlConfigPath).DirectoryName!, "ExtensionLibraries", "LoaderCore", "Styles.css");
            string htmlContent = MdToHtmlConverter.Convert(patchNotesPath, stylesPath);
            wbUpdates.NavigateToString(htmlContent);
        }

        public void IncludeCheckChanged(object sender, RoutedEventArgs e)
        {
            //CheckBox checkBox = sender as CheckBox;
            //if ((bool)checkBox.IsChecked)
            //    Loader.StructureComparer.IncludedModules.Add(checkBox.Tag as string);
            //else
            //    Loader.StructureComparer.IncludedModules.Remove(checkBox.Tag as string);
            //e.Handled = true;
        }

        public void AutoUpdateCheckChanged(object sender, RoutedEventArgs e)
        {
            //CheckBox checkBox = sender as CheckBox;
            //if ((bool)checkBox.IsChecked)
            //    Loader.FileUpdater.UpdatedModules.Add(checkBox.Tag as string);
            //else
            //    Loader.FileUpdater.UpdatedModules.Remove(checkBox.Tag as string);
            //e.Handled = true;
        }

        private void SetUpdatePathButtonClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new()
            {
                Multiselect = false,
                ShowNewFolderButton = false
            };
            dialog.ShowDialog();
            DirectoryInfo dir = new(dialog.SelectedPath);
            string startupPath = Path.Combine(dir.FullName, "NC_EngTools_StartUp.dll");
            string loaderCorePath = Path.Combine(dir.FullName, "ExtensionLibraries", "LoaderCore", "LoaderCore.dll");
            bool dirExists = dir.Exists;
            bool startupExists = File.Exists(startupPath);
            bool loaderCoreExists = File.Exists(loaderCorePath);
            if (dirExists && startupExists && loaderCoreExists)
            {
                tbSourcePath.Text = dialog.SelectedPath;
            }
            else
            {
                MessageBox.Show("Указанная директория не содержит файлов для обновления программы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            e.Handled = true;
        }


        private void StartUpWindowClosing(object sender, CancelEventArgs e)
        {
#nullable disable warnings
            _xmlConfig.Root.Element("ShowStartup").Value = XmlConvert.ToString((bool)chbShowOnStartUp.IsChecked);

            //_xmlConfig.Root.Element("LayerWorksConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeLayerWorks.IsChecked);
            //_xmlConfig.Root.Element("LayerWorksConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateLayerWorks.IsChecked);
            //_xmlConfig.Root.Element("UtilitiesConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeUtilities.IsChecked);
            //_xmlConfig.Root.Element("UtilitiesConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateUtilities.IsChecked);
            //_xmlConfig.Root.Element("GeoModConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeGeoMod.IsChecked);
            //_xmlConfig.Root.Element("GeoModConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateGeoMod.IsChecked);

            DirectoryInfo checkdir = new(tbSourcePath.Text);
            if (checkdir.Exists)
                _xmlConfig.Root.Element("Directories").Element("UpdateDirectory").Value = tbSourcePath.Text;

            //var binding = gridMain.GetBindingExpression(DataContextProperty);
            //binding.UpdateSource();
            _xmlConfig.Save(_xmlConfigPath);
#nullable restore
        }

        private void UpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            NcetCore.Modules.Where(m => m.Name == button.Tag.ToString()).Single().Update();
        }


        private void CommandHelpClick(object sender, RoutedEventArgs e)
        {
            string mdPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Команды.md", SearchOption.AllDirectories).Single();
            string stylesPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Styles.css", SearchOption.AllDirectories).Single();
            var html = MdToHtmlConverter.Convert(mdPath, stylesPath);
            InfoDisplayWindow window = new(html, "Список команд")
            {
                Owner = this
            };
            window.ShowDialog();
        }
        private void LaunchEditorClick(object sender, RoutedEventArgs e)
        {
            string path = new DirectoryInfo(NcetCore.RootLocalDirectory).GetFiles("LayersDatabaseEditor.exe", SearchOption.AllDirectories).Single().FullName;
            System.Diagnostics.Process.Start(path);
        }
        private void KnownIssuesClick(object sender, RoutedEventArgs e)
        {
            string mdPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Известные проблемы.md", SearchOption.AllDirectories).Single();
            string stylesPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Styles.css", SearchOption.AllDirectories).Single();
            var html = MdToHtmlConverter.Convert(mdPath, stylesPath);
            InfoDisplayWindow window = new(html, "Список команд")
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
        }
    }
}
