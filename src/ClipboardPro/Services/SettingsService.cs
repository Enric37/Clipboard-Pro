using System.Text.Json;
using ClipboardPro.Models;

namespace ClipboardPro.Services;

public sealed class SettingsService
{
    public AppSettings Current { get; private set; } = new();
    public async Task LoadAsync()
    {
        Directory.CreateDirectory(Branding.DataDirectory);
        if (!File.Exists(Branding.SettingsPath)) { await SaveAsync(); return; }
        try { Current = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(Branding.SettingsPath)) ?? new(); }
        catch { Current = new(); }
    }
    public Task SaveAsync() => File.WriteAllTextAsync(Branding.SettingsPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
}
