using System.Security.Cryptography;
using System.Text;
using ClipboardPro.Models;
using Microsoft.Data.Sqlite;

namespace ClipboardPro.Services;

/// <summary>SQLite repository. Connections are short-lived so background queries never retain UI resources.</summary>
public sealed class ClipDatabase
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder { DataSource = Branding.DatabasePath, Cache = SqliteCacheMode.Shared, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Branding.DataDirectory); Directory.CreateDirectory(Branding.ImageDirectory);
        await using var c = Open(); await c.OpenAsync();
        var sql = """
            PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS clips (
              id INTEGER PRIMARY KEY AUTOINCREMENT, type INTEGER NOT NULL, content TEXT NOT NULL, html TEXT, rtf TEXT,
              image_path TEXT, thumbnail_path TEXT, metadata TEXT, source_process TEXT, source_path TEXT,
              created_utc TEXT NOT NULL, last_used_utc TEXT NOT NULL, use_count INTEGER NOT NULL DEFAULT 0,
              favorite INTEGER NOT NULL DEFAULT 0, pinned INTEGER NOT NULL DEFAULT 0, collection TEXT, hash TEXT NOT NULL);
            DROP INDEX IF EXISTS ix_clips_hash_unique;
            CREATE INDEX IF NOT EXISTS ix_clips_hash ON clips(hash);
            CREATE INDEX IF NOT EXISTS ix_clips_recent ON clips(pinned DESC, favorite DESC, created_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_clips_type ON clips(type, created_utc DESC);
            CREATE VIRTUAL TABLE IF NOT EXISTS clips_fts USING fts5(content, source_process, collection, content='clips', content_rowid='id');
            CREATE TRIGGER IF NOT EXISTS clips_ai AFTER INSERT ON clips BEGIN INSERT INTO clips_fts(rowid,content,source_process,collection) VALUES(new.id,new.content,coalesce(new.source_process,''),coalesce(new.collection,'')); END;
            CREATE TRIGGER IF NOT EXISTS clips_ad AFTER DELETE ON clips BEGIN INSERT INTO clips_fts(clips_fts,rowid,content,source_process,collection) VALUES('delete',old.id,old.content,coalesce(old.source_process,''),coalesce(old.collection,'')); END;
            CREATE TRIGGER IF NOT EXISTS clips_au AFTER UPDATE OF content,source_process,collection ON clips BEGIN INSERT INTO clips_fts(clips_fts,rowid,content,source_process,collection) VALUES('delete',old.id,old.content,coalesce(old.source_process,''),coalesce(old.collection,'')); INSERT INTO clips_fts(rowid,content,source_process,collection) VALUES(new.id,new.content,coalesce(new.source_process,''),coalesce(new.collection,'')); END;
            """;
        await ExecuteAsync(c, sql);
        await using var version=c.CreateCommand(); version.CommandText="PRAGMA user_version"; var currentVersion=Convert.ToInt32(await version.ExecuteScalarAsync() ?? 0);
        if(currentVersion<2) { await ExecuteAsync(c,"INSERT INTO clips_fts(clips_fts) VALUES('rebuild'); PRAGMA user_version=2;"); }
    }
    public static string HashFor(ClipType type, string content, string? secondary = null)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{(int)type}|{content}|{secondary}"));
        return Convert.ToHexString(bytes);
    }
    public async Task<long> UpsertAsync(ClipItem clip, bool allowDuplicates)
    {
        await using var c = Open(); await c.OpenAsync();
        if (!allowDuplicates)
        {
            await using var existing = c.CreateCommand();
            existing.CommandText = "SELECT id FROM clips WHERE hash=$hash LIMIT 1"; existing.Parameters.AddWithValue("$hash", clip.Hash);
            var found = await existing.ExecuteScalarAsync();
            if (found is long id)
            {
                await using var update = c.CreateCommand(); update.CommandText = "UPDATE clips SET created_utc=$now, last_used_utc=$now WHERE id=$id"; update.Parameters.AddWithValue("$id", id); update.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O")); await update.ExecuteNonQueryAsync(); return id;
            }
        }
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO clips(type,content,html,rtf,image_path,thumbnail_path,metadata,source_process,source_path,created_utc,last_used_utc,use_count,favorite,pinned,collection,hash) VALUES($type,$content,$html,$rtf,$image,$thumb,$metadata,$process,$source,$created,$used,0,0,0,$collection,$hash); SELECT last_insert_rowid();";
        Bind(cmd, clip); return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
    }
    public async Task<IReadOnlyList<ClipItem>> SearchAsync(string? query, ClipType? type, bool favorite, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        await using var c = Open(); await c.OpenAsync(ct); await using var cmd = c.CreateCommand();
        var where = new List<string>();
        if (type is not null) { where.Add("c.type=$type"); cmd.Parameters.AddWithValue("$type", (int)type); }
        if (favorite) where.Add("c.favorite=1");
        if (!string.IsNullOrWhiteSpace(query))
        {
            var words=query.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(word=>new string(word.Where(char.IsLetterOrDigit).ToArray())).Where(word=>word.Length>0).ToArray();
            var like="%"+query.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";
            if(words.Length==0) where.Add("(c.content LIKE $like ESCAPE '\\' OR c.source_process LIKE $like ESCAPE '\\')");
            else { var prefixTerms=string.Join(" AND ",words.Select(word=>$"{word}*")); where.Add("(c.id IN (SELECT rowid FROM clips_fts WHERE clips_fts MATCH $query) OR c.content LIKE $like ESCAPE '\\' OR c.source_process LIKE $like ESCAPE '\\')"); cmd.Parameters.AddWithValue("$query",prefixTerms); }
            cmd.Parameters.AddWithValue("$like",like);
        }
        cmd.CommandText = $"SELECT c.* FROM clips c {(where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "")} ORDER BY c.pinned DESC,c.favorite DESC,c.created_utc DESC LIMIT $take OFFSET $skip";
        cmd.Parameters.AddWithValue("$take", take); cmd.Parameters.AddWithValue("$skip", skip);
        var list = new List<ClipItem>(); await using var reader = await cmd.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) list.Add(Read(reader)); return list;
    }
    public async Task SetFlagAsync(long id, string column, bool enabled)
    {
        if (column is not ("favorite" or "pinned")) throw new ArgumentException("Unsupported field", nameof(column));
        await using var c = Open(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = $"UPDATE clips SET {column}=$value WHERE id=$id"; cmd.Parameters.AddWithValue("$value", enabled ? 1 : 0); cmd.Parameters.AddWithValue("$id", id); await cmd.ExecuteNonQueryAsync();
    }
    public async Task UpdateContentAsync(long id, string content)
    { await using var c = Open(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "UPDATE clips SET content=$content, hash=$hash WHERE id=$id"; cmd.Parameters.AddWithValue("$content", content); cmd.Parameters.AddWithValue("$hash", HashFor(ClipType.Text, content)); cmd.Parameters.AddWithValue("$id", id); await cmd.ExecuteNonQueryAsync(); }
    public async Task MarkUsedAsync(long id) { await using var c = Open(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "UPDATE clips SET use_count=use_count+1,last_used_utc=$now WHERE id=$id"; cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O")); await cmd.ExecuteNonQueryAsync(); }
    public Task DeleteAsync(long id) => DeleteWhereAsync("id=$id", cmd => cmd.Parameters.AddWithValue("$id", id));
    public async Task CleanupAsync(AppSettings settings)
    {
        await using var c = Open(); await c.OpenAsync(); await using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM clips WHERE favorite=0 AND pinned=0 AND (created_utc < $date OR id NOT IN (SELECT id FROM clips ORDER BY pinned DESC,favorite DESC,created_utc DESC LIMIT $max))";
        cmd.Parameters.AddWithValue("$date", DateTime.UtcNow.AddDays(-settings.HistoryDays).ToString("O")); cmd.Parameters.AddWithValue("$max", settings.MaxItems); await cmd.ExecuteNonQueryAsync();
    }
    public Task ClearAsync(DateTime? newerThan = null, bool preserveFavorites = true)
    {
        var where=(newerThan is null ? "1=1" : "created_utc >= $date") + (preserveFavorites ? " AND favorite=0" : "");
        return DeleteWhereAsync(where, cmd => { if (newerThan is not null) cmd.Parameters.AddWithValue("$date", newerThan.Value.ToString("O")); });
    }
    public Task ClearFavoritesAsync() => DeleteWhereAsync("favorite=1", _ => { });
    private async Task DeleteWhereAsync(string where, Action<SqliteCommand> bind)
    {
        var paths=new List<string>();
        await using (var c=Open())
        {
            await c.OpenAsync();
            await using (var select=c.CreateCommand())
            {
                select.CommandText=$"SELECT image_path,thumbnail_path FROM clips WHERE {where}"; bind(select);
                await using var reader=await select.ExecuteReaderAsync();
                while(await reader.ReadAsync()) { if(!reader.IsDBNull(0)) paths.Add(reader.GetString(0)); if(!reader.IsDBNull(1)) paths.Add(reader.GetString(1)); }
            }
            await using var delete=c.CreateCommand(); delete.CommandText=$"DELETE FROM clips WHERE {where}"; bind(delete); await delete.ExecuteNonQueryAsync();
        }
        DeleteStoredFiles(paths); await DeleteOrphanedImageFilesAsync();
    }
    private static void DeleteStoredFiles(IEnumerable<string> paths)
    {
        var root=Path.GetFullPath(Branding.ImageDirectory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        foreach(var path in paths.Distinct(StringComparer.OrdinalIgnoreCase)) try { var full=Path.GetFullPath(path); if(full.StartsWith(root,StringComparison.OrdinalIgnoreCase) && File.Exists(full)) File.Delete(full); } catch { }
    }
    private async Task DeleteOrphanedImageFilesAsync()
    {
        if(!Directory.Exists(Branding.ImageDirectory)) return;
        var referenced=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using(var c=Open())
        {
            await c.OpenAsync(); await using var cmd=c.CreateCommand(); cmd.CommandText="SELECT image_path,thumbnail_path FROM clips WHERE image_path IS NOT NULL OR thumbnail_path IS NOT NULL";
            await using var reader=await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync()) { if(!reader.IsDBNull(0)) referenced.Add(Path.GetFullPath(reader.GetString(0))); if(!reader.IsDBNull(1)) referenced.Add(Path.GetFullPath(reader.GetString(1))); }
        }
        var files=Directory.EnumerateFiles(Branding.ImageDirectory,"*.png",SearchOption.TopDirectoryOnly).Where(path=>!referenced.Contains(Path.GetFullPath(path))).ToArray(); DeleteStoredFiles(files);
    }
    private SqliteConnection Open() => new(_connectionString);
    private static async Task ExecuteAsync(SqliteConnection connection, string sql) { await using var cmd = connection.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync(); }
    private static void Bind(SqliteCommand c, ClipItem x) { c.Parameters.AddWithValue("$type", (int)x.Type); c.Parameters.AddWithValue("$content", x.Content); c.Parameters.AddWithValue("$html", (object?)x.Html ?? DBNull.Value); c.Parameters.AddWithValue("$rtf", (object?)x.Rtf ?? DBNull.Value); c.Parameters.AddWithValue("$image", (object?)x.ImagePath ?? DBNull.Value); c.Parameters.AddWithValue("$thumb", (object?)x.ThumbnailPath ?? DBNull.Value); c.Parameters.AddWithValue("$metadata", (object?)x.Metadata ?? DBNull.Value); c.Parameters.AddWithValue("$process", (object?)x.SourceProcess ?? DBNull.Value); c.Parameters.AddWithValue("$source", (object?)x.SourcePath ?? DBNull.Value); c.Parameters.AddWithValue("$created", x.CreatedUtc.ToString("O")); c.Parameters.AddWithValue("$used", x.LastUsedUtc.ToString("O")); c.Parameters.AddWithValue("$collection", (object?)x.Collection ?? DBNull.Value); c.Parameters.AddWithValue("$hash", x.Hash); }
    private static ClipItem Read(SqliteDataReader r) => new() { Id=r.GetInt64(0), Type=(ClipType)r.GetInt32(1), Content=r.GetString(2), Html=r.IsDBNull(3)?null:r.GetString(3), Rtf=r.IsDBNull(4)?null:r.GetString(4), ImagePath=r.IsDBNull(5)?null:r.GetString(5), ThumbnailPath=r.IsDBNull(6)?null:r.GetString(6), Metadata=r.IsDBNull(7)?null:r.GetString(7), SourceProcess=r.IsDBNull(8)?null:r.GetString(8), SourcePath=r.IsDBNull(9)?null:r.GetString(9), CreatedUtc=DateTime.Parse(r.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind), LastUsedUtc=DateTime.Parse(r.GetString(11), null, System.Globalization.DateTimeStyles.RoundtripKind), UseCount=r.GetInt32(12), IsFavorite=r.GetInt32(13)!=0, IsPinned=r.GetInt32(14)!=0, Collection=r.IsDBNull(15)?null:r.GetString(15), Hash=r.GetString(16) };
}
