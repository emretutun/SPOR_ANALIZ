using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace SporAnaliz.Data.Feed;

// Maç kadroları: açıklanan ilk 11 ya da son maçlardan muhtemel 11.
public sealed partial class FeedSource
{
    public MatchLineups? GetLineups(FixtureRow match)
    {
        Load();
        using var c = Open();
        TeamLineup? home = null, away = null;
        var json = c.ExecuteScalar<string?>("SELECT lineups FROM soccer_lineups WHERE match_id = @id ORDER BY id DESC LIMIT 1", new { id = match.Id });
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                home = ReadLineup(doc.RootElement, "homeTeam", true);
                away = ReadLineup(doc.RootElement, "awayTeam", true);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }
        }
        // Kadro açıklanmamışsa (ya da eksikse) son maçlardan muhtemel 11
        if (home == null || home.Starting.Count < 11) home = ProbableLineup(c, match.HomeTeamId, match.MatchDate);
        if (away == null || away.Starting.Count < 11) away = ProbableLineup(c, match.AwayTeamId, match.MatchDate);
        return home == null || away == null ? null : new MatchLineups(home, away);
    }

    private TeamLineup? ProbableLineup(MySqlConnection c, int teamId, DateTime before)
    {
        var ids = _finished.Where(m => m.Date < before && (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
            .OrderByDescending(m => m.Date).Take(8).Select(m => m.MatchId).ToList();
        if (ids.Count == 0) return null;
        var order = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var seen = new Dictionary<int, (LineupPlayer P, double Score)>();
        string formation = ""; string? manager = null; int bestOrder = int.MaxValue, used = 0;
        foreach (var (matchId, json) in c.Query<(int, string)>("SELECT match_id, lineups FROM soccer_lineups WHERE match_id IN @ids", new { ids }))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var side in new[] { "homeTeam", "awayTeam" })
                {
                    if (!doc.RootElement.TryGetProperty(side, out var t) || t.ValueKind != JsonValueKind.Object || !t.TryGetProperty("id", out var tid) || tid.GetInt32() != teamId) continue;
                    var l = ReadLineup(doc.RootElement, side, true);
                    if (l == null || l.Starting.Count < 11) continue;
                    used++;
                    int o = order.GetValueOrDefault(matchId, 9);
                    if (o < bestOrder) { bestOrder = o; formation = l.Formation; manager = l.Manager; }
                    double w = 1.0 / (1 + o * 0.35); // yakın maçlar daha ağır
                    foreach (var p in l.Starting) seen[p.Id] = (p, seen.GetValueOrDefault(p.Id).Score + w);
                    foreach (var p in l.Bench) if (!seen.ContainsKey(p.Id)) seen[p.Id] = (p, 0);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }
        }
        if (used == 0) return null;
        var ranked = seen.Values.OrderByDescending(x => x.Score).ToList();
        var gk = ranked.Where(x => x.P.Pos == "KL").Take(1).ToList();
        var outfield = ranked.Where(x => x.P.Pos != "KL").Take(11 - gk.Count).ToList();
        int Rank(string pos) => pos switch { "KL" => 0, "DF" => 1, "OS" => 2, _ => 3 };
        var starting = gk.Concat(outfield).Select(x => x.P).OrderBy(p => Rank(p.Pos)).ToList();
        if (starting.Count < 11) return null;
        // Diziliş, seçilen oyuncuların mevki sayılarından türetilir (4-4-2 gibi)
        int d = starting.Count(p => p.Pos == "DF"), o2 = starting.Count(p => p.Pos == "OS"), f = starting.Count(p => p.Pos == "FV");
        string derived = f > 0 ? $"{d}-{o2}-{f}" : $"{d}-{o2}";
        var startIds = starting.Select(p => p.Id).ToHashSet();
        var bench = ranked.Where(x => !startIds.Contains(x.P.Id)).Take(12).Select(x => x.P).ToList();
        // Mevki sayıları gerçekçi bir diziliş vermiyorsa (ör. 3-6-1) takımın son kullandığı diziliş esas alınır
        bool sane = d is >= 3 and <= 5 && o2 is >= 2 and <= 5 && f is >= 1 and <= 3;
        return new TeamLineup(sane || formation == "" ? derived : formation, manager, starting, bench, false);
    }

    private static TeamLineup? ReadLineup(JsonElement root, string side, bool confirmed)
    {
        if (!root.TryGetProperty(side, out var t) || t.ValueKind != JsonValueKind.Object) return null;
        List<LineupPlayer> Read(string key)
        {
            var list = new List<LineupPlayer>();
            if (!t.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var p in arr.EnumerateArray())
            {
                if (!p.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number) continue;
                string Str(string k) => p.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
                int number = 0;
                if (p.TryGetProperty("shirtNumber", out var sn))
                    number = sn.ValueKind == JsonValueKind.Number ? sn.GetInt32() : int.TryParse(sn.GetString(), out var n) ? n : 0;
                int posId = 3; string tactical = "";
                if (p.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.Object)
                {
                    if (pos.TryGetProperty("id", out var pi) && pi.ValueKind == JsonValueKind.Number) posId = pi.GetInt32();
                    if (pos.TryGetProperty("tactical", out var tc) && tc.ValueKind == JsonValueKind.Object && tc.TryGetProperty("name", out var tn) && tn.ValueKind == JsonValueKind.String) tactical = tn.GetString()!;
                }
                string name = Str("knownName"), shortName = Str("knownNameShort");
                list.Add(new LineupPlayer(idEl.GetInt32(), name == "" ? "Oyuncu" : name, shortName == "" ? name : shortName, number,
                    posId switch { 1 => "KL", 2 => "DF", 3 => "OS", 4 => "FV", _ => "OS" }, tactical));
            }
            return list;
        }
        string formation = t.TryGetProperty("formation", out var f) && f.ValueKind == JsonValueKind.Object && f.TryGetProperty("name", out var fn) && fn.ValueKind == JsonValueKind.String ? fn.GetString()! : "";
        string? manager = t.TryGetProperty("manager", out var mg) && mg.ValueKind == JsonValueKind.Object && mg.TryGetProperty("knownName", out var mn) && mn.ValueKind == JsonValueKind.String ? mn.GetString() : null;
        return new TeamLineup(formation, manager, Read("starting"), Read("bench"), confirmed);
    }
}
