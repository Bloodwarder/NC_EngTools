using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace LoaderUI
{
    /// <summary>
    /// Логика взаимодействия для StartUpWindow.xaml
    /// </summary>
    public partial class StartUpWindow : Window
    {
        XDocument _xmlConfig;
        string _xmlConfigPath;
        XDocument _xmlStructure;
        string _xmlStructurePath;

        public StartUpWindow(string xmlConfigPath, string xmlStructurePath)
        {
            _xmlConfigPath = xmlConfigPath;
            _xmlConfig = XDocument.Load(xmlConfigPath);
            _xmlStructurePath = xmlStructurePath;
            _xmlStructure = XDocument.Load(xmlStructurePath);
            InitializeComponent();
            chbShowOnStartUp.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("StartUpShow").Attribute("Enabled").Value);
            chbIncludeLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Include").Value);
            chbAutoUpdateLayerWorks.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("AutoUpdate").Value);
            chbIncludeUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Include").Value);
            chbAutoUpdateUtilities.IsChecked = XmlConvert.ToBoolean(_xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("AutoUpdate").Value);
            tbSourcePath.Text = _xmlStructure.Root.Element("basepath").Element("source").Value;
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
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog()
            {
                Multiselect = false,
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.NetworkShortcuts
            };
            dialog.ShowDialog();
            if (dialog.SelectedPath != null)
                tbSourcePath.Text = dialog.SelectedPath;
            e.Handled = true;
        }

        private void StartUpWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _xmlConfig.Root.Element("StartUpShow").Attribute("Enabled").Value = XmlConvert.ToString((bool)chbShowOnStartUp.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("Include").Value = XmlConvert.ToString((bool)chbIncludeLayerWorks.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("LayerWorks").Attribute("AutoUpdate").Value = XmlConvert.ToString((bool)chbAutoUpdateLayerWorks.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("Include").Value = XmlConvert.ToString((bool)chbIncludeUtilities.IsChecked);
            _xmlConfig.Root.Element("Modules").Element("Utilities").Attribute("AutoUpdate").Value = XmlConvert.ToString((bool)chbAutoUpdateUtilities.IsChecked);
            _xmlConfig.Save(_xmlConfigPath);
            DirectoryInfo checkdir = new DirectoryInfo(tbSourcePath.Text);
            if (checkdir.Exists)
                _xmlStructure.Root.Element("basepath").Element("source").Value = tbSourcePath.Text;
            _xmlStructure.Save(_xmlStructurePath);
        }
    }

    public class StartUpWindowViewModel
    {
        public StartUpWindowViewModel()
        {

        }
    }
}
