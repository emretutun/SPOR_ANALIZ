using System.Globalization;
using SporAnaliz.Core.Analysis;
using SporAnaliz.Core.Playback;

namespace SporAnaliz.Kumanda.Viz;

/// <summary>
/// Oynatılan maçı mac_sim sahnesine aktarır. Sahne yalnızca paylaşımlı bellekten (VIZ_COMMUNICATION*MAP) yönetilir;
/// anahtarlar ve biçimleri sahnedeki betikle (viz/MacSim.vbs) birebir aynıdır.
/// </summary>
public sealed class VizYayin(VizBaglanti viz)
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    // Viz yalnızca değer DEĞİŞİNCE sahneye haber verir; son gönderilenleri tutup gereksiz gönderimi önleriz
    readonly Dictionary<string, string> _son = new();
    int _sonSahip = int.MinValue;
    int _sonSlot = -1;
    bool _devreGosterildi;
    string _skor = "0 - 0";
    string _dakika = "0'";
    string _evAd = "", _depAd = "";
    Rgb _ev, _dep;

    public readonly record struct Rgb(int R, int G, int B)
    {
        public override string ToString() => $"{R},{G},{B}";
    }

    void Yaz(string anahtar, string deger, bool zorla = false)
    {
        if (_son.TryGetValue(anahtar, out var onceki) && onceki == deger)
        {
            if (!zorla) return;
            viz.MapYaz(anahtar, "");          // aynı değeri tekrar göstermek için önce boşalt
        }
        viz.MapYaz(anahtar, deger);
        _son[anahtar] = deger;
    }

    /// <summary>Maçı sahneye yükler: kadro, renkler, tabela, başlama dizilişi. Bant ve etiket kapanır.</summary>
    public void Yukle(MatchPlayer p)
    {
        _son.Clear();
        _sonSahip = int.MinValue;
        _sonSlot = -1;
        _devreGosterildi = false;
        var m = p.Analysis.Match;
        _evAd = m.Home.Name.ToUpper(Tr);
        _depAd = m.Away.Name.ToUpper(Tr);
        (_ev, _dep) = TakimRenkleri(m.Home.Color, m.Away.Color);
        _skor = "0 - 0";
        _dakika = "0'";

        Yaz("sim_renk", $"{_ev};{_dep}", zorla: true);
        Yaz("sim_kadro", Kadro(p), zorla: true);
        Yaz("sim_metin", Metin(), zorla: true);
        Yaz("sim_bant", "", zorla: true);
        Yaz("sim_etiket", "", zorla: true);
        Yaz("sim_sahip", "-1", zorla: true);
        _sonSahip = -1;
        Yaz("sim_poz", Konumlar(p), zorla: true);
        Yaz("sim_atak", "", zorla: true);
    }

    /// <summary>Her gönderim anında: oyuncu ve top konumları, topu taşıyan oyuncu, dakika.</summary>
    public void Kare(MatchPlayer p)
    {
        Yaz("sim_poz", Konumlar(p));
        int sahip = p.Ball.Holder >= 0 && p.Ball.Alpha >= 1 && !p.Dots[p.Ball.Holder].Off ? p.Ball.Holder : -1;
        if (sahip != _sonSahip)
        {
            Yaz("sim_sahip", sahip.ToString(Inv));
            _sonSahip = sahip;
        }
        if (p.Slot != _sonSlot)
        {
            _sonSlot = p.Slot;
            _dakika = MatchPlayer.SlotLabel(p.Slot);
            Yaz("sim_metin", Metin());
            if (!_devreGosterildi && p.Slot >= TimelineBuilder.HalfSlot)
            {
                _devreGosterildi = true;
                var m = p.Analysis.Match;
                Yaz("sim_bant", $"DEVRE ARASI|{m.Home.Short}  {p.HomeGoals} - {p.AwayGoals}  {m.Away.Short}|136,147,168|2.2", zorla: true);
            }
        }
    }

    /// <summary>Bir maç olayı ekrana düştüğünde tabela, bant ya da etiket.</summary>
    public void Olay(MatchPlayer p, PlayEvent e)
    {
        var m = p.Analysis.Match;
        bool ev = e.Side == "home";
        var renk = ev ? _ev : _dep;
        string takim = ev ? m.Home.Name : m.Away.Name;
        string dk = MatchPlayer.SlotLabel(e.Slot);
        string kim = string.IsNullOrEmpty(e.Player) ? takim : e.Player!;
        switch (e.Type)
        {
            case "goal":
                _skor = $"{e.HomeGoals} - {e.AwayGoals}";
                Yaz("sim_metin", Metin());
                Yaz("sim_bant", $"GOL!  {e.HomeGoals} - {e.AwayGoals}|{kim}  ·  {dk}|{renk}|3.5", zorla: true);
                break;
            case "shot_on":
                Yaz("sim_etiket", $"{dk}  İSABETLİ ŞUT  ·  {kim}|{renk}|2", zorla: true);
                break;
            case "shot_off":
                Yaz("sim_etiket", $"{dk}  ŞUT DIŞARIDA  ·  {kim}|{renk}|2", zorla: true);
                break;
            case "corner":
                Yaz("sim_etiket", $"{dk}  KORNER  ·  {takim}|{renk}|2", zorla: true);
                break;
            case "yellow":
                Yaz("sim_etiket", $"{dk}  SARI KART  ·  {kim}|242,195,0|2.5", zorla: true);
                break;
            case "red":
                Yaz("sim_bant", $"KIRMIZI KART|{kim}  ·  {takim}  ·  {dk}|224,58,58|2.5", zorla: true);
                break;
            case "sub":
                // Çıkanın dairesi girene geçti: numara ve isimleri yenile
                Yaz("sim_kadro", Kadro(p));
                var giren = p.FindPlayer(e.PlayerId)?.ShortName ?? e.Player ?? "";
                var cikan = p.FindPlayer(e.OutPlayerId)?.ShortName ?? e.OutPlayer ?? "";
                Yaz("sim_etiket", $"{dk}  DEĞİŞİKLİK  ·  GİREN {giren}  ·  ÇIKAN {cikan}|{renk}|2.5", zorla: true);
                break;
        }
    }

    public void MacSonu(MatchPlayer p)
    {
        var m = p.Analysis.Match;
        _dakika = "MS";
        Yaz("sim_metin", Metin());
        Yaz("sim_sahip", "-1");
        _sonSahip = -1;
        Yaz("sim_bant", $"MAÇ SONU  {p.HomeGoals} - {p.AwayGoals}|{m.Home.Name}  –  {m.Away.Name}|227,6,19|0", zorla: true);
    }

    /// <summary>Bant ve etiketi kapatır, isim etiketini gizler.</summary>
    public void Temizle()
    {
        Yaz("sim_bant", "", zorla: true);
        Yaz("sim_etiket", "", zorla: true);
        Yaz("sim_sahip", "-1", zorla: true);
        _sonSahip = -1;
    }

    // ---------------------------------------------------------------- canlı maç: gol atağı

    /// <summary>Canlı maç için sahneyi hazırlar: renkler, tabela; oyuncular ve top gizlenir, bant ve etiket kapanır.</summary>
    public void CanliHazirla(string evAd, string depAd, int skorEv, int skorDep, string dakika, Rgb ev, Rgb dep)
    {
        Yaz("sim_renk", $"{ev};{dep}");
        Yaz("sim_metin", $"EV_AD={evAd.ToUpper(Tr)}|DEP_AD={depAd.ToUpper(Tr)}|SKOR={skorEv} - {skorDep}|DAKIKA={dakika}");
        Yaz("sim_poz", string.Join(";", Enumerable.Repeat("0.5000,0.5000,0", 23)));
        Yaz("sim_sahip", "-1");
        _sonSahip = -1;
        Yaz("sim_bant", "", zorla: true);
        Yaz("sim_etiket", "", zorla: true);
    }

    /// <summary>Çizilmiş atağı sahnede baştan oynatır.</summary>
    public void Atak(AtakPlani plan, Rgb renk, string baslik, string oyuncular) =>
        Yaz("sim_atak", AtakMetni(plan, renk, baslik, oyuncular), zorla: true);

    public void AtakKapat() => Yaz("sim_atak", "", zorla: true);

    /// <summary>Sahnedeki hazır parça sayısı (viz/MacSim.vbs ile aynı).</summary>
    public const int SahneCizgi = 80, SahneNokta = 16, SahneForma = 16;
    /// <summary>Formanın noktanın ne kadar üstünde durduğu (metre); kumandadaki çizimle aynı görünüm.</summary>
    const double FormaUstu = 2.9;

    /// <summary>sim_atak değeri: "r,g,b|başlık|oyuncular|öğeler" (biçim için viz/MacSim.vbs başındaki açıklamaya bakın).</summary>
    public static string AtakMetni(AtakPlani plan, Rgb renk, string baslik, string oyuncular)
    {
        static string F(double v) => v.ToString("0.###", Inv);
        static (double X, double Y) M(Vec v) => ((v.X - 0.5) * AtakPlani.SahaBoy, (0.5 - v.Y) * AtakPlani.SahaEn);

        // Top sürme kesikli çizilir: parça sayısı sahnedeki havuzu aşarsa kesikler uzatılır
        double kesik = 1.6, bosluk = 1.1;
        int duz = plan.Cizgiler.Count(c => c.Tur != AtakCizgiTuru.Surus);
        for (int deneme = 0; deneme < 12; deneme++)
        {
            int gerek = duz + plan.Cizgiler.Where(c => c.Tur == AtakCizgiTuru.Surus)
                .Sum(c => Math.Max(1, (int)Math.Floor((AtakPlani.Metre(c.Bas, c.Son) + bosluk) / (kesik + bosluk))));
            if (gerek <= SahneCizgi) break;
            kesik *= 1.3;
            bosluk *= 1.3;
        }

        var oge = new List<string>();
        foreach (var c in plan.Cizgiler)
        {
            var (ax, ay) = M(c.Bas);
            var (bx, by) = M(c.Son);
            double dx = bx - ax, dy = by - ay, L = Math.Sqrt(dx * dx + dy * dy);
            if (L < 0.05) continue;
            double ux = dx / L, uy = dy / L, aci = Math.Atan2(dy, dx) * 180 / Math.PI;
            if (c.Tur != AtakCizgiTuru.Surus)
            {
                double kal = c.Tur == AtakCizgiTuru.Sut ? 0.45 : 0.38;
                oge.Add($"C,{F(c.T0)},{F(c.Sure)},{F(ax)},{F(ay)},{F(dx)},{F(dy)},{F(L)},{F(aci)},{F(kal)}");
                continue;
            }
            int n = Math.Max(1, (int)Math.Floor((L + bosluk) / (kesik + bosluk)));
            for (int k = 0; k < n; k++)
            {
                double s0 = k * (kesik + bosluk), uz = Math.Min(kesik, L - s0);
                if (uz <= 0.05) break;
                oge.Add($"C,{F(c.T0 + c.Sure * s0 / L)},{F(c.Sure * uz / L)},{F(ax + ux * s0)},{F(ay + uy * s0)},{F(ux * uz)},{F(uy * uz)},{F(uz)},{F(aci)},0.32");
            }
        }
        foreach (var n in plan.Noktalar.Take(SahneNokta))
        {
            var (x, y) = M(n.Yer);
            oge.Add($"N,{F(n.T0)},{F(x)},{F(y)}");
        }
        foreach (var n in plan.Noktalar.Where(n => n.Forma).Take(SahneForma))
        {
            var (x, y) = M(n.Yer);
            oge.Add($"F,{F(n.T0)},{F(x)},{F(y + FormaUstu)},{n.Numara},{n.Ad.Replace(',', ' ').Replace(';', ' ').Replace('|', ' ')}");
        }
        oge.Add($"P,{F(plan.PanelT0)}");
        static string Temiz(string s) => s.Replace('|', '/').Replace(';', ',');
        return $"{renk}|{Temiz(baslik)}|{Temiz(oyuncular)}|{string.Join(";", oge)}";
    }

    string Metin() => $"EV_AD={_evAd}|DEP_AD={_depAd}|SKOR={_skor}|DAKIKA={_dakika}";

    static string Kadro(MatchPlayer p) => string.Join(";", p.Dots.Select(d => d.Number + "|" + d.Name));

    // 22 oyuncu "x,y,g" + top; g 1 = göster, 0 = gizle (kırmızı kart, top oyun dışında).
    // Görünürlük her seferinde açıkça yazılır: sahne yalnızca g gelince görünürlüğü değiştirir,
    // g'siz bir konum gizlenmiş nesneyi geri açmaz (top ilk aut/kornerden sonra kayboluyordu).
    static string Konumlar(MatchPlayer p)
    {
        var parca = new List<string>(23);
        for (int i = 0; i < p.Positions.Length; i++)
        {
            var v = p.Positions[i];
            parca.Add(v.X.ToString("0.0000", Inv) + "," + v.Y.ToString("0.0000", Inv) + (p.Dots[i].Off ? ",0" : ",1"));
        }
        var b = p.Ball;
        parca.Add(b.Pos.X.ToString("0.0000", Inv) + "," + b.Pos.Y.ToString("0.0000", Inv) + (b.Alpha < 0.5 ? ",0" : ",1"));
        return string.Join(";", parca);
    }

    /// <summary>Takım renkleri; iki renk birbirine çok yakınsa deplasman açık maviye geçer.</summary>
    public static (Rgb Ev, Rgb Dep) TakimRenkleri(string evHex, string depHex)
    {
        var ev = Coz(evHex, new Rgb(232, 163, 23));
        var dep = Coz(depHex, new Rgb(122, 0, 60));
        double fark = Math.Abs(ev.R - dep.R) + Math.Abs(ev.G - dep.G) + Math.Abs(ev.B - dep.B);
        if (fark < 90) dep = new Rgb(108, 171, 221);
        return (ev, dep);
    }

    static Rgb Coz(string hex, Rgb varsayilan)
    {
        hex = (hex ?? "").Trim().TrimStart('#');
        if (hex.Length < 6) return varsayilan;
        try
        {
            return new Rgb(Convert.ToInt32(hex[..2], 16), Convert.ToInt32(hex[2..4], 16), Convert.ToInt32(hex[4..6], 16));
        }
        catch { return varsayilan; }
    }
}

public static class VizBaglantiUzanti
{
    /// <summary>Paylaşımlı belleğe değer yazar. Tırnak işareti değerin içinde kullanılamaz; tek tırnağa çevrilir.</summary>
    public static bool MapYaz(this VizBaglanti viz, string anahtar, string deger) =>
        viz.Gonder($"VIZ_COMMUNICATION*MAP SET_STRING_ELEMENT \"{anahtar}\" \"{deger.Replace('"', '\'')}\"");
}
