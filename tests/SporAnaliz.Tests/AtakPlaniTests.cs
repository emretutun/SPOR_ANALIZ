using SporAnaliz.Core.Playback;

namespace SporAnaliz.Tests;

public class AtakPlaniTests
{
    static AtakDokunus D(double x, double y, int no) => new(x, y, no * 100, no, "Oyuncu " + no);

    // 14 kendi yarısında alır, sürer, 23'e verir, 23 10'a verir, 10 sürüp atar
    static readonly AtakDokunus[] Ornek =
    {
        D(0.30, 0.60, 14), D(0.42, 0.55, 14), D(0.60, 0.30, 23), D(0.80, 0.45, 10), D(0.88, 0.50, 10),
    };

    [Fact]
    public void Ayni_oyuncu_surus_farkli_oyuncu_pas_sonda_sut()
    {
        var p = AtakPlani.Kur(Ornek);
        Assert.Equal(new[] { AtakCizgiTuru.Surus, AtakCizgiTuru.Pas, AtakCizgiTuru.Pas, AtakCizgiTuru.Surus, AtakCizgiTuru.Sut },
            p.Cizgiler.Select(c => c.Tur));
        Assert.Equal(2, p.PasSayisi);
        Assert.Equal(new[] { 14, 23, 10 }, p.Oyuncular.Select(o => o.Numara));
        Assert.Equal(10, p.Golcu!.Numara);
    }

    [Fact]
    public void Forma_ilk_dokunusta_ve_pasla_gelen_dokunusta_cikar()
    {
        var p = AtakPlani.Kur(Ornek);
        Assert.Equal(new[] { true, false, true, true, false }, p.Noktalar.Select(n => n.Forma));
    }

    [Fact]
    public void Zamanlama_sirali_ve_top_kalede_biter()
    {
        var p = AtakPlani.Kur(Ornek);
        for (int i = 1; i < p.Cizgiler.Count; i++)
            Assert.True(p.Cizgiler[i].T0 >= p.Cizgiler[i - 1].T0 + p.Cizgiler[i - 1].Sure);
        Assert.True(p.PanelT0 > p.Cizgiler[^1].T0 + p.Cizgiler[^1].Sure);
        Assert.InRange(p.Toplam, 3, 10);

        Assert.Equal(new Vec(0.30, 0.60), p.TopYeri(0));
        var son = p.TopYeri(p.Toplam);
        Assert.Equal(1.0, son.X, 6);
        Assert.InRange(son.Y, 0.455, 0.545);
    }

    [Fact]
    public void Tek_dokunus_dogrudan_sut()
    {
        var p = AtakPlani.Kur(new[] { D(0.85, 0.4, 9) });
        Assert.Single(p.Cizgiler);
        Assert.Equal(AtakCizgiTuru.Sut, p.Cizgiler[0].Tur);
        Assert.Equal(0, p.PasSayisi);
    }

    [Fact]
    public void Bos_atak_bos_plan()
    {
        var p = AtakPlani.Kur(Array.Empty<AtakDokunus>());
        Assert.Empty(p.Cizgiler);
        Assert.Equal(0, p.Toplam);
    }
}
