using System.Diagnostics;

namespace SporAnaliz.Tools;

/// <summary>Veritabanı olmadan veri üretimini, modeli ve simülasyonu bellekte dener.</summary>
public static class SelfTest
{
    public static void Run()
    {
        var today = DateTime.Today;
        var sw = Stopwatch.StartNew();
        var d = Seeder.Generate(today);
        Console.WriteLine($"Veri üretimi: {sw.ElapsedMilliseconds} ms | takım {d.Teams.Count}, maç {d.Matches.Count}, oynanan {d.Matches.Count(m => m.Played)}");

        // Lig bazında gol ortalaması ve ev sahibi galibiyet payı
        foreach (var c in d.Competitions)
        {
            var ms = d.Matches.Where(m => m.Played && m.CompetitionId == c.Id).ToList();
            double g = ms.Average(m => (double)(m.Result!.Goals[0] + m.Result.Goals[1]));
            double hw = ms.Average(m => m.Result!.Goals[0] > m.Result.Goals[1] ? 1.0 : 0), dr = ms.Average(m => m.Result!.Goals[0] == m.Result.Goals[1] ? 1.0 : 0);
            double shots = ms.Average(m => (double)(m.Result!.Shots[0] + m.Result.Shots[1])) / 2;
            Console.WriteLine($"  {c.Name,-18} maç {ms.Count,4} | gol/maç {g:0.00} | ev {hw:P0} berabere {dr:P0} dep {1 - hw - dr:P0} | şut/takım {shots:0.0}");
        }

        // Model, gizli güçleri veriden geri çıkarabiliyor mu?
        var history = d.History(today);
        var fit = StrengthModel.Fit(history, today);
        var truth = d.Teams.Select(t => Math.Log(t.Att / t.Def)).ToList();
        var fitted = d.Teams.Select(t => Math.Log(fit.AttOf(t.Row.Id) / fit.DefOf(t.Row.Id))).ToList();
        Console.WriteLine($"Model: mu {fit.Mu:0.000} (gerçek {Seeder.Mu}), iç saha {fit.HomeAdv:0.000} (gerçek {Seeder.HomeAdv}), güç korelasyonu {Correlation(truth, fitted):0.000}");

        // Günün maçı
        var teams = d.Teams.ToDictionary(t => t.Row.Id, t => t.Row);
        var todays = d.Matches.Where(m => !m.Played && m.Date.Date == today).ToList();
        Console.WriteLine($"Bugünkü maç sayısı: {todays.Count}");
        var pick = todays.FirstOrDefault(m => teams[m.Home].Name == "Galatasaray") ?? d.Matches.First(m => !m.Played);

        var comp = d.Competitions.First(c => c.Id == pick.CompetitionId);
        var fx = new FixtureRow
        {
            Id = pick.Id, CompetitionId = comp.Id, CompetitionName = comp.Name, Season = pick.Season, RoundNo = pick.RoundNo,
            MatchDate = pick.Date, Status = "scheduled", HomeTeamId = pick.Home, AwayTeamId = pick.Away,
            HomeName = teams[pick.Home].Name, AwayName = teams[pick.Away].Name
        };

        sw.Restart();
        var res = Analyzer.Compute(new AnalysisInput
        {
            Match = fx, History = d.History(pick.Date), Teams = teams, Stats = Stats(d, pick.Date),
            HomePlayers = Goals(d, pick.Home, pick.Date), AwayPlayers = Goals(d, pick.Away, pick.Date),
        });
        Console.WriteLine($"\nAnaliz ({res.Simulations} simülasyon): {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"{res.Match.Competition} | {res.Match.Home.Name} (Elo {res.Match.Home.Elo}) - {res.Match.Away.Name} (Elo {res.Match.Away.Elo})");
        Console.WriteLine($"Beklenen gol: {res.Model.ExpectedGoalsHome} - {res.Model.ExpectedGoalsAway}");
        Console.WriteLine($"Sonuç: ev %{res.Probabilities.Home}  beraberlik %{res.Probabilities.Draw}  deplasman %{res.Probabilities.Away}");
        Console.WriteLine("Skorlar: " + string.Join(", ", res.TopScores.Select(s => $"{s.Home}-{s.Away} %{s.Pct}")));
        foreach (var e in res.Expected) Console.WriteLine($"  {e.Label,-20} {e.Home,6} {e.Away,6}");
        Console.WriteLine("Karşılaştırma: " + string.Join(" | ", res.Comparison.Select(c => $"{c.Label} {c.Home}/{c.Away}")));
        Console.WriteLine("Golcüler: " + string.Join(", ", res.Scorers.Select(s => $"{s.Player} %{s.Pct}")));
        Console.WriteLine("Yorumlar:");
        foreach (var c in res.Comments) Console.WriteLine("  - " + c);
        Console.WriteLine($"Temsili maç: {res.Simulation.HomeGoals}-{res.Simulation.AwayGoals}, olay sayısı {res.Simulation.Events.Count}");
        foreach (var e in res.Simulation.Events.Where(e => e.Type is "goal" or "red"))
            Console.WriteLine($"  {e.Minute}{(e.Added > 0 ? "+" + e.Added : "")}' {e.Type} {e.Side} {e.Player} ({e.HomeGoals}-{e.AwayGoals})");
    }

    private static Dictionary<int, TeamStatAvg> Stats(SeedData d, DateTime before)
    {
        var acc = new Dictionary<int, List<(int sf, int sa, int c, int y, int p)>>();
        foreach (var m in d.Matches.Where(m => m.Played && m.Date < before && m.Date >= before.AddDays(-550)))
        {
            var r = m.Result!;
            void Add(int team, int s, int poss)
            {
                if (!acc.TryGetValue(team, out var l)) acc[team] = l = new();
                l.Add((r.Shots[s], r.Shots[1 - s], r.Corners[s], r.Yellows[s], poss));
            }
            Add(m.Home, 0, m.PossHome);
            Add(m.Away, 1, 100 - m.PossHome);
        }
        return acc.ToDictionary(k => k.Key, k => new TeamStatAvg
        {
            TeamId = k.Key, ShotsFor = k.Value.Average(x => x.sf), ShotsAgainst = k.Value.Average(x => x.sa),
            Corners = k.Value.Average(x => x.c), Yellows = k.Value.Average(x => x.y), Possession = k.Value.Average(x => x.p), N = k.Value.Count
        });
    }

    private static List<PlayerGoals> Goals(SeedData d, int teamId, DateTime before)
    {
        var team = d.Teams.First(t => t.Row.Id == teamId);
        var cnt = new Dictionary<int, long>();
        foreach (var m in d.Matches.Where(m => m.Played && m.Date < before && m.Date >= before.AddDays(-550) && (m.Home == teamId || m.Away == teamId)))
            foreach (var e in m.Result!.Events!.Where(e => e.Type == "goal" && e.PlayerId != null && e.Side == (m.Home == teamId ? 0 : 1)))
                cnt[e.PlayerId!.Value] = cnt.GetValueOrDefault(e.PlayerId.Value) + 1;
        return team.Players.Select(p => new PlayerGoals { Id = p.Row.Id, Name = p.Row.Name, Pos = p.Row.Pos, Goals = cnt.GetValueOrDefault(p.Row.Id) }).ToList();
    }

    public static double Correlation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < x.Count; i++)
        {
            sxy += (x[i] - mx) * (y[i] - my); sxx += (x[i] - mx) * (x[i] - mx); syy += (y[i] - my) * (y[i] - my);
        }
        return sxy / Math.Sqrt(sxx * syy);
    }
}
