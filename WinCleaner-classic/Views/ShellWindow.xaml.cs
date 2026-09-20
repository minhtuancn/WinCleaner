using System.ComponentModel;
using System.Windows;
using WinCleaner.Services;
using WinCleaner.ViewModels;

namespace WinCleaner.Views
{
    public partial class ShellWindow : Window
    {
        private readonly IWindowStateService _windowStateService;
        private ShellViewModel? _viewModel;

        public ShellWindow() : this(null!)
        {
        }

        public ShellWindow(IWindowStateService windowStateService)
        {
            _windowStateService = windowStateService;
            InitializeComponent();
            Loaded += ShellWindow_Loaded;
            Closing += ShellWindow_Closing;
        }

        private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as ShellViewModel;
            if (_viewModel == null) return;

            var state = await _windowStateService.LoadStateAsync();
            ApplyWindowState(state);
        }

        private async void ShellWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_viewModel == null) return;

            var state = CaptureWindowState();
            await _windowStateService.SaveStateAsync(state);
        }

        private void ApplyWindowState(WindowStateData state)
        {
            if (state == null) return;

            Width = state.Width;
            Height = state.Height;
            Left = state.Left;
            Top = state.Top;
            WindowState = state.WindowState;

            if (_viewModel != null)
            {
                _viewModel.IsSidebarOpen = state.IsSidebarOpen;
                
                if (state.SelectedNavItem != null)
                {
                    var navItem = _viewModel.NavigationItems.FirstOrDefault(n => n.Label == state.SelectedNavItem);
                    if (navItem != null)
                    {
                        _viewModel.Navigate(navItem.Label);
                    }
                }
            }

            SidebarColumn.Width = new GridLength(state.SidebarWidth);
        }

        private WindowStateData CaptureWindowState()
        {
            var state = new WindowStateData
            {
                Width = Width,
                Height = Height,
                Left = Left,
                Top = Top,
                WindowState = WindowState,
                IsMaximized = WindowState == WindowState.Maximized,
                SidebarWidth = SidebarColumn.ActualWidth,
                IsSidebarOpen = _viewModel?.IsSidebarOpen ?? true,
                SelectedNavItem = _viewModel?.CurrentView?.GetType().Name.Replace("ViewModel", "") ?? "Dashboard",
                LastUpdated = DateTime.Now
            };

            return state;
        }
    }
}