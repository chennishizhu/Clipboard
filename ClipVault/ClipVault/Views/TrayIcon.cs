using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using ModernWpf;

namespace ClipVault.Views;

public class TrayIcon : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private readonly MainWindow _mainWindow;
    private MenuItem _darkModeItem = null!;

    public event Action? ExitRequested;

    public TrayIcon(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        _taskbarIcon = new TaskbarIcon { ToolTipText = "ClipVault" };

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

        // Dark mode toggle — reflect current setting
        bool isDark = App.Settings.Settings.Theme == "Dark";
        _darkModeItem = new MenuItem
        {
            Header = isDark ? "切换到浅色模式" : "切换到深色模式",
            IsCheckable = false
        };
        _darkModeItem.Click += (_, _) => ToggleTheme();

        var settingsItem = new MenuItem { Header = "设置" };
        settingsItem.Click += (_, _) => OpenSettings();

        var pauseItem = new MenuItem { Header = "暂停监听", IsCheckable = true };
        pauseItem.Click += (_, _) => App.Clipboard.IsPaused = pauseItem.IsChecked;

        var clearItem = new MenuItem { Header = "全部清除" };
        clearItem.Click += (_, _) => ClearAll();

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_darkModeItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(clearItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private void ToggleTheme()
    {
        var settings = App.Settings.Settings;
        bool goingDark = settings.Theme != "Dark";
        settings.Theme = goingDark ? "Dark" : "Light";
        App.Settings.Save();
        ApplyTheme(settings.Theme);
        _darkModeItem.Header = goingDark ? "切换到浅色模式" : "切换到深色模式";
    }

    public static void ApplyTheme(string theme)
    {
        ThemeManager.Current.ApplicationTheme = theme switch
        {
            "Dark" => ApplicationTheme.Dark,
            "Light" => ApplicationTheme.Light,
            _ => null // System
        };
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

    private void ClearAll()
    {
        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
        {
            // 以主窗口为 owner，确保 MessageBox 不会因失焦而消失
            _mainWindow.Show();
            _mainWindow.Activate();

            var result = MessageBox.Show(
                _mainWindow,
                "将删除所有非收藏、非置顶的记录，此操作不可撤销。\n\n确定要继续吗？",
                "全部清除",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            _mainWindow.Hide();

            if (result != MessageBoxResult.OK) return;

            _mainWindow.ViewModel.ClearNonFavoritedNonPinned();
        });
    }

    public void Dispose() => _taskbarIcon.Dispose();
}
