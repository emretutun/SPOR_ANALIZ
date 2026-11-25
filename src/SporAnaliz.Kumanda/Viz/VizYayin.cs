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
