using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClipboardPro.Interop;
using ClipboardPro.Models;

namespace ClipboardPro.Services;

/// <summary>Event-driven Windows clipboard listener. It performs bounded retries only when Windows temporarily owns the clipboard.</summary>
public sealed class ClipboardCaptureService : IDisposable
{
    private readonly ClipDatabase _database; private readonly SettingsService _settings; private readonly LogService _log; private HwndSource? _source; private int _ignoreUpdates;
    public event EventHandler? ClipStored;
    public ClipboardCaptureService(ClipDatabase database, SettingsService settings, LogService log) { _database=database; _settings=settings; _log=log; }
    public void Start()
    {
        var p = new HwndSourceParameters("ClipboardPro.ClipboardListener") { Width=0, Height=0, WindowStyle=0x800000, ParentWindow=IntPtr.Zero };
        _source=new HwndSource(p); _source.AddHook(WndProc); if (!NativeMethods.AddClipboardFormatListener(_source.Handle)) throw new InvalidOperationException("No se pudo registrar el listener del portapapeles.");
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE) { _ = HandleChangedAsync(); }
        return IntPtr.Zero;
    }
    private async Task HandleChangedAsync()
    {
        if (Interlocked.Exchange(ref _ignoreUpdates, 0) == 1 || IsPaused()) return;
        for (var attempt=0; attempt<3; attempt++)
        {
            try
            {
                var clip = await Application.Current.Dispatcher.InvokeAsync(ReadClipboard); if (clip is null) return;
                await _database.UpsertAsync(clip, _settings.Current.AllowDuplicates); ClipStored?.Invoke(this, EventArgs.Empty); return;
            }
            catch (Exception ex) when (attempt < 2) { await Task.Delay(25 * (attempt + 1)); if (attempt==1) _log.Write("clipboard-read-retry", ex); }
            catch (Exception ex) { _log.Write("clipboard-read-failed", ex); }
        }
    }
    private bool IsPaused() => _settings.Current.CapturePaused || (_settings.Current.PausedUntilUtc is DateTime until && until > DateTime.UtcNow);
    private ClipItem? ReadClipboard()
    {
        var data = Clipboard.GetDataObject(); if (data is null) return null; var source = ForegroundProcess();
        if (_settings.Current.ExcludedProcesses.Any(p => string.Equals(p, source, StringComparison.OrdinalIgnoreCase))) return null;
        var now = DateTime.UtcNow;
        if (_settings.Current.SaveFiles && data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = (string[]?)data.GetData(DataFormats.FileDrop); if (paths is { Length: > 0 }) return New(ClipType.Files, string.Join("\n", paths), source, now, string.Join("\n", paths.Select(p => File.Exists(p) ? new FileInfo(p).Length.ToString() : "missing")));
        }
        if (_settings.Current.SaveImages && data.GetDataPresent(DataFormats.Bitmap) && data.GetData(DataFormats.Bitmap) is BitmapSource bitmap)
        {
            var id = Guid.NewGuid().ToString("N"); var full=Path.Combine(Branding.ImageDirectory, id+".png"); var thumb=Path.Combine(Branding.ImageDirectory, id+".thumb.png");
            SavePng(bitmap, full); var ratio=Math.Min(1d, 480d/Math.Max(bitmap.PixelWidth, bitmap.PixelHeight)); SavePng(new TransformedBitmap(bitmap, new ScaleTransform(ratio, ratio)), thumb);
            return new ClipItem { Type=ClipType.Image, Content=id, ImagePath=full, ThumbnailPath=thumb, Metadata=$"{bitmap.PixelWidth} × {bitmap.PixelHeight} px", SourceProcess=source, CreatedUtc=now, LastUsedUtc=now, Hash=HashForImage(full) };
        }
        if (data.GetDataPresent(DataFormats.UnicodeText))
        {
            var text=(data.GetData(DataFormats.UnicodeText) as string)?.Trim(); if (string.IsNullOrWhiteSpace(text)) return null;
            if (_settings.Current.ProtectSensitiveContent && SensitiveContentDetector.LooksSensitive(text)) { _log.Write("sensitive-content-skipped"); return null; }
            var kind=ClassifyText(text); var c=New(kind, text, source, now); if (_settings.Current.SaveFormattedText) { c.Html=data.GetDataPresent(DataFormats.Html) ? data.GetData(DataFormats.Html) as string : null; c.Rtf=data.GetDataPresent(DataFormats.Rtf) ? data.GetData(DataFormats.Rtf) as string : null; } return c;
        }
        return null;
    }
    private static ClipItem New(ClipType type,string content,string? process,DateTime now,string? metadata=null) => new() { Type=type, Content=content, Metadata=metadata, SourceProcess=process, CreatedUtc=now, LastUsedUtc=now, Hash=ClipDatabase.HashFor(type,content) };
    private static ClipType ClassifyText(string x) { if (Uri.TryCreate(x, UriKind.Absolute, out var u) && (u.Scheme=="http" || u.Scheme=="https")) return ClipType.Url; if (System.Text.RegularExpressions.Regex.IsMatch(x, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$|^(rgb|rgba|hsl)a?\\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return ClipType.Color; if (x.Length>30 && (x.Contains('{') || x.Contains("=>") || x.Contains("<") || x.Contains("def ") || x.Contains("SELECT "))) return ClipType.Code; return ClipType.Text; }
    private static string? ForegroundProcess() { try { var h=NativeMethods.GetForegroundWindow(); NativeMethods.GetWindowThreadProcessId(h,out var pid); return Process.GetProcessById((int)pid).ProcessName; } catch { return null; } }
    private static void SavePng(BitmapSource bitmap,string path) { var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var f=File.Create(path); encoder.Save(f); }
    private static string HashForImage(string path) { using var stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    public void PutOnClipboard(ClipItem item)
    {
        Interlocked.Exchange(ref _ignoreUpdates, 1); var data=new DataObject();
        if (item.Type==ClipType.Files) data.SetData(DataFormats.FileDrop,item.Content.Split('\n'));
        else if (item.Type==ClipType.Image && item.ImagePath is not null && File.Exists(item.ImagePath)) data.SetData(DataFormats.Bitmap,new BitmapImage(new Uri(item.ImagePath)));
        else { data.SetData(DataFormats.UnicodeText,item.Content); if (!string.IsNullOrEmpty(item.Html)) data.SetData(DataFormats.Html,item.Html); if (!string.IsNullOrEmpty(item.Rtf)) data.SetData(DataFormats.Rtf,item.Rtf); }
        Clipboard.SetDataObject(data, true);
    }
    public void Dispose() { if (_source is null) return; NativeMethods.RemoveClipboardFormatListener(_source.Handle); _source.RemoveHook(WndProc); _source.Dispose(); }
}
