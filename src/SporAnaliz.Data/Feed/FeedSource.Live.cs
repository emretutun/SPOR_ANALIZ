using Dapper;

namespace SporAnaliz.Data.Feed;

// Canlı ekran için günün bütün maçları: önbelleğe bakmadan, her çağrıda veritabanından taze okunur.
public sealed partial class FeedSource
{
    // Başlamadı, tamamlandı, ertelendi, iptal vb. dışında kalan her durum "oynanıyor" sayılır (1. yarı, devre arası, 2. yarı, uzatma, penaltılar)
    private static readonly int[] NotLiveStatuses = { 1, 5, 9, 11, 21, 22, 23, 71, 72, 73 };

    private sealed class DayRow
    {
        public long Id { get; set; }
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string? TournamentName { get; set; }
        public DateTime Date { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public int? CurrentMinute { get; set; }
        public int? Stoppage { get; set; }
        public int HomeTeamId { get; set; }
        public string? HomeTeamName { get; set; }
        public string? HomeTeamShortName { get; set; }
        public int AwayTeamId { get; set; }
        public string? AwayTeamName { get; set; }
        public string? AwayTeamShortName { get; set; }
        public int? HomeCurrent { get; set; }
        public int? AwayCurrent { get; set; }
    }

    public List<FixtureRow> GetDayMatches(DateTime from, DateTime to)
    {
        using var c = Open();
        var rows = c.Query<DayRow>("""
            SELECT m.id, m.match_id, m.tournament_id, t.name AS tournament_name, m.date, m.status_id, m.status_name,
                   m.current_minute, m.stoppage,
                   m.home_team_id, m.home_team_name, m.home_team_short_name,
                   m.away_team_id, m.away_team_name, m.away_team_short_name,
                   m.home_team_score_current AS home_current, m.away_team_score_current AS away_current
            FROM soccer_matches m
            LEFT JOIN (SELECT tournament_id, MAX(name) AS name FROM soccer_tournaments GROUP BY tournament_id) t ON t.tournament_id = m.tournament_id
            WHERE m.date >= @from AND m.date < @to AND m.home_team_id > 0 AND m.away_team_id > 0
            """, new { from, to }, commandTimeout: 60);

        // Aynı maç birden fazla satırda olabiliyor; en son kayıt geçerli
        var son = new Dictionary<int, DayRow>();
        foreach (var r in rows.OrderBy(r => r.Id)) son[r.MatchId] = r;

        return son.Values
            .Select(r =>
            {
                bool canli = !NotLiveStatuses.Contains(r.StatusId);
                bool bitti = FinishedStatuses.Contains(r.StatusId);
                string ev = (r.HomeTeamName ?? "").Trim(), dep = (r.AwayTeamName ?? "").Trim();
                int year = r.Date.Month >= 7 ? r.Date.Year : r.Date.Year - 1;
                return new FixtureRow
                {
                    Id = r.MatchId, CompetitionId = r.TournamentId, CompetitionName = r.TournamentName ?? "Turnuva",
                    Season = $"{year}-{(year + 1) % 100:00}", MatchDate = r.Date,
                    Status = bitti ? "played" : canli ? "live" : "scheduled", StatusName = r.StatusName ?? "", Live = canli,
                    Minute = canli ? r.CurrentMinute : null, Stoppage = canli ? r.Stoppage : null,
                    HomeGoals = bitti || canli ? r.HomeCurrent ?? 0 : null, AwayGoals = bitti || canli ? r.AwayCurrent ?? 0 : null,
                    HomeTeamId = r.HomeTeamId, HomeName = ev, HomeShort = Kisa(r.HomeTeamShortName, ev), HomeColor = TeamColors.For(r.HomeTeamId, ev),
                    AwayTeamId = r.AwayTeamId, AwayName = dep, AwayShort = Kisa(r.AwayTeamShortName, dep), AwayColor = TeamColors.For(r.AwayTeamId, dep),
                };
            })
            .OrderByDescending(f => f.Live).ThenBy(f => f.MatchDate).ThenBy(f => f.CompetitionId)
            .ToList();

        static string Kisa(string? kisa, string ad) =>
            string.IsNullOrWhiteSpace(kisa) ? ad[..Math.Min(3, ad.Length)].ToUpperInvariant() : kisa.Trim();
    }
}
