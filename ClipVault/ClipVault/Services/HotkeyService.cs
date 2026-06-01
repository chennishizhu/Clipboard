using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipVault.Services;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 9000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifier flags
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private HwndSource? _hwndSource;
    private IntPtr _hwnd = IntPtr.Zero;

    public event Action? HotkeyPressed;

    public void Start(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
    }

    public bool Register(string hotkeyStr)
    {
        UnregisterHotKey(_hwnd, HotkeyId);
        if (!TryParse(hotkeyStr, out var mod, out var vk)) return false;
        return RegisterHotKey(_hwnd, HotkeyId, mod, vk);
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, HotkeyId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static bool TryParse(string hotkey, out uint mod, out uint vk)
    {
        mod = 0; vk = 0;
        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": mod |= MOD_CONTROL; break;
                case "ALT": mod |= MOD_ALT; break;
                case "SHIFT": mod |= MOD_SHIFT; break;
                case "WIN": mod |= MOD_WIN; break;
                default:
                    if (Enum.TryParse<System.Windows.Input.Key>(part, true, out var key))
                        vk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
                    else
                        return false;
                    break;
            }
        }
        return vk != 0;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(WndProc);
    }
}
