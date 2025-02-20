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
using System.Windows.Shapes;

namespace LoaderCore.UI
{
    /// <summary>
    /// Логика взаимодействия для InfoDisplayWindow.xaml
    /// </summary>
    public partial class InfoDisplayWindow : Window
    {
        public InfoDisplayWindow(string htmlText, string title)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = title;
            wbDisplayInfo.NavigateToString(htmlText);
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
