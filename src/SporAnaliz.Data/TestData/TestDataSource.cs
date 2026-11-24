using Dapper;

namespace SporAnaliz.Data.TestData;

/// <summary>Uygulamanın kendi veritabanındaki üretilmiş test verisini okuyan kaynak.</summary>
public sealed class TestDataSource(Db db) : IMatchData
{
    public string SourceName => "Test verisi";
    public bool TestData => true;
    public bool EloEditable => true;
    public DateTime? DataUntil => null;

    private const string FixtureSelect = """
        SELECT m.id, m.competition_id, c.name AS competition_name, m.season, m.round_no, m.match_date, m.status,
               m.home_goals, m.away_goals,
               m.home_team_id, h.name AS home_name, h.short_name AS home_short, h.color AS home_color,
               m.away_team_id, a.name AS away_name, a.short_name AS away_short, a.color AS away_color,
               (an.match_id IS NOT NULL) AS has_analysis
        FROM matches m
        JOIN competitions c ON c.id = m.competition_id
        JOIN teams h ON h.id = m.home_team_id
        JOIN teams a ON a.id = m.away_team_id
        LEFT JOIN analyses an ON an.match_id = m.id
        """;

    public int CountTeams()
    {
        using var c = db.Open();
        return c.ExecuteScalar<int>("SELECT COUNT(*) FROM teams");
    }

    public FixtureRow? GetFixture(int id)
    {
        using var c = db.Open();
        return c.QueryFirstOrDefault<FixtureRow>(FixtureSelect + " WHERE m.id = @id", new { id });
    }

    public List<FixtureRow> GetMatches(DateTime from, DateTime to)
    {
        using var c = db.Open();
        return c.Query<FixtureRow>(FixtureSelect +
            " WHERE m.match_date >= @from AND m.match_date < @to ORDER BY m.match_date, m.competition_id DESC, m.id",
            new { from, to }).ToList();
    }

    public List<MatchRow> GetHistory(DateTime before)
    {
        using var c = db.Open();
        return c.Query<MatchRow>(
            "SELECT id, match_date, home_team_id, away_team_id, home_goals, away_goals FROM matches WHERE status = 'played' AND match_date < @before ORDER BY match_date",
            new { before }).ToList();
    }

    public Dictionary<int, TeamRow> GetTeams()
    {
        using var c = db.Open();
        return c.Query<TeamRow>("SELECT id, name, short_name, country, competition_id, color, elo, elo_approx FROM teams ORDER BY elo DESC")
            .ToDictionary(t => t.Id);
    }

    public Dictionary<int, TeamStatAvg> GetStats(DateTime before)
    {
        using var c = db.Open();
        return c.Query<TeamStatAvg>("""
            SELECT s.team_id,
                   CAST(AVG(s.shots) AS DOUBLE) AS shots_for,
                   CAST(AVG(o.shots) AS DOUBLE) AS shots_against,
                   CAST(AVG(s.corners) AS DOUBLE) AS corners,
                   CAST(AVG(s.yellow_cards) AS DOUBLE) AS yellows,
                   CAST(AVG(s.possession) AS DOUBLE) AS possession,
                   COUNT(*) AS n
            FROM match_stats s
            JOIN match_stats o ON o.match_id = s.match_id AND o.team_id <> s.team_id
            JOIN matches m ON m.id = s.match_id
            WHERE m.match_date < @before AND m.match_date >= @since
            GROUP BY s.team_id
            """, new { before, since = before.AddDays(-550) }).ToDictionary(x => x.TeamId);
    }

    public List<PlayerGoals> GetPlayerGoals(int teamId, DateTime before)
    {
        using var c = db.Open();
        return c.Query<PlayerGoals>("""
            SELECT p.id, p.name, p.pos,
                   (SELECT COUNT(*) FROM match_events e JOIN matches m ON m.id = e.match_id
                     WHERE e.player_id = p.id AND e.type = 'goal' AND m.match_date < @before AND m.match_date >= @since) AS goals
            FROM players p WHERE p.team_id = @teamId ORDER BY p.shirt_no
            """, new { teamId, before, since = before.AddDays(-550) }).ToList();
    }

    public string? GetAnalysisJson(int matchId)
    {
        using var c = db.Open();
        return c.ExecuteScalar<string?>("SELECT result_json FROM analyses WHERE match_id = @matchId", new { matchId });
    }

    public void SaveAnalysis(int matchId, int sims, string json)
    {
        using var c = db.Open();
        c.Execute("""
            INSERT INTO analyses (match_id, created_at, sim_count, result_json) VALUES (@matchId, NOW(), @sims, @json)
            ON DUPLICATE KEY UPDATE created_at = NOW(), sim_count = @sims, result_json = @json
            """, new { matchId, sims, json });
    }

    /// <summary>Elo puanını günceller; puan değiştiği için saklanan analizler silinir.</summary>
    public bool UpdateElo(int teamId, double elo, bool approx)
    {
        using var c = db.Open();
        int n = c.Execute("UPDATE teams SET elo = @elo, elo_approx = @approx WHERE id = @teamId", new { teamId, elo, approx });
        if (n > 0) c.Execute("DELETE FROM analyses");
        return n > 0;
    }

    public List<(int TeamId, double Att, double Def)> GetSeedTruth()
    {
        using var c = db.Open();
        return c.Query<(int, double, double)>("SELECT team_id, att_true, def_true FROM seed_truth").ToList();
    }
}
