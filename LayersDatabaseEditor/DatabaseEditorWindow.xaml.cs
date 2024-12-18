using LayersIO.Excel;
using LoaderCore;
using LoaderCore.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Connection;
using LoaderCore.Interfaces;
using LayersDatabaseEditor.ViewModel;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace LayersDatabaseEditor
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class DatabaseEditorWindow : Window
    {
        readonly ILogger? _logger = NcetCore.ServiceProvider.GetService<ILogger>();
        readonly IFilePathProvider _pathProvider = NcetCore.ServiceProvider.GetRequiredService<IFilePathProvider>();

        public DatabaseEditorWindow()
        {

            InitializeComponent();
            PreInitializeSimpleLogger.Log += LogWrite;
            EditorViewModel = NcetCore.ServiceProvider.GetRequiredService<EditorViewModel>();
            EditorViewModel.LayerGroupNames.CollectionChanged += (s, e) => UpdateTreeView(s, e);

        }

        internal EditorViewModel EditorViewModel { get; private set; }

        private async void miTestRun_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("Запущена тестовая команда");
            Task<string> task = TestMethod1Async();
            await LogWriteAsync(task);

        }

        private static async Task<string> TestMethod1Async()
        {
            await Task.Delay(3000);
            return "Test1 completed";
        }

        private void expLog_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height -= 175d;
            ((Expander)sender).Height = 25d;
        }

        private void expLog_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).Height = 200d;
            this.Height += 175d;
        }

        private async Task LogWriteAsync(Task<string> task)
        {
            Run run = new();
            fdLog.Blocks.Add(new Paragraph(run) { Margin = new(0d) });
            await LogBuffer.Instance.Message(run, task);

            //await Task.Run(() => fdLog.Blocks.Add(new Paragraph(new Run(message))));
        }
        private void LogWrite(string message)
        {
            fdLog.Blocks.Add(new Paragraph(new Run(message)) { Margin = new(0d) });
        }

        private void LogClear()
        {
            fdLog.Blocks.Clear();
        }

        private async void miExportLayersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            throw new InvalidOperationException("Метод не переработан. Может нарушить целостность данных");
            LogWrite("Запущен импорт слоёв из Excel");
            var reader = new ExcelLayerReader();
            Task<string> task = reader.ReadWorkbookAsync(_pathProvider.GetPath("Layer_Props.xlsm"));
            await LogWriteAsync(task);
            //ExcelLayerReader.ReadWorkbook(PathProvider.GetPath("Layer_Props.xlsm"));
        }


        private void miTestRun2_Click(object sender, RoutedEventArgs e)
        {
            using (LayersDatabaseContextSqlite db = new(_pathProvider.GetPath("LayerData.db"), null))
            {
                var layers = db.Layers.Skip(25).Take(5).ToArray();
                foreach (var layer in layers)
                {
                    LogWrite($"{layer.Name}   {layer.LayerDrawTemplateData?.DrawTemplate}  {layer.LayerPropertiesData?.LinetypeScale}");
                }
            }
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpdateTreeView(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (string str in e.NewItems!.Cast<string>())
                    {
                        string[] decomp = str.Split("_");
                        TreeView treeView = twLayerGroups;
                        var items = treeView.Items;
                        for (int i = 0; i < decomp.Length; i++)
                        {
                            var item = items.Cast<object>()
                                            .Where(i => i is TreeViewItem)
                                            .Select(item => (TreeViewItem)item)
                                            .Where(twi => twi.Header.ToString() == decomp[i])
                                            .FirstOrDefault();
                            if (item == null)
                            {
                                item = new TreeViewItem()
                                {
                                    Header = decomp[i],
                                    Tag = string.Join("_", decomp.Take(i + 1).ToArray())
                                };
                                items.Add(item);
                            }
                            items = item.Items;
                        }
                        LogWrite($"Добавлен элемент {str}");
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (string str in e.OldItems!)
                    {
                        string[] decomp = str.Split("_");
                        TreeView treeView = twLayerGroups;
                        var items = treeView.Items;
                        TreeViewItem item = items.Cast<TreeViewItem>().Where(item => (string)item.Header == decomp[0]).Single();
                        for (int i = 1; i < decomp.Length; i++)
                        {
                            var childItem = items.Cast<TreeViewItem>().Where(item => item.Header == decomp[i]).FirstOrDefault();
                            if (childItem != null)
                            {
                                item = childItem;
                                items = childItem.Items;
                            }
                            else
                            {
                                items.Remove(item);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    twLayerGroups.Items.Clear();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new InvalidOperationException("Как оно сюда попало?");

                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException("Возможно попадёт при переименовании группы. Как попадёт, так и допишу");

                default:
                    break;
            }
        }

        private void miSqliteConnect_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo di = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.GetDirectories("UserData").First();
            OpenFileDialog ofd = new OpenFileDialog()
            {
                DefaultExt = ".db",
                Filter = "SQLite database files|*.db;*.sqlite",
                CheckFileExists = true,
                Multiselect = false,
                InitialDirectory = di.FullName
            };
            var result = ofd.ShowDialog(this);
            if (result == true)
            {
                string fileName = ofd.FileName;
                EditorViewModel.ConnectCommand.Execute(fileName);
            }
        }

        private void twLayerGroups_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = (TreeViewItem)e.NewValue;
            if (EditorViewModel.ChangeSelectedGroupCommand.CanExecute(item))
                EditorViewModel.ChangeSelectedGroupCommand.Execute(item?.Tag);
            if (EditorViewModel.SelectedGroup != null)
            {
                this.DataContext = EditorViewModel.SelectedGroup;
            }
            else
            {
                this.DataContext = null;
            }
        }

        private void lvLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            if (EditorViewModel.ChangeSelectedLayerCommand.CanExecute(listView.SelectedItem))
                EditorViewModel.ChangeSelectedLayerCommand.Execute(listView.SelectedItem);
            if (EditorViewModel.SelectedLayer != null)
            {
                tcProperties.DataContext = EditorViewModel.SelectedLayer;
            }
            else
            {
                tcProperties.DataContext = null;
            }
        }
    }

}
