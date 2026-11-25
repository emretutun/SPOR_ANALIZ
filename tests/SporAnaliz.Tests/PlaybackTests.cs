using SporAnaliz.Core.Analysis;
using SporAnaliz.Core.Domain;
using SporAnaliz.Core.Playback;
using SporAnaliz.Data.TestData;

namespace SporAnaliz.Tests;

public class PlaybackTests
{
    // Üretilmiş veriyle, kadrolu bir analiz (ilk 11 + 3 yedek)
    static AnalysisResult Analiz(int mac = 0)
    {
        var today = new DateTime(2026, 11, 24);
        var data = Seeder.Generate(today);
        var teams = data.Teams.ToDictionary(t => t.Row.Id, t => t.Row);
        var m = data.Matches.Where(x => !x.Played).Skip(mac).First();
        var fixture = new FixtureRow
        {
            Id = m.Id, CompetitionId = m.CompetitionId, CompetitionName = "Test", Season = m.Season, MatchDate = m.Date, Status = "scheduled",
            HomeTeamId = m.Home, AwayTeamId = m.Away, HomeName = teams[m.Home].Name, AwayName = teams[m.Away].Name
        };
        List<PlayerGoals> Squad(int team) => data.Teams.First(t => t.Row.Id == team).Players
            .Select(p => new PlayerGoals { Id = p.Row.Id, Name = p.Row.Name, Pos = p.Row.Pos, Goals = (long)p.Weight }).ToList();
        TeamLineup Lineup(int team)
        {
            var all = data.Teams.First(t => t.Row.Id == team).Players
                .Select(p => new LineupPlayer(p.Row.Id, p.Row.Name, p.Row.Name, p.Row.ShirtNo, p.Row.Pos, "")).ToList();
            return new TeamLineup("4-2-3-1", null, all.Take(11).ToList(), all.Skip(11).Take(3).ToList(), true);
        }
        return Analyzer.Compute(new AnalysisInput
        {
            Match = fixture, History = data.History(m.Date), Teams = teams, Stats = new Dictionary<int, TeamStatAvg>(),
            HomePlayers = Squad(m.Home), AwayPlayers = Squad(m.Away), Simulations = 2000,
            Lineups = new MatchLineups(Lineup(m.Home), Lineup(m.Away)),
        });
    }

    // Maçı sabit adımlarla sonuna kadar oynatır; her adımda denetim yapılabilir
    static List<PlayEvent> Oynat(MatchPlayer p, Action<Vec[], Vec[]>? adim = null, double dt = 1 / 50.0)
    {
        var tum = new List<PlayEvent>();
        int guard = 0;
        while (!p.Done && guard++ < 200000)
        {
            var once = (Vec[])p.Positions.Clone();
            tum.AddRange(p.Step(dt));
            adim?.Invoke(once, p.Positions);
        }
        Assert.True(p.Done, "Maç bitmedi.");
        return tum;
    }

    [Fact]
    public void ButunOlaylarSirayiBozmadanBirKezDuser()
    {
        var a = Analiz();
        var p = new MatchPlayer(a);
        var duşen = Oynat(p);
        Assert.Equal(a.Simulation.Events.Count, duşen.Count);
        Assert.Equal(a.Simulation.Events.OrderBy(e => e.Slot).Select(e => e.Slot), duşen.Select(e => e.Slot));
        Assert.Equal(a.Simulation.HomeGoals, p.HomeGoals);
        Assert.Equal(a.Simulation.AwayGoals, p.AwayGoals);
    }

    [Fact]
    public void OyuncularHizSiniriniAsmaz()
    {
        var p = new MatchPlayer(Analiz());
        double dt = 1 / 50.0, sinir = MatchPlayer.PlayerSpeed * MatchPlayer.RunSpeedFactor * dt * 1.0001;
        double enHizli = 0;
        Oynat(p, (once, sonra) =>
        {
            for (int i = 0; i < once.Length; i++) enHizli = Math.Max(enHizli, TimelineBuilder.Dist(once[i], sonra[i]));
        }, dt);
        Assert.True(enHizli <= sinir, $"Bir oyuncu karede {enHizli:0.0000} ilerledi; sınır {sinir:0.0000}.");
    }

    [Fact]
    public void OyuncularVeTopSahadaKalir()
    {
        var p = new MatchPlayer(Analiz());
        Oynat(p, (_, sonra) =>
        {
            foreach (var v in sonra) { Assert.InRange(v.X, 0.0, 1.0); Assert.InRange(v.Y, 0.0, 1.0); }
            // Top kalenin içine (x biraz 0/1 dışı) gidebilir ama sahadan kopmaz
            Assert.InRange(p.Ball.Pos.X, -0.05, 1.05);
            Assert.InRange(p.Ball.Pos.Y, -0.05, 1.05);
        });
    }

    [Fact]
    public void DegisiklikDaireyiGirenOyuncuyaGecirir()
    {
        var a = Analiz();
        var p = new MatchPlayer(a);
        var subs = Oynat(p).Where(e => e.Type == "sub").ToList();
        Assert.NotEmpty(subs);
        foreach (var s in subs)
        {
            int side = s.Side == "home" ? 0 : 1;
            Assert.Contains(p.Dots, d => d.Side == side && d.Id == s.PlayerId);
        }
    }

    [Fact]
    public void AyniMacHerSeferindeAyniOynar()
    {
        var a = Analiz();
        var p1 = new MatchPlayer(a); var p2 = new MatchPlayer(a);
        Oynat(p1); Oynat(p2);
        Assert.Equal(p1.Positions, p2.Positions);
        // Baştan sarınca yine aynı
        p1.Reset();
        Oynat(p1);
        Assert.Equal(p2.Positions, p1.Positions);
    }

    [Fact]
    public void HizArttikcaMacKisalir()
    {
        var a = Analiz();
        int Adim(double hiz) { var p = new MatchPlayer(a) { Speed = hiz }; int n = 0; while (!p.Done) { p.Step(0.02); n++; } return n; }
        int bir = Adim(1), iki = Adim(2);
        Assert.InRange(iki, bir / 2 - 2, bir / 2 + 2);
    }

    [Theory]
    [InlineData(0, "1'")]
    [InlineData(44, "45'")]
    [InlineData(45, "45+1'")]
    [InlineData(46, "45+2'")]
    [InlineData(47, "46'")]
    [InlineData(91, "90'")]
    [InlineData(96, "90+5'")]
    public void DakikaYazisi(int slot, string beklenen) => Assert.Equal(beklenen, MatchPlayer.SlotLabel(slot));
}
