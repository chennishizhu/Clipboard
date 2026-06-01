using ClipVault.Models;
using ClipVault.Services;

namespace ClipVault.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Settings;

        _autoStart = settingsService.IsAutoStartEnabled();
        _databasePath = _settings.DatabasePath;
        _maxRecords = _settings.MaxRecords;
        _globalHotkey = _settings.GlobalHotkey;
        _globalHotkeyEnabled = _settings.GlobalHotkeyEnabled;
    }

    private bool _autoStart;
    public bool AutoStart
    {
        get => _autoStart;
        set => SetField(ref _autoStart, value);
    }

    private string _databasePath;
    public string DatabasePath
    {
        get => _databasePath;
        set => SetField(ref _databasePath, value);
    }

    private int _maxRecords;
    public int MaxRecords
    {
        get => _maxRecords;
        set => SetField(ref _maxRecords, Math.Clamp(value, 100, 2000));
    }

    private string _globalHotkey;
    public string GlobalHotkey
    {
        get => _globalHotkey;
        set => SetField(ref _globalHotkey, value);
    }

    private bool _globalHotkeyEnabled;
    public bool GlobalHotkeyEnabled
    {
        get => _globalHotkeyEnabled;
        set => SetField(ref _globalHotkeyEnabled, value);
    }

    public void ApplyAutoStart()
    {
        _settingsService.SetAutoStart(AutoStart);
    }

    public void SaveToSettings()
    {
        _settings.DatabasePath = DatabasePath;
        _settings.MaxRecords = MaxRecords;
        _settings.GlobalHotkey = GlobalHotkey;
        _settings.GlobalHotkeyEnabled = GlobalHotkeyEnabled;
        _settingsService.Save();
        _settingsService.SetAutoStart(AutoStart);
    }
}
