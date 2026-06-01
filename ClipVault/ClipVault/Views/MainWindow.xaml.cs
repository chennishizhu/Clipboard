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
        _vm.Refresh();
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
        // Save window position and size before hiding
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
            if (!string.IsNullOrEmpty(_vm.SearchText))
            {
                _vm.SearchText = string.Empty;
                e.Handled = true;
            }
            else
            {
                HideToTray();
                e.Handled = true;
            }
        }
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
