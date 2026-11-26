namespace SporAnaliz.Core.Playback;

/// <summary>
/// Operatörün çizdiği gol atağındaki bir dokunuş. Saha 0..1 arasıdır ve atak her zaman sağdaki kaleye akar:
/// x = 0 sol kale çizgisi, x = 1 sağ kale çizgisi; y = 0 üst kenar, y = 1 alt kenar.
/// </summary>
public sealed record AtakDokunus(double X, double Y, int OyuncuId, int Numara, string Ad);

public enum AtakCizgiTuru { Pas, Surus, Sut }

/// <summary>Atağın bir çizgisi: T0 anında başlar, Sure boyunca baştan sona doğru uzar.</summary>
public sealed record AtakCizgi(AtakCizgiTuru Tur, double T0, double Sure, Vec Bas, Vec Son);

/// <summary>Bir dokunuşun noktası (ve pasla gelen dokunuşta oyuncunun forması) T0 anında belirir.</summary>
public sealed record AtakNokta(double T0, Vec Yer, bool Forma, int Numara, string Ad);

/// <summary>
/// Çizilmiş bir atağı oynatılacak sıraya çevirir: hangi çizgi ne zaman uzar, forma ne zaman belirir, top nerededir.
/// Kumandadaki önizleme ve Viz sahnesi aynı planı oynatır.
/// Kural: ardışık iki dokunuş aynı oyuncudaysa top sürülmüştür (kesikli), farklı oyuncudaysa pastır (düz).
/// Son dokunuştan kaleye şut çizilir.
/// </summary>
public sealed class AtakPlani
{
    public const double SahaBoy = 105, SahaEn = 68;
    public const int EnCokDokunus = 14;

    public List<AtakCizgi> Cizgiler { get; } = new();
    public List<AtakNokta> Noktalar { get; } = new();
    /// <summary>Alt panelin açıldığı an.</summary>
    public double PanelT0 { get; private set; }
    /// <summary>Animasyonun toplam süresi (panel dahil).</summary>
    public double Toplam { get; private set; }
    public int PasSayisi { get; private set; }
    /// <summary>Atakta topa dokunan oyuncular, ilk dokunuş sırasıyla (aynı oyuncu bir kez).</summary>
    public List<AtakDokunus> Oyuncular { get; } = new();
    public AtakDokunus? Golcu { get; private set; }

    public static double Metre(Vec a, Vec b)
    {
        double dx = (b.X - a.X) * SahaBoy, dy = (b.Y - a.Y) * SahaEn;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Şutun bittiği yer: kale çizgisi ortası, son dokunuşun yüksekliğine göre direkler arasında.</summary>
    public static Vec KaleNoktasi(AtakDokunus son) => new(1.0, Math.Clamp(son.Y, 0.455, 0.545));

    public static AtakPlani Kur(IReadOnlyList<AtakDokunus> dokunuslar)
    {
        var p = new AtakPlani();
        if (dokunuslar.Count == 0) return p;
        var d = dokunuslar.Take(EnCokDokunus).ToList();

        double t = 0;
        p.Noktalar.Add(new AtakNokta(t, new Vec(d[0].X, d[0].Y), true, d[0].Numara, d[0].Ad));
        t += 0.45;
        for (int i = 1; i < d.Count; i++)
        {
            var a = new Vec(d[i - 1].X, d[i - 1].Y);
            var b = new Vec(d[i].X, d[i].Y);
            bool surus = d[i].OyuncuId == d[i - 1].OyuncuId && d[i].Numara == d[i - 1].Numara;
            double m = Metre(a, b);
            double sure = surus ? 0.30 + m / 16.0 : 0.25 + m / 40.0;
            sure = Math.Clamp(sure, 0.3, 1.8);
            p.Cizgiler.Add(new AtakCizgi(surus ? AtakCizgiTuru.Surus : AtakCizgiTuru.Pas, t, sure, a, b));
            if (!surus) p.PasSayisi++;
            t += sure;
            p.Noktalar.Add(new AtakNokta(t, b, !surus, d[i].Numara, d[i].Ad));
            t += surus ? 0.1 : 0.22;
        }
        var son = d[^1];
        var sb = new Vec(son.X, son.Y);
        var kale = KaleNoktasi(son);
        double sutSure = Math.Clamp(0.25 + Metre(sb, kale) / 55.0, 0.3, 1.2);
        t += 0.1;
        p.Cizgiler.Add(new AtakCizgi(AtakCizgiTuru.Sut, t, sutSure, sb, kale));
        t += sutSure;
        p.PanelT0 = t + 0.35;
        p.Toplam = p.PanelT0 + 0.5;

        foreach (var x in d)
            if (!p.Oyuncular.Any(o => o.OyuncuId == x.OyuncuId && o.Numara == x.Numara)) p.Oyuncular.Add(x);
        p.Golcu = son;
        return p;
    }

    /// <summary>Bir çizginin t anında ne kadarının çizildiği (0..1).</summary>
    public static double Ilerleme(AtakCizgi c, double t) => Math.Clamp((t - c.T0) / c.Sure, 0, 1);

    /// <summary>Topun t anındaki yeri: en son uzamaya başlamış çizginin ucu.</summary>
    public Vec TopYeri(double t)
    {
        var yer = Noktalar.Count > 0 ? Noktalar[0].Yer : new Vec(0.5, 0.5);
        foreach (var c in Cizgiler)
        {
            double u = Ilerleme(c, t);
            if (t <= c.T0) break;
            yer = new Vec(c.Bas.X + (c.Son.X - c.Bas.X) * u, c.Bas.Y + (c.Son.Y - c.Bas.Y) * u);
        }
        return yer;
    }
}
