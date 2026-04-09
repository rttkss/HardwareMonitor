using System.Windows;

namespace HardwareMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppViewModel vm = DataContext as AppViewModel;
            if (vm != null)
            {
                vm.Shutdown();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "System Inspector\n\n" +
                "Обзор — сводка по системе\n" +
                "CPU — процессор и график\n" +
                "RAM — память и модули\n" +
                "Диски — накопители\n" +
                "Задачи — процессы\n\n" +
                "Обновление каждые 1.5 сек.",
                "Справка",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}