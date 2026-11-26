using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SporAnaliz.Kumanda;

/// <summary>Kuşbakışı saha çizimi (simülasyon önizlemesi ve atak çizim ekranı ortak kullanır).</summary>
public static class SahaCizer
{
    public static readonly Color Zemin = Color.FromArgb(24, 27, 33);

    /// <summary>Sahayı alanın ortasına 105 x 68 oranında çizer ve sahanın kapladığı dikdörtgeni döndürür.</summary>
    public static RectangleF Ciz(Graphics g, Rectangle alan, ISahaGorunumu r)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(r.ZeminRengi);
        float pad = r.SahaBoslugu;
        float w = alan.Width - 2 * pad, h = alan.Height - 2 * pad;
        if (w <= 10 || h <= 10) return RectangleF.Empty;
        if (w / h > 105f / 68f) w = h * 105f / 68f; else h = w * 68f / 105f;
        float fx = alan.X + (alan.Width - w) / 2, fy = alan.Y + (alan.Height - h) / 2;
        float X(double v) => fx + (float)v * w;
        float Y(double v) => fy + (float)v * h;

        using (var b = new SolidBrush(r.CimRengi)) g.FillRectangle(b, fx, fy, w, h);
        using (var b = new SolidBrush(r.SeritRengi))
            for (int i = 0; i < 14; i += 2) g.FillRectangle(b, fx + i * w / 14, fy, w / 14, h);

        using var cizgi = new Pen(r.SahaCizgiRengi, r.SahaCizgiKalinligi);
        g.DrawRectangle(cizgi, fx, fy, w, h);
        g.DrawLine(cizgi, X(0.5), fy, X(0.5), fy + h);
        float rr = h * 0.135f;
        g.DrawEllipse(cizgi, X(0.5) - rr, Y(0.5) - rr, 2 * rr, 2 * rr);
        for (int s = 0; s < 2; s++)
        {
            float x0 = X(s);
            float bw = w * 0.157f, bh = h * 0.593f, gw = w * 0.052f, gh = h * 0.27f;
            g.DrawRectangle(cizgi, s == 0 ? x0 : x0 - bw, Y(0.5) - bh / 2, bw, bh);
            g.DrawRectangle(cizgi, s == 0 ? x0 : x0 - gw, Y(0.5) - gh / 2, gw, gh);
            float kx = s == 0 ? x0 - 10 : x0, kh = h * 0.108f;
            g.DrawRectangle(cizgi, kx, Y(0.5) - kh / 2, 10, kh);
        }
        return new RectangleF(fx, fy, w, h);
    }

    public static bool Acik(Color c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B > 140;
}

/// <summary>Sahanın görünüm ayarları (iki saha kontrolü de bunları Properties penceresinde gösterir).</summary>
public interface ISahaGorunumu
{
    Color ZeminRengi { get; }
    Color CimRengi { get; }
    Color SeritRengi { get; }
    Color SahaCizgiRengi { get; }
    float SahaCizgiKalinligi { get; }
    int SahaBoslugu { get; }
}

/// <summary>Saha görünümü özelliklerini taşıyan ortak taban: renk değişince kendini yeniden çizer.</summary>
public abstract class SahaKontrolu : Control, ISahaGorunumu
{
    Color _zemin = Color.FromArgb(24, 27, 33), _cim = Color.FromArgb(38, 92, 60), _serit = Color.FromArgb(42, 100, 66),
        _cizgi = Color.FromArgb(170, 235, 240, 236);
    float _kalin = 1.5f;
    int _bosluk = 18;

    protected SahaKontrolu()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = _zemin;
    }

    [Category("Saha"), Description("Sahanın çevresi"), DefaultValue(typeof(Color), "24, 27, 33")]
    public Color ZeminRengi { get => _zemin; set { _zemin = value; Invalidate(); } }

    [Category("Saha"), Description("Çim"), DefaultValue(typeof(Color), "38, 92, 60")]
    public Color CimRengi { get => _cim; set { _cim = value; Invalidate(); } }

    [Category("Saha"), Description("Biçilmiş çim şeritleri"), DefaultValue(typeof(Color), "42, 100, 66")]
    public Color SeritRengi { get => _serit; set { _serit = value; Invalidate(); } }

    [Category("Saha"), Description("Saha çizgileri (saydamlık dahil)"), DefaultValue(typeof(Color), "170, 235, 240, 236")]
    public Color SahaCizgiRengi { get => _cizgi; set { _cizgi = value; Invalidate(); } }

    [Category("Saha"), Description("Saha çizgilerinin kalınlığı (piksel)"), DefaultValue(1.5f)]
    public float SahaCizgiKalinligi { get => _kalin; set { _kalin = Math.Max(0.5f, value); Invalidate(); } }

    [Category("Saha"), Description("Sahanın kenarlara uzaklığı (piksel)"), DefaultValue(18)]
    public int SahaBoslugu { get => _bosluk; set { _bosluk = Math.Max(0, value); Invalidate(); } }
}
