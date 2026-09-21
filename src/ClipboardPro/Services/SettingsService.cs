using System.Text.Json;
using ClipboardPro.Models;

namespace ClipboardPro.Services;

public sealed class SettingsService
{
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    public AppSettings Current { get; private set; } = new();
    public async Task LoadAsync()
    {
        Directory.CreateDirectory(Branding.DataDirectory);
        if (!File.Exists(Branding.SettingsPath)) { await SaveAsync(); return; }
        try { Current = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(Branding.SettingsPath)) ?? new(); }
        catch { Current = new(); }
    }
    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(Branding.SettingsPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { _saveLock.Release(); }
    }
}
