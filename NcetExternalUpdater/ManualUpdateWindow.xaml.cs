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

namespace NcetExternalUpdater
{
    /// <summary>
    /// Логика взаимодействия для ManualUpdateWindow.xaml
    /// </summary>
    public partial class ManualUpdateWindow : Window
    {
        private static SolidColorBrush? _errorBrush = new(Colors.DarkRed);
        private static SolidColorBrush? _actualBrush = new(Colors.DarkGray);

        public ManualUpdateWindow(Action action)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Program.FileCheckedEvent += HandleWindowUpdateAction;
            this.Loaded += (s, e) => action?.Invoke();
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        internal void PushProgressBar(int? rounds = null)
        {
            double max = 0;
            double val = 0;
            Dispatcher.Invoke(() => max = pbUpdateBar.Maximum);
            Dispatcher.Invoke(() => val = pbUpdateBar.Value);
            var random = new Random();
            if (rounds == null)
            {
                while (val < max)
                {
                    Task.Delay(random.Next(50, 100)).Wait();
                    Dispatcher.Invoke(() => pbUpdateBar.Value += 1);
                    val += 1;
                }
            }
            else
            {
                for (int i = 0; i < rounds.Value && val < max; i++)
                {
                    Task.Delay(random.Next(50, 100)).Wait();
                    Dispatcher.Invoke(() => pbUpdateBar.Value += 1);
                    val += 1;
                }
            }
        }

        private void HandleWindowUpdateAction(object? sender, FileCheckedEventArgs e)
        {
            if (e.LocalPath?.Name == "NcetExternalUpdater.exe")
                return;
            Run run;
            switch (e.CheckState)
            {
                case FileCheckState.Actual:
                    run = new($"Файл {e.SourcePath!.Name} актуальной версии");
                    run.Foreground = _actualBrush;
                    fdUpdateLog.Blocks.Add(new Paragraph(run));
                    Dispatcher.Invoke(() => pbUpdateBar.Value += 1);
                    break;
                case FileCheckState.Outdated:
                    run = new($"Обновление файла {e.SourcePath!.Name}");
                    fdUpdateLog.Blocks.Add(new Paragraph(run));
                    Dispatcher.Invoke(() => pbUpdateBar.Value += 1);
                    break;
                case FileCheckState.Deleted:
                    run = new(e.SourcePath is null ? e.Message : $"Удаление файла {e.SourcePath.Name}");
                    fdUpdateLog.Blocks.Add(new Paragraph(run));
                    break;
                case FileCheckState.Added:
                    run = new($"Копирование файла {e.SourcePath!.Name}");
                    fdUpdateLog.Blocks.Add(new Paragraph(run));
                    Dispatcher.Invoke(() => pbUpdateBar.Value += 1);
                    break;
                case FileCheckState.Error:
                    run = new($"Ошибка:{e.Message}\nИсключение: {e.Exception?.Message}");
                    run.Foreground = _errorBrush;
                    fdUpdateLog.Blocks.Add(new Paragraph(run));
                    break;
            }
        }
    }
}
