using System.Threading;
using System.Windows;
using ClipVault.Services;
using ClipVault.ViewModels;
using ClipVault.Views;

namespace ClipVault;

public partial class App : Application
{
    private Mutex? _mutex;
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;

    public static DatabaseService Database { get; } = new();
    public static ClipboardService Clipboard { get; } = new();
    public static SettingsService Settings { get; } = new();
    public static HotkeyService Hotkey { get; } = new();
    public static MainViewModel? MainVM { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // ── Single instance guard ──────────────────────────────────
        _mutex = new Mutex(initiallyOwned: true, "ClipVault_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ClipVault 已在运行中。", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // ── Load settings & init DB ───────────────────────────────
        Settings.Load();
        Database.Initialize(Settings.Settings.DatabasePath);

        // ── Apply saved theme ─────────────────────────────────────
        TrayIcon.ApplyTheme(Settings.Settings.Theme);

        // ── Create main window (hidden) ───────────────────────────
        MainVM = new MainViewModel(Database, Clipboard);
        _mainWindow = new MainWindow(MainVM);

        // ── System tray ───────────────────────────────────────────
        _trayIcon = new TrayIcon(_mainWindow);
        _trayIcon.ExitRequested += () => Shutdown();

        // ── Clipboard listener (needs window handle) ───────────────
        _mainWindow.Loaded += (_, _) =>
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle;
            Clipboard.Start(handle);
            Hotkey.Start(handle);

            if (Settings.Settings.GlobalHotkeyEnabled)
                Hotkey.Register(Settings.Settings.GlobalHotkey);
        };

        Clipboard.ContentCaptured += (content, type) =>
        {
            // Check dedup against latest DB entry
            var latest = Database.GetLatestContent();
            if (latest == content) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainVM.AddNewItem(content, type);
                Database.EnforceLimit(Settings.Settings.MaxRecords);
            });
        };

        Hotkey.HotkeyPressed += () =>
        {
            if (_mainWindow.IsVisible)
                _mainWindow.HideToTray();
            else
                _mainWindow.ShowAndActivate();
        };

        // Show main window hidden; it will appear on tray click or hotkey
        _mainWindow.Show();
        _mainWindow.HideToTray();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        Hotkey.Dispose();
        Clipboard.Dispose();
        Database.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
