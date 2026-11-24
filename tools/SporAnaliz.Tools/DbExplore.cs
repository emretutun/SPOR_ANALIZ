using Dapper;
using MySqlConnector;

namespace SporAnaliz.Tools;

/// <summary>
/// Yerel MySQL'deki başka bir veritabanını yalnızca okuyarak inceler (tablo, satır sayısı, sütunlar, örnek satır).
/// Kullanım:  dotnet run --project tools/SporAnaliz.Tools -- explore [veritabani] [tablo]
/// </summary>
public static class DbExplore
{
    public static void Run(DataOptions options, string[] args)
    {
        var b = new MySqlConnectionStringBuilder(options.ConnectionString) { Database = "" };
        using var c = new MySqlConnection(b.ConnectionString);
        c.Open();
        string? schema = args.ElementAtOrDefault(0), table = args.ElementAtOrDefault(1);

        // explore sql "SELECT ..."   (yalnızca SELECT)
        if (schema == "sql" && table != null)
        {
            if (!table.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || table.Contains(';'))
            {
                Console.WriteLine("Yalnızca tek bir SELECT sorgusu çalıştırılır.");
                return;
            }
            foreach (IDictionary<string, object> row in c.Query(table, commandTimeout: 120))
                Console.WriteLine(string.Join(" | ", row.Select(kv => $"{kv.Key}={Trim(kv.Value, 900)}")));
            return;
        }
        if (schema == null)
        {
            foreach (var r in c.Query("SELECT table_schema AS s, COUNT(*) AS n, SUM(table_rows) AS r FROM information_schema.tables WHERE table_schema NOT IN ('mysql','sys','information_schema','performance_schema') GROUP BY table_schema"))
                Console.WriteLine($"{r.s}: {r.n} tablo, ~{r.r} satır");
            return;
        }
        if (table == null)
        {
            foreach (var r in c.Query("SELECT table_name AS t, table_rows AS r, ROUND(data_length/1048576,1) AS mb FROM information_schema.tables WHERE table_schema=@schema ORDER BY table_name", new { schema }))
            {
                var cols = c.Query<string>("SELECT column_name FROM information_schema.columns WHERE table_schema=@schema AND table_name=@t ORDER BY ordinal_position", new { schema, t = (string)r.t });
                Console.WriteLine($"{r.t} (~{r.r} satır, {r.mb} MB): {string.Join(", ", cols)}");
            }
            return;
        }
        foreach (var r in c.Query("SELECT column_name AS n, column_type AS ty FROM information_schema.columns WHERE table_schema=@schema AND table_name=@table ORDER BY ordinal_position", new { schema, table }))
            Console.WriteLine($"{r.n}: {r.ty}");
        Console.WriteLine("--- örnek satırlar");
        string limit = args.ElementAtOrDefault(2) ?? "2";
        foreach (IDictionary<string, object> row in c.Query($"SELECT * FROM `{schema.Replace("`", "")}`.`{table.Replace("`", "")}` ORDER BY 1 DESC LIMIT {int.Parse(limit)}"))
            Console.WriteLine(string.Join(" | ", row.Select(kv => $"{kv.Key}={Trim(kv.Value)}")));
    }

    private static string Trim(object? v, int max = 160)
    {
        var s = v?.ToString() ?? "NULL";
        return s.Length > max ? s[..max] + "…" : s;
    }
}
