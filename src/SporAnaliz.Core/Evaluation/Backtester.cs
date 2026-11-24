namespace SporAnaliz.Core.Evaluation;

public sealed record BacktestRange(DateTime From, DateTime To, int StepDays = 30, int HistoryYears = 4);

public sealed record GroupScore(string Name, int Matches, double Accuracy, double LogLoss);
public sealed record CountryBias(string Country, int Matches, double ExpectedPoints, double ActualPoints, double ExpectedGoalDiff, double ActualGoalDiff);
public sealed record CalibrationBin(double Predicted, double Actual, int Matches);

public sealed class BacktestResult
{
    public int Matches { get; init; }
    /// <summary>En olası dediği sonucun tutma oranı.</summary>
    public double Accuracy { get; init; }
    /// <summary>Sonuç (ev/beraberlik/deplasman) log-kaybı; düşük olan iyi.</summary>
    public double LogLoss { get; init; }
    /// <summary>Hiçbir şey bilmeyen tahminin (genel oranlar) log-kaybı.</summary>
    public double BaselineLogLoss { get; init; }
    /// <summary>Sıralı olasılık skoru (RPS); düşük olan iyi.</summary>
    public double Rps { get; init; }
    /// <summary>Tam skorun log-kaybı; düşük olan iyi.</summary>
    public double ScoreLogLoss { get; init; }
    public double PredictedGoals { get; init; }
    public double ActualGoals { get; init; }
    public double PredictedDraw { get; init; }
    public double ActualDraw { get; init; }
    public List<CalibrationBin> Calibration { get; init; } = new();
    public List<GroupScore> ByCompetition { get; init; } = new();
    public List<CountryBias> ByCountry { get; init; } = new();
}

/// <summary>
/// Geçmişe dönük sınama: belirli aralıklarla, yalnızca o güne kadarki maçlarla model kurulur ve
/// sonraki dönemin maçları tahmin edilip gerçek sonuçlarla karşılaştırılır. Model hiçbir zaman geleceği görmez.
/// </summary>
public static class Backtester
{
    private static readonly HashSet<string> TrackedCountries = new() { "Türkiye", "İngiltere", "İspanya", "Almanya", "İtalya", "Fransa", "Portekiz", "Hollanda" };

    public static BacktestResult Run(IReadOnlyList<MatchRow> allMatches, IReadOnlyDictionary<int, TeamRow> teams,
        IReadOnlyDictionary<int, int>? groups, ModelOptions model, BacktestRange range, IReadOnlyDictionary<int, string>? competitionNames = null)
    {
        var all = allMatches.OrderBy(m => m.MatchDate).ToList();
        int n = 0, hit = 0;
        double ll = 0, llBase = 0, rps = 0, sll = 0, predGoals = 0, actGoals = 0, predDraw = 0, actDraw = 0;
        var calib = new (int N, double P, double W)[5];
        var comp = new Dictionary<int, (int N, int Hit, double Ll)>();
        var country = new Dictionary<string, (int N, double ExpPts, double ActPts, double ExpGd, double ActGd)>();
        // Taban çizgisi: tüm maçlardaki ev / beraberlik / deplasman oranı
        double bh = all.Average(m => m.HomeGoals > m.AwayGoals ? 1.0 : 0), bd = all.Average(m => m.HomeGoals == m.AwayGoals ? 1.0 : 0), ba = 1 - bh - bd;

        for (var cut = range.From; cut < range.To; cut = cut.AddDays(range.StepDays))
        {
            var until = cut.AddDays(range.StepDays) < range.To ? cut.AddDays(range.StepDays) : range.To;
            var since = cut.AddYears(-range.HistoryYears);
            var train = all.Where(m => m.MatchDate < cut && m.MatchDate >= since).ToList();
            var test = all.Where(m => m.MatchDate >= cut && m.MatchDate < until).ToList();
            if (test.Count == 0 || train.Count < 500) continue;
            var fit = StrengthModel.Fit(train, cut, groups, model);

            foreach (var m in test)
            {
                if (!fit.Att.ContainsKey(m.HomeTeamId) || !fit.Att.ContainsKey(m.AwayTeamId)) continue;
                var (lh, la) = fit.Lambdas(m.HomeTeamId, m.AwayTeamId);
                var (ph, pd, pa) = PoissonMath.Outcome(lh, la);
                int res = m.HomeGoals > m.AwayGoals ? 0 : m.HomeGoals == m.AwayGoals ? 1 : 2;
                double p = res == 0 ? ph : res == 1 ? pd : pa, loss = -Math.Log(Math.Max(p, 1e-9));
                bool correct = (ph >= pd && ph >= pa ? 0 : pa >= pd ? 2 : 1) == res;
                ll += loss; llBase += -Math.Log(res == 0 ? bh : res == 1 ? bd : ba);
                if (correct) hit++;
                double oh = res == 0 ? 1 : 0, od = res == 1 ? 1 : 0;
                rps += (Math.Pow(ph - oh, 2) + Math.Pow(ph + pd - oh - od, 2)) / 2;
                sll += -Math.Log(Math.Max(PoissonMath.Score(lh, la, m.HomeGoals, m.AwayGoals), 1e-6));
                predGoals += lh + la; actGoals += m.HomeGoals + m.AwayGoals;
                predDraw += pd; actDraw += od;
                n++;

                double fav = Math.Max(ph, pa); int bin = Math.Min(4, (int)((fav - 0.3) / 0.12));
                if (bin >= 0) calib[bin] = (calib[bin].N + 1, calib[bin].P + fav, calib[bin].W + ((ph >= pa ? 0 : 2) == res ? 1 : 0));

                var c = comp.GetValueOrDefault(m.CompetitionId);
                comp[m.CompetitionId] = (c.N + 1, c.Hit + (correct ? 1 : 0), c.Ll + loss);

                // Ülke dışı rakibe karşı beklenen / gerçek: liglerin birbirine göre doğru ölçeklenip ölçeklenmediğini gösterir
                string ch = teams.TryGetValue(m.HomeTeamId, out var th) ? th.Country : "", ca = teams.TryGetValue(m.AwayTeamId, out var ta) ? ta.Country : "";
                if (ch == ca || ch == "" || ca == "") continue;
                void Add(string key, double expPts, int gf, int ga, double expGd)
                {
                    if (!TrackedCountries.Contains(key)) return;
                    var g = country.GetValueOrDefault(key);
                    country[key] = (g.N + 1, g.ExpPts + expPts, g.ActPts + (gf > ga ? 3 : gf == ga ? 1 : 0), g.ExpGd + expGd, g.ActGd + gf - ga);
                }
                Add(ch, 3 * ph + pd, m.HomeGoals, m.AwayGoals, lh - la);
                Add(ca, 3 * pa + pd, m.AwayGoals, m.HomeGoals, la - lh);
            }
        }

        double d = Math.Max(n, 1);
        return new BacktestResult
        {
            Matches = n, Accuracy = hit / d, LogLoss = ll / d, BaselineLogLoss = llBase / d, Rps = rps / d, ScoreLogLoss = sll / d,
            PredictedGoals = predGoals / d, ActualGoals = actGoals / d, PredictedDraw = predDraw / d, ActualDraw = actDraw / d,
            Calibration = calib.Where(c => c.N > 0).Select(c => new CalibrationBin(c.P / c.N, c.W / c.N, c.N)).ToList(),
            ByCompetition = comp.OrderByDescending(c => c.Value.N).Select(c => new GroupScore(
                competitionNames != null && competitionNames.TryGetValue(c.Key, out var name) ? name : "Turnuva " + c.Key,
                c.Value.N, (double)c.Value.Hit / c.Value.N, c.Value.Ll / c.Value.N)).ToList(),
            ByCountry = country.OrderByDescending(c => c.Value.N).Select(c => new CountryBias(c.Key, c.Value.N,
                c.Value.ExpPts / c.Value.N, c.Value.ActPts / c.Value.N, c.Value.ExpGd / c.Value.N, c.Value.ActGd / c.Value.N)).ToList(),
        };
    }
}
