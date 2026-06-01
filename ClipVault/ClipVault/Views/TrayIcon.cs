using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace ClipVault.Views;

public class TrayIcon : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private readonly MainWindow _mainWindow;

    public event Action? ExitRequested;

    public TrayIcon(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "ClipVault",
            // Icon will be loaded from resource; fallback to default if not found
        };

        try
        {
            var uri = new Uri("pack://application:,,,/Resources/tray.ico");
            _taskbarIcon.Icon = new System.Drawing.Icon(
                Application.GetResourceStream(uri)!.Stream);
        }
        catch { /* use default icon */ }

        _taskbarIcon.TrayLeftMouseDown += (_, _) => ToggleMainWindow();
        _taskbarIcon.ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "打开主窗口" };
        openItem.Click += (_, _) => _mainWindow.ShowAndActivate();

        var settingsItem = new MenuItem { Header = "设置" };
        settingsItem.Click += (_, _) => OpenSettings();

        var pauseItem = new MenuItem { Header = "暂停监听", IsCheckable = true };
        pauseItem.Click += (_, _) =>
        {
            App.Clipboard.IsPaused = pauseItem.IsChecked;
        };

        var separator = new Separator();

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(openItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);
        return menu;
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow.IsVisible)
            _mainWindow.HideToTray();
        else
            _mainWindow.ShowAndActivate();
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow();
        win.ShowDialog();
    }

    public void Dispose() => _taskbarIcon.Dispose();
}
