using System.Drawing.Drawing2D;
using SporAnaliz.Core.Playback;

namespace SporAnaliz.Kumanda;

/// <summary>Viz'e gidenin aynısını çizen küçük saha önizlemesi (kuşbakışı).</summary>
public sealed class SahaOnizleme : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public MatchPlayer? Oynatici { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color EvRengi { get; set; } = Color.FromArgb(232, 163, 23);
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color DepRengi { get; set; } = Color.FromArgb(122, 0, 60);

    static readonly Color Zemin = Color.FromArgb(24, 27, 33);
    static readonly Color Cim = Color.FromArgb(38, 92, 60);
    static readonly Color Serit = Color.FromArgb(42, 100, 66);
    static readonly Pen Cizgi = new(Color.FromArgb(170, 235, 240, 236), 1.5f);

    public SahaOnizleme()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Zemin;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Zemin);

        // 105 x 68 oranını koruyarak sığdır
        float pad = 18, w = Width - 2 * pad, h = Height - 2 * pad;
        if (w <= 10 || h <= 10) return;
        if (w / h > 105f / 68f) w = h * 105f / 68f; else h = w * 68f / 105f;
        float fx = (Width - w) / 2, fy = (Height - h) / 2;
        float X(double v) => fx + (float)v * w;
        float Y(double v) => fy + (float)v * h;

        using (var b = new SolidBrush(Cim)) g.FillRectangle(b, fx, fy, w, h);
        using (var b = new SolidBrush(Serit))
            for (int i = 0; i < 14; i += 2) g.FillRectangle(b, fx + i * w / 14, fy, w / 14, h);

        g.DrawRectangle(Cizgi, fx, fy, w, h);
        g.DrawLine(Cizgi, X(0.5), fy, X(0.5), fy + h);
        float r = h * 0.135f;
        g.DrawEllipse(Cizgi, X(0.5) - r, Y(0.5) - r, 2 * r, 2 * r);
        for (int s = 0; s < 2; s++)
        {
            float x0 = X(s), d = s == 0 ? 1 : -1;
            float bw = w * 0.157f, bh = h * 0.593f, gw = w * 0.052f, gh = h * 0.27f;
            g.DrawRectangle(Cizgi, s == 0 ? x0 : x0 - bw, Y(0.5) - bh / 2, bw, bh);
            g.DrawRectangle(Cizgi, s == 0 ? x0 : x0 - gw, Y(0.5) - gh / 2, gw, gh);
            float kx = s == 0 ? x0 - 10 : x0, kh = h * 0.108f;
            g.DrawRectangle(Cizgi, kx, Y(0.5) - kh / 2, 10, kh);
            _ = d;
        }

        var p = Oynatici;
        if (p == null) return;

        // Oyuncular
        float R = Math.Max(6, h * 0.026f);
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
            using (var b = new SolidBrush(kaleci ? Color.FromArgb(26, 30, 37) : renk)) g.FillEllipse(b, cx - R, cy - R, 2 * R, 2 * R);
            using (var pen = new Pen(kaleci ? renk : Color.FromArgb(230, 255, 255, 255), 1.6f)) g.DrawEllipse(pen, cx - R, cy - R, 2 * R, 2 * R);
            var yazi = kaleci || !Acik(renk) ? Brushes.White : Brushes.Black;
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
            using (var b = new SolidBrush(Color.FromArgb(220, 8, 11, 17))) g.FillRectangle(b, kutu);
            g.DrawString(ad, font, Brushes.White, kutu, orta);
        }

        // Top
        if (ball.Alpha > 0.05)
        {
            float br = Math.Max(3, R * 0.45f);
            using var b = new SolidBrush(Color.FromArgb((int)(255 * Math.Min(1, ball.Alpha)), 255, 255, 255));
            g.FillEllipse(b, X(ball.Pos.X) - br, Y(ball.Pos.Y) - br, 2 * br, 2 * br);
        }
    }

    static bool Acik(Color c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B > 140;
}
