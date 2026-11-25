namespace SporAnaliz.Core.Playback;

/// <summary>
/// Temsili maçın olaylarından ve baskı eğrisinden sahadaki kurguyu çıkarır. Ev sahibi sağa hücum eder.
/// Hareket ilkesi: oyuncular dizilişte yavaş kayar; hızlı olan toptur ve oyuncudan oyuncuya pasla gider.
/// Kimse topun peşinden koşmaz; koşan tek kişi şut noktasına giden oyuncudur.
/// (Web sayfasındaki pitch.js ile aynı mantık.)
/// </summary>
public static class TimelineBuilder
{
    public const int HalfSlot = 47;              // ikinci yarının ilk dilimi
    public const double SlotSeconds = 0.56;      // olaysız bir dakikanın 1x hızdaki süresi
    public const double BlockRate = 1.0;         // takım bloğunun yeni bölgeye kayma hızı (saniyede)

    static readonly Vec Center = new(0.5, 0.5);

    public static double Clamp(double v, double a, double b) => Math.Min(b, Math.Max(a, v));

    /// <summary>"4-2-3-1" gibi bir dizilişi 11 konuma çevirir: [kendi kalesinden uzaklık, yan konum] (0..1).</summary>
    public static List<Vec> FormationCoords(string? formation)
    {
        var rows = (formation ?? "").Split('-').Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0 && n < 7).ToList();
        if (rows.Sum() != 10) rows = new List<int> { 4, 3, 3 };
        var outList = new List<Vec> { new(0.045, 0.5) };
        int r = rows.Count;
        for (int row = 0; row < r; row++)
        {
            int c = rows[row];
            double x = r == 1 ? 0.36 : 0.17 + row * (0.42 / (r - 1));
            for (int j = 0; j < c; j++) outList.Add(new Vec(x, 0.5 + (j - (c - 1) / 2.0) * Math.Min(0.24, 0.8 / c)));
        }
        return outList;
    }

    /// <summary>İki takımın 11'er oyuncusu: kadro varsa gerçek isim ve forma numarasıyla, dizilişe göre.</summary>
    public static List<PitchDot> BuildDots(Analysis.AnalysisResult a)
    {
        var dots = new List<PitchDot>(22);
        var rnd = new Mulberry32(unchecked((uint)(a.Match.Id * 131 + 7)));
        for (int side = 0; side < 2; side++)
        {
            var lu = a.Lineups == null ? null : side == 0 ? a.Lineups.Home : a.Lineups.Away;
            var coords = FormationCoords(lu?.Formation);
            for (int i = 0; i < 11; i++)
            {
                var p = lu != null && i < lu.Starting.Count ? lu.Starting[i] : null;
                bool wide = i > 0 && Math.Abs(coords[i].Y - 0.5) > 0.24;
                // Sapmalar her durumda çekilir ki rastgele dizi kadrodan bağımsız aynı kalsın
                double ox = (rnd.Next() - 0.5) * 0.06 + (wide ? 0.012 : -0.008), oy = (rnd.Next() - 0.5) * 0.05;
                dots.Add(new PitchDot
                {
                    Side = side, Idx = i, BaseX = coords[i].X, BaseY = coords[i].Y, Wide = wide,
                    Ox = i == 0 ? 0 : ox, Oy = i == 0 ? 0 : oy,
                    Id = p?.Id,
                    Number = p != null && p.Number > 0 ? p.Number.ToString() : (i + 1).ToString(),
                    Name = p?.ShortName ?? "",
                });
            }
        }
        return dots;
    }

    /// <summary>
    /// Oyuncunun dizilişteki yeri. Hesap takımın kendi yönüne göre yapılır (0 = kendi kalesi), sonra sahaya çevrilir.
    /// Savunma hattı oyunun oynandığı bölgeye göre ilerler; topun olduğu kanattaki oyuncular oyuna yaklaşır.
    /// </summary>
    public static Vec TargetOf(PitchDot d, Vec zone)
    {
        bool home = d.Side == 0;
        double zx = home ? zone.X : 1 - zone.X, zy = home ? zone.Y : 1 - zone.Y, x, y;
        if (d.Idx == 0)
        {
            x = 0.045 + Clamp(zx - 0.5, -0.3, 0.5) * 0.07;
            y = 0.5 + (zy - 0.5) * 0.12;
        }
        else
        {
            double line = Clamp(zx - 0.33, 0.13, 0.52);
            double near = Math.Max(0, 1 - Math.Abs(zy - d.BaseY) / 0.35);
            x = line + (d.BaseX - 0.17) * 0.72 + d.Ox;
            x += 0.035 * near * (zx > x ? 1 : -0.5);
            if (d.Wide) x += 0.04 * Clamp((zx - 0.4) / 0.4, 0, 1);
            y = d.BaseY + (zy - d.BaseY) * 0.18 + d.Oy;
            x = Clamp(x, 0.07, 0.93);
            y = Clamp(y, 0.04, 0.96);
        }
        return home ? new Vec(x, y) : new Vec(1 - x, 1 - y);
    }

    static bool NoBallMove(string type) => type is "yellow" or "red" or "sub";

    public static MatchTimeline Build(Analysis.AnalysisResult a, List<PitchDot> dots)
    {
        var rnd = new Mulberry32(unchecked((uint)(a.Match.Id * 97 + 5)));
        var m = a.Simulation.Momentum;
        int n = m.Length;
        double max = Math.Max(4, m.Length == 0 ? 0 : m.Max(Math.Abs));
        var moves = new List<BallMove>();
        var fires = new List<EventFire>();
        var zones = new List<Zone>();
        var orders = new List<RunOrder>();
        var slotStart = new List<double>(n + 1);
        var perSlot = a.Simulation.Events.GroupBy(e => e.Slot).ToDictionary(g => g.Key, g => g.ToList());

        // Her dairenin o an hangi oyuncu olduğu: değişiklikle değişir; kırmızı kart gören daire oyundan çıkar
        var curId = dots.Select(d => d.Id).ToArray();
        var gone = new HashSet<int>();
        List<int> Outfield(int side) => Enumerable.Range(0, dots.Count).Where(i => dots[i].Side == side && dots[i].Idx > 0 && !gone.Contains(i)).ToList();
        int Keeper(int side) => dots.FindIndex(d => d.Side == side && d.Idx == 0);
        static int SideOf(string s) => s == "home" ? 0 : 1;

        double T = 0, y = 0.5, lastArrive = 0, nextPass = 0.35, blockT = 0;
        var zone = new Vec(0.5, 0.5);
        var block = new Vec(0.5, 0.5);
        int holder = -1, holderSide = 0;

        void Settle(double tNow)
        {
            double k = 1 - Math.Exp(-Math.Max(0, tNow - blockT) * SlotSeconds * BlockRate);
            block = new Vec(block.X + (zone.X - block.X) * k, block.Y + (zone.Y - block.Y) * k);
            blockT = Math.Max(blockT, tNow);
        }
        void SetZone(double t, double x, double yy) { Settle(t); zone = new Vec(x, yy); zones.Add(new Zone(t, x, yy)); }
        SetZone(-1, 0.5, 0.5);

        // Topun beklenen yeri izlenir ki pas süresi mesafeye göre ayarlansın: kısa pas çabuk, uzun pas daha uzun
        var ballAt = new Vec(0.5, 0.5);
        Vec Where(int? dot, Vec to) => dot != null ? TargetOf(dots[dot.Value], block) : to;
        double PassTime(int? dot, Vec to) { var p = Where(dot, to); return Clamp(Dist(p, ballAt) / 0.5, 0.4, 1.2); }
        BallMove Move(double depart, double? dur, int? dot, Vec to, BallMoveKind kind)
        {
            double dep = Math.Max(depart, lastArrive);
            Settle(dep);
            var mv = new BallMove { Depart = dep, Dur = dur ?? PassTime(dot, to), ToDot = dot, To = to, Kind = kind };
            moves.Add(mv);
            lastArrive = mv.Arrive;
            ballAt = Where(dot, to);
            return mv;
        }
        // Pasın gideceği oyuncu: istenen noktaya en yakın duran takım arkadaşı (biraz rastgelelikle)
        int Pick(int side, Vec point, int exclude)
        {
            int best = -1; double bs = 9;
            foreach (int i in Outfield(side))
            {
                if (i == exclude) continue;
                var p = TargetOf(dots[i], block);
                double s = Math.Sqrt(Math.Pow(p.X - point.X, 2) + Math.Pow((p.Y - point.Y) * 0.62, 2)) + rnd.Next() * 0.10;
                if (s < bs) { bs = s; best = i; }
            }
            return best;
        }
        BallMove PassTo(double t, int side, Vec point)
        {
            int r = Pick(side, point, holder);
            var mv = Move(t, null, r, default, BallMoveKind.Pass);
            holder = r; holderSide = side;
            return mv;
        }

        for (int i = 0; i < n; i++)
        {
            slotStart.Add(T);
            var evs = perSlot.TryGetValue(i, out var l) ? l : new List<Analysis.PlayEvent>();
            var ballEvs = evs.Where(e => !NoBallMove(e.Type)).ToList();
            int j0 = 0;
            foreach (var e in evs.Where(e => NoBallMove(e.Type)))
            {
                fires.Add(new EventFire(T + 0.05 + j0++ * 0.9, e));
                int sideE = SideOf(e.Side);
                int? who = e.Type == "sub" ? e.OutPlayerId : e.PlayerId;
                int k = -1;
                for (int q = 0; q < dots.Count; q++) if (dots[q].Side == sideE && curId[q] != null && curId[q] == who) { k = q; break; }
                if (k < 0) continue;
                if (e.Type == "sub") curId[k] = e.PlayerId;
                else if (e.Type == "red") gone.Add(k);
            }
            y = Clamp(y + (rnd.Next() - 0.5) * 0.12, 0.2, 0.8);
            double ms = (m[Math.Max(0, i - 1)] + 2 * m[i] + m[Math.Min(n - 1, i + 1)]) / 4;
            int side = ms >= 0 ? 0 : 1;
            (int Side, int Away)? buildUp = null;
            for (int j = i + 1; j <= i + 5 && j < n && buildUp == null; j++)
                if (perSlot.TryGetValue(j, out var lj))
                {
                    var ev = lj.FirstOrDefault(e => e.Type is "goal" or "shot_on" or "shot_off");
                    if (ev != null) buildUp = (SideOf(ev.Side), j - i);
                }
            if (buildUp != null) side = buildUp.Value.Side;

            if (i == HalfSlot)
            {
                // Devre arası: top santraya döner, ikinci yarıyı deplasman başlatır
                SetZone(T, 0.5, 0.5);
                Move(T, 0.7, null, Center, BallMoveKind.Fade);
                holder = -1; holderSide = 1; nextPass = lastArrive + 0.6; y = 0.5;
            }

            if (ballEvs.Count == 0)
            {
                // Açık oyun: top, baskının olduğu bölgede oyuncudan oyuncuya dolaşır
                if (i != 0 && i != HalfSlot)
                {
                    if (buildUp != null) SetZone(T, 0.5 + (buildUp.Value.Side == 0 ? 1 : -1) * (0.30 - 0.02 * buildUp.Value.Away), y);
                    else SetZone(T, 0.5 + Clamp(ms / max, -1, 1) * 0.28, y);
                }
                while (nextPass < T + 1)
                {
                    double t = Math.Max(nextPass, lastArrive + 0.2);
                    int s = holder < 0 ? holderSide : side;
                    PassTo(t, s, new Vec(zone.X + (s == 0 ? 0.03 : -0.03), y + (rnd.Next() - 0.5) * 0.3));
                    nextPass = t + 1.3 + rnd.Next() * 0.8;
                }
                T += 1;
                continue;
            }

            double c = Math.Max(T, lastArrive + 0.15);
            foreach (var e in ballEvs)
            {
                int sideE = SideOf(e.Side), other = 1 - sideE;
                double dir = sideE == 0 ? 1 : -1;
                double Gx(double v) => 0.5 + dir * (v - 0.5);
                if (e.Type == "corner")
                {
                    double cy = rnd.Next() < 0.5 ? 0.015 : 0.985;
                    SetZone(c, Gx(0.86), 0.5);                                           // takım ceza sahasına yerleşir
                    var outMv = Move(c, 0.5, null, new Vec(Gx(0.99), cy), BallMoveKind.Fade); // top köşe bayrağında belirir
                    fires.Add(new EventFire(outMv.Arrive - 0.1, e));
                    var targets = Outfield(sideE).Where(k => dots[k].Idx >= 5).ToList();
                    if (targets.Count == 0) targets = Outfield(sideE);
                    int r = targets[(int)Math.Floor(rnd.Next() * targets.Count)];
                    var cross = Move(outMv.Arrive + 0.35, null, r, default, BallMoveKind.Pass); // bekler, sonra orta
                    holder = r; holderSide = sideE; c = cross.Arrive + 0.2;
                    continue;
                }

                // Şut: şutu atacak oyuncu şut noktasına gelir, pas ona ulaşır, vurur. Olmadık yerden şut atılmaz.
                int sh = -1;
                for (int q = 0; q < dots.Count; q++)
                    if (dots[q].Side == sideE && dots[q].Idx > 0 && curId[q] != null && curId[q] == e.PlayerId && !gone.Contains(q)) { sh = q; break; }
                if (sh < 0) { var f = Outfield(sideE); sh = f[f.Count - 1 - (int)Math.Floor(rnd.Next() * Math.Min(3, f.Count))]; }
                if (holder < 0 || dots[holder].Side != sideE) { var mv = PassTo(c, sideE, new Vec(zone.X, y)); c = mv.Arrive + 0.25; }
                SetZone(c, Gx(0.82), y);
                Settle(c);
                var from = TargetOf(dots[sh], block);
                bool off = e.Type == "shot_off";
                var want = new Vec(Gx((off ? 0.72 : 0.80) + rnd.Next() * 0.06), 0.5 + (rnd.Next() - 0.5) * (off ? 0.44 : 0.34));
                bool inRange = dir * (from.X - want.X) >= 0 && Math.Abs(from.Y - 0.5) < 0.3;
                var spotTo = off && inRange ? from : want;
                double dist = Dist(spotTo, from), run = 0;
                var origin = spotTo;
                if (dist > 0.30)
                {
                    // Oyuncu çok geride (çoğunlukla savunmacı): duran top olarak oynanır
                    var spot = new Vec(Gx(0.76), want.Y < 0.5 ? 0.86 : 0.14);
                    SetZone(c, Gx(0.86), 0.5);
                    Move(c, 0.5, null, spot, BallMoveKind.Fade);
                    run = Clamp(dist / 0.12, 1.6, 3.6);
                    orders.Add(new RunOrder(sh, c, c + run, spotTo.X, spotTo.Y));
                    double d = Clamp(Dist(spotTo, spot) / 0.5, 0.4, 1.2);
                    Move(c + run - d, d, sh, default, BallMoveKind.Pass);
                    ballAt = spotTo;
                }
                else if (dist > 0.015)
                {
                    run = Clamp(dist / 0.12, 0.6, 2.5);
                    orders.Add(new RunOrder(sh, c, c + run, spotTo.X, spotTo.Y));
                    if (holder != sh) { double d = Clamp(Dist(spotTo, ballAt) / 0.5, 0.4, 1.2); Move(c + run - d, d, sh, default, BallMoveKind.Pass); }
                    ballAt = spotTo;
                }
                else if (holder != sh)
                {
                    var mv = Move(c, null, sh, default, BallMoveKind.Pass); c = mv.Arrive;
                }
                holder = sh; holderSide = sideE;
                int gk = Keeper(other);
                // Şutun süresi mesafeyle orantılı: uzaktan şutta top sahayı bir anda geçmez
                BallMove Shot(Vec end)
                {
                    double d = Dist(end, origin);
                    var s = Move(Math.Max(c + run, lastArrive) + 0.1, Clamp(d / 0.7, 0.35, 1.0), null, end, BallMoveKind.Shot);
                    fires.Add(new EventFire(s.Arrive, e));
                    return s;
                }
                if (e.Type == "goal")
                {
                    var s = Shot(new Vec(Gx(1.012), 0.5 + (rnd.Next() - 0.5) * 0.07));
                    SetZone(s.Arrive + 0.8, 0.5, 0.5);                                   // top ağlarda bekler, takımlar yerine döner
                    var back = Move(s.Arrive + 0.9, 0.6, null, Center, BallMoveKind.Fade); // santra
                    holder = -1; holderSide = other; c = back.Arrive + 0.3; y = 0.5;
                }
                else if (e.Type == "shot_on")
                {
                    var s = Shot(new Vec(Gx(0.972), 0.5 + (rnd.Next() - 0.5) * 0.08));
                    var mv = Move(s.Arrive + 0.1, 0.22, gk, default, BallMoveKind.Pass);   // kaleci topu aldı
                    holder = gk; holderSide = other; c = mv.Arrive + 0.3;
                }
                else
                {
                    var s = Shot(new Vec(Gx(1.03), 0.5 + (rnd.Next() < 0.5 ? -1 : 1) * (0.1 + rnd.Next() * 0.12)));
                    var mv = Move(s.Arrive + 0.25, 0.5, gk, default, BallMoveKind.Fade);  // kale vuruşu
                    holder = gk; holderSide = other; c = mv.Arrive + 0.3;
                }
            }
            T = Math.Max(T + 1, c);
            nextPass = Math.Max(nextPass, c + 0.3);
        }
        slotStart.Add(T);
        // Sıralama kararlı olsun: aynı anda düşen olaylar kurgudaki sırasını korur
        fires = fires.Select((f, k) => (f, k)).OrderBy(p => p.f.T).ThenBy(p => p.k).Select(p => p.f).ToList();
        return new MatchTimeline { Moves = moves, Zones = zones, Orders = orders, Fires = fires, SlotStart = slotStart, Total = T };
    }

    public static double Dist(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}

/// <summary>Web sayfasındakiyle aynı tohumlu rastgele sayı üreteci (mulberry32): aynı maç her seferinde aynı oynar.</summary>
public sealed class Mulberry32(uint seed)
{
    uint _a = seed;

    public double Next()
    {
        unchecked
        {
            _a += 0x6D2B79F5;
            uint t = (_a ^ (_a >> 15)) * (1 | _a);
            t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }
}
