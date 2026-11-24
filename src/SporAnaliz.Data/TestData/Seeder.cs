using Dapper;

namespace SporAnaliz.Data.TestData;

public sealed class SeedPlayer
{
    public required PlayerRow Row;
    public double Weight;
}

public sealed class SeedTeam
{
    public required TeamRow Row;
    public double Att, Def, ShotBase, PossStyle, YellowBase;
    public List<SeedPlayer> Players = new();
    public List<SimPlayer> SimPlayers = new();
}

public sealed class SeedMatch
{
    public int Id, CompetitionId, RoundNo, Home, Away, PossHome;
    public string Season = "";
    public DateTime Date;
    public bool Played;
    public SimResult? Result;
}

public sealed class SeedData
{
    public List<(int Id, string Name, string Kind)> Competitions = new();
    public List<SeedTeam> Teams = new();
    public List<SeedMatch> Matches = new();

    public List<MatchRow> History(DateTime before) => Matches
        .Where(m => m.Played && m.Date < before)
        .Select(m => new MatchRow
        {
            Id = m.Id, MatchDate = m.Date, HomeTeamId = m.Home, AwayTeamId = m.Away,
            HomeGoals = m.Result!.Goals[0], AwayGoals = m.Result.Goals[1]
        }).ToList();
}

public sealed record SeedSummary(int Teams, int Players, int Matches, int Played, int Scheduled, int Events);

/// <summary>
/// Varsayımsal veri üretici. Takım güçleri Elo tablosundan türetilir; geçmiş maçlar bu güçlerle
/// simüle edilerek istatistik, olay ve oyuncu verisi oluşturulur. Dışarıdan hiçbir veri çekilmez.
/// Elo değerleri YAKLAŞIKTIR; panelden düzeltilebilir.
/// </summary>
public sealed class Seeder(Db db)
{
    public const double Mu = 1.12, HomeAdv = 1.24;
    public const int CompSuperLig = 1, CompPremier = 2, CompChampions = 3;

    private static readonly (string Name, string Short, double Elo, string Color)[] SuperLig =
    {
        ("Galatasaray", "GS", 1750, "#E8A317"), ("Fenerbahçe", "FB", 1720, "#F5D90A"),
        ("Beşiktaş", "BJK", 1600, "#D9D9D9"), ("Trabzonspor", "TS", 1590, "#8A1538"),
        ("Samsunspor", "SAM", 1560, "#D52B1E"), ("Başakşehir", "IBFK", 1540, "#F26522"),
        ("Göztepe", "GÖZ", 1530, "#F2C300"), ("Eyüpspor", "EYP", 1470, "#7B3FA0"),
        ("Konyaspor", "KON", 1460, "#0B8A3E"), ("Alanyaspor", "ALN", 1450, "#F28C28"),
        ("Çaykur Rizespor", "RİZ", 1450, "#1B8E5A"), ("Kasımpaşa", "KAS", 1440, "#1F4E9E"),
        ("Gaziantep FK", "GFK", 1440, "#C8102E"), ("Kocaelispor", "KOC", 1430, "#1E8C45"),
        ("Antalyaspor", "ANT", 1420, "#E03A3E"), ("Gençlerbirliği", "GB", 1420, "#B5121B"),
        ("Kayserispor", "KAY", 1410, "#F2B705"), ("Fatih Karagümrük", "FKG", 1380, "#B22222"),
    };

    private static readonly (string Name, string Short, double Elo, string Color)[] Premier =
    {
        ("Arsenal", "ARS", 2050, "#EF0107"), ("Aston Villa", "AVL", 1997, "#7A003C"),
        ("Manchester City", "MCI", 1990, "#6CABDD"), ("Liverpool", "LIV", 1990, "#C8102E"),
        ("Chelsea", "CHE", 1930, "#034694"), ("Newcastle United", "NEW", 1880, "#B8B8B8"),
        ("Manchester United", "MUN", 1850, "#DA291C"), ("Crystal Palace", "CRY", 1850, "#1B458F"),
        ("Tottenham", "TOT", 1840, "#C9D1E0"), ("Brighton", "BHA", 1840, "#0057B8"),
        ("Bournemouth", "BOU", 1840, "#DA291C"), ("Nottingham Forest", "NFO", 1820, "#DD0000"),
        ("Brentford", "BRE", 1820, "#E30613"), ("Fulham", "FUL", 1790, "#CCCCCC"),
        ("Everton", "EVE", 1790, "#003399"), ("Sunderland", "SUN", 1750, "#EB172B"),
        ("West Ham", "WHU", 1740, "#7A263A"), ("Leeds United", "LEE", 1730, "#FFCD00"),
        ("Wolverhampton", "WOL", 1700, "#FDB913"), ("Burnley", "BUR", 1680, "#6C1D45"),
    };

    private static readonly (string Name, string Short, string Country, double Elo, string Color)[] Europe =
    {
        ("Bayern Münih", "BAY", "Almanya", 2010, "#DC052D"), ("Paris Saint-Germain", "PSG", "Fransa", 2000, "#1B3A8C"),
        ("Barcelona", "BAR", "İspanya", 1980, "#A50044"), ("Real Madrid", "RMA", "İspanya", 1960, "#E6E6E6"),
        ("Inter", "INT", "İtalya", 1960, "#0068A8"), ("Atlético Madrid", "ATM", "İspanya", 1900, "#CB3524"),
        ("Borussia Dortmund", "BVB", "Almanya", 1880, "#FDE100"), ("Napoli", "NAP", "İtalya", 1880, "#12A0D7"),
        ("Bayer Leverkusen", "B04", "Almanya", 1860, "#E32221"), ("Juventus", "JUV", "İtalya", 1850, "#D0D0D0"),
        ("Sporting CP", "SCP", "Portekiz", 1840, "#008057"), ("Benfica", "BEN", "Portekiz", 1830, "#E83030"),
    };

    private static readonly string[] TrFirst = { "Ahmet", "Mehmet", "Emre", "Burak", "Kerem", "Yusuf", "Barış", "Oğuz", "Cenk", "Hakan", "Umut", "Berkay", "Efe", "Arda", "Salih", "İsmail", "Kaan", "Onur", "Tolga", "Serkan", "Ozan", "Eren", "Mert", "Taylan", "Halil" };
    private static readonly string[] TrLast = { "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Koç", "Kurt", "Özkan", "Polat", "Güneş", "Bulut", "Aktaş", "Erdem", "Tekin", "Yavuz", "Bozkurt", "Karaca", "Uçar" };
    private static readonly string[] EnFirst = { "James", "Harry", "Jack", "Oliver", "Mason", "Declan", "Ben", "Luke", "Callum", "Reece", "Tom", "Kyle", "Jordan", "Marcus", "Aaron", "Conor", "Lewis", "Nathan", "Ryan", "Dominic", "Morgan", "Elliot", "Curtis", "Jacob", "Tyler" };
    private static readonly string[] EnLast = { "Walker", "Stone", "Mitchell", "Barnes", "Foster", "Hughes", "Palmer", "Chambers", "Brooks", "Hayes", "Marsh", "Gibson", "Reid", "Dawson", "Holt", "Sutton", "Bishop", "Carver", "Ellison", "Rowe", "Whitlock", "Garner", "Pryce", "Lowe", "Hartley" };
    private static readonly string[] IntFirst = { "Lucas", "Mateo", "Diego", "Rafael", "Hugo", "Leon", "Niklas", "Jonas", "Luca", "Matteo", "Federico", "Thiago", "Bruno", "André", "Julien", "Antoine", "Sergio", "Pablo", "Florian", "Kai", "Davide", "Nicolò", "Tiago", "Gonçalo", "Maxime" };
    private static readonly string[] IntLast = { "Moreno", "Santos", "Costa", "Ferreira", "Navarro", "Vidal", "Romero", "Castillo", "Weber", "Krüger", "Brandt", "Hoffmann", "Ricci", "Conti", "Marchetti", "Galli", "Dubois", "Lefèvre", "Girard", "Renard", "Pereira", "Carvalho", "Mendes", "Ortega", "Serrano" };

    // ---------------------------------------------------------------- üretim

    public static SeedData Generate(DateTime today, int seed = 2026)
    {
        var rng = new Random(seed);
        var d = new SeedData();
        d.Competitions.Add((CompSuperLig, "Süper Lig", "lig"));
        d.Competitions.Add((CompPremier, "Premier Lig", "lig"));
        d.Competitions.Add((CompChampions, "Şampiyonlar Ligi", "avrupa"));

        int teamId = 0, playerId = 0;
        void AddTeam(string name, string shortName, string country, int? comp, double elo, string color)
        {
            double s = StrengthModel.EloToStrength(elo);
            double style = Gauss(rng) * 0.10; // + açık/hücumcu, - kapalı/savunmacı
            var t = new SeedTeam
            {
                Row = new TeamRow { Id = ++teamId, Name = name, ShortName = shortName, Country = country, CompetitionId = comp, Color = color, Elo = elo, EloApprox = true },
                Att = Math.Exp(s + style + Gauss(rng) * 0.03),
                Def = Math.Exp(-s + style + Gauss(rng) * 0.03),
                PossStyle = 50 + s * StrengthModel.PossessionPerStrength + Gauss(rng) * 2.5,
                YellowBase = 1.6 + rng.NextDouble() * 1.0,
            };
            t.ShotBase = 12.5 * Math.Pow(t.Att, 0.55) * Math.Exp(Gauss(rng) * 0.07);

            var (first, last) = country switch
            {
                "Türkiye" => (TrFirst, TrLast),
                "İngiltere" => (EnFirst, EnLast),
                _ => (IntFirst, IntLast)
            };
            var used = new HashSet<string>();
            string[] positions = { "KL", "KL", "DF", "DF", "DF", "DF", "DF", "OS", "OS", "OS", "OS", "OS", "FV", "FV", "FV", "FV" };
            bool star = true;
            for (int i = 0; i < positions.Length; i++)
            {
                // Her takımda birkaç "yabancı" oyuncu olsun
                bool foreign = rng.NextDouble() < 0.25;
                var f = foreign ? IntFirst : first;
                var l = foreign ? IntLast : last;
                string nm;
                do { nm = $"{f[rng.Next(f.Length)]} {l[rng.Next(l.Length)]}"; } while (!used.Add(nm));
                double w = positions[i] switch
                {
                    "KL" => 0,
                    "DF" => 0.2 + rng.NextDouble() * 0.6,
                    "OS" => 1.0 + rng.NextDouble() * 2.5,
                    _ => 4.0 + rng.NextDouble() * 5.0
                };
                if (positions[i] == "FV" && star) { w *= 1.4; star = false; }
                var p = new SeedPlayer
                {
                    Row = new PlayerRow { Id = ++playerId, TeamId = teamId, Name = nm, Pos = positions[i], ShirtNo = i + 1 },
                    Weight = w
                };
                t.Players.Add(p);
                t.SimPlayers.Add(new SimPlayer(p.Row.Id, nm, w));
            }
            d.Teams.Add(t);
        }

        foreach (var x in SuperLig) AddTeam(x.Name, x.Short, "Türkiye", CompSuperLig, x.Elo, x.Color);
        foreach (var x in Premier) AddTeam(x.Name, x.Short, "İngiltere", CompPremier, x.Elo, x.Color);
        foreach (var x in Europe) AddTeam(x.Name, x.Short, x.Country, null, x.Elo, x.Color);

        var byId = d.Teams.ToDictionary(t => t.Row.Id);
        int matchId = 0;

        void AddMatch(int comp, string season, int round, DateTime date, int h, int a)
        {
            var m = new SeedMatch { Id = ++matchId, CompetitionId = comp, Season = season, RoundNo = round, Date = date, Home = h, Away = a, Played = date.Date < today.Date };
            if (m.Played)
            {
                var th = byId[h]; var ta = byId[a];
                int poss = (int)Math.Round(Math.Clamp(50 + (th.PossStyle - ta.PossStyle) / 2 + 1.5 + Gauss(rng) * 3, 28, 72));
                m.PossHome = poss;
                m.Result = MatchSimulator.Run(TrueParams(th, ta, true, poss), TrueParams(ta, th, false, 100 - poss), seed * 31 + m.Id, record: true);
            }
            d.Matches.Add(m);
        }

        int startYear = today.Month >= 7 ? today.Year : today.Year - 1;
        int[] dayOffsets = { -1, 0, 0, 0, 1, 1, 2 };

        void Domestic(int comp, int year)
        {
            string season = SeasonLabel(year);
            var arr = d.Teams.Where(t => t.Row.CompetitionId == comp).Select(t => t.Row.Id).OrderBy(_ => rng.Next()).ToList();
            int n = arr.Count;
            var firstSat = new DateTime(year, 8, 10);
            while (firstSat.DayOfWeek != DayOfWeek.Saturday) firstSat = firstSat.AddDays(1);

            var half = new List<List<(int h, int a)>>();
            for (int r = 0; r < n - 1; r++)
            {
                var round = new List<(int, int)>();
                for (int i = 0; i < n / 2; i++)
                {
                    int x = arr[i], y = arr[n - 1 - i];
                    bool swap = i == 0 ? r % 2 == 1 : i % 2 == 1;
                    round.Add(swap ? (y, x) : (x, y));
                }
                half.Add(round);
                int last = arr[n - 1]; arr.RemoveAt(n - 1); arr.Insert(1, last);
            }
            var all = half.Concat(half.Select(r => r.Select(p => (p.a, p.h)).ToList())).ToList();
            for (int r = 0; r < all.Count; r++)
                foreach (var (h, a) in all[r])
                    AddMatch(comp, season, r + 1, firstSat.AddDays(7 * r + dayOffsets[rng.Next(dayOffsets.Length)]).AddHours(20), h, a);
        }

        int Find(string name) => d.Teams.First(t => t.Row.Name == name).Row.Id;
        int gs = Find("Galatasaray"), villa = Find("Aston Villa");
        var clTeams = d.Teams.Where(t => t.Row.CompetitionId == null).Select(t => t.Row.Id)
            .Concat(d.Teams.Where(t => t.Row.CompetitionId == CompSuperLig).OrderByDescending(t => t.Row.Elo).Take(4).Select(t => t.Row.Id))
            .Concat(d.Teams.Where(t => t.Row.CompetitionId == CompPremier).OrderByDescending(t => t.Row.Elo).Take(8).Select(t => t.Row.Id))
            .ToList();
        if (!clTeams.Contains(villa)) clTeams[^1] = villa;

        void Champions(int year)
        {
            string season = SeasonLabel(year);
            var dates = new List<DateTime>
            {
                new(year, 9, 16), new(year, 10, 7), new(year, 10, 28), new(year, 11, 25),
                new(year, 12, 9), new(year + 1, 1, 20), new(year + 1, 1, 27), new(year + 1, 2, 17)
            };
            int todayIdx = -1;
            if (year == startYear)
            {
                // Bugüne denk gelen ilk hafta "günün maçları" olsun
                todayIdx = dates.FindIndex(x => x.Date >= today.Date);
                if (todayIdx >= 0) dates[todayIdx] = today.Date;
            }
            var usedPairs = new HashSet<(int, int)>();
            if (todayIdx >= 0) usedPairs.Add(Key(gs, villa));
            var homeCount = clTeams.ToDictionary(i => i, _ => 0);

            for (int md = 0; md < dates.Count; md++)
            {
                bool forced = md == todayIdx;
                List<(int, int)>? pairs = null;
                for (int attempt = 0; attempt < 20000 && pairs == null; attempt++)
                {
                    var pool = clTeams.Where(i => !forced || (i != gs && i != villa)).OrderBy(_ => rng.Next()).ToList();
                    var cand = new List<(int, int)>();
                    bool ok = true;
                    for (int i = 0; i + 1 < pool.Count && ok; i += 2)
                    {
                        if (usedPairs.Contains(Key(pool[i], pool[i + 1]))) ok = false;
                        else cand.Add((pool[i], pool[i + 1]));
                    }
                    if (ok) pairs = cand;
                }
                pairs ??= new List<(int, int)>();
                if (forced) pairs.Insert(0, (gs, villa));

                int k = 0;
                foreach (var (x, y) in pairs)
                {
                    bool keep = forced && x == gs || !(forced && x == gs) && homeCount[x] <= homeCount[y];
                    int h = keep ? x : y, a = keep ? y : x;
                    homeCount[h]++;
                    usedPairs.Add(Key(h, a));
                    var kick = dates[md].AddHours(k % 3 == 0 ? 20 : 23).AddMinutes(k % 3 == 0 ? 45 : 0);
                    AddMatch(CompChampions, season, md + 1, kick, h, a);
                    k++;
                }
            }
        }

        for (int y = startYear - 2; y <= startYear; y++)
        {
            Domestic(CompSuperLig, y);
            Domestic(CompPremier, y);
            Champions(y);
        }
        return d;
    }

    private static SimTeam TrueParams(SeedTeam t, SeedTeam opp, bool home, double possession)
    {
        double lam = (home ? Mu * HomeAdv : Mu) * t.Att * opp.Def;
        double shots = Math.Max(t.ShotBase * Math.Pow(opp.Def, 0.4) * (home ? 1.06 : 0.96), lam / 0.30);
        return new SimTeam
        {
            Lambda = lam, Shots = shots, Corners = 0.36 * shots + 0.6, Yellows = t.YellowBase,
            Possession = possession, Players = t.SimPlayers
        };
    }

    private static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);
    private static string SeasonLabel(int y) => $"{y}-{(y + 1) % 100:00}";

    private static double Gauss(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    // ---------------------------------------------------------------- kayıt

    /// <summary>Kendi veritabanındaki tabloları silip yeniden kurar ve üretilen veriyi yazar.</summary>
    public SeedSummary Persist(SeedData d)
    {
        db.DropAllTables();
        db.EnsureSchema();
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();

        Db.BulkInsert(conn, tx, "competitions", new[] { "id", "name", "kind" },
            d.Competitions.Select(c => new object?[] { c.Id, c.Name, c.Kind }).ToList());
        Db.BulkInsert(conn, tx, "teams", new[] { "id", "name", "short_name", "country", "competition_id", "color", "elo", "elo_approx" },
            d.Teams.Select(t => new object?[] { t.Row.Id, t.Row.Name, t.Row.ShortName, t.Row.Country, t.Row.CompetitionId, t.Row.Color, t.Row.Elo, 1 }).ToList());
        Db.BulkInsert(conn, tx, "seed_truth", new[] { "team_id", "att_true", "def_true" },
            d.Teams.Select(t => new object?[] { t.Row.Id, t.Att, t.Def }).ToList());
        var players = d.Teams.SelectMany(t => t.Players).ToList();
        Db.BulkInsert(conn, tx, "players", new[] { "id", "team_id", "name", "pos", "shirt_no" },
            players.Select(p => new object?[] { p.Row.Id, p.Row.TeamId, p.Row.Name, p.Row.Pos, p.Row.ShirtNo }).ToList());

        Db.BulkInsert(conn, tx, "matches",
            new[] { "id", "competition_id", "season", "round_no", "match_date", "home_team_id", "away_team_id", "home_goals", "away_goals", "status" },
            d.Matches.Select(m => new object?[]
            {
                m.Id, m.CompetitionId, m.Season, m.RoundNo, m.Date, m.Home, m.Away,
                m.Result?.Goals[0], m.Result?.Goals[1], m.Played ? "played" : "scheduled"
            }).ToList());

        var stats = new List<object?[]>();
        var events = new List<object?[]>();
        foreach (var m in d.Matches.Where(x => x.Played))
        {
            var r = m.Result!;
            for (int s = 0; s < 2; s++)
                stats.Add(new object?[] { m.Id, s == 0 ? m.Home : m.Away, r.Shots[s], r.OnTarget[s], s == 0 ? m.PossHome : 100 - m.PossHome, r.Corners[s], r.Yellows[s], r.Reds[s] });
            foreach (var e in r.Events!.Where(e => e.Type is "goal" or "yellow" or "red"))
                events.Add(new object?[] { m.Id, e.Minute, e.Added, e.Side == 0 ? m.Home : m.Away, e.PlayerId, e.Type });
        }
        Db.BulkInsert(conn, tx, "match_stats",
            new[] { "match_id", "team_id", "shots", "shots_on_target", "possession", "corners", "yellow_cards", "red_cards" }, stats);
        Db.BulkInsert(conn, tx, "match_events",
            new[] { "match_id", "minute_no", "added_min", "team_id", "player_id", "type" }, events);
        tx.Commit();

        int played = d.Matches.Count(m => m.Played);
        return new SeedSummary(d.Teams.Count, players.Count, d.Matches.Count, played, d.Matches.Count - played, events.Count);
    }
}
