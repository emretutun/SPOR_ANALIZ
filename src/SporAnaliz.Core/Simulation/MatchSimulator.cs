namespace SporAnaliz.Core.Simulation;

/// <param name="Pos">Mevki: KL, DF, OS, FV (değişikliklerde benzer mevkideki oyuncu seçilir).</param>
public sealed record SimPlayer(int Id, string Name, double Weight, string Pos = "");

/// <summary>Bir takımın tek maçlık simülasyon girdileri.</summary>
public sealed class SimTeam
{
    public double Lambda;      // beklenen gol
    public double Shots;       // beklenen şut
    public double Corners;     // beklenen korner
    public double Yellows;     // beklenen sarı kart
    public double Possession;  // beklenen top hakimiyeti (%)
    /// <summary>Maç kadrosu: önce ilk 11, sonra yedekler.</summary>
    public IReadOnlyList<SimPlayer> Players = Array.Empty<SimPlayer>();
    /// <summary>
    /// Listenin ilk kaç oyuncusunun maça başladığı; kalanı yedektir ve oyuna ancak değişiklikle girer.
    /// -1: kadro bilinmiyor, herkes sahada sayılır ve değişiklik yapılmaz.
    /// </summary>
    public int StartingCount = -1;
}

/// <summary>Maç olayı. Oyuncu değişikliğinde PlayerId/Player oyuna gireni, OutPlayerId/OutPlayer çıkanı gösterir.</summary>
public sealed record SimEvent(int Minute, int Added, int Side, string Type, int? PlayerId, string? Player,
    int HomeGoals, int AwayGoals, int? OutPlayerId = null, string? OutPlayer = null);

public readonly record struct GoalRec(int Side, int Minute, int PlayerIndex);

public sealed class SimResult
{
    public readonly int[] Goals = new int[2];
    public readonly int[] Shots = new int[2];
    public readonly int[] OnTarget = new int[2];
    public readonly int[] Corners = new int[2];
    public readonly int[] Yellows = new int[2];
    public readonly int[] Reds = new int[2];
    public readonly int[] Subs = new int[2];
    public readonly List<GoalRec> GoalList = new(4);
    public List<SimEvent>? Events;   // yalnızca kayıt modunda
    public double[]? Momentum;       // yalnızca kayıt modunda; + ev sahibi, - deplasman
}

/// <summary>
/// Maçı dakika dakika oynatır. Her dakikada her takım için şut, gol, korner ve kart olasılıkları çekilir.
/// Beklenen gol = Shots × (Lambda / Shots) olacak şekilde kalibre edilir; skor durumuna göre baskı değişir.
/// Kadro verilmişse olaylar yalnızca o an SAHADA olan oyunculara yazılır: yedekler ancak değişiklikle girer,
/// kırmızı kart gören oyuncu çıkar.
/// </summary>
public static class MatchSimulator
{
    public const int FirstHalfAdded = 2, SecondHalfAdded = 5;
    public const int MaxSubs = 5;
    private const double Trailing = 1.03, TrailingLate = 1.06, Leading = 0.98;

    // (dakika, uzatma) çiftleri: 1..45, 45+1..45+2, 46..90, 90+1..90+5
    public static readonly (int Minute, int Added)[] Slots = BuildSlots();
    private static readonly double[] SlotWeight = BuildWeights();

    private static (int, int)[] BuildSlots()
    {
        var l = new List<(int, int)>();
        for (int m = 1; m <= 45; m++) l.Add((m, 0));
        for (int a = 1; a <= FirstHalfAdded; a++) l.Add((45, a));
        for (int m = 46; m <= 90; m++) l.Add((m, 0));
        for (int a = 1; a <= SecondHalfAdded; a++) l.Add((90, a));
        return l.ToArray();
    }

    // Maçın sonuna doğru tempo artar; ağırlıkların toplamı 1'dir.
    private static double[] BuildWeights()
    {
        var w = Slots.Select(s => 0.85 + 0.30 * s.Minute / 90.0).ToArray();
        double sum = w.Sum();
        return w.Select(x => x / sum).ToArray();
    }

    public static SimResult Run(SimTeam home, SimTeam away, int seed, bool record)
    {
        var rng = new Random(seed);
        // Değişiklik kararları ayrı bir rastgele dizi kullanır: kadro verilip verilmemesi maçın gidişatını
        // (şutlar, goller, skor) değiştirmez; yalnızca olayların kime yazıldığını değiştirir.
        var subRng = new Random(unchecked(seed * 31 + 7));
        var teams = new[] { home, away };
        var r = new SimResult();
        double[]? raw = null;
        if (record)
        {
            r.Events = new List<SimEvent>(56);
            raw = new double[Slots.Length];
        }

        var conv = new double[2];
        var weightSum = new double[2];
        var active = new List<int>[2];       // sahadaki oyuncular (Players içindeki sıra numaraları)
        var bench = new List<int>[2];        // henüz girmemiş yedekler
        var plan = new List<(int Slot, int Count)>[2];
        var entered = new[] { new HashSet<int>(), new HashSet<int>() };   // oyuna sonradan girenler: tekrar değiştirilmez
        for (int s = 0; s < 2; s++)
        {
            var t = teams[s];
            conv[s] = Math.Clamp(t.Lambda / Math.Max(t.Shots, 0.1), 0.01, 0.45);
            int starting = t.StartingCount < 0 ? t.Players.Count : Math.Min(t.StartingCount, t.Players.Count);
            active[s] = Enumerable.Range(0, starting).ToList();
            bench[s] = Enumerable.Range(starting, t.Players.Count - starting).ToList();
            weightSum[s] = SumWeights(t, active[s]);
            plan[s] = PlanSubstitutions(bench[s].Count, subRng);
        }
        double possBias = (home.Possession - 50.0) / 50.0 * 0.35;

        for (int slot = 0; slot < Slots.Length; slot++)
        {
            var (minute, added) = Slots[slot];
            double w = SlotWeight[slot];
            if (raw != null) raw[slot] += possBias;

            // Oyuncu değişiklikleri dakikanın başında yapılır
            for (int s = 0; s < 2; s++)
                foreach (var (planSlot, count) in plan[s])
                    if (planSlot == slot)
                        for (int k = 0; k < count; k++)
                            if (Substitute(teams[s], active[s], bench[s], entered[s], subRng, out int inIdx, out int outIdx))
                            {
                                r.Subs[s]++;
                                weightSum[s] = SumWeights(teams[s], active[s]);
                                var pin = teams[s].Players[inIdx]; var pout = teams[s].Players[outIdx];
                                r.Events?.Add(new SimEvent(minute, added, s, "sub", pin.Id, pin.Name, r.Goals[0], r.Goals[1], pout.Id, pout.Name));
                            }

            for (int s = 0; s < 2; s++)
            {
                var t = teams[s];
                int o = 1 - s;
                int diff = r.Goals[s] - r.Goals[o];
                // Skor etkisi: gerideki takım biraz daha bastırır, öndeki biraz çekilir. Değerler bilerek küçük tutuldu:
                // ilk hali (1,10 / 1,20 / 0,93) beraberliği gerçeğe göre yaklaşık 3 puan fazla üretiyordu (simcheck ile ölçüldü).
                double state = diff < 0 ? (minute > 60 ? TrailingLate : Trailing) : diff > 0 ? Leading : 1.0;
                double manMod = 1.0;
                if (r.Reds[s] > 0) manMod *= 0.75;
                if (r.Reds[o] > 0) manMod *= 1.15;
                double sign = s == 0 ? 1 : -1;

                if (rng.NextDouble() < t.Shots * w * state * manMod)
                {
                    r.Shots[s]++;
                    int pi = PickPlayer(t, active[s], weightSum[s], rng);
                    var pl = pi >= 0 ? t.Players[pi] : null;
                    if (rng.NextDouble() < conv[s])
                    {
                        r.Goals[s]++;
                        r.OnTarget[s]++;
                        r.GoalList.Add(new GoalRec(s, minute, pi));
                        if (raw != null) raw[slot] += sign * 4;
                        r.Events?.Add(new SimEvent(minute, added, s, "goal", pl?.Id, pl?.Name, r.Goals[0], r.Goals[1]));
                    }
                    else if (rng.NextDouble() < 0.30)
                    {
                        r.OnTarget[s]++;
                        if (raw != null) raw[slot] += sign * 2;
                        r.Events?.Add(new SimEvent(minute, added, s, "shot_on", pl?.Id, pl?.Name, r.Goals[0], r.Goals[1]));
                    }
                    else
                    {
                        if (raw != null) raw[slot] += sign * 1;
                        r.Events?.Add(new SimEvent(minute, added, s, "shot_off", pl?.Id, pl?.Name, r.Goals[0], r.Goals[1]));
                    }
                }

                if (rng.NextDouble() < t.Corners * w * state * manMod)
                {
                    r.Corners[s]++;
                    if (raw != null) raw[slot] += sign * 0.6;
                    r.Events?.Add(new SimEvent(minute, added, s, "corner", null, null, r.Goals[0], r.Goals[1]));
                }

                if (rng.NextDouble() < t.Yellows * w)
                {
                    r.Yellows[s]++;
                    int pi = PickUniform(active[s], rng);
                    var pl = pi >= 0 ? t.Players[pi] : null;
                    r.Events?.Add(new SimEvent(minute, added, s, "yellow", pl?.Id, pl?.Name, r.Goals[0], r.Goals[1]));
                }

                if (r.Reds[s] == 0 && rng.NextDouble() < 0.04 * w)
                {
                    r.Reds[s]++;
                    int pi = PickUniform(active[s], rng);
                    // Kaleci atılırsa yerine kaleci gelmesi gerekir; basitlik için kırmızı kart saha oyuncusuna yazılır
                    if (pi >= 0 && IsKeeper(t, pi)) pi = active[s].FirstOrDefault(i => !IsKeeper(t, i), pi);
                    var pl = pi >= 0 ? t.Players[pi] : null;
                    if (pi >= 0 && t.StartingCount >= 0)
                    {
                        active[s].Remove(pi);            // atılan oyuncu artık olaya giremez
                        weightSum[s] = SumWeights(t, active[s]);
                    }
                    r.Events?.Add(new SimEvent(minute, added, s, "red", pl?.Id, pl?.Name, r.Goals[0], r.Goals[1]));
                }
            }
        }

        if (raw != null)
        {
            // Baskı zamanla söner: üstel yumuşatma
            var m = new double[raw.Length];
            double acc = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                acc = acc * 0.82 + raw[i];
                m[i] = Math.Round(acc, 2);
            }
            r.Momentum = m;
        }
        return r;
    }

    /// <summary>
    /// Takımın değişiklik planı: çoğu takım 4-5 değişiklik yapar, en fazla üç ayrı anda (devre arası ayrıca),
    /// büyük kısmı 58-86. dakikalar arasında.
    /// </summary>
    private static List<(int Slot, int Count)> PlanSubstitutions(int benchCount, Random rng)
    {
        var plan = new List<(int, int)>();
        if (benchCount <= 0) return plan;
        double a = rng.NextDouble();
        int total = Math.Min(benchCount, a < 0.12 ? 3 : a < 0.42 ? 4 : MaxSubs);
        int windows = Math.Min(total, rng.NextDouble() < 0.35 ? 2 : 3);
        var minutes = new SortedSet<int>();
        if (rng.NextDouble() < 0.2) minutes.Add(46);                      // devre arası değişikliği
        while (minutes.Count < windows)
        {
            // 58-86 arası, ortası (yaklaşık 70) daha olası
            int minute = 58 + (int)Math.Round((rng.NextDouble() + rng.NextDouble()) / 2 * 28);
            minutes.Add(minute);
        }
        var counts = new int[minutes.Count];
        for (int i = 0; i < counts.Length; i++) counts[i] = 1;
        for (int left = total - counts.Length; left > 0; left--) counts[rng.Next(counts.Length)]++;
        int k = 0;
        foreach (int minute in minutes) plan.Add((minute - 1 + FirstHalfAdded, counts[k++]));
        return plan;
    }

    /// <summary>Bir yedeği oyuna alır; mümkünse aynı mevkideki bir oyuncunun yerine. Kaleci değiştirilmez.</summary>
    private static bool Substitute(SimTeam t, List<int> active, List<int> bench, HashSet<int> entered, Random rng, out int inIdx, out int outIdx)
    {
        inIdx = outIdx = -1;
        var candidatesIn = bench.Where(i => !IsKeeper(t, i)).ToList();
        if (candidatesIn.Count == 0) return false;
        int chosenIn = candidatesIn[rng.Next(candidatesIn.Count)];
        var outfield = active.Where(i => !IsKeeper(t, i) && !entered.Contains(i)).ToList();
        if (outfield.Count == 0) return false;
        string pos = t.Players[chosenIn].Pos;
        var same = outfield.Where(i => t.Players[i].Pos == pos).ToList();
        var pool = same.Count > 0 && pos != "" ? same : outfield;
        int chosenOut = pool[rng.Next(pool.Count)];
        bench.Remove(chosenIn);
        entered.Add(chosenIn);
        active[active.IndexOf(chosenOut)] = chosenIn;
        inIdx = chosenIn; outIdx = chosenOut;
        return true;
    }

    // Mevki bilgisi yoksa listenin ilk oyuncusu kaleci sayılır
    private static bool IsKeeper(SimTeam t, int index) => t.Players[index].Pos == "KL" || (t.Players[index].Pos == "" && index == 0 && t.StartingCount >= 0);

    private static double SumWeights(SimTeam t, List<int> active)
    {
        double sum = 0;
        foreach (int i in active) sum += t.Players[i].Weight;
        return sum;
    }

    private static int PickPlayer(SimTeam t, List<int> active, double sum, Random rng)
    {
        if (active.Count == 0 || sum <= 0) return -1;
        double x = rng.NextDouble() * sum;
        foreach (int i in active)
        {
            x -= t.Players[i].Weight;
            if (x <= 0) return i;
        }
        return active[^1];
    }

    private static int PickUniform(List<int> active, Random rng) =>
        active.Count == 0 ? -1 : active[rng.Next(active.Count)];
}
