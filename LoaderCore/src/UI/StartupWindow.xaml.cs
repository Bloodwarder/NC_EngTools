using LoaderCore.Integrity;
using LoaderCore.Utilities;
using Markdig;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

        public StartUpWindow(string xmlConfigPath)
        {
            _xmlConfigPath = xmlConfigPath;
            _xmlConfig = XDocument.Load(xmlConfigPath);

            InitializeComponent();

            PreInitializeSimpleLogger.Log += LogWindow;

            // TODO: заменить на XmlDataProvider и привязки
#nullable disable warnings
            chbShowOnStartUp.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("ShowStartup").Value);

            chbIncludeLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("LayerWorksConfiguration").Element("Enabled").Value);
            chbAutoUpdateLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("LayerWorksConfiguration").Element("UpdateEnabled").Value);
            chbIncludeUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("UtilitiesConfiguration").Element("Enabled").Value);
            chbAutoUpdateUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("UtilitiesConfiguration").Element("UpdateEnabled").Value);
            chbIncludeGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("GeoModConfiguration").Element("Enabled").Value);
            chbAutoUpdateGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("GeoModConfiguration").Element("UpdateEnabled").Value);

            tbSourcePath.Text = _xmlConfig.Root.Element("Directories").Element("UpdateDirectory").Value;
#nullable restore
            // Вывод данных о последнем обновлении
            DisplayPatchNotes();
        }

        private void DisplayPatchNotes()
        {
            var patchNotesPath = Path.Combine(new FileInfo(_xmlConfigPath).DirectoryName!, "ExtensionLibraries", "LoaderCore", "Список изменений.md");
            var stylesPath = Path.Combine(new FileInfo(_xmlConfigPath).DirectoryName!, "ExtensionLibraries", "LoaderCore", "Styles.css");
            string? markdownText;
            string? styles;
            using (StreamReader reader = new(patchNotesPath))
            {
                markdownText = reader.ReadToEnd();
            }
            using (StreamReader stylesReader = new(stylesPath))
            {
                styles = stylesReader.ReadToEnd();
            }
            var pipeline = new MarkdownPipelineBuilder().Build();

            var content = Markdown.ToHtml(markdownText, pipeline);

            string htmlContent = $@"<!DOCTYPE html>
                                    <html lang='en'>
                                    <head>
                                    <meta charset='utf-8'>
                                    <style>
                                    {styles}
                                    </style>
                                    </head>
                                    <body>
                                    {content}
                                    </body>
                                    </html>";
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
            //    VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog()
            //    {
            //        Multiselect = false,
            //        ShowNewFolderButton = false,
            //        RootFolder = Environment.SpecialFolder.NetworkShortcuts
            //    };
            //    dialog.ShowDialog();
            //    DirectoryInfo dir = new DirectoryInfo(dialog.SelectedPath);
            //    if (dir.Exists)
            //    {
            //        tbSourcePath.Text = dialog.SelectedPath;
            //    }
            //    e.Handled = true;
        }


        private void StartUpWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
#nullable disable warnings
            _xmlConfig.Root.Element("ShowStartup").Value = XmlConvert.ToString((bool)chbShowOnStartUp.IsChecked);

            _xmlConfig.Root.Element("LayerWorksConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeLayerWorks.IsChecked);
            _xmlConfig.Root.Element("LayerWorksConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateLayerWorks.IsChecked);
            _xmlConfig.Root.Element("UtilitiesConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeUtilities.IsChecked);
            _xmlConfig.Root.Element("UtilitiesConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateUtilities.IsChecked);
            _xmlConfig.Root.Element("GeoModConfiguration").Element("Enabled").Value = XmlConvert.ToString((bool)chbIncludeGeoMod.IsChecked);
            _xmlConfig.Root.Element("GeoModConfiguration").Element("UpdateEnabled").Value = XmlConvert.ToString((bool)chbAutoUpdateGeoMod.IsChecked);

            _xmlConfig.Save(_xmlConfigPath);

            //DirectoryInfo checkdir = new DirectoryInfo(tbSourcePath.Text);
            //if (checkdir.Exists)
            //    _xmlConfig.Root.Element("Directories").Element("UpdateDirectory").Value = tbSourcePath.Text;
            _xmlConfig.Save(_xmlConfigPath);
#nullable restore
            PreInitializeSimpleLogger.Log -= LogWindow;
        }

        private void UpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            NcetCore.Modules.Where(m => m.Name == button.Tag.ToString()).Single().Update();
        }

        private void LogWindow(string message)
        {
            //fdLog.Blocks.Add(new Paragraph(new Run($"{message}")));
            //tbLog.Text += $"\n{message}";
        }

        private void CommandHelpClick(object sender, RoutedEventArgs e)
        {
            string path = new DirectoryInfo(NcetCore.RootLocalDirectory).GetFiles("Команды.txt").Single().FullName;
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
        private void UpdateLogClick(object sender, RoutedEventArgs e)
        {
            string path = new DirectoryInfo(NcetCore.RootLocalDirectory).GetFiles("Список изменений.txt").Single().FullName;
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
        private void KnownIssuesClick(object sender, RoutedEventArgs e)
        {
            string path = new DirectoryInfo(NcetCore.RootLocalDirectory).GetFiles("Известные проблемы.txt").Single().FullName;
            System.Diagnostics.Process.Start("notepad.exe", path);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Escape)
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
