namespace ClipboardPro.Services;

/// <summary>Metadata-only local logging: clipboard payloads are never logged.</summary>
public sealed class LogService
{
    private readonly object _gate = new();
    public void Write(string eventName, Exception? error = null)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Branding.LogDirectory);
                var path = Path.Combine(Branding.LogDirectory, "clipboardpro.log");
                if (File.Exists(path) && new FileInfo(path).Length > 5 * 1024 * 1024)
                {
                    var old = Path.Combine(Branding.LogDirectory, $"clipboardpro-{DateTime.UtcNow:yyyyMMddHHmmss}.log");
                    File.Move(path, old);
                    foreach (var f in Directory.EnumerateFiles(Branding.LogDirectory, "clipboardpro-*.log").OrderByDescending(File.GetCreationTimeUtc).Skip(4)) File.Delete(f);
                }
                File.AppendAllText(path, $"{DateTime.UtcNow:O} {eventName}{(error is null ? "" : " | " + error.GetType().Name + ": " + error.Message)}{Environment.NewLine}");
            }
        }
        catch { /* Logging must never compromise clipboard operation. */ }
    }
}
