namespace ClipboardPro;

/// <summary>Single source for product-facing identifiers and user-data locations.</summary>
public static class Branding
{
    public const string AppName = "Clipboard Pro";
    public const string ProductName = "Clipboard Pro";
    public const string ExecutableName = "ClipboardPro.exe";
    public const string CompanyName = "Clipboard Pro";
    public const string Version = "1.0.3";
    public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyName, "ClipboardPro");
    public static readonly string DatabasePath = Path.Combine(DataDirectory, "clipboard.db");
    public static readonly string ImageDirectory = Path.Combine(DataDirectory, "images");
    public static readonly string LogDirectory = Path.Combine(DataDirectory, "logs");
    public static readonly string SettingsPath = Path.Combine(DataDirectory, "settings.json");
}
