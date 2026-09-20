using System.Net.Http;
using System.Text.Json;

namespace ClipboardPro.Services;

/// <summary>Checks the public GitHub Releases endpoint at most once per day. It never uploads user data or installs silently.</summary>
public sealed class UpdateService
{
    private const string Repository = "Enric37/Clipboard-Pro";
    private readonly SettingsService _settings;
    public UpdateRelease? AvailableRelease { get; private set; }
    public event EventHandler<UpdateRelease>? UpdateAvailable;
    public UpdateService(SettingsService settings) => _settings=settings;

    public async Task<UpdateRelease?> CheckAsync(bool manual = false, CancellationToken cancellationToken = default)
    {
        if (!_settings.Current.CheckForUpdates) return null;
        if (!manual && _settings.Current.LastUpdateCheckUtc is DateTime last && DateTime.UtcNow - last < TimeSpan.FromHours(24)) return AvailableRelease;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"ClipboardPro/{Branding.Version}");
            using var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement; var tag=root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            _settings.Current.LastUpdateCheckUtc=DateTime.UtcNow; await _settings.SaveAsync();
            if (!Version.TryParse(tag, out var newest) || newest <= new Version(Branding.Version)) return null;
            var asset=root.GetProperty("assets").EnumerateArray().FirstOrDefault(x => x.GetProperty("name").GetString()=="ClipboardPro-Setup.exe");
            if (asset.ValueKind == JsonValueKind.Undefined) return null;
            var release=new UpdateRelease(tag, asset.GetProperty("browser_download_url").GetString()!, root.TryGetProperty("html_url",out var page)?page.GetString()!:null, asset.TryGetProperty("digest",out var digest)?digest.GetString():null);
            AvailableRelease=release; UpdateAvailable?.Invoke(this,release); return release;
        }
        catch { return null; } // Offline is normal; updates must never affect clipboard operation.
    }
}

public sealed record UpdateRelease(string Version, string DownloadUrl, string? ReleasePageUrl, string? Sha256Digest);
