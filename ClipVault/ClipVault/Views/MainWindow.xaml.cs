using System.Windows;
using System.Windows.Input;
using ClipVault.ViewModels;

namespace ClipVault.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // Restore window position/size from settings
        var s = App.Settings.Settings;
        Width = s.WindowWidth;
        Height = s.WindowHeight;
        if (s.WindowLeft.HasValue) Left = s.WindowLeft.Value;
        if (s.WindowTop.HasValue) Top = s.WindowTop.Value;
    }

    public void ShowAndActivate()
    {
        // Reset type filter to "all" (clears favorites mode too)
        if (_vm.TypeFilter == "favorites")
            _vm.TypeFilter = "all";
        else
            _vm.Refresh();
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public void ShowFavorites()
    {
        _vm.TypeFilter = "favorites";
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public void HideToTray()
    {
        Hide();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var s = App.Settings.Settings;
        s.WindowLeft = Left;
        s.WindowTop = Top;
        s.WindowWidth = Width;
        s.WindowHeight = Height;
        App.Settings.Save();

        e.Cancel = true;
        HideToTray();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(_vm.SearchInputText))
            {
                _vm.ClearSearch();
                SearchBox.Text = string.Empty;
                e.Handled = true;
            }
            else
            {
                HideToTray();
                e.Handled = true;
            }
        }
    }

    // Enter 触发搜索，Esc 清空
    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _vm.ExecuteSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _vm.ClearSearch();
            SearchBox.Text = string.Empty;
            e.Handled = true;
        }
    }

    private void ExitFavorites_Click(object sender, RoutedEventArgs e)
    {
        _vm.TypeFilter = "all";
    }

    private void PreviewText_Click(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ClipItemViewModel vm)
        {
            App.Clipboard.CopyWithoutRecording(vm.Content);
            HideToTray();
        }
    }
}
