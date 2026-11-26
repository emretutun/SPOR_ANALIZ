using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using MySqlConnector;
using SporAnaliz.Core.Playback;

namespace SporAnaliz.Data.Atak;

/// <summary>Kaydedilmiş bir gol atağı.</summary>
public sealed class GolAtakKaydi
{
    public int Id { get; set; }
    public int MacId { get; set; }
    /// <summary>"home" ya da "away": golü atan takım.</summary>
    public string Takim { get; set; } = "home";
    public string Dakika { get; set; } = "";
    public string Golcu { get; set; } = "";
    public int SkorEv { get; set; }
    public int SkorDep { get; set; }
    public List<AtakDokunus> Dokunuslar { get; set; } = new();
    public DateTime Guncelleme { get; set; }

    public override string ToString() => $"{Dakika}'  ·  {Golcu}  ·  {SkorEv}-{SkorDep}";
}

/// <summary>
/// Operatörün çizdiği gol ataklarını saklar. YALNIZCA bu bilgisayardaki MySQL'e (localhost) ve bağlantı
/// dizesindeki kendi veritabanımıza (htspor_tahmin) yazar; maç veritabanına dokunmaz.
/// Tablo ilk kullanımda kurulur.
/// </summary>
public sealed class AtakDeposu
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    readonly string _cs;
    bool _hazir;

    public AtakDeposu(DataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("appsettings.json içinde ConnectionStrings:MySql tanımlı değil.");
        var b = new MySqlConnectionStringBuilder(options.ConnectionString);
        var host = (b.Server ?? "").Trim().ToLowerInvariant();
        if (host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException($"Gol atakları yalnızca bu bilgisayardaki veritabanına kaydedilir (sunucu: {b.Server}).");
        if (string.IsNullOrWhiteSpace(b.Database) || !Regex.IsMatch(b.Database, "^[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("Bağlantı dizesinde geçerli bir veritabanı adı yok (Database=htspor_tahmin).");
        if (string.Equals(b.Database, options.Database, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gol atakları maç veritabanına yazılmaz; bağlantı dizesinde kendi veritabanınızı (htspor_tahmin) gösterin.");
        _cs = b.ConnectionString;
        Veritabani = b.Database;
    }

    public string Veritabani { get; }

    MySqlConnection Ac()
    {
        var c = new MySqlConnection(_cs);
        c.Open();
        if (!_hazir)
        {
            c.Execute("""
                CREATE TABLE IF NOT EXISTS gol_atak (
                  id INT AUTO_INCREMENT PRIMARY KEY,
                  mac_id INT NOT NULL,
                  takim VARCHAR(4) NOT NULL,
                  dakika VARCHAR(10) NOT NULL,
                  golcu VARCHAR(120) NOT NULL,
                  skor_ev INT NOT NULL,
                  skor_dep INT NOT NULL,
                  dokunuslar TEXT NOT NULL,
                  olusturma DATETIME NOT NULL,
                  guncelleme DATETIME NOT NULL,
                  KEY ix_gol_atak_mac (mac_id)
                ) CHARACTER SET utf8mb4
                """);
            _hazir = true;
        }
        return c;
    }

    public List<GolAtakKaydi> Listele(int macId)
    {
        using var c = Ac();
        return c.Query<Satir>("SELECT id, mac_id AS MacId, takim, dakika, golcu, skor_ev AS SkorEv, skor_dep AS SkorDep, dokunuslar, guncelleme FROM gol_atak WHERE mac_id = @macId ORDER BY id", new { macId })
            .Select(s => s.Kayit()).ToList();
    }

    /// <summary>Yeni kaydı ekler (Id = 0) ya da var olanı günceller; kaydın Id'sini döndürür.</summary>
    public int Kaydet(GolAtakKaydi k)
    {
        using var c = Ac();
        var p = new
        {
            k.Id, k.MacId, k.Takim, k.Dakika, k.Golcu, k.SkorEv, k.SkorDep,
            Dokunuslar = JsonSerializer.Serialize(k.Dokunuslar, Json), Simdi = DateTime.Now,
        };
        if (k.Id > 0)
        {
            int n = c.Execute("UPDATE gol_atak SET takim=@Takim, dakika=@Dakika, golcu=@Golcu, skor_ev=@SkorEv, skor_dep=@SkorDep, dokunuslar=@Dokunuslar, guncelleme=@Simdi WHERE id=@Id", p);
            if (n > 0) return k.Id;
        }
        k.Id = c.ExecuteScalar<int>("INSERT INTO gol_atak (mac_id, takim, dakika, golcu, skor_ev, skor_dep, dokunuslar, olusturma, guncelleme) VALUES (@MacId, @Takim, @Dakika, @Golcu, @SkorEv, @SkorDep, @Dokunuslar, @Simdi, @Simdi); SELECT LAST_INSERT_ID();", p);
        return k.Id;
    }

    public void Sil(int id)
    {
        using var c = Ac();
        c.Execute("DELETE FROM gol_atak WHERE id = @id", new { id });
    }

    sealed class Satir
    {
        public int Id { get; set; }
        public int MacId { get; set; }
        public string Takim { get; set; } = "";
        public string Dakika { get; set; } = "";
        public string Golcu { get; set; } = "";
        public int SkorEv { get; set; }
        public int SkorDep { get; set; }
        public string Dokunuslar { get; set; } = "[]";
        public DateTime Guncelleme { get; set; }

        public GolAtakKaydi Kayit() => new()
        {
            Id = Id, MacId = MacId, Takim = Takim, Dakika = Dakika, Golcu = Golcu, SkorEv = SkorEv, SkorDep = SkorDep,
            Guncelleme = Guncelleme,
            Dokunuslar = JsonSerializer.Deserialize<List<AtakDokunus>>(Dokunuslar, Json) ?? new(),
        };
    }
}
