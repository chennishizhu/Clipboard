using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

namespace ClipVault.Services;

public class ClipboardService : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private const int MaxContentBytes = 1024 * 1024; // 1MB

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private static readonly Regex UrlRegex =
        new(@"^https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private HwndSource? _hwndSource;
    private bool _isSelfCopying;

    public bool IsPaused { get; set; }

    public event Action<string, string>? ContentCaptured; // (content, type)

    public void Start(IntPtr windowHandle)
    {
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
        AddClipboardFormatListener(windowHandle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            OnClipboardChanged();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        if (_isSelfCopying || IsPaused) return;

        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText();

            if (string.IsNullOrWhiteSpace(text)) return;
            if (System.Text.Encoding.UTF8.GetByteCount(text) > MaxContentBytes) return;

            var type = UrlRegex.IsMatch(text.Trim()) ? "link" : "text";
            ContentCaptured?.Invoke(text, type);
        }
        catch
        {
            // Clipboard access can fail transiently; ignore silently
        }
    }

    /// <summary>Copy text from within the app without triggering recording.</summary>
    public void CopyWithoutRecording(string text)
    {
        _isSelfCopying = true;
        try
        {
            Clipboard.SetText(text);
        }
        finally
        {
            // Reset after the next clipboard message has been dispatched
            Application.Current.Dispatcher.BeginInvoke(() => _isSelfCopying = false,
                System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    public void Dispose()
    {
        if (_hwndSource != null)
        {
            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
        }
    }
}
