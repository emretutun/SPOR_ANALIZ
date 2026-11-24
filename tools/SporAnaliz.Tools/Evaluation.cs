using System.Globalization;

namespace SporAnaliz.Tools;

/// <summary>Model ölçüm komutları: geçmiş sınaması, ayar arama, formül–simülasyon karşılaştırması.</summary>
public static class Evaluation
{
    private static readonly DateTime Start = new(2024, 8, 1);
    // Ayarlar eğitim döneminde seçilir, doğrulama döneminde sınanır: aynı maçlara bakıp ezberlemeyi önler
    private static readonly DateTime Split = new(2025, 12, 1);

    private sealed record Loaded(FeedSource Source, List<MatchRow> Matches, Dictionary<int, TeamRow> Teams,
        IReadOnlyDictionary<int, int>? Groups, Dictionary<int, string> Competitions);

    private static Loaded Load(DataOptions options)
    {
        var src = new FeedSource(options);
        return new Loaded(src, src.GetHistory(DateTime.Today.AddDays(1), years: 10), src.GetTeams(), src.GetTeamGroups(),
            src.GetCompetitions().ToDictionary(c => c.Id, c => c.Name));
    }

    /// <summary>half=270 prior=4 pull=0.85 euro=2 shot=0.5 gibi değerlerle varsayılan ayarları ezer.</summary>
    public static ModelOptions ParseModel(string[] args)
    {
        var m = new ModelOptions();
        foreach (var a in args)
        {
            var kv = a.Split('=', 2);
            if (kv.Length != 2 || !double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) continue;
            m = kv[0] switch
            {
                "half" => m with { HalfLifeDays = v }, "prior" => m with { PriorMatches = v }, "pull" => m with { GroupPull = v },
                "euro" => m with { EuropeanWeight = v }, "shot" => m with { ShotWeight = v }, _ => m
            };
        }
        return m;
    }

    private static string Describe(ModelOptions m) =>
        FormattableString.Invariant($"yarı ömür {m.HalfLifeDays} gün · önsel {m.PriorMatches} maç · grup çekimi {m.GroupPull} · Avrupa ağırlığı {m.EuropeanWeight} · şut payı {m.ShotWeight}");

    public static void Backtest(DataOptions options, ModelOptions model)
    {
        var d = Load(options);
        Console.WriteLine($"Ayarlar: {Describe(model)}");
        Console.WriteLine($"Veri: {d.Matches.Count} tamamlanmış maç ({d.Matches.Min(m => m.MatchDate):dd.MM.yyyy} – {d.Matches.Max(m => m.MatchDate):dd.MM.yyyy}), şut verisi olan: {d.Matches.Count(m => m.HomeShotsOn != null)}");
        var r = Backtester.Run(d.Matches, d.Teams, d.Groups, model, new BacktestRange(Start, DateTime.Today), d.Competitions);
        Print(r);
        var train = Backtester.Run(d.Matches, d.Teams, d.Groups, model, new BacktestRange(Start, Split));
        var valid = Backtester.Run(d.Matches, d.Teams, d.Groups, model, new BacktestRange(Split, DateTime.Today));
        Console.WriteLine($"\nDönemlere göre log-kayıp: eğitim {train.LogLoss:0.0000} ({train.Matches} maç) · doğrulama {valid.LogLoss:0.0000} ({valid.Matches} maç)");
    }

    private static void Print(BacktestResult r)
    {
        Console.WriteLine($"\nTahmin edilen maç: {r.Matches}");
        Console.WriteLine($"  Sonuç isabeti      %{100 * r.Accuracy:0.0}");
        Console.WriteLine($"  Log-kayıp (sonuç)  {r.LogLoss:0.0000}   (bilgisiz tahmin {r.BaselineLogLoss:0.0000})");
        Console.WriteLine($"  RPS                {r.Rps:0.0000}");
        Console.WriteLine($"  Log-kayıp (skor)   {r.ScoreLogLoss:0.0000}");
        Console.WriteLine($"  Maç başına gol     tahmin {r.PredictedGoals:0.00} / gerçek {r.ActualGoals:0.00}");
        Console.WriteLine($"  Beraberlik         tahmin %{100 * r.PredictedDraw:0.0} / gerçek %{100 * r.ActualDraw:0.0}");
        Console.WriteLine("Favori olasılığı → gerçekleşme:");
        foreach (var c in r.Calibration) Console.WriteLine($"  tahmin %{100 * c.Predicted:0}  gerçek %{100 * c.Actual:0}  ({c.Matches} maç)");
        if (r.ByCompetition.Count > 0)
        {
            Console.WriteLine("Turnuvaya göre (isabet / log-kayıp):");
            foreach (var c in r.ByCompetition) Console.WriteLine($"  {c.Name,-28} {c.Matches,5} maç | %{100 * c.Accuracy:0.0} | {c.LogLoss:0.000}");
        }
        Console.WriteLine("Ülke dışı rakiplere karşı (maç başına): beklenen puan / gerçek puan | beklenen averaj / gerçek averaj");
        foreach (var c in r.ByCountry)
            Console.WriteLine($"  {c.Country,-10} {c.Matches,4} maç | {c.ExpectedPoints:0.00} / {c.ActualPoints:0.00} | {c.ExpectedGoalDiff:+0.00;-0.00} / {c.ActualGoalDiff:+0.00;-0.00}");
    }

    /// <summary>
    /// Ayarları sırayla tek tek dener (diğerleri sabitken) ve eğitim dönemindeki log-kaybı en düşük olanı seçer;
    /// iki tur atılır. Seçilen ayarlar hiç bakılmamış doğrulama döneminde sınanır.
    /// </summary>
    public static void Tune(DataOptions options)
    {
        var d = Load(options);
        var trainRange = new BacktestRange(Start, Split); var validRange = new BacktestRange(Split, DateTime.Today);
        double Loss(ModelOptions m) => Backtester.Run(d.Matches, d.Teams, d.Groups, m, trainRange).LogLoss;

        var baseline = new ModelOptions();
        var best = baseline; double bestLoss = Loss(best);
        Console.WriteLine($"Başlangıç: {Describe(best)} → eğitim log-kaybı {bestLoss:0.0000}");

        var grid = new (string Name, double[] Values, Func<ModelOptions, double, ModelOptions> Set)[]
        {
            ("şut payı", new[] { 0, 0.2, 0.35, 0.5, 0.65, 0.8 }, (m, v) => m with { ShotWeight = v }),
            ("yarı ömür", new[] { 180.0, 270, 365, 500, 730, 1100 }, (m, v) => m with { HalfLifeDays = v }),
            ("önsel maç", new[] { 2.0, 3.5, 5, 8, 12 }, (m, v) => m with { PriorMatches = v }),
            ("grup çekimi", new[] { 0.5, 0.7, 0.85, 1.0 }, (m, v) => m with { GroupPull = v }),
            ("Avrupa ağırlığı", new[] { 1.0, 1.5, 2, 3, 4 }, (m, v) => m with { EuropeanWeight = v }),
        };
        for (int pass = 1; pass <= 2; pass++)
        {
            Console.WriteLine($"\n--- {pass}. tur");
            foreach (var (name, values, set) in grid)
            {
                var line = new List<string>();
                foreach (var v in values)
                {
                    var cand = set(best, v); double loss = Loss(cand);
                    line.Add(FormattableString.Invariant($"{v}:{loss:0.0000}"));
                    if (loss < bestLoss - 1e-6) { bestLoss = loss; best = cand; }
                }
                Console.WriteLine($"{name,-16} {string.Join("  ", line)}");
            }
            Console.WriteLine($"Tur sonu en iyi: {Describe(best)} → {bestLoss:0.0000}");
        }

        Console.WriteLine("\n=== Doğrulama dönemi (ayar seçiminde hiç kullanılmadı)");
        foreach (var (label, m) in new[] { ("önceki ayarlar", baseline), ("seçilen ayarlar", best) })
        {
            var t = Backtester.Run(d.Matches, d.Teams, d.Groups, m, trainRange);
            var v = Backtester.Run(d.Matches, d.Teams, d.Groups, m, validRange);
            Console.WriteLine($"{label,-16} eğitim: isabet %{100 * t.Accuracy:0.0} log-kayıp {t.LogLoss:0.0000} | doğrulama: isabet %{100 * v.Accuracy:0.0} log-kayıp {v.LogLoss:0.0000} RPS {v.Rps:0.0000} skor {v.ScoreLogLoss:0.0000} ({v.Matches} maç)");
        }
        Console.WriteLine(FormattableString.Invariant($"\nKomut satırı karşılığı: half={best.HalfLifeDays} prior={best.PriorMatches} pull={best.GroupPull} euro={best.EuropeanWeight} shot={best.ShotWeight}"));
    }

    /// <summary>
    /// Sınama güç modelini formülle değerlendirir; ekranda görünen yüzdeler ise simülasyondan gelir.
    /// Bu komut ikisini aynı maçlarda yan yana koyar: fark büyükse sınama sonuçları ekrana tam yansımıyor demektir.
    /// </summary>
    public static void SimCheck(DataOptions options, ModelOptions model, int sample)
    {
        var d = Load(options);
        var service = new AnalysisService(d.Source, model);
        var until = d.Matches.Max(m => m.MatchDate);
        var played = d.Source.GetMatches(until.AddDays(-240), until.AddDays(1)).Where(f => f.Status == "played").ToList();
        int step = Math.Max(1, played.Count / sample);
        int n = 0; double llSim = 0, llFormula = 0, diffH = 0, diffD = 0, diffA = 0, simDraw = 0, formulaDraw = 0, actDraw = 0;
        for (int i = 0; i < played.Count; i += step)
        {
            var f = played[i];
            var r = service.Compute(f.Id, 4000);
            if (r == null) continue;
            var (fh, fd, fa) = PoissonMath.Outcome(r.Model.ExpectedGoalsHome, r.Model.ExpectedGoalsAway);
            double sh = r.Probabilities.Home / 100, sd = r.Probabilities.Draw / 100, sa = r.Probabilities.Away / 100;
            int res = f.HomeGoals > f.AwayGoals ? 0 : f.HomeGoals == f.AwayGoals ? 1 : 2;
            llSim += -Math.Log(Math.Max(res == 0 ? sh : res == 1 ? sd : sa, 1e-6));
            llFormula += -Math.Log(Math.Max(res == 0 ? fh : res == 1 ? fd : fa, 1e-6));
            diffH += Math.Abs(sh - fh); diffD += Math.Abs(sd - fd); diffA += Math.Abs(sa - fa);
            simDraw += sd; formulaDraw += fd; actDraw += res == 1 ? 1 : 0;
            n++;
        }
        Console.WriteLine($"Ayarlar: {Describe(model)}");
        Console.WriteLine($"Karşılaştırılan maç: {n}");
        Console.WriteLine($"  Log-kayıp          simülasyon {llSim / n:0.0000} · formül {llFormula / n:0.0000}");
        Console.WriteLine($"  Ortalama fark      ev {100 * diffH / n:0.0} · beraberlik {100 * diffD / n:0.0} · deplasman {100 * diffA / n:0.0} yüzde puan");
        Console.WriteLine($"  Beraberlik         simülasyon %{100 * simDraw / n:0.0} · formül %{100 * formulaDraw / n:0.0} · gerçek %{100 * actDraw / n:0.0}");
    }
}
