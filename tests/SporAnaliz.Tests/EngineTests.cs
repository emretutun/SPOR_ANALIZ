using SporAnaliz.Core.Analysis;
using SporAnaliz.Core.Domain;
using SporAnaliz.Core.Model;
using SporAnaliz.Core.Simulation;
using SporAnaliz.Data.TestData;

namespace SporAnaliz.Tests;

public class MatchSimulatorTests
{
    private static SimTeam Team(double lambda, double shots = 13) => new()
    {
        Lambda = lambda, Shots = shots, Corners = 5, Yellows = 2, Possession = 50,
        Players = new List<SimPlayer> { new(1, "A", 5), new(2, "B", 1) }
    };

    [Fact]
    public void AyniTohumAyniMaciVerir()
    {
        var a = MatchSimulator.Run(Team(1.5), Team(1.1), seed: 42, record: true);
        var b = MatchSimulator.Run(Team(1.5), Team(1.1), seed: 42, record: true);
        Assert.Equal(a.Goals, b.Goals);
        Assert.Equal(a.Shots, b.Shots);
        Assert.Equal(a.Events!.Count, b.Events!.Count);
    }

    [Fact]
    public void OrtalamaGolBeklentiyeYakin()
    {
        const int n = 20000;
        double home = 0, away = 0;
        for (int i = 0; i < n; i++)
        {
            var r = MatchSimulator.Run(Team(1.6), Team(1.0), seed: i, record: false);
            home += r.Goals[0]; away += r.Goals[1];
        }
        // Skora göre baskı değiştiği için birebir tutmaz; yüzde 12 pay bırakılır
        Assert.InRange(home / n, 1.6 * 0.88, 1.6 * 1.12);
        Assert.InRange(away / n, 1.0 * 0.88, 1.0 * 1.12);
    }

    [Fact]
    public void KayitModundaOlaylarSkoruTutar()
    {
        var r = MatchSimulator.Run(Team(2.0), Team(1.5), seed: 7, record: true);
        Assert.Equal(r.Goals[0] + r.Goals[1], r.Events!.Count(e => e.Type == "goal"));
        Assert.Equal(MatchSimulator.Slots.Length, r.Momentum!.Length);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(45, 0, 44)]
    [InlineData(45, 2, 46)]
    [InlineData(46, 0, 47)]
    [InlineData(90, 0, 91)]
    [InlineData(90, 5, 96)]
    public void DakikaDilimeDogruCevrilir(int minute, int added, int expected) =>
        Assert.Equal(expected, Analyzer.SlotIndex(minute, added));
}

public class StrengthModelTests
{
    [Fact]
    public void UretilenVerideGizliGucuGeriBulur()
    {
        var today = new DateTime(2026, 11, 24);
        var data = Seeder.Generate(today);
        // Gerçek sistemdeki gibi: her takım kendi liginin grubunda (lig seviyesi buradan öğrenilir)
        var groups = data.Teams.ToDictionary(t => t.Row.Id, t => t.Row.CompetitionId ?? 99);
        var truth = data.Teams.Select(t => Math.Log(t.Att / t.Def)).ToList();
        List<double> Fitted(Strengths f) => data.Teams.Select(t => Math.Log(f.AttOf(t.Row.Id) / f.DefOf(t.Row.Id))).ToList();
        var fit = StrengthModel.Fit(data.History(today), today, groups);
        double grouped = Correlation(truth, Fitted(fit));
        double ungrouped = Correlation(truth, Fitted(StrengthModel.Fit(data.History(today), today)));
        Assert.True(grouped > 0.9, $"Model, veriyi üreten güçleri yeterince iyi geri bulamadı (korelasyon {grouped:0.000}).");
        Assert.True(grouped > ungrouped, "Lig grupları güç tahminini iyileştirmeli.");
        Assert.InRange(fit.Mu, 0.9, 1.6);
        Assert.InRange(fit.HomeAdv, 1.0, 1.6);
    }

    /// <summary>
    /// Gerçek veride yaşanan hata: az maçlı yüzlerce takım varken hesap salınıma girip
    /// beklenen golü 0,01'e düşürüyordu. Bu test o durumu yeniden kurar.
    /// </summary>
    [Fact]
    public void AzMacliCokTakimVarkenDagilmaz()
    {
        var rng = new Random(1);
        var asOf = new DateTime(2026, 1, 1);
        var matches = new List<MatchRow>();
        int id = 0;
        // 20 takımlı, iki devreli bir lig
        for (int h = 1; h <= 20; h++)
            for (int a = 1; a <= 20; a++)
                if (h != a) matches.Add(new MatchRow { Id = ++id, MatchDate = asOf.AddDays(-rng.Next(1, 300)), HomeTeamId = h, AwayTeamId = a, HomeGoals = Poisson(rng, 1.5), AwayGoals = Poisson(rng, 1.2) });
        // Her biri lig takımlarıyla yalnızca iki maç yapmış 300 takım
        for (int t = 100; t < 400; t++)
            for (int k = 0; k < 2; k++)
                matches.Add(new MatchRow { Id = ++id, MatchDate = asOf.AddDays(-rng.Next(1, 300)), HomeTeamId = rng.Next(1, 21), AwayTeamId = t, HomeGoals = Poisson(rng, 2.5), AwayGoals = Poisson(rng, 0.6) });

        var fit = StrengthModel.Fit(matches, asOf);
        var (home, away) = fit.Lambdas(1, 2);
        Assert.InRange(home, 0.8, 2.6);
        Assert.InRange(away, 0.6, 2.2);
    }

    [Fact]
    public void EloVeGucBirbirininTersi()
    {
        Assert.Equal(1850, StrengthModel.StrengthToElo(StrengthModel.EloToStrength(1850)), 6);
    }

    private static int Poisson(Random rng, double lambda)
    {
        double l = Math.Exp(-lambda), p = 1; int k = 0;
        do { k++; p *= rng.NextDouble(); } while (p > l);
        return k - 1;
    }

    private static double Correlation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < x.Count; i++) { sxy += (x[i] - mx) * (y[i] - my); sxx += (x[i] - mx) * (x[i] - mx); syy += (y[i] - my) * (y[i] - my); }
        return sxy / Math.Sqrt(sxx * syy);
    }
}

public class AnalyzerTests
{
    private static AnalysisResult Analyze(int simulations = 3000, bool withLineups = false)
    {
        var today = new DateTime(2026, 11, 24);
        var data = Seeder.Generate(today);
        var teams = data.Teams.ToDictionary(t => t.Row.Id, t => t.Row);
        var m = data.Matches.First(x => !x.Played);
        var fixture = new FixtureRow
        {
            Id = m.Id, CompetitionId = m.CompetitionId, CompetitionName = "Test", Season = m.Season, MatchDate = m.Date, Status = "scheduled",
            HomeTeamId = m.Home, AwayTeamId = m.Away, HomeName = teams[m.Home].Name, AwayName = teams[m.Away].Name
        };
        List<PlayerGoals> Squad(int team) => data.Teams.First(t => t.Row.Id == team).Players
            .Select(p => new PlayerGoals { Id = p.Row.Id, Name = p.Row.Name, Pos = p.Row.Pos, Goals = (long)p.Weight }).ToList();
        // Kadro: geniş kadronun ilk 11'i sahada, sonraki 3'ü yedek, son 2 oyuncu maç kadrosunda değil
        TeamLineup Lineup(int team)
        {
            var all = data.Teams.First(t => t.Row.Id == team).Players
                .Select(p => new LineupPlayer(p.Row.Id, p.Row.Name, p.Row.Name, p.Row.ShirtNo, p.Row.Pos, "")).ToList();
            // Forvetler listenin sonunda olduğu için sondan başa alınır: maç dışında kalanlar kaleci ve defans olur
            all.Reverse();
            return new TeamLineup("4-3-3", null, all.Take(11).ToList(), all.Skip(11).Take(3).ToList(), true);
        }
        return Analyzer.Compute(new AnalysisInput
        {
            Match = fixture, History = data.History(m.Date), Teams = teams, Stats = new Dictionary<int, TeamStatAvg>(),
            HomePlayers = Squad(m.Home), AwayPlayers = Squad(m.Away), Simulations = simulations,
            Lineups = withLineups ? new MatchLineups(Lineup(m.Home), Lineup(m.Away)) : null,
        });
    }

    /// <summary>
    /// Yaşanan hata: kadroda olmayan bir oyuncunun adı olay akışına düşüyordu.
    /// Her olayın sahibi o an SAHADA olmalı: yedek ancak değişiklikle girdikten sonra, atılan oyuncu bir daha asla.
    /// </summary>
    [Fact]
    public void OlayinSahibiOAndaSahadadir()
    {
        var r = Analyze(withLineups: true);
        var lineups = r.Lineups!;
        var onPitch = new Dictionary<string, HashSet<int>>
        {
            ["home"] = lineups.Home.Starting.Select(p => p.Id).ToHashSet(), ["away"] = lineups.Away.Starting.Select(p => p.Id).ToHashSet()
        };
        var benchLeft = new Dictionary<string, HashSet<int>>
        {
            ["home"] = lineups.Home.Bench.Select(p => p.Id).ToHashSet(), ["away"] = lineups.Away.Bench.Select(p => p.Id).ToHashSet()
        };
        var subs = new Dictionary<string, int> { ["home"] = 0, ["away"] = 0 };
        var cameOn = new HashSet<int>();
        foreach (var e in r.Simulation.Events)
        {
            if (e.Type == "sub")
            {
                Assert.True(onPitch[e.Side].Remove(e.OutPlayerId!.Value), "Çıkan oyuncu sahada değildi.");
                Assert.DoesNotContain(e.OutPlayerId.Value, cameOn);   // sonradan giren oyuncu tekrar çıkarılmaz
                cameOn.Add(e.PlayerId!.Value);
                Assert.True(benchLeft[e.Side].Remove(e.PlayerId!.Value), "Giren oyuncu yedekte değildi (ya da ikinci kez girdi).");
                onPitch[e.Side].Add(e.PlayerId.Value);
                Assert.True(e.Minute >= 46, "Değişiklik ilk yarıda yapılmamalı.");
                subs[e.Side]++;
                continue;
            }
            if (e.PlayerId == null) continue;
            Assert.Contains(e.PlayerId.Value, onPitch[e.Side]);
            if (e.Type == "red") onPitch[e.Side].Remove(e.PlayerId.Value);
        }
        Assert.InRange(subs["home"], 1, MatchSimulator.MaxSubs);
        Assert.InRange(subs["away"], 1, MatchSimulator.MaxSubs);
        var squadNames = lineups.Home.Starting.Concat(lineups.Home.Bench).Concat(lineups.Away.Starting).Concat(lineups.Away.Bench).Select(p => p.Name).ToHashSet();
        Assert.All(r.Scorers, s => Assert.Contains(s.Player, squadNames));
    }

    [Fact]
    public void KadroVerilinceSkorVeOlasiliklarDegismez()
    {
        // Kadro yalnızca olayın SAHİBİNİ değiştirir; maçın gidişatı aynı kalmalı
        var a = Analyze(); var b = Analyze(withLineups: true);
        Assert.Equal(a.Probabilities, b.Probabilities);
        Assert.Equal(a.Simulation.HomeGoals, b.Simulation.HomeGoals);
        Assert.Equal(a.Simulation.AwayGoals, b.Simulation.AwayGoals);
        // Değişiklikler ek olaydır; diğer olayların sayısı ve sırası aynı kalır
        Assert.Equal(a.Simulation.Events.Select(e => (e.Slot, e.Side, e.Type)),
            b.Simulation.Events.Where(e => e.Type != "sub").Select(e => (e.Slot, e.Side, e.Type)));
    }

    [Fact]
    public void OlasiliklarYuzeTamamlanir()
    {
        var r = Analyze();
        Assert.InRange(r.Probabilities.Home + r.Probabilities.Draw + r.Probabilities.Away, 99.8, 100.2);
        Assert.InRange(r.AllScores.Sum(s => s.Pct), 99.0, 101.0);
        Assert.InRange(r.FirstGoal.Home + r.FirstGoal.Away + r.FirstGoal.None, 99.8, 100.2);
    }

    [Fact]
    public void TemsiliMacEnOlasiSonucunEnOlasiSkoruylaBiter()
    {
        var r = Analyze();
        var p = r.Probabilities;
        Func<ScoreProb, bool> favourite = p.Home >= p.Draw && p.Home >= p.Away ? s => s.Home > s.Away
            : p.Away >= p.Draw ? s => s.Home < s.Away : s => s.Home == s.Away;
        var expected = r.AllScores.First(favourite);
        Assert.Equal(expected.Home, r.Simulation.HomeGoals);
        Assert.Equal(expected.Away, r.Simulation.AwayGoals);
        Assert.Equal(r.Simulation.HomeGoals + r.Simulation.AwayGoals, r.Simulation.Events.Count(e => e.Type == "goal"));
    }

    [Fact]
    public void AyniGirdiAyniSonucuVerir()
    {
        var a = Analyze(1000); var b = Analyze(1000);
        Assert.Equal(a.Probabilities, b.Probabilities);
        Assert.Equal(a.Simulation.Events.Count, b.Simulation.Events.Count);
    }
}
