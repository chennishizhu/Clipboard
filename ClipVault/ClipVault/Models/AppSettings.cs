namespace ClipVault.Models;

public class AppSettings
{
    public string DatabasePath { get; set; } = string.Empty;
    public int MaxRecords { get; set; } = 500;
    public bool AutoStart { get; set; } = false;
    public string GlobalHotkey { get; set; } = "Ctrl+Shift+V";
    public bool GlobalHotkeyEnabled { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double WindowWidth { get; set; } = 400;
    public double WindowHeight { get; set; } = 600;
}
