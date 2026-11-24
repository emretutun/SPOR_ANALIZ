using System.Text.Json;

namespace SporAnaliz.Data;

/// <summary>Veri katmanının ayarları (appsettings.json içindeki ConnectionStrings:MySql ve Data bölümü).</summary>
public sealed record DataOptions
{
    public string ConnectionString { get; init; } = "";
    /// <summary>"feed": yerel maç veritabanı yalnızca okunur. "test": üretilmiş varsayımsal veri.</summary>
    public string Source { get; init; } = "feed";
    /// <summary>Maç veritabanının adı (Source = "feed" iken zorunlu).</summary>
    public string Database { get; init; } = "";

    public bool UseFeed => !string.Equals(Source, "test", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ayarları doğrudan bir appsettings.json dosyasından okur (komut satırı araçları için).</summary>
    public static DataOptions FromJsonFile(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = doc.RootElement;
        string? Get(string section, string key) =>
            root.TryGetProperty(section, out var s) && s.ValueKind == JsonValueKind.Object && s.TryGetProperty(key, out var v)
                ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()
                : null;
        return Create(Get("ConnectionStrings", "MySql"), Get("Data", "Source"), Get("Data", "Database"));
    }

    public static DataOptions Create(string? connectionString, string? source, string? database) => new()
    {
        ConnectionString = connectionString ?? "",
        Source = string.IsNullOrWhiteSpace(source) ? "feed" : source,
        Database = database ?? "",
    };
}
