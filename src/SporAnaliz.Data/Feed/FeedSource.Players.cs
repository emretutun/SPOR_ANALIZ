using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace SporAnaliz.Data.Feed;

// Takım kadrosu ve oyuncuların son dönemdeki golleri.
public sealed partial class FeedSource
{
    /// <summary>Takımın kadrosu ve son dönemdeki golleri. Kadro, güncel kadro tablosundan; yoksa son maç kadrolarından alınır.</summary>
    public List<PlayerGoals> GetPlayerGoals(int teamId, DateTime before)
    {
        Load();
        var since = before.AddDays(-550);
        var matchIds = _finished.Where(m => m.Date < before && m.Date >= since && (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
            .OrderByDescending(m => m.Date).Take(80).Select(m => m.MatchId).ToList();
        using var c = Open();
        var squad = new Dictionary<int, PlayerGoals>();

        var teamData = c.ExecuteScalar<string?>("SELECT team_data FROM soccer_players WHERE team_id = @teamId ORDER BY id DESC LIMIT 1", new { teamId });
        if (!string.IsNullOrEmpty(teamData))
        {
            try
            {
                using var doc = JsonDocument.Parse(teamData);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var e in doc.RootElement.EnumerateArray())
                        if (e.TryGetProperty("player", out var p)) AddPlayer(squad, p);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }
        }
        if (squad.Count < 11 && matchIds.Count > 0)
        {
            // Güncel kadro kaydı yok: son 6 maçın ilk 11'i ve yedeklerinden kadro çıkarılır
            squad.Clear();
            foreach (var json in c.Query<string>("SELECT lineups FROM soccer_lineups WHERE match_id IN @ids", new { ids = matchIds.Take(6) }))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    foreach (var side in new[] { "homeTeam", "awayTeam" })
                    {
                        if (!doc.RootElement.TryGetProperty(side, out var t) || t.ValueKind != JsonValueKind.Object || !t.TryGetProperty("id", out var tid) || tid.GetInt32() != teamId) continue;
                        foreach (var list in new[] { "starting", "bench" })
                            if (t.TryGetProperty(list, out var arr) && arr.ValueKind == JsonValueKind.Array)
                                foreach (var p in arr.EnumerateArray()) AddPlayer(squad, p);
                    }
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }
            }
        }
        if (squad.Count == 0 || matchIds.Count == 0) return squad.Values.ToList();

        foreach (var json in c.Query<string>("SELECT events FROM soccer_events WHERE match_id IN @ids", new { ids = matchIds }, commandTimeout: 120))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                foreach (var e in doc.RootElement.EnumerateArray())
                {
                    if (!e.TryGetProperty("type", out var ty) || !ty.TryGetProperty("id", out var tyId)) continue;
                    int type = tyId.GetInt32();
                    if (type != 6 && type != 7) continue; // gol, penaltı golü (kendi kalesine gol sayılmaz)
                    if (!e.TryGetProperty("teamId", out var tid) || tid.GetInt32() != teamId) continue;
                    if (e.TryGetProperty("player", out var p) && p.TryGetProperty("id", out var pid) && squad.TryGetValue(pid.GetInt32(), out var pg)) pg.Goals++;
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }
        }
        return squad.Values.OrderBy(p => p.Pos switch { "KL" => 0, "DF" => 1, "OS" => 2, _ => 3 }).ThenBy(p => p.Name).ToList();
    }

    private static void AddPlayer(Dictionary<int, PlayerGoals> squad, JsonElement p)
    {
        if (!p.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number) return;
        int id = idEl.GetInt32();
        if (squad.ContainsKey(id)) return;
        string name = p.TryGetProperty("knownName", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString()! : "Oyuncu " + id;
        int posId = p.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.Object && pos.TryGetProperty("id", out var pi) && pi.ValueKind == JsonValueKind.Number ? pi.GetInt32() : 3;
        squad[id] = new PlayerGoals { Id = id, Name = name, Pos = posId switch { 1 => "KL", 2 => "DF", 3 => "OS", 4 => "FV", _ => "OS" } };
    }
}
