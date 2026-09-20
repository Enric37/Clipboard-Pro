using System.Runtime.InteropServices;

namespace ClipboardPro.Interop;

internal static class NativeMethods
{
    public const int WM_CLIPBOARDUPDATE = 0x031D, WM_HOTKEY = 0x0312, WM_QUERYENDSESSION = 0x0011;
    public const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008, MOD_NOREPEAT = 0x4000;
    public const uint VK_V = 0x56;
    [DllImport("user32.dll", SetLastError = true)] public static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] public struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    public const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 2;
    public static void SendPaste()
    {
        var inputs = new[] { new INPUT { type=INPUT_KEYBOARD, U=new() { ki=new() { wVk=0x11 } } }, new INPUT { type=INPUT_KEYBOARD, U=new() { ki=new() { wVk=0x56 } } }, new INPUT { type=INPUT_KEYBOARD, U=new() { ki=new() { wVk=0x56, dwFlags=KEYEVENTF_KEYUP } } }, new INPUT { type=INPUT_KEYBOARD, U=new() { ki=new() { wVk=0x11, dwFlags=KEYEVENTF_KEYUP } } } };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
