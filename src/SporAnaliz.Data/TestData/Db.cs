using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using MySqlConnector;

namespace SporAnaliz.Data.TestData;

/// <summary>MySQL bağlantısı, şema kurulumu ve toplu ekleme yardımcıları.</summary>
public sealed class Db
{
    private readonly string _cs;

    public Db(DataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("appsettings.json içinde ConnectionStrings:MySql tanımlı değil.");
        _cs = options.ConnectionString;
        DatabaseName = new MySqlConnectionStringBuilder(_cs).Database;
        if (!Regex.IsMatch(DatabaseName, "^[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("Veritabanı adı yalnızca harf, rakam ve alt çizgi içerebilir.");
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public string DatabaseName { get; }

    public MySqlConnection Open()
    {
        var c = new MySqlConnection(_cs);
        c.Open();
        return c;
    }

    /// <summary>Veritabanı yoksa oluşturur, tabloları kurar. Yalnızca kendi veritabanına dokunur.</summary>
    public void EnsureSchema()
    {
        var b = new MySqlConnectionStringBuilder(_cs) { Database = "" };
        using (var c = new MySqlConnection(b.ConnectionString))
        {
            c.Open();
            c.Execute($"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4");
        }
        using var conn = Open();
        conn.Execute(SchemaSql);
    }

    public void DropAllTables()
    {
        using var conn = Open();
        conn.Execute("DROP TABLE IF EXISTS analyses, seed_truth, match_events, match_stats, matches, players, teams, competitions");
    }

    public static void BulkInsert(MySqlConnection conn, MySqlTransaction tx, string table, string[] columns,
        IReadOnlyList<object?[]> rows, int chunk = 400)
    {
        for (int start = 0; start < rows.Count; start += chunk)
        {
            int n = Math.Min(chunk, rows.Count - start);
            var sb = new StringBuilder($"INSERT INTO {table} ({string.Join(",", columns)}) VALUES ");
            using var cmd = new MySqlCommand { Connection = conn, Transaction = tx };
            for (int i = 0; i < n; i++)
            {
                sb.Append(i == 0 ? "(" : ",(");
                var row = rows[start + i];
                for (int k = 0; k < columns.Length; k++)
                {
                    string p = $"@p{i}_{k}";
                    sb.Append(k == 0 ? p : "," + p);
                    cmd.Parameters.AddWithValue(p, row[k] ?? DBNull.Value);
                }
                sb.Append(')');
            }
            cmd.CommandText = sb.ToString();
            cmd.ExecuteNonQuery();
        }
    }

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS competitions (
            id INT PRIMARY KEY,
            name VARCHAR(80) NOT NULL,
            kind VARCHAR(20) NOT NULL
        );
        CREATE TABLE IF NOT EXISTS teams (
            id INT PRIMARY KEY,
            name VARCHAR(80) NOT NULL,
            short_name VARCHAR(8) NOT NULL,
            country VARCHAR(40) NOT NULL,
            competition_id INT NULL,
            color VARCHAR(9) NOT NULL,
            elo DOUBLE NOT NULL,
            elo_approx TINYINT(1) NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS players (
            id INT PRIMARY KEY,
            team_id INT NOT NULL,
            name VARCHAR(80) NOT NULL,
            pos VARCHAR(3) NOT NULL,
            shirt_no INT NOT NULL,
            INDEX ix_players_team (team_id)
        );
        CREATE TABLE IF NOT EXISTS matches (
            id INT PRIMARY KEY,
            competition_id INT NOT NULL,
            season VARCHAR(9) NOT NULL,
            round_no INT NOT NULL,
            match_date DATETIME NOT NULL,
            home_team_id INT NOT NULL,
            away_team_id INT NOT NULL,
            home_goals INT NULL,
            away_goals INT NULL,
            status VARCHAR(12) NOT NULL,
            INDEX ix_matches_date (match_date),
            INDEX ix_matches_home (home_team_id),
            INDEX ix_matches_away (away_team_id)
        );
        CREATE TABLE IF NOT EXISTS match_stats (
            match_id INT NOT NULL,
            team_id INT NOT NULL,
            shots INT NOT NULL,
            shots_on_target INT NOT NULL,
            possession INT NOT NULL,
            corners INT NOT NULL,
            yellow_cards INT NOT NULL,
            red_cards INT NOT NULL,
            PRIMARY KEY (match_id, team_id)
        );
        CREATE TABLE IF NOT EXISTS match_events (
            id INT AUTO_INCREMENT PRIMARY KEY,
            match_id INT NOT NULL,
            minute_no INT NOT NULL,
            added_min INT NOT NULL,
            team_id INT NOT NULL,
            player_id INT NULL,
            type VARCHAR(12) NOT NULL,
            INDEX ix_events_match (match_id),
            INDEX ix_events_player (player_id)
        );
        CREATE TABLE IF NOT EXISTS analyses (
            match_id INT PRIMARY KEY,
            created_at DATETIME NOT NULL,
            sim_count INT NOT NULL,
            result_json LONGTEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS seed_truth (
            team_id INT PRIMARY KEY,
            att_true DOUBLE NOT NULL,
            def_true DOUBLE NOT NULL
        );
        """;
}
