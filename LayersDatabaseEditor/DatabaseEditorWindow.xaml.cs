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
using System.Windows.Media;
using LayersIO.DataTransfer;
using LayersDatabaseEditor.Utilities;

namespace LayersDatabaseEditor
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class DatabaseEditorWindow : Window
    {
        public static readonly DependencyProperty EditorViewModelProperty =
            DependencyProperty.Register("EditorViewModel", typeof(EditorViewModel), typeof(DatabaseEditorWindow), new PropertyMetadata());

        readonly ILogger? _logger;
        readonly IFilePathProvider _pathProvider;

        public DatabaseEditorWindow()
        {
            InitializeComponent();
#if !DEBUG
            miTestRun.Visibility = Visibility.Collapsed;
            miDevSqliteConnect.Visibility = Visibility.Collapsed;
            bSpecialZoneEditor.IsEnabled = false;
#endif
            // TODO: перестроить на constructor injection
            _logger = NcetCore.ServiceProvider.GetService<ILogger>();
            if (_logger is EditorWindowLogger ewLogger)
                ewLogger.RegisterWindow(this);

            _pathProvider = NcetCore.ServiceProvider.GetRequiredService<IFilePathProvider>();

            PreInitializeSimpleLogger.Log += LogWrite;
            EditorViewModel = NcetCore.ServiceProvider.GetRequiredService<EditorViewModel>();
            EditorViewModel.LayerGroupNames.CollectionChanged += (s, e) => UpdateTreeView(s, e);

            cmUpdate.DataContext = EditorViewModel;
            cmReset.DataContext = EditorViewModel;
        }


        public EditorViewModel EditorViewModel
        {
            get { return (EditorViewModel)GetValue(EditorViewModelProperty); }
            set { SetValue(EditorViewModelProperty, value); }
        }

        private void miTestRun_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("Запущена тестовая команда");
            var foo = EditorViewModel.SelectedGroup;
            var bar = bullshitMenuItem;
            //Task<string> task = TestMethod1Async();
            //await LogWriteAsync(task);

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

        private void LogClear(object sender, RoutedEventArgs e)
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
        private void miTestRun3_Click(object sender, RoutedEventArgs e)
        {
            var color1 = caBaseColor.Color;
            Color color2 = EditorViewModel.SelectedLayer?.LayerProperties.Color ?? Color.FromRgb(127, 127, 127);
            var color3 = brajInnerHatchShift.BaseColor;
            string message = $"\nЦвет caBaseColor.Color:\t\t\t{color1.R}-{color1.G}-{color1.B}\nЦвет ViewModel:\t\t\t\t{color2.R}-{color2.G}-{color2.B}\nЦвет brajInnerHatchShift.BaseColor:\t\t{color3.R}-{color3.G}-{color3.B}";
            LogWrite(message);
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
                        if (string.IsNullOrEmpty(str))
                            continue;

                        string[] decomp = str.Split("_"); // UNDONE: сепаратор в хардкоде
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
                            items = item.Items;
                            var childItem = items.Cast<TreeViewItem>().Where(chItem => chItem.Header as string == decomp[i]).FirstOrDefault();
                            if (childItem != null)
                            {
                                item = childItem;
                            }
                        }
                        var parentItem = item.Parent;
                        items.Remove(item);
                        while (parentItem is TreeViewItem twi && !twi.Items.Cast<TreeViewItem>().Any())
                        {
                            TreeViewItem? newParentItem = twi.Parent as TreeViewItem;
                            newParentItem?.Items.Remove(parentItem);
                            parentItem = newParentItem;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    twLayerGroups.Items.Clear();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break; // TODO: разобраться как попадает сюда

                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException("Возможно попадёт при переименовании группы. Как попадёт, так и допишу");

                default:
                    break;
            }
        }

        private void miSqliteConnect_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo di = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.GetDirectories("UserData").First();
            OpenFileDialog ofd = new()
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
            {
                EditorViewModel.ChangeSelectedGroupCommand.Execute(item?.Tag);
            }
            else
            {
                return;
            }
        }

        private void lvLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Костыль
            if (EditorViewModel.SelectedLayer != null)
            {
                caBaseColor.UpdateRgbControls(); // BUG: Надо апдейтить где-то внутри контрола, чтобы он был самодостаточным. Пока работает только с этим
            }
        }

        private void tcProperties_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TabControl tc = (TabControl)sender;
            var context = tc.DataContext as LayerDataViewModel;

            if (context != null)
            {
                var drw = context.LayerDrawTemplate.DrawTemplate ?? DrawTemplate.Undefined;
                CheckTemplateSectionsVisibility(drw);
            }
        }
        private void cbDrawTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (ComboBox)sender;
            var dt = box.SelectedItem as DrawTemplate?;
            if (dt != null)
                CheckTemplateSectionsVisibility(dt.Value);
        }

        private void CheckTemplateSectionsVisibility(DrawTemplate drw)
        {
            spBlockReference.Visibility = (drw & (DrawTemplate.BlockReference)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spCircles.Visibility = (drw & (DrawTemplate.HatchedCircle)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spFence.Visibility = (drw & (DrawTemplate.FencedRectangle | DrawTemplate.HatchedFencedRectangle)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spFenceHatch.Visibility = (drw & (DrawTemplate.HatchedFencedRectangle)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spLines.Visibility = (drw & (DrawTemplate.MarkedSolidLine | DrawTemplate.MarkedDashedLine)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spRectangles.Visibility = (drw & (DrawTemplate.Rectangle |
                                              DrawTemplate.HatchedRectangle |
                                              DrawTemplate.FencedRectangle |
                                              DrawTemplate.HatchedFencedRectangle)) != 0 ?
                Visibility.Visible : Visibility.Collapsed;
            spHatch.Visibility = EditorViewModel.SelectedGroup?.Name.Contains("_п_") ?? false ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void ButtonWithContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = !button.ContextMenu.IsOpen;
            }
        }

        private void databaseEditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EditorViewModel.Database?.Dispose();
        }

        private void svLayers_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }

}
