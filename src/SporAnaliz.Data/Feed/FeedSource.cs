using System.Collections.Concurrent;
using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace SporAnaliz.Data.Feed;

/// <summary>
/// Yerel maç veritabanını doğrudan ve YALNIZCA OKUYARAK kullanan veri kaynağı.
/// Hiçbir tabloya yazmaz; üretilen analizler bellekte tutulur.
/// </summary>
public sealed partial class FeedSource : IMatchData
{
    // Kulüp turnuvaları: Süper Lig, 1. Lig, Türkiye Kupası, Süper Kupa, ŞL, Avrupa Ligi, Konferans Ligi,
    // UEFA Süper Kupa ve beş büyük lig. Hazırlık maçları ve milli maçlar güç hesabına katılmaz.
    private static readonly int[] ClubTournaments = { 1, 4, 71, 81, 3, 351, 698, 91, 2, 5, 6, 7, 8 };
    private static readonly int[] FinishedStatuses = { 5, 9, 11 }; // tamamlandı, uzatmalarla, penaltılarla
    private const int ScheduledStatus = 1;
    private static readonly int[] EuropeanTournaments = { 3, 351, 698, 91 };

    private sealed class BMatch
    {
        public long Id { get; set; }
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string? RoundName { get; set; }
        public int StageId { get; set; }
        public string? StageName { get; set; }
        public DateTime Date { get; set; }
        public int StatusId { get; set; }
        public int HomeTeamId { get; set; }
        public string? HomeTeamName { get; set; }
        public string? HomeTeamShortName { get; set; }
        public int AwayTeamId { get; set; }
        public string? AwayTeamName { get; set; }
        public string? AwayTeamShortName { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public bool Finished => FinishedStatuses.Contains(StatusId) && HomeScore != null && AwayScore != null;
    }

    private readonly record struct TeamStat(int Shots, int ShotsOn, int Corners, int Yellows, int Possession);

    private readonly string _cs;
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<int, string> _analyses = new();
    private Dictionary<int, BMatch> _matches = new();
    private List<BMatch> _finished = new();
    private Dictionary<int, TeamRow> _teams = new();
    private Dictionary<int, string> _tournaments = new();
    private Dictionary<int, (TeamStat Home, TeamStat Away)> _stats = new();
    private Dictionary<int, int> _groups = new();
    private bool _loaded;

    public FeedSource(DataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("appsettings.json içinde ConnectionStrings:MySql tanımlı değil.");
        if (string.IsNullOrWhiteSpace(options.Database))
            throw new InvalidOperationException("appsettings.json içinde Data:Database (maç veritabanının adı) tanımlı değil.");
        var b = new MySqlConnectionStringBuilder(options.ConnectionString) { Database = options.Database };
        _cs = b.ConnectionString;
        DatabaseName = b.Database;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public string DatabaseName { get; }
    public string SourceName => "Maç veritabanı";
    public bool TestData => false;
    public bool EloEditable => false;
    public DateTime? DataUntil { get { Load(); return _finished.Count > 0 ? _finished[^1].Date : null; } }

    private MySqlConnection Open()
    {
        var c = new MySqlConnection(_cs);
        c.Open();
        return c;
    }

    private void Load()
    {
        if (_loaded) return;
        lock (_gate)
        {
            if (_loaded) return;
            using var c = Open();
            string ids = string.Join(",", ClubTournaments);

            _tournaments = c.Query<(int Id, string Name)>($"SELECT tournament_id, name FROM soccer_tournaments WHERE tournament_id IN ({ids})")
                .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Name);

            var rows = c.Query<BMatch>($"""
                SELECT id, match_id, tournament_id, stage_id, stage_name, round_name, date, status_id,
                       home_team_id, home_team_name, home_team_short_name,
                       away_team_id, away_team_name, away_team_short_name,
                       CAST(home_team_score_regular AS SIGNED) AS home_score,
                       CAST(away_team_score_regular AS SIGNED) AS away_score
                FROM soccer_matches WHERE tournament_id IN ({ids}) AND home_team_id > 0 AND away_team_id > 0
                """, commandTimeout: 120);
            // Aynı maç birden fazla satırda olabiliyor; en son kayıt geçerli
            var matches = new Dictionary<int, BMatch>();
            foreach (var r in rows.OrderBy(r => r.Id)) matches[r.MatchId] = r;

            var countries = c.Query<(int TeamId, string? Country)>("SELECT team_id, MAX(country_name) FROM soccer_teams GROUP BY team_id")
                .ToDictionary(x => x.TeamId, x => x.Country ?? "");
            var teams = new Dictionary<int, TeamRow>();
            foreach (var m in matches.Values.OrderBy(m => m.Date))
            {
                AddTeam(teams, countries, m.HomeTeamId, m.HomeTeamName, m.HomeTeamShortName);
                AddTeam(teams, countries, m.AwayTeamId, m.AwayTeamName, m.AwayTeamShortName);
            }

            var stats = new Dictionary<int, (TeamStat, TeamStat)>();
            foreach (var (matchId, json) in c.Query<(int, string)>("SELECT match_id, stats FROM soccer_stats ORDER BY id", commandTimeout: 180, buffered: false))
            {
                if (!matches.TryGetValue(matchId, out var m) || !m.Finished || string.IsNullOrEmpty(json)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var h = ReadStat(doc.RootElement, "homeTeam"); var a = ReadStat(doc.RootElement, "awayTeam");
                    if (h != null && a != null && h.Value.Shots + a.Value.Shots > 0) stats[matchId] = (h.Value, a.Value);
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException) { /* bozuk kayıt atlanır */ }
            }

            // Takım grubu: son dönemde en çok oynadığı lig; ligi veride yoksa ülkesi (kupada görünen alt lig takımları ayrı grup)
            int[] leagues = { 1, 4, 2, 5, 6, 7, 8 };
            var countryIds = new Dictionary<string, int>();
            var recentLeague = new Dictionary<int, List<int>>();
            foreach (var m in matches.Values.Where(m => leagues.Contains(m.TournamentId)).OrderByDescending(m => m.Date))
                foreach (int tid in new[] { m.HomeTeamId, m.AwayTeamId })
                {
                    if (!recentLeague.TryGetValue(tid, out var l)) recentLeague[tid] = l = new List<int>();
                    if (l.Count < 20) l.Add(m.TournamentId);
                }
            var groups = new Dictionary<int, int>();
            foreach (var t in teams.Values)
            {
                if (recentLeague.TryGetValue(t.Id, out var l)) { groups[t.Id] = l.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key; continue; }
                string key = t.Country == "Türkiye" ? "TR-alt" : t.Country == "" ? "?" + t.Id : t.Country;
                if (!countryIds.TryGetValue(key, out int cid)) countryIds[key] = cid = 1000 + countryIds.Count;
                groups[t.Id] = cid;
            }
            _groups = groups;

            _matches = matches;
            _finished = matches.Values.Where(m => m.Finished).OrderBy(m => m.Date).ToList();
            _teams = teams;
            _stats = stats;
            _loaded = true;
        }
    }

    private static void AddTeam(Dictionary<int, TeamRow> teams, Dictionary<int, string> countries, int id, string? name, string? shortName)
    {
        // Sıralı gezildiği için en güncel ad kalır
        name = string.IsNullOrWhiteSpace(name) ? "Takım " + id : name.Trim();
        teams[id] = new TeamRow
        {
            Id = id, Name = name, ShortName = string.IsNullOrWhiteSpace(shortName) ? name[..Math.Min(3, name.Length)].ToUpperInvariant() : shortName.Trim(),
            Country = countries.GetValueOrDefault(id, ""), Color = TeamColors.For(id, name), Elo = 0, EloApprox = false
        };
    }

    private static TeamStat? ReadStat(JsonElement root, string side)
    {
        if (!root.TryGetProperty(side, out var t) || t.ValueKind != JsonValueKind.Object || !t.TryGetProperty("statistics", out var s) || s.ValueKind != JsonValueKind.Object) return null;
        int I(string k) => s.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        return new TeamStat(I("shotsOnTarget") + I("shotsOffTarget"), I("shotsOnTarget"), I("corners"), I("yellowCards"), I("ballPossession"));
    }

    private FixtureRow ToFixture(BMatch m)
    {
        var h = _teams[m.HomeTeamId]; var a = _teams[m.AwayTeamId];
        int year = m.Date.Month >= 7 ? m.Date.Year : m.Date.Year - 1;
        return new FixtureRow
        {
            Id = m.MatchId, CompetitionId = m.TournamentId, CompetitionName = _tournaments.GetValueOrDefault(m.TournamentId, "Turnuva"),
            Season = $"{year}-{(year + 1) % 100:00}", MatchDate = m.Date, Status = m.Finished ? "played" : "scheduled",
            HomeGoals = m.HomeScore, AwayGoals = m.AwayScore,
            HomeTeamId = h.Id, HomeName = h.Name, HomeShort = h.ShortName, HomeColor = h.Color,
            AwayTeamId = a.Id, AwayName = a.Name, AwayShort = a.ShortName, AwayColor = a.Color,
            HasAnalysis = _analyses.ContainsKey(m.MatchId) ? 1 : 0
        };
    }

    public int CountTeams() { Load(); return _teams.Count; }

    public FixtureRow? GetFixture(int id) { Load(); return _matches.TryGetValue(id, out var m) ? ToFixture(m) : null; }

    public List<FixtureRow> GetMatches(DateTime from, DateTime to)
    {
        Load();
        bool Turkish(BMatch m) => _teams[m.HomeTeamId].Country == "Türkiye" || _teams[m.AwayTeamId].Country == "Türkiye";
        return _matches.Values.Where(m => (m.StatusId == ScheduledStatus || m.Finished) && m.Date >= from && m.Date < to)
            .OrderBy(m => m.Date.Date).ThenByDescending(Turkish).ThenBy(m => m.Date).ThenBy(m => m.TournamentId)
            .Select(ToFixture).ToList();
    }

    public List<MatchRow> GetHistory(DateTime before) => GetHistory(before, years: 4);

    /// <summary>Verilen tarihten önceki <paramref name="years"/> yılın tamamlanmış maçları.</summary>
    public List<MatchRow> GetHistory(DateTime before, int years)
    {
        Load();
        var since = before.AddYears(-years);
        return _finished.Where(m => m.Date < before && m.Date >= since).Select(m =>
        {
            var row = new MatchRow
            {
                Id = m.MatchId, MatchDate = m.Date, HomeTeamId = m.HomeTeamId, AwayTeamId = m.AwayTeamId,
                HomeGoals = m.HomeScore!.Value, AwayGoals = m.AwayScore!.Value,
                CompetitionId = m.TournamentId, European = EuropeanTournaments.Contains(m.TournamentId),
            };
            if (_stats.TryGetValue(m.MatchId, out var s))
            {
                row.HomeShotsOn = s.Home.ShotsOn; row.HomeShotsOff = s.Home.Shots - s.Home.ShotsOn;
                row.AwayShotsOn = s.Away.ShotsOn; row.AwayShotsOff = s.Away.Shots - s.Away.ShotsOn;
            }
            return row;
        }).ToList();
    }

    public Dictionary<int, TeamRow> GetTeams() { Load(); return _teams; }

    public Dictionary<int, TeamStatAvg> GetStats(DateTime before)
    {
        Load();
        var since = before.AddDays(-550);
        var acc = new Dictionary<int, (double Sf, double Sa, double C, double Y, double P, int N)>();
        void Add(int team, TeamStat own, TeamStat opp)
        {
            var a = acc.GetValueOrDefault(team);
            acc[team] = (a.Sf + own.Shots, a.Sa + opp.Shots, a.C + own.Corners, a.Y + own.Yellows, a.P + own.Possession, a.N + 1);
        }
        foreach (var m in _finished)
        {
            if (m.Date >= before || m.Date < since || !_stats.TryGetValue(m.MatchId, out var s)) continue;
            Add(m.HomeTeamId, s.Home, s.Away);
            Add(m.AwayTeamId, s.Away, s.Home);
        }
        return acc.Where(k => k.Value.N >= 3).ToDictionary(k => k.Key, k => new TeamStatAvg
        {
            TeamId = k.Key, ShotsFor = k.Value.Sf / k.Value.N, ShotsAgainst = k.Value.Sa / k.Value.N, Corners = k.Value.C / k.Value.N,
            Yellows = k.Value.Y / k.Value.N, Possession = k.Value.P / k.Value.N, N = k.Value.N
        });
    }

    public IReadOnlyDictionary<int, int>? GetTeamGroups() { Load(); return _groups; }

    public string? GetAnalysisJson(int matchId) => _analyses.GetValueOrDefault(matchId);
    public void SaveAnalysis(int matchId, int sims, string json) => _analyses[matchId] = json;
}
