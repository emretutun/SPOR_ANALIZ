using SporAnaliz.Core.Analysis;

namespace SporAnaliz.Core.Playback;

/// <summary>
/// Temsili maçı gerçek zamanda oynatır. <see cref="Step"/> her çağrıldığında saati ilerletir, oyuncuları hız sınırıyla
/// dizilişteki yerlerine doğru yürütür, topun yerini hesaplar ve bu adımda düşen olayları döndürür.
/// Ekrandan bağımsızdır: aynı durumu Viz'e de, önizlemeye de verir.
/// </summary>
public sealed class MatchPlayer
{
    public const double PlayerSpeed = 0.11;   // oyuncunun olağan hız sınırı: saniyede saha boyunun %11'i
    public const double RunSpeedFactor = 2.2; // şut noktasına koşan oyuncu en fazla bu kat hızlı

    readonly AnalysisResult _a;
    MatchTimeline _tl = null!;
    int _fi, _mi, _zi;
    Vec _block;
    bool _blockInit;
    double _realTime;

    public MatchPlayer(AnalysisResult analysis)
    {
        _a = analysis;
        Reset();
    }

    public AnalysisResult Analysis => _a;
    public List<PitchDot> Dots { get; private set; } = null!;
    /// <summary>22 oyuncunun o anki yeri (Dots ile aynı sıra).</summary>
    public Vec[] Positions { get; private set; } = null!;
    public BallState Ball { get; private set; }
    /// <summary>Kurgu zamanı (dilim).</summary>
    public double T { get; private set; }
    /// <summary>O an gösterilen maç dakikası (0..96 dilim sırası).</summary>
    public int Slot { get; private set; }
    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }
    public bool Done { get; private set; }
    /// <summary>1, 2, 4...</summary>
    public double Speed { get; set; } = 1;
    public double TotalSlots => _tl.Total + 0.4;
    /// <summary>1x hızda maçın toplam süresi (saniye).</summary>
    public double DurationSeconds => TotalSlots * TimelineBuilder.SlotSeconds;
    public MatchTimeline Timeline => _tl;

    /// <summary>Maçı başa sarar: kadro, değişiklikler, kırmızı kartlar ve skor sıfırlanır.</summary>
    public void Reset()
    {
        Dots = TimelineBuilder.BuildDots(_a);
        _tl = TimelineBuilder.Build(_a, TimelineBuilder.BuildDots(_a));
        _fi = _mi = _zi = 0;
        T = 0; Slot = 0; HomeGoals = AwayGoals = 0; Done = false; _realTime = 0;
        _block = new Vec(0.5, 0.5);
        _blockInit = false;
        Positions = Dots.Select(d => TimelineBuilder.TargetOf(d, _block)).ToArray();
        Ball = new BallState(new Vec(0.5, 0.5), 1, -1, false);
    }

    /// <summary>Saati <paramref name="realSeconds"/> kadar (hız çarpanıyla) ilerletir; bu adımda düşen olayları döndürür.</summary>
    public List<PlayEvent> Step(double realSeconds)
    {
        var fired = new List<PlayEvent>();
        if (Done) return fired;
        double dts = Math.Min(realSeconds, 0.1) * Speed;   // tek adımda en fazla 0,1 sn: takılmadan sonra sıçrama olmasın
        _realTime += dts;
        T += dts / TimelineBuilder.SlotSeconds;

        while (_fi < _tl.Fires.Count && _tl.Fires[_fi].T <= T) fired.Add(Apply(_tl.Fires[_fi++].Event));
        var ss = _tl.SlotStart;
        int n = ss.Count - 1;
        while (Slot < n - 1 && ss[Slot + 1] <= T) Slot++;
        if (T >= TotalSlots)
        {
            while (_fi < _tl.Fires.Count) fired.Add(Apply(_tl.Fires[_fi++].Event));
            T = TotalSlots;
            Done = true;
        }
        MovePlayers(dts);
        Ball = BallAt(T);
        return fired;
    }

    // Olay oyuncu dairelerini etkiliyorsa uygular: değişiklik, kırmızı kart, gol
    PlayEvent Apply(PlayEvent e)
    {
        int side = e.Side == "home" ? 0 : 1;
        if (e.Type == "sub")
        {
            var d = Dots.FirstOrDefault(x => x.Side == side && x.Id == e.OutPlayerId);
            if (d != null)
            {
                var info = FindPlayer(e.PlayerId);
                d.Id = e.PlayerId;
                d.Name = info?.ShortName ?? e.Player ?? "";
                d.Number = info != null && info.Number > 0 ? info.Number.ToString() : "";
            }
        }
        else if (e.Type == "red")
        {
            var d = Dots.FirstOrDefault(x => x.Side == side && x.Id == e.PlayerId);
            if (d != null) d.Off = true;
        }
        else if (e.Type == "goal")
        {
            HomeGoals = e.HomeGoals;
            AwayGoals = e.AwayGoals;
        }
        return e;
    }

    public Domain.LineupPlayer? FindPlayer(int? id)
    {
        if (id == null || _a.Lineups == null) return null;
        foreach (var lu in new[] { _a.Lineups.Home, _a.Lineups.Away })
            foreach (var p in lu.Starting.Concat(lu.Bench))
                if (p.Id == id) return p;
        return null;
    }

    // Oyuncuları bir adım ilerletir: herkes dizilişteki yerine doğru, hız sınırını aşmadan yürür
    void MovePlayers(double dts)
    {
        var zones = _tl.Zones;
        while (_zi < zones.Count - 1 && zones[_zi + 1].T <= T) _zi++;
        var zone = zones[_zi];
        if (!_blockInit) { _block = new Vec(zone.X, zone.Y); _blockInit = true; }
        // Bölge değişimi yumuşatılır: takım bloğu yeni yerine bir anda değil, yavaşça kayar
        double k = 1 - Math.Exp(-dts * TimelineBuilder.BlockRate);
        _block = new Vec(_block.X + (zone.X - _block.X) * k, _block.Y + (zone.Y - _block.Y) * k);

        var running = new Dictionary<int, RunOrder>();
        foreach (var o in _tl.Orders) if (T >= o.T0 && T <= o.T1 + 0.6) running[o.Dot] = o;

        for (int i = 0; i < Dots.Count; i++)
        {
            var p = Positions[i];
            var target = TimelineBuilder.TargetOf(Dots[i], _block);
            double tx = target.X, ty = target.Y, cap = PlayerSpeed * dts;
            if (running.TryGetValue(i, out var o))
            {
                // Şut noktasına koşu: tam zamanında varacak hızda, ama olağan hızın en fazla 2,2 katı
                tx = o.X; ty = o.Y;
                double left = Math.Max(0.08, (o.T1 - T) * TimelineBuilder.SlotSeconds);
                double need = Math.Sqrt((tx - p.X) * (tx - p.X) + (ty - p.Y) * (ty - p.Y)) / left;
                cap = TimelineBuilder.Clamp(need, PlayerSpeed, PlayerSpeed * RunSpeedFactor) * dts;
            }
            else
            {
                // Yerinde hafif salınım: oyuncular donmuş görünmesin
                tx += Math.Sin(_realTime / 3.9 + i * 2.3) * 0.012;
                ty += Math.Cos(_realTime / 4.7 + i * 1.7) * 0.012;
            }
            double dx = tx - p.X, dy = ty - p.Y, d = Math.Sqrt(dx * dx + dy * dy);
            // Hedefe yaklaştıkça yavaşlar (ani duruş olmaz), hız sınırını hiçbir zaman geçmez
            double step = Math.Min(cap, d * (1 - Math.Exp(-dts * 4)));
            if (d > 1e-6) Positions[i] = new Vec(p.X + dx / d * step, p.Y + dy / d * step);
        }
    }

    // Bir top hedefinin o anki yeri: sabit nokta ya da bir oyuncunun ayağı
    Vec RefPos(BallMove mv)
    {
        if (mv.ToDot == null) return mv.To;
        var p = Positions[mv.ToDot.Value];
        return new Vec(p.X + (Dots[mv.ToDot.Value].Side == 0 ? 0.013 : -0.013), p.Y);
    }

    // Topun o anki durumu: bir oyuncudaysa onunla birlikte, pastaysa iki oyuncu arasında
    BallState BallAt(double t)
    {
        var mv = _tl.Moves;
        if (mv.Count == 0) return new BallState(new Vec(0.5, 0.5), 1, -1, false);
        while (_mi < mv.Count - 1 && mv[_mi + 1].Depart <= t) _mi++;
        while (_mi > 0 && mv[_mi].Depart > t) _mi--;
        var cur = mv[_mi];
        if (cur.Depart > t) return new BallState(new Vec(0.5, 0.5), 1, -1, false);
        var from = _mi > 0 ? RefPos(mv[_mi - 1]) : new Vec(0.5, 0.5);
        var to = RefPos(cur);
        if (t >= cur.Arrive) return new BallState(to, 1, cur.ToDot ?? -1, false);
        double u = (t - cur.Depart) / cur.Dur;
        if (cur.Kind == BallMoveKind.Fade)
        {
            // Top oyundan çıktı (aut, korner, santra): kayarak gitmez, kaybolup yeni yerinde belirir
            var at = u < 0.5 ? from : to;
            double alpha = u < 0.3 ? 1 - u / 0.3 : u > 0.7 ? (u - 0.7) / 0.3 : 0;
            return new BallState(at, alpha, -1, false);
        }
        u = cur.Kind == BallMoveKind.Shot ? u * (0.6 + 0.4 * u) : u * u * (3 - 2 * u);
        return new BallState(new Vec(from.X + (to.X - from.X) * u, from.Y + (to.Y - from.Y) * u), 1, -1, true);
    }

    /// <summary>Dilim sırasını maç dakikası yazısına çevirir: 1', 45+2', 90+5'.</summary>
    public static string SlotLabel(int slot)
    {
        if (slot < 45) return (slot + 1) + "'";
        if (slot < 47) return "45+" + (slot - 44) + "'";
        if (slot < 92) return (slot - 1) + "'";
        return "90+" + (slot - 91) + "'";
    }
}
