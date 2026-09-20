using Microsoft.Win32;

namespace ClipboardPro.Services;

public static class StartupService
{
    private const string KeyName = "ClipboardPro";
    public static bool IsEnabled()
    {
        using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); return key?.GetValue(KeyName) is string;
    }
    public static void SetEnabled(bool enabled)
    {
        using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)!;
        if (enabled) key.SetValue(KeyName,$"\"{Environment.ProcessPath}\" --background"); else key.DeleteValue(KeyName,false);
    }
}
