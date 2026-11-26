using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using SporAnaliz.Core.Playback;

namespace SporAnaliz.Kumanda;

/// <summary>
/// Operatörün gol atağını çizdiği saha. Atak her zaman sağdaki kaleye akar.
///  - Sahaya sol tık: yeni dokunuş. Oyuncusu bir öncekiyle aynı kabul edilir (top sürme); forma düğmesiyle değiştirilir.
///  - Noktayı sürükle: yerini değiştir. Noktaya tık: seç (forma düğmesi artık ona uygulanır).
///  - Sağ tık: tıklanan noktayı, boşluğa tıklandıysa son noktayı siler.
/// </summary>
public sealed class AtakCizimAlani : SahaKontrolu
{
    Color _cizgi = Color.FromArgb(235, 255, 255, 255), _nokta = Color.White, _isim = Color.FromArgb(226, 230, 236),
        _ipucu = Color.FromArgb(226, 230, 236), _yon = Color.FromArgb(150, 158, 170), _secim = Color.FromArgb(214, 180, 100),
        _eksik = Color.FromArgb(222, 120, 120), _top = Color.White;
    float _pas = 2.4f, _surus = 2.4f, _sut = 3f, _noktaCap = 8f, _forma = 0.03f;
    string _yonYazi = "HÜCUM YÖNÜ  →";
    string _ipucuYazi = VarsayilanIpucu;
    const string VarsayilanIpucu =
        "Atağın başladığı yere tıklayın, sonra sağdan oyuncuyu seçin.\n" +
        "Pas: yeni yere tıklayıp oyuncuyu seçin.   Top sürme: sadece tıklayın.\n" +
        "Son nokta golü atan oyuncudur; kaleye şut kendiliğinden çizilir.\n" +
        "Sağ tık: noktayı siler.   Noktayı sürükleyerek yerini değiştirebilirsiniz.";

    [Category("Atak çizgileri"), Description("Pas, top sürme ve şut çizgilerinin rengi"), DefaultValue(typeof(Color), "235, 255, 255, 255")]
    public Color AtakCizgiRengi { get => _cizgi; set { _cizgi = value; Invalidate(); } }

    [Category("Atak çizgileri"), Description("Pas çizgisinin kalınlığı (piksel)"), DefaultValue(2.4f)]
    public float PasKalinligi { get => _pas; set { _pas = Math.Max(0.5f, value); Invalidate(); } }

    [Category("Atak çizgileri"), Description("Top sürme (kesikli) çizgisinin kalınlığı (piksel)"), DefaultValue(2.4f)]
    public float SurusKalinligi { get => _surus; set { _surus = Math.Max(0.5f, value); Invalidate(); } }

    [Category("Atak çizgileri"), Description("Şut çizgisinin kalınlığı (piksel)"), DefaultValue(3f)]
    public float SutKalinligi { get => _sut; set { _sut = Math.Max(0.5f, value); Invalidate(); } }

    [Category("Atak çizgileri"), Description("Dokunuş noktalarının rengi"), DefaultValue(typeof(Color), "White")]
    public Color NoktaRengi { get => _nokta; set { _nokta = value; Invalidate(); } }

    [Category("Atak çizgileri"), Description("Dokunuş noktasının çapı (piksel)"), DefaultValue(8f)]
    public float NoktaCapi { get => _noktaCap; set { _noktaCap = Math.Max(2f, value); Invalidate(); } }

    [Category("Atak çizgileri"), Description("Önizlemedeki top"), DefaultValue(typeof(Color), "White")]
    public Color TopRengi { get => _top; set { _top = value; Invalidate(); } }

    [Category("Formalar"), Description("Forma dairesinin saha yüksekliğine oranı"), DefaultValue(0.03f)]
    public float FormaOrani { get => _forma; set { _forma = Math.Clamp(value, 0.01f, 0.08f); Invalidate(); } }

    [Category("Formalar"), Description("Formanın üstündeki oyuncu adı"), DefaultValue(typeof(Color), "226, 230, 236")]
    public Color IsimRengi { get => _isim; set { _isim = value; Invalidate(); } }

    [Category("Formalar"), Description("Seçili noktanın halkası"), DefaultValue(typeof(Color), "214, 180, 100")]
    public Color SecimRengi { get => _secim; set { _secim = value; Invalidate(); } }

    [Category("Formalar"), Description("Oyuncusu seçilmemiş nokta ('?')"), DefaultValue(typeof(Color), "222, 120, 120")]
    public Color EksikRengi { get => _eksik; set { _eksik = value; Invalidate(); } }

    [Category("Yazılar"), Description("Boş sahada görünen yardım yazısı"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
    public string IpucuYazisi { get => _ipucuYazi; set { _ipucuYazi = value ?? ""; Invalidate(); } }
    bool ShouldSerializeIpucuYazisi() => _ipucuYazi != VarsayilanIpucu;
    void ResetIpucuYazisi() => IpucuYazisi = VarsayilanIpucu;

    [Category("Yazılar"), Description("Yardım yazısının rengi"), DefaultValue(typeof(Color), "226, 230, 236")]
    public Color IpucuRengi { get => _ipucu; set { _ipucu = value; Invalidate(); } }

    [Category("Yazılar"), Description("Sahanın üstündeki yön yazısı"), DefaultValue("HÜCUM YÖNÜ  →")]
    public string HucumYonuYazisi { get => _yonYazi; set { _yonYazi = value ?? ""; Invalidate(); } }

    [Category("Yazılar"), Description("Yön yazısının rengi"), DefaultValue(typeof(Color), "150, 158, 170")]
    public Color HucumYonuRengi { get => _yon; set { _yon = value; Invalidate(); } }

    readonly List<AtakDokunus> _d = new();
    readonly Stopwatch _saat = new();
    readonly System.Windows.Forms.Timer _tik = new() { Interval = 15 };
    AtakPlani? _oynayan;
    RectangleF _saha;
    int _surukle = -1;

    public AtakCizimAlani()
    {
        SetStyle(ControlStyles.Selectable, true);
        SahaBoslugu = 26;
        _tik.Tick += (_, _) =>
        {
            if (_oynayan != null && _saat.Elapsed.TotalSeconds > _oynayan.Toplam + 1.5) OnizlemeyiDurdur();
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TakimRengi { get; set; } = Color.FromArgb(232, 163, 23);

    /// <summary>Seçili dokunuş; forma düğmesi bu noktaya uygulanır.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Secili { get; private set; } = -1;

    public IReadOnlyList<AtakDokunus> Dokunuslar => _d;
    public bool OyuncusuEksik => _d.Any(x => x.Numara == 0);
    public bool Oynuyor => _oynayan != null;

    /// <summary>Noktalar ya da oyuncuları değişince.</summary>
    public event EventHandler? Degisti;

    void Bildir()
    {
        OnizlemeyiDurdur();
        Invalidate();
        Degisti?.Invoke(this, EventArgs.Empty);
    }

    public void Yukle(IEnumerable<AtakDokunus> dokunuslar)
    {
        _d.Clear();
        _d.AddRange(dokunuslar.Take(AtakPlani.EnCokDokunus));
        Secili = _d.Count - 1;
        Bildir();
    }

    public void Temizle()
    {
        _d.Clear();
        Secili = -1;
        Bildir();
    }

    public void GeriAl()
    {
        if (_d.Count == 0) return;
        _d.RemoveAt(_d.Count - 1);
        Secili = _d.Count - 1;
        Bildir();
    }

    /// <summary>Seçili noktaya (yoksa sonuncuya) oyuncu atar.</summary>
    public void OyuncuAta(int id, int numara, string ad)
    {
        if (_d.Count == 0) return;
        int i = Secili >= 0 && Secili < _d.Count ? Secili : _d.Count - 1;
        _d[i] = _d[i] with { OyuncuId = id, Numara = numara, Ad = ad };
        Bildir();
    }

    public void Onizle()
    {
        if (_d.Count == 0) return;
        _oynayan = AtakPlani.Kur(_d);
        _saat.Restart();
        _tik.Start();
        Invalidate();
    }

    public void OnizlemeyiDurdur()
    {
        if (_oynayan == null) return;
        _oynayan = null;
        _tik.Stop();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tik.Dispose();
        base.Dispose(disposing);
    }

    // ---------------------------------------------------------------- fare

    PointF Ekran(double x, double y) => new(_saha.X + (float)x * _saha.Width, _saha.Y + (float)y * _saha.Height);

    int Bul(Point p)
    {
        for (int i = _d.Count - 1; i >= 0; i--)
        {
            var e = Ekran(_d[i].X, _d[i].Y);
            if (Math.Abs(e.X - p.X) <= 14 && Math.Abs(e.Y - p.Y) <= 14) return i;
        }
        return -1;
    }

    (double X, double Y) Saha(Point p) => (
        Math.Clamp((p.X - _saha.X) / _saha.Width, 0.0, 0.995),
        Math.Clamp((p.Y - _saha.Y) / _saha.Height, 0.0, 1.0));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (_saha.IsEmpty) return;
        int i = Bul(e.Location);
        if (e.Button == MouseButtons.Right)
        {
            if (i >= 0) { _d.RemoveAt(i); Secili = _d.Count - 1; Bildir(); }
            else GeriAl();
            return;
        }
        if (e.Button != MouseButtons.Left) return;
        if (i >= 0)
        {
            Secili = i;
            _surukle = i;
            Invalidate();
            Degisti?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (!_saha.Contains(e.Location) || _d.Count >= AtakPlani.EnCokDokunus) return;
        var (x, y) = Saha(e.Location);
        var onceki = _d.Count > 0 ? _d[^1] : null;
        _d.Add(new AtakDokunus(x, y, onceki?.OyuncuId ?? 0, onceki?.Numara ?? 0, onceki?.Ad ?? ""));
        Secili = _d.Count - 1;
        _surukle = Secili;
        Bildir();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_surukle >= 0 && _surukle < _d.Count && e.Button == MouseButtons.Left)
        {
            var (x, y) = Saha(e.Location);
            _d[_surukle] = _d[_surukle] with { X = x, Y = y };
            Invalidate();
        }
        Cursor = _saha.Contains(e.Location) ? (Bul(e.Location) >= 0 ? Cursors.SizeAll : Cursors.Cross) : Cursors.Default;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_surukle >= 0) { _surukle = -1; Degisti?.Invoke(this, EventArgs.Empty); }
    }

    // ---------------------------------------------------------------- çizim

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        _saha = SahaCizer.Ciz(g, ClientRectangle, this);
        if (_saha.IsEmpty) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var kucuk = new Font("Segoe UI", 9f);
        var yonBoy = TextRenderer.MeasureText(_yonYazi, kucuk);
        TextRenderer.DrawText(g, _yonYazi, kucuk, new Point((int)_saha.Right - yonBoy.Width, (int)_saha.Y - yonBoy.Height - 4), _yon);

        if (_d.Count == 0)
        {
            using var f = new Font("Segoe UI", 12f);
            var r = Rectangle.Round(_saha);
            TextRenderer.DrawText(g, _ipucuYazi, f, r, _ipucu, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        var plan = _oynayan ?? AtakPlani.Kur(_d);
        double t = _oynayan != null ? _saat.Elapsed.TotalSeconds : double.MaxValue;
        float R = Math.Max(9, _saha.Height * _forma);

        // Çizgiler
        foreach (var c in plan.Cizgiler)
        {
            double u = AtakPlani.Ilerleme(c, t);
            if (u <= 0) continue;
            var a = Ekran(c.Bas.X, c.Bas.Y);
            var b = Ekran(c.Bas.X + (c.Son.X - c.Bas.X) * u, c.Bas.Y + (c.Son.Y - c.Bas.Y) * u);
            using var pen = new Pen(_cizgi, c.Tur == AtakCizgiTuru.Sut ? _sut : c.Tur == AtakCizgiTuru.Surus ? _surus : _pas);
            if (c.Tur == AtakCizgiTuru.Surus) pen.DashPattern = new[] { 3f, 2.2f };
            g.DrawLine(pen, a, b);
        }

        // Noktalar
        foreach (var n in plan.Noktalar)
        {
            if (t < n.T0) continue;
            var p = Ekran(n.Yer.X, n.Yer.Y);
            using var nb = new SolidBrush(_nokta);
            g.FillEllipse(nb, p.X - _noktaCap / 2, p.Y - _noktaCap / 2, _noktaCap, _noktaCap);
        }

        // Formalar (ilk dokunuş ve pasla gelen her dokunuş); çizim modunda isim de yazılır
        using var no = new Font("Segoe UI", Math.Max(8f, R * 0.8f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var ad = new Font("Segoe UI", 8.5f);
        using var orta = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (int i = 0; i < plan.Noktalar.Count; i++)
        {
            var n = plan.Noktalar[i];
            var dok = i < _d.Count ? _d[i] : null;
            bool eksik = _oynayan == null && dok != null && dok.Numara == 0;
            if (!n.Forma && !eksik) continue;
            if (t < n.T0) continue;
            double k = _oynayan != null ? Math.Clamp((t - n.T0) / 0.22, 0, 1) : 1;
            k *= 2 - k;
            float r = R * (float)k;
            var p = Ekran(n.Yer.X, n.Yer.Y);
            p = new PointF(p.X, p.Y - R * 1.25f);           // forma noktanın biraz üstünde, çizgiyi kapatmasın
            if (eksik)
            {
                using var kalem = new Pen(_eksik, 2f) { DashPattern = new[] { 2f, 1.5f } };
                using var koyu = new SolidBrush(Color.FromArgb(150, 20, 22, 28));
                using var eb = new SolidBrush(_eksik);
                g.FillEllipse(koyu, p.X - r, p.Y - r, 2 * r, 2 * r);
                g.DrawEllipse(kalem, p.X - r, p.Y - r, 2 * r, 2 * r);
                g.DrawString("?", no, eb, new RectangleF(p.X - r, p.Y - r, 2 * r, 2 * r), orta);
                continue;
            }
            using (var b = new SolidBrush(TakimRengi)) g.FillEllipse(b, p.X - r, p.Y - r, 2 * r, 2 * r);
            bool acik = SahaCizer.Acik(TakimRengi);
            using (var kalem = new Pen(acik ? Color.FromArgb(200, 10, 12, 16) : Color.FromArgb(235, 255, 255, 255), 1.8f))
                g.DrawEllipse(kalem, p.X - r, p.Y - r, 2 * r, 2 * r);
            if (k > 0.6)
                g.DrawString(n.Numara.ToString(), no, acik ? Brushes.Black : Brushes.White, new RectangleF(p.X - r, p.Y - r, 2 * r, 2 * r), orta);
            if (k > 0.6 && !string.IsNullOrEmpty(n.Ad))
                TextRenderer.DrawText(g, n.Ad, ad, new Rectangle((int)(p.X - 70), (int)(p.Y - r - 18), 140, 16), _isim,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
        }

        // Seçili nokta (çizim modunda)
        if (_oynayan == null && Secili >= 0 && Secili < _d.Count)
        {
            var p = Ekran(_d[Secili].X, _d[Secili].Y);
            using var kalem = new Pen(_secim, 2.2f);
            g.DrawEllipse(kalem, p.X - 9, p.Y - 9, 18, 18);
        }

        // Top (önizlemede)
        if (_oynayan != null)
        {
            var tp = plan.TopYeri(t);
            var p = Ekran(tp.X, tp.Y);
            float br = Math.Max(4, R * 0.42f);
            using (var gol = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillEllipse(gol, p.X - br + 2, p.Y - br + 2, 2 * br, 2 * br);
            using var tb = new SolidBrush(_top);
            g.FillEllipse(tb, p.X - br, p.Y - br, 2 * br, 2 * br);
        }
    }
}
