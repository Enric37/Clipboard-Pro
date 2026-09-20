namespace ClipboardPro.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string AccentColor { get; set; } = "#7C5CFF";
    public string Hotkey { get; set; } = "Ctrl+Shift+V";
    public bool StartWithWindows { get; set; }
    public bool KeepRunning { get; set; } = true;
    public bool CloseAfterPaste { get; set; } = true;
    public bool PanelPinned { get; set; }
    public string PinnedPanelSize { get; set; } = "Normal";
    public bool SaveImages { get; set; } = true;
    public bool SaveFiles { get; set; } = true;
    public bool SaveFormattedText { get; set; } = true;
    public bool AllowDuplicates { get; set; }
    public bool ProtectSensitiveContent { get; set; } = true;
    public bool CapturePaused { get; set; }
    public DateTime? PausedUntilUtc { get; set; }
    public int HistoryDays { get; set; } = 90;
    public int MaxItems { get; set; } = 10000;
    public int PreviewCacheMb { get; set; } = 80;
    public bool CheckForUpdates { get; set; } = true;
    public DateTime? LastUpdateCheckUtc { get; set; }
    public List<string> ExcludedProcesses { get; set; } = new() { "KeePass", "1Password", "Bitwarden" };
}
