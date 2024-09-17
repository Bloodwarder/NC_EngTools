﻿using LoaderCore.Integrity;
using LoaderCore.Utilities;
using System.IO;
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
        private readonly XDocument _xmlStructure;
        private readonly string _xmlStructurePath;

        public StartUpWindow(string xmlConfigPath, string xmlStructurePath)
        {
            _xmlConfigPath = xmlConfigPath;
            _xmlConfig = XDocument.Load(xmlConfigPath);

            _xmlStructurePath = xmlStructurePath;
            _xmlStructure = XDocument.Load(xmlStructurePath);

            InitializeComponent();

            LoggingRouter.WriteLog += LogWindow;

            chbShowOnStartUp.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("StartUpShow").Attribute("Enabled").Value);

            chbIncludeLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Include").Value);
            chbAutoUpdateLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Update").Value);
            chbIncludeUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Include").Value);
            chbAutoUpdateUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Update").Value);
            chbIncludeGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("GeoMod").Attribute("Include").Value);
            chbAutoUpdateGeoMod.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("GeoMod").Attribute("Update").Value);

            tbSourcePath.Text = _xmlStructure.Root.Element("basepath").Element("source").Value;
            // Вывод данных о последнем обновлении
            using (StreamReader reader = new StreamReader(PathProvider.GetPath("Список изменений.txt")))
            {
                string line;
                LoggingRouter.WriteLog("Последние обновления:");
                while ((line = reader.ReadLine()) != "" || reader.EndOfStream)
                {
                    LoggingRouter.WriteLog(line.Replace("\t", ""));
                }
                LoggingRouter.WriteLog("\n");
            }
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
            _xmlConfig.Root.Element("StartUpShow").Attribute("Enabled").Value = XmlConvert.ToString((bool)chbShowOnStartUp.IsChecked);

            _xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Include").Value = XmlConvert.ToString((bool)chbIncludeLayerWorks.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Update").Value = XmlConvert.ToString((bool)chbAutoUpdateLayerWorks.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Include").Value = XmlConvert.ToString((bool)chbIncludeUtilities.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Update").Value = XmlConvert.ToString((bool)chbAutoUpdateUtilities.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("GeoMod").Attribute("Include").Value = XmlConvert.ToString((bool)chbIncludeGeoMod.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("GeoMod").Attribute("Update").Value = XmlConvert.ToString((bool)chbAutoUpdateGeoMod.IsChecked);

            _xmlConfig.Save(_xmlConfigPath);

            DirectoryInfo checkdir = new DirectoryInfo(tbSourcePath.Text);
            if (checkdir.Exists)
                _xmlStructure.Root.Element("basepath").Element("source").Value = tbSourcePath.Text;
            _xmlStructure.Save(_xmlStructurePath);
            LoggingRouter.WriteLog -= LogWindow;
        }

        private void UpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            FileUpdater.UpdateRange(StructureComparer.GetFiles(_xmlStructure), button.Tag.ToString());
        }

        private void LogWindow(string message)
        {
            fdLog.Blocks.Add(new Paragraph(new Run($"{message}\n")));
            //tbLog.Text += $"\n{message}";
        }

        private void CommandHelpClick(object sender, RoutedEventArgs e)
        {
            string path = PathProvider.GetPath("Команды.txt");
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
        private void UpdateLogClick(object sender, RoutedEventArgs e)
        {
            string path = PathProvider.GetPath("Список изменений.txt");
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
        private void KnownIssuesClick(object sender, RoutedEventArgs e)
        {
            string path = PathProvider.GetPath("Известные проблемы.txt");
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
    }
}
