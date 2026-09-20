using System.Windows.Interop;
using ClipboardPro.Interop;

namespace ClipboardPro.Services;

public sealed class HotkeyService : IDisposable
{
    private const int OpenId = 0x434250; private HwndSource? _source;
    public event EventHandler? Pressed;
    public void Start()
    {
        var p = new HwndSourceParameters("ClipboardPro.MessageWindow") { Width=0, Height=0, WindowStyle=0x800000, ParentWindow=IntPtr.Zero };
        _source = new HwndSource(p); _source.AddHook(WndProc);
        // Win+V is intentionally never registered: it belongs to Windows clipboard history.
        if (!NativeMethods.RegisterHotKey(_source.Handle, OpenId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, NativeMethods.VK_V)) throw new InvalidOperationException("Ctrl+Shift+V ya está asignado a otra aplicación.");
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) { if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == OpenId) { handled=true; Pressed?.Invoke(this, EventArgs.Empty); } return IntPtr.Zero; }
    public void Dispose() { if (_source is null) return; NativeMethods.UnregisterHotKey(_source.Handle, OpenId); _source.RemoveHook(WndProc); _source.Dispose(); }
}
