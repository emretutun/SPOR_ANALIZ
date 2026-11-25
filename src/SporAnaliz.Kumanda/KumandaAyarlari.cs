using System.Globalization;
using System.Text.Json;
using SporAnaliz.Data;

namespace SporAnaliz.Kumanda;

/// <summary>Programın yanındaki appsettings.json: veritabanı bağlantısı ve Viz Engine adresi.</summary>
public sealed class KumandaAyarlari
{
    public required DataOptions Veri { get; init; }
    public string VizHost { get; init; } = "127.0.0.1";
    public int VizPort { get; init; } = 6100;
    /// <summary>Konumların Viz'e saniyede kaç kez gönderileceği (sahne aradaki kareleri kendisi doldurur).</summary>
    public int GonderimHz { get; init; } = 10;

    public static string Yol => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static KumandaAyarlari Yukle()
    {
        if (!File.Exists(Yol))
            throw new FileNotFoundException("appsettings.json bulunamadı. appsettings.example.json dosyasını appsettings.json adıyla kopyalayıp doldurun.", Yol);
        var veri = DataOptions.FromJsonFile(Yol);
        using var doc = JsonDocument.Parse(File.ReadAllText(Yol), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        string host = "127.0.0.1";
        int port = 6100, hz = 10;
        if (doc.RootElement.TryGetProperty("Viz", out var viz) && viz.ValueKind == JsonValueKind.Object)
        {
            if (viz.TryGetProperty("Host", out var h) && h.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(h.GetString())) host = h.GetString()!;
            if (viz.TryGetProperty("Port", out var p) && p.TryGetInt32(out var pi)) port = pi;
            if (viz.TryGetProperty("GonderimHz", out var g) && g.TryGetInt32(out var gi)) hz = Math.Clamp(gi, 2, 30);
        }
        return new KumandaAyarlari { Veri = veri, VizHost = host, VizPort = port, GonderimHz = hz };
    }
}
