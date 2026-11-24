using System.Globalization;

namespace SporAnaliz.Core.Analysis;

/// <summary>
/// Maç analizi: güç modeli + 10.000 simülasyon + temsili 90 dakika + şablon yorumlar.
/// Genel güç Elo tablosundan, oyun karakteri (hücum/savunma dengesi, şut, top hakimiyeti) maç verisinden gelir.
/// </summary>
public static class Analyzer
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] Buckets = { "1-15", "16-30", "31-45", "46-60", "61-75", "76-90+" };

    public static AnalysisResult Compute(AnalysisInput inp)
    {
        var m = inp.Match;
        int H = m.HomeTeamId, A = m.AwayTeamId, N = Math.Max(100, inp.Simulations);
        var th = inp.Teams[H]; var ta = inp.Teams[A];

        // 1) Güç: veri → karakter, Elo → genel seviye
        var fit = StrengthModel.Fit(inp.History, m.MatchDate, inp.Groups, inp.Model);
        // Elo tablosu varsa genel seviye oradan gelir; yoksa (gerçek veri) doğrudan maç sonuçlarından hesaplanır.
        var att = new Dictionary<int, double>(); var def = new Dictionary<int, double>(); var str = new Dictionary<int, double>();
        foreach (int id in fit.Att.Keys.Append(H).Append(A).Distinct())
        {
            double la = Math.Log(fit.AttOf(id)), ld = Math.Log(fit.DefOf(id));
            double style = (la + ld) / 2;
            double elo = inp.Teams.TryGetValue(id, out var tr) ? tr.Elo : 0;
            double s = elo > 0 ? StrengthModel.EloToStrength(elo) : (la - ld) / 2;
            att[id] = Math.Exp(s + style);
            def[id] = Math.Exp(-s + style);
            str[id] = s;
        }
        double Rating(int id) => inp.Teams[id].Elo > 0 ? inp.Teams[id].Elo : Math.Round(StrengthModel.StrengthToElo(str[id]));
        double lamH = fit.Mu * fit.HomeAdv * att[H] * def[A];
        double lamA = fit.Mu * att[A] * def[H];

        // 2) İstatistik beklentileri
        var sh = inp.Stats.GetValueOrDefault(H) ?? new TeamStatAvg { TeamId = H };
        var sa = inp.Stats.GetValueOrDefault(A) ?? new TeamStatAvg { TeamId = A };
        double gShots = inp.Stats.Count > 0 ? inp.Stats.Values.Average(x => x.ShotsFor) : 12;

        // İstatistikler lig içi rakiplere karşı toplandığı için rakip gücüne göre düzeltilir;
        // yoksa zayıf ligde oynayan takım, güçlü ligdeki rakibinden üstün görünür.
        var windowStart = m.MatchDate.AddDays(-550);
        var agg = new Dictionary<int, (double Gf, double Ga, double OppS, int N)>();
        void Acc(int team, int opp, int gf, int ga)
        {
            var a = agg.GetValueOrDefault(team);
            agg[team] = (a.Gf + gf, a.Ga + ga, a.OppS + str.GetValueOrDefault(opp), a.N + 1);
        }
        double totGoals = 0; int totTeamMatches = 0;
        foreach (var x in inp.History)
        {
            if (x.MatchDate < windowStart) continue;
            Acc(x.HomeTeamId, x.AwayTeamId, x.HomeGoals, x.AwayGoals);
            Acc(x.AwayTeamId, x.HomeTeamId, x.AwayGoals, x.HomeGoals);
            totGoals += x.HomeGoals + x.AwayGoals; totTeamMatches += 2;
        }
        double gConv = totTeamMatches > 0 ? totGoals / totTeamMatches / gShots : 0.11;

        double PossRating(int team)
        {
            double s = str.GetValueOrDefault(team);
            if (!inp.Stats.TryGetValue(team, out var st) || !agg.TryGetValue(team, out var a) || a.N == 0)
                return 50 + StrengthModel.PossessionPerStrength * s;
            return 50 + 2 * (st.Possession - 50) + StrengthModel.PossessionPerStrength * a.OppS / a.N;
        }
        double ExpectedShots(int team, int opp, TeamStatAvg st, TeamStatAvg so, double lam, double venue)
        {
            double volume = st.ShotsFor * so.ShotsAgainst / gShots * venue;
            double conv = gConv;
            if (agg.TryGetValue(team, out var a) && agg.TryGetValue(opp, out var o) && a.N > 0 && o.N > 0)
                conv = (a.Gf / a.N / Math.Max(st.ShotsFor, 1)) * (o.Ga / o.N / Math.Max(so.ShotsAgainst, 1)) / gConv;
            double byConversion = lam / Math.Clamp(conv, 0.05, 0.25);
            return Math.Clamp(Math.Sqrt(volume * byConversion), lam / 0.30, 30);
        }
        double shotsH = ExpectedShots(H, A, sh, sa, lamH, 1.04);
        double shotsA = ExpectedShots(A, H, sa, sh, lamA, 0.96);
        double possH = Math.Clamp(50 + (PossRating(H) - PossRating(A)) / 2 + 1.5, 30, 70);

        // Kadro biliniyorsa gol atacak oyuncu havuzu ona göre ağırlıklandırılır: ilk 11 tam, yedekler düşük
        var homePlayers = MergeSquad(inp.HomePlayers, inp.Lineups?.Home);
        var awayPlayers = MergeSquad(inp.AwayPlayers, inp.Lineups?.Away);

        var simH = new SimTeam
        {
            Lambda = lamH, Shots = shotsH, Corners = sh.Corners * shotsH / Math.Max(sh.ShotsFor, 1),
            Yellows = sh.Yellows, Possession = possH, Players = ToSimPlayers(homePlayers), StartingCount = StartingCount(homePlayers, inp.Lineups?.Home)
        };
        var simA = new SimTeam
        {
            Lambda = lamA, Shots = shotsA, Corners = sa.Corners * shotsA / Math.Max(sa.ShotsFor, 1),
            Yellows = sa.Yellows, Possession = 100 - possH, Players = ToSimPlayers(awayPlayers), StartingCount = StartingCount(awayPlayers, inp.Lineups?.Away)
        };

        // 3) Simülasyonlar
        int baseSeed = unchecked(m.Id * 7919 + 17);
        var scoreCount = new int[10, 10];
        var bucketGoals = new double[2, 6];
        int[] outcome = new int[3], first = new int[3];
        double[] sumG = new double[2], sumS = new double[2], sumT = new double[2], sumC = new double[2], sumY = new double[2];
        var scored = new[] { new int[simH.Players.Count], new int[simA.Players.Count] };
        var lastSim = new[] { Enumerable.Repeat(-1, simH.Players.Count).ToArray(), Enumerable.Repeat(-1, simA.Players.Count).ToArray() };
        var simGoals = new (byte h, byte a)[N];
        var simShots = new (byte h, byte a)[N];
        var simReds = new byte[N];

        for (int i = 0; i < N; i++)
        {
            var r = MatchSimulator.Run(simH, simA, baseSeed + i, record: false);
            int gh = r.Goals[0], ga = r.Goals[1];
            scoreCount[Math.Min(gh, 9), Math.Min(ga, 9)]++;
            outcome[gh > ga ? 0 : gh == ga ? 1 : 2]++;
            first[r.GoalList.Count == 0 ? 2 : r.GoalList[0].Side]++;
            for (int s = 0; s < 2; s++)
            {
                sumG[s] += r.Goals[s]; sumS[s] += r.Shots[s]; sumT[s] += r.OnTarget[s];
                sumC[s] += r.Corners[s]; sumY[s] += r.Yellows[s];
            }
            foreach (var g in r.GoalList)
            {
                bucketGoals[g.Side, Math.Min(5, (g.Minute - 1) / 15)]++;
                if (g.PlayerIndex >= 0 && lastSim[g.Side][g.PlayerIndex] != i)
                {
                    lastSim[g.Side][g.PlayerIndex] = i;
                    scored[g.Side][g.PlayerIndex]++;
                }
            }
            simGoals[i] = ((byte)Math.Min(gh, 9), (byte)Math.Min(ga, 9));
            simShots[i] = ((byte)Math.Min(r.Shots[0], 255), (byte)Math.Min(r.Shots[1], 255));
            simReds[i] = (byte)(r.Reds[0] + r.Reds[1]);
        }

        double Pct(double c) => Math.Round(100.0 * c / N, 1);
        var probs = new Outcome(Pct(outcome[0]), Pct(outcome[1]), Pct(outcome[2]));

        var scores = new List<ScoreProb>();
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
                if (scoreCount[x, y] > 0) scores.Add(new ScoreProb(x, y, Pct(scoreCount[x, y])));
        var allScores = scores.OrderByDescending(x => x.Pct).ToList();
        scores = allScores.Take(6).ToList();

        var expected = new List<PairStat>
        {
            new("Beklenen gol", R2(sumG[0] / N), R2(sumG[1] / N)),
            new("Şut", R1(sumS[0] / N), R1(sumS[1] / N)),
            new("İsabetli şut", R1(sumT[0] / N), R1(sumT[1] / N)),
            new("Korner", R1(sumC[0] / N), R1(sumC[1] / N)),
            new("Sarı kart", R1(sumY[0] / N), R1(sumY[1] / N)),
            new("Top hakimiyeti (%)", Math.Round(possH), Math.Round(100 - possH)),
        };
        var timing = Buckets.Select((b, k) => new PairStat(b, Math.Round(bucketGoals[0, k] / N, 3), Math.Round(bucketGoals[1, k] / N, 3))).ToList();

        var scorers = new List<ScorerProb>();
        void AddScorers(string side, IReadOnlyList<PlayerGoals> ps, int[] cnt)
        {
            scorers.AddRange(ps.Select((p, k) => new ScorerProb(side, p.Name, p.Pos, p.Goals, Pct(cnt[k])))
                .OrderByDescending(x => x.Pct).Take(4));
        }
        AddScorers("home", homePlayers, scored[0]);
        AddScorers("away", awayPlayers, scored[1]);

        // 4) Temsili maç: en olası skoru veren, şut sayıları ortalamaya en yakın simülasyon
        // Tek tek skorlarda 1-1 neredeyse her dengeli maçta birinci çıkar; bu yüzden skorlar sonuca göre ayrılır.
        ScoreProb Best(Func<ScoreProb, bool> match) => allScores.FirstOrDefault(match) ?? allScores[0];
        var bestHome = Best(s => s.Home > s.Away); var bestDraw = Best(s => s.Home == s.Away); var bestAway = Best(s => s.Home < s.Away);
        // Temsili maç: en olası SONUCUN en olası skoru (üstteki yüzdelerle çelişmesin diye)
        var top = probs.Home >= probs.Draw && probs.Home >= probs.Away ? bestHome : probs.Away >= probs.Draw ? bestAway : bestDraw;
        double avgSh = sumS[0] / N, avgSa = sumS[1] / N;
        int best = 0; double bestCost = double.MaxValue;
        for (int i = 0; i < N; i++)
        {
            if (simGoals[i].h != top.Home || simGoals[i].a != top.Away) continue;
            double cost = Math.Abs(simShots[i].h - avgSh) + Math.Abs(simShots[i].a - avgSa) + simReds[i] * 6;
            if (cost < bestCost) { bestCost = cost; best = i; }
        }
        var rep = MatchSimulator.Run(simH, simA, baseSeed + best, record: true);
        var playback = new Playback(rep.Goals[0], rep.Goals[1],
            rep.Events!.Select(e => new PlayEvent(SlotIndex(e.Minute, e.Added), e.Minute, e.Added, e.Side == 0 ? "home" : "away", e.Type, e.PlayerId, e.Player, e.HomeGoals, e.AwayGoals, e.OutPlayerId, e.OutPlayer)).ToList(),
            rep.Momentum!,
            new List<PairStat>
            {
                new("Şut", rep.Shots[0], rep.Shots[1]), new("İsabetli şut", rep.OnTarget[0], rep.OnTarget[1]),
                new("Korner", rep.Corners[0], rep.Corners[1]), new("Sarı kart", rep.Yellows[0], rep.Yellows[1]),
                new("Kırmızı kart", rep.Reds[0], rep.Reds[1]), new("Oyuncu değişikliği", rep.Subs[0], rep.Subs[1]),
            });

        // 5) Form, aralarındaki maçlar, karşılaştırma
        var formH = Form(inp, H, 5); var formA = Form(inp, A, 5);
        var h2h = inp.History.Where(x => (x.HomeTeamId == H && x.AwayTeamId == A) || (x.HomeTeamId == A && x.AwayTeamId == H))
            .OrderByDescending(x => x.MatchDate).Take(5)
            .Select(x => new H2HItem(x.MatchDate, inp.Teams[x.HomeTeamId].Name, inp.Teams[x.AwayTeamId].Name, x.HomeGoals, x.AwayGoals)).ToList();

        var since = m.MatchDate.AddDays(-365);
        double ppgHome = Ppg(inp.History.Where(x => x.HomeTeamId == H && x.MatchDate >= since), true);
        double ppgAway = Ppg(inp.History.Where(x => x.AwayTeamId == A && x.MatchDate >= since), false);
        var allAtt = att.Values.ToList(); var allDefInv = def.Values.Select(v => 1 / v).ToList();
        var allPoss = att.Keys.Select(PossRating).ToList();
        var comparison = new List<PairStat>
        {
            new("Hücum", Pctl(allAtt, att[H]), Pctl(allAtt, att[A])),
            new("Savunma", Pctl(allDefInv, 1 / def[H]), Pctl(allDefInv, 1 / def[A])),
            new("Form (son 5)", Math.Round(FormPoints(formH) / 15.0 * 100), Math.Round(FormPoints(formA) / 15.0 * 100)),
            new("Saha (iç / deplasman)", Math.Round(ppgHome / 3 * 100), Math.Round(ppgAway / 3 * 100)),
            new("Top hakimiyeti", Pctl(allPoss, PossRating(H)), Pctl(allPoss, PossRating(A))),
        };

        // 6) Şablon yorumlar
        var comments = new List<string>
        {
            $"{N.ToString("N0", Tr)} simülasyon: {th.Name} %{F1(probs.Home)}, beraberlik %{F1(probs.Draw)}, {ta.Name} %{F1(probs.Away)}."
        };
        double gap = Math.Abs(probs.Home - probs.Away);
        string fav = probs.Home >= probs.Away ? th.Name : ta.Name;
        comments.Add(gap < 8 ? "Model iki takımı birbirine çok yakın görüyor; maçı küçük detaylar belirleyebilir."
            : gap < 25 ? $"Model {fav} tarafını bir adım önde görüyor, ancak sürpriz payı yüksek."
            : $"Model {fav} tarafını net favori görüyor.");
        comments.Add($"{th.Name} kazanırsa en olası skor {bestHome.Home}-{bestHome.Away}, {ta.Name} kazanırsa {bestAway.Home}-{bestAway.Away}, beraberlikte {bestDraw.Home}-{bestDraw.Away}. " +
                     $"Beklenen gol: {th.Name} {F2(lamH)}, {ta.Name} {F2(lamA)}.");
        if (formH.Count > 0 && formA.Count > 0)
            comments.Add($"Son {formH.Count} maç: {th.Name} {formH.Count(f => f.Result == "G")} galibiyet, {formH.Sum(f => f.GoalsFor)} gol; " +
                         $"{ta.Name} {formA.Count(f => f.Result == "G")} galibiyet, {formA.Sum(f => f.GoalsFor)} gol.");
        int peak = Enumerable.Range(0, 6).OrderByDescending(k => bucketGoals[0, k] + bucketGoals[1, k]).First();
        comments.Add($"Simülasyonlarda goller en çok {Buckets[peak]} dakikaları arasında geldi.");
        var topScorer = scorers.OrderByDescending(x => x.Pct).FirstOrDefault();
        if (topScorer != null)
            comments.Add($"Gol atması en olası isim {topScorer.Player} ({(topScorer.Side == "home" ? th.Name : ta.Name)}): simülasyonların %{F1(topScorer.Pct)} kadarında gol attı.");
        if (first[2] < N)
            comments.Add($"İlk golü atan taraf: {th.Name} %{F1(Pct(first[0]))}, {ta.Name} %{F1(Pct(first[1]))}.");

        return new AnalysisResult
        {
            TestData = inp.TestData,
            Simulations = N,
            Match = new MatchInfo(m.Id, m.CompetitionName, m.Season, m.MatchDate,
                new TeamInfo(H, th.Name, th.ShortName, th.Color, Rating(H), th.Elo > 0 && th.EloApprox),
                new TeamInfo(A, ta.Name, ta.ShortName, ta.Color, Rating(A), ta.Elo > 0 && ta.EloApprox),
                m.Status == "played" ? m.HomeGoals : null, m.Status == "played" ? m.AwayGoals : null),
            Model = new ModelInfo(R2(lamH), R2(lamA), R2(fit.HomeAdv), R2(att[H]), R2(def[H]), R2(att[A]), R2(def[A])),
            Probabilities = probs,
            TopScores = scores,
            AllScores = allScores,
            Expected = expected,
            GoalTiming = timing,
            FirstGoal = new FirstGoal(Pct(first[0]), Pct(first[1]), Pct(first[2])),
            Scorers = scorers,
            Comparison = comparison,
            HomeForm = formH,
            AwayForm = formA,
            HeadToHead = h2h,
            Comments = comments,
            Lineups = inp.Lineups,
            Simulation = playback,
        };
    }

    public static int SlotIndex(int minute, int added)
    {
        if (minute <= 45 && added == 0) return minute - 1;
        if (minute == 45) return 44 + added;
        if (added == 0) return minute - 1 + MatchSimulator.FirstHalfAdded;
        return 89 + MatchSimulator.FirstHalfAdded + added;
    }

    // Oyuncunun olaya girme ağırlığı: son dönemdeki golleri + mevkiine göre taban.
    // Yedekler için ayrı bir indirim yoktur: simülasyonda oyuna ancak değişiklikle girdikleri için payları kendiliğinden düşer.
    private static List<SimPlayer> ToSimPlayers(IReadOnlyList<PlayerGoals> ps) => ps.Select(p => new SimPlayer(p.Id, p.Name,
        p.Goals + p.Pos switch { "FV" => 3.0, "OS" => 1.5, "DF" => 0.4, _ => 0.0 }, p.Pos)).ToList();

    /// <summary>Listenin başındaki ilk 11 sayısı (MergeSquad önce ilk 11'i, sonra yedekleri dizer). Kadro yoksa -1.</summary>
    private static int StartingCount(IReadOnlyList<PlayerGoals> ps, TeamLineup? lineup)
    {
        if (lineup == null) return -1;
        var starting = lineup.Starting.Select(p => p.Id).ToHashSet();
        return ps.TakeWhile(p => starting.Contains(p.Id)).Count();
    }

    /// <summary>
    /// Maçta yer alabilecek oyuncular. Kadro biliniyorsa YALNIZCA kadrodakiler (ilk 11 + yedekler) döner;
    /// takımın geniş kadrosunda olup maç kadrosunda olmayan oyuncu hiçbir olaya giremez.
    /// Kadroda olup oyuncu listesinde bulunmayanlar (yeni transfer vb.) golsüz olarak eklenir.
    /// </summary>
    private static List<PlayerGoals> MergeSquad(IReadOnlyList<PlayerGoals> ps, TeamLineup? lineup)
    {
        if (lineup == null) return ps.ToList();
        var known = new Dictionary<int, PlayerGoals>();
        foreach (var p in ps) known.TryAdd(p.Id, p);
        var list = new List<PlayerGoals>();
        var seen = new HashSet<int>();
        foreach (var p in lineup.Starting.Concat(lineup.Bench))
            // Ad her yerde kadro listesindeki haliyle görünür (aynı oyuncu iki farklı yazımla çıkmasın)
            if (seen.Add(p.Id)) list.Add(new PlayerGoals { Id = p.Id, Name = p.Name, Pos = known.TryGetValue(p.Id, out var k) ? k.Pos : p.Pos, Goals = k?.Goals ?? 0 });
        return list;
    }

    private static List<FormItem> Form(AnalysisInput inp, int team, int n) => inp.History
        .Where(x => x.HomeTeamId == team || x.AwayTeamId == team)
        .OrderByDescending(x => x.MatchDate).Take(n)
        .Select(x =>
        {
            bool home = x.HomeTeamId == team;
            int gf = home ? x.HomeGoals : x.AwayGoals, ga = home ? x.AwayGoals : x.HomeGoals;
            return new FormItem(x.MatchDate, inp.Teams[home ? x.AwayTeamId : x.HomeTeamId].Name,
                home ? "İç saha" : "Deplasman", gf, ga, gf > ga ? "G" : gf == ga ? "B" : "M");
        }).ToList();

    private static int FormPoints(List<FormItem> f) => f.Sum(x => x.Result == "G" ? 3 : x.Result == "B" ? 1 : 0);

    private static double Ppg(IEnumerable<MatchRow> ms, bool home)
    {
        int n = 0, pts = 0;
        foreach (var x in ms)
        {
            int gf = home ? x.HomeGoals : x.AwayGoals, ga = home ? x.AwayGoals : x.HomeGoals;
            pts += gf > ga ? 3 : gf == ga ? 1 : 0; n++;
        }
        return n == 0 ? 1.3 : (double)pts / n;
    }

    private static double Pctl(List<double> all, double v) =>
        all.Count == 0 ? 50 : Math.Round(100.0 * all.Count(x => x <= v) / all.Count);

    private static double R1(double v) => Math.Round(v, 1);
    private static double R2(double v) => Math.Round(v, 2);
    private static string F1(double v) => v.ToString("0.#", Tr);
    private static string F2(double v) => v.ToString("0.00", Tr);
}
