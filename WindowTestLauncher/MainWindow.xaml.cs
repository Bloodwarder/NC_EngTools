using LayersIO.Connection;
using LayerWorks.UI;
using LoaderCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

namespace WindowTestLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

#pragma warning disable IDE1006 // Стили именования
        private void bNewStandLayerTest_Click(object sender, RoutedEventArgs e)
        {
            var factory = NcetCore.ServiceProvider.GetService<IDbContextFactory<LayersDatabaseContextSqlite>>();
            var context = factory?.CreateDbContext();
            var layers = context?.Layers.Include(layer => layer.LayerGroup).AsEnumerable().Select(l => l.Name);
            NewStandardLayerWindow window = new(layers ?? Enumerable.Empty<string>())
            {
                Owner = this,
            };
            window.ShowDialog();
            var result = window.GetResultLayers();
        }
#pragma warning restore IDE1006 // Стили именования
    }
}
