using System.Windows;
using System.Windows.Controls;
using WinCleaner.ViewModels;

namespace WinCleaner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure we have admin rights for system cleaning
            if (DataContext is MainViewModel vm)
            {
                vm.LogMessage += (msg) => System.Diagnostics.Debug.WriteLine(msg);
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (DataContext is MainViewModel vm)
                {
                    // Minimize to tray logic would go here
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm && (vm.IsScanning || vm.IsCleaning))
            {
                var result = MessageBox.Show(
                    "Đang có tác vụ đang chạy. Bạn có chắc muốn thoát?",
                    "Xác nhận thoát",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            base.OnClosing(e);
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is WinCleaner.Models.CleanCategoryGroup group)
            {
                group.IsExpanded = !group.IsExpanded;
                e.Handled = true;
            }
        }
    }
}