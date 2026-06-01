using System.IO;
using System.Text.Json;
using ClipVault.Models;
using Microsoft.Win32;

namespace ClipVault.Services;

public class SettingsService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipVault");

    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");

    private static readonly string DefaultDbPath =
        Path.Combine(AppDataDir, "clipvault.db");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public void Load()
    {
        Directory.CreateDirectory(AppDataDir);
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        if (string.IsNullOrEmpty(Settings.DatabasePath))
            Settings.DatabasePath = DefaultDbPath;
    }

    public void Save()
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, JsonOpts));
    }

    public void SetAutoStart(bool enable)
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key == null) return;

        if (enable)
        {
            var exe = Environment.ProcessPath ?? AppContext.BaseDirectory + "ClipVault.exe";
            key.SetValue("ClipVault", $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue("ClipVault", throwOnMissingValue: false);
        }

        Settings.AutoStart = enable;
        Save();
    }

    public bool IsAutoStartEnabled()
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue("ClipVault") != null;
    }
}
