namespace ClipboardPro.Models;

public sealed class ClipItem
{
    public long Id { get; set; }
    public ClipType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Html { get; set; }
    public string? Rtf { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? Metadata { get; set; }
    public string? SourceProcess { get; set; }
    public string? SourcePath { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
    public int UseCount { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public bool IsImage => Type == ClipType.Image;
    public string? Collection { get; set; }
    public string Hash { get; set; } = string.Empty;
    public bool IsAvailable => Type != ClipType.Files || Content.Split('\n').Any(File.Exists);
    public string Title => Type switch
    {
        ClipType.Url => Uri.TryCreate(Content, UriKind.Absolute, out var uri) ? uri.Host : Content,
        ClipType.Files => Content.Split('\n').Length == 1 ? Path.GetFileName(Content) : $"{Content.Split('\n').Length} archivos",
        ClipType.Image => "Imagen",
        ClipType.Code => "Código",
        ClipType.Color => Content,
        _ => FirstLine(Content)
    };
    public string Preview => Type switch
    {
        ClipType.Files => IsAvailable ? Content.Split('\n').Length == 1 ? Content : string.Join(" · ", Content.Split('\n').Select(Path.GetFileName).Take(3)) : "Archivo no disponible",
        ClipType.Image => Metadata ?? "Imagen copiada",
        _ => Content.Replace("\r", " ").Replace("\n", "  ")
    };
    public string RelativeTime
    {
        get
        {
            var age = DateTime.UtcNow - CreatedUtc;
            if (age.TotalMinutes < 1) return "ahora";
            if (age.TotalHours < 1) return $"hace {(int)age.TotalMinutes} min";
            if (age.TotalDays < 1) return $"hace {(int)age.TotalHours} h";
            if (age.TotalDays < 2) return "ayer";
            return CreatedUtc.ToLocalTime().ToString("d MMM", new System.Globalization.CultureInfo("es-ES"));
        }
    }
    public string TypeLabel => Type switch { ClipType.Url => "ENLACE", ClipType.Files => "ARCHIVO", ClipType.Image => "IMAGEN", ClipType.Code => "CÓDIGO", ClipType.Color => "COLOR", _ => "TEXTO" };
    private static string FirstLine(string text) => text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault()?.Trim() ?? string.Empty;
}
