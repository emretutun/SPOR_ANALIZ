using System.ComponentModel;
using SporAnaliz.Core.Playback;

namespace SporAnaliz.Kumanda;

/// <summary>Viz'e gidenin aynısını çizen küçük saha önizlemesi (kuşbakışı). Görünümü Properties penceresinden değişir.</summary>
public sealed class SahaOnizleme : SahaKontrolu
{
    Color _kaleci = Color.FromArgb(26, 30, 37), _kenar = Color.FromArgb(230, 255, 255, 255), _top = Color.White,
        _isimArka = Color.FromArgb(220, 8, 11, 17), _isimYazi = Color.White;
    float _oran = 0.026f;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MatchPlayer? Oynatici { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EvRengi { get; set; } = Color.FromArgb(232, 163, 23);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DepRengi { get; set; } = Color.FromArgb(122, 0, 60);

    [Category("Oyuncular"), Description("Oyuncu dairesinin saha yüksekliğine oranı"), DefaultValue(0.026f)]
    public float OyuncuOrani { get => _oran; set { _oran = Math.Clamp(value, 0.01f, 0.08f); Invalidate(); } }

    [Category("Oyuncular"), Description("Kalecinin forma rengi (halkası takım rengindedir)"), DefaultValue(typeof(Color), "26, 30, 37")]
    public Color KaleciRengi { get => _kaleci; set { _kaleci = value; Invalidate(); } }

    [Category("Oyuncular"), Description("Oyuncu dairesinin kenarı"), DefaultValue(typeof(Color), "230, 255, 255, 255")]
    public Color OyuncuKenarRengi { get => _kenar; set { _kenar = value; Invalidate(); } }

    [Category("Oyuncular"), Description("Top"), DefaultValue(typeof(Color), "White")]
    public Color TopRengi { get => _top; set { _top = value; Invalidate(); } }

    [Category("Oyuncular"), Description("Topu taşıyan oyuncunun isim kutusu"), DefaultValue(typeof(Color), "220, 8, 11, 17")]
    public Color IsimArkaRengi { get => _isimArka; set { _isimArka = value; Invalidate(); } }

    [Category("Oyuncular"), Description("İsim yazısı"), DefaultValue(typeof(Color), "White")]
    public Color IsimYaziRengi { get => _isimYazi; set { _isimYazi = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var saha = SahaCizer.Ciz(g, ClientRectangle, this);
        var p = Oynatici;
        if (saha.IsEmpty || p == null) return;
        float w = saha.Width, h = saha.Height;
        float X(double v) => saha.X + (float)v * w;
        float Y(double v) => saha.Y + (float)v * h;

        // Oyuncular
        float R = Math.Max(6, h * _oran);
        using var font = new Font("Segoe UI", Math.Max(6.5f, R * 0.85f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var orta = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (int i = 0; i < p.Dots.Count; i++)
        {
            var d = p.Dots[i];
            if (d.Off) continue;
            var v = p.Positions[i];
            var renk = d.Side == 0 ? EvRengi : DepRengi;
            bool kaleci = d.Idx == 0;
            float cx = X(v.X), cy = Y(v.Y);
            var govde = kaleci ? _kaleci : renk;
            using (var b = new SolidBrush(govde)) g.FillEllipse(b, cx - R, cy - R, 2 * R, 2 * R);
            using (var pen = new Pen(kaleci ? renk : _kenar, 1.6f)) g.DrawEllipse(pen, cx - R, cy - R, 2 * R, 2 * R);
            var yazi = SahaCizer.Acik(govde) ? Brushes.Black : Brushes.White;
            g.DrawString(d.Number, font, yazi, new RectangleF(cx - R, cy - R, 2 * R, 2 * R), orta);
        }

        // Top kimdeyse adı
        var ball = p.Ball;
        if (ball.Holder >= 0 && ball.Alpha >= 1 && !string.IsNullOrEmpty(p.Dots[ball.Holder].Name))
        {
            var hp = p.Positions[ball.Holder];
            var ad = p.Dots[ball.Holder].Name;
            var boy = g.MeasureString(ad, font);
            var kutu = new RectangleF(X(hp.X) - boy.Width / 2 - 4, Y(hp.Y) - R - boy.Height - 6, boy.Width + 8, boy.Height + 2);
            using (var b = new SolidBrush(_isimArka)) g.FillRectangle(b, kutu);
            using (var b = new SolidBrush(_isimYazi)) g.DrawString(ad, font, b, kutu, orta);
        }

        // Top
        if (ball.Alpha > 0.05)
        {
            float br = Math.Max(3, R * 0.45f);
            using var b = new SolidBrush(Color.FromArgb((int)(_top.A * Math.Min(1, ball.Alpha)), _top));
            g.FillEllipse(b, X(ball.Pos.X) - br, Y(ball.Pos.Y) - br, 2 * br, 2 * br);
        }
    }
}
