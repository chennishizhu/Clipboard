using System.IO;
using System.Text.Json;
using ClipVault.Models;
using Microsoft.Data.Sqlite;

namespace ClipVault.Services;

public class DatabaseService : IDisposable
{
    private SqliteConnection? _connection;
    private string _dbPath = string.Empty;

    public void Initialize(string dbPath)
    {
        _dbPath = dbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        // Enable WAL mode for concurrent read/write
        Execute("PRAGMA journal_mode=WAL;");
        Execute("PRAGMA synchronous=NORMAL;");

        Execute("""
            CREATE TABLE IF NOT EXISTS ClipItems (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                content     TEXT    NOT NULL,
                type        TEXT    NOT NULL,
                created_at  TEXT    NOT NULL,
                is_favorite INTEGER NOT NULL DEFAULT 0,
                is_pinned   INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private void Execute(string sql)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Insert(ClipItem item)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipItems (content, type, created_at, is_favorite, is_pinned)
            VALUES ($content, $type, $created_at, 0, 0);
            """;
        cmd.Parameters.AddWithValue("$content", item.Content);
        cmd.Parameters.AddWithValue("$type", item.Type);
        cmd.Parameters.AddWithValue("$created_at", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();

        using var idCmd = _connection!.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        item.Id = (long)idCmd.ExecuteScalar()!;
    }

    public void EnforceLimit(int maxRecords)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            DELETE FROM ClipItems
            WHERE id IN (
                SELECT id FROM ClipItems
                WHERE is_favorite = 0 AND is_pinned = 0
                ORDER BY created_at ASC
                LIMIT MAX(0, (SELECT COUNT(*) FROM ClipItems WHERE is_favorite = 0 AND is_pinned = 0) - $max)
            );
            """;
        cmd.Parameters.AddWithValue("$max", maxRecords);
        cmd.ExecuteNonQuery();
    }

    public List<ClipItem> Query(string? keyword, string? timeFilter, string? typeFilter, bool favoritesOnly = false)
    {
        using var cmd = _connection!.CreateCommand();
        var where = new List<string>();

        if (favoritesOnly) where.Add("is_favorite = 1");

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where.Add("content LIKE $kw");
            cmd.Parameters.AddWithValue("$kw", $"%{keyword}%");
        }

        if (timeFilter == "today")
        {
            where.Add("date(created_at) = date('now','localtime')");
        }
        else if (timeFilter == "7days")
        {
            where.Add("created_at >= datetime('now','-7 days','localtime')");
        }
        else if (timeFilter == "30days")
        {
            where.Add("created_at >= datetime('now','-30 days','localtime')");
        }

        if (typeFilter == "text") where.Add("type = 'text'");
        else if (typeFilter == "link") where.Add("type = 'link'");

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        cmd.CommandText = $"""
            SELECT id, content, type, created_at, is_favorite, is_pinned
            FROM ClipItems
            {whereClause}
            ORDER BY is_pinned DESC, created_at DESC;
            """;

        var list = new List<ClipItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ClipItem
            {
                Id = reader.GetInt64(0),
                Content = reader.GetString(1),
                Type = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                IsFavorite = reader.GetInt32(4) == 1,
                IsPinned = reader.GetInt32(5) == 1
            });
        }
        return list;
    }

    public int GetTotalCount()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ClipItems;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public string? GetLatestContent()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT content FROM ClipItems ORDER BY id DESC LIMIT 1;";
        return cmd.ExecuteScalar() as string;
    }

    public void UpdateFavorite(long id, bool isFavorite)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE ClipItems SET is_favorite = $v WHERE id = $id;";
        cmd.Parameters.AddWithValue("$v", isFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePinned(long id, bool isPinned)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE ClipItems SET is_pinned = $v WHERE id = $id;";
        cmd.Parameters.AddWithValue("$v", isPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipItems WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void ClearAll(bool includeFavorites)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = includeFavorites
            ? "DELETE FROM ClipItems;"
            : "DELETE FROM ClipItems WHERE is_favorite = 0 AND is_pinned = 0;";
        cmd.ExecuteNonQuery();
    }

    public void ExportToJson(string filePath)
    {
        var items = Query(null, null, null);
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public void ImportFromJson(string filePath, bool merge)
    {
        var json = File.ReadAllText(filePath);
        var items = JsonSerializer.Deserialize<List<ClipItem>>(json);
        if (items == null) return;

        if (!merge) ClearAll(includeFavorites: true);

        foreach (var item in items)
            Insert(item);
    }

    public void MigrateDatabase(string newPath)
    {
        _connection?.Close();
        _connection?.Dispose();
        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.Copy(_dbPath, newPath, overwrite: true);
        _dbPath = newPath;
        _connection = new SqliteConnection($"Data Source={newPath}");
        _connection.Open();
        Execute("PRAGMA journal_mode=WAL;");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
