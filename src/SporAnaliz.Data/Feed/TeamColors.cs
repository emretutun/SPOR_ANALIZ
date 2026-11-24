namespace SporAnaliz.Data.Feed;

/// <summary>Veride takım rengi yok: bilinen kulüpler için elle, diğerleri için takım numarasından türetilmiş renk.</summary>
public static class TeamColors
{
    private static readonly (string Key, string Color)[] Known =
    {
        ("Galatasaray", "#E8A317"), ("Fenerbahçe", "#F5D90A"), ("Beşiktaş", "#D9D9D9"), ("Trabzonspor", "#8A1538"),
        ("Başakşehir", "#F26522"), ("Samsunspor", "#D52B1E"), ("Göztepe", "#F2C300"), ("Konyaspor", "#0B8A3E"),
        ("Alanyaspor", "#F28C28"), ("Rizespor", "#1B8E5A"), ("Kasımpaşa", "#1F4E9E"), ("Antalyaspor", "#E03A3E"),
        ("Kayserispor", "#F2B705"), ("Kocaelispor", "#1E8C45"), ("Eyüpspor", "#7B3FA0"), ("Gaziantep", "#C8102E"),
        ("Arsenal", "#EF0107"), ("Aston Villa", "#7A003C"), ("Manchester City", "#6CABDD"), ("Liverpool", "#C8102E"),
        ("Chelsea", "#034694"), ("Newcastle", "#B8B8B8"), ("Manchester United", "#DA291C"), ("Tottenham", "#C9D1E0"),
        ("Bayern", "#DC052D"), ("Paris", "#1B3A8C"), ("Barcelona", "#A50044"), ("Real Madrid", "#E6E6E6"),
        ("Inter", "#0068A8"), ("Atletico", "#CB3524"), ("Dortmund", "#FDE100"), ("Napoli", "#12A0D7"),
        ("Leverkusen", "#E32221"), ("Juventus", "#D0D0D0"), ("Sporting", "#008057"), ("Benfica", "#E83030"),
        ("Milan", "#C8102E"), ("Roma", "#8E1F2F"), ("Porto", "#1B4FA0"), ("Ajax", "#D2122E"),
    };

    public static string For(int id, string name)
    {
        foreach (var (key, color) in Known)
            if (name.Contains(key, StringComparison.OrdinalIgnoreCase)) return color;
        // Kimliğe bağlı, her açılışta aynı kalan canlı bir renk
        double h = (uint)(id * 2654435761u) % 360, s = 0.62, l = 0.52;
        double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        var (r, g, b) = h < 60 ? (c, x, 0.0) : h < 120 ? (x, c, 0.0) : h < 180 ? (0.0, c, x) : h < 240 ? (0.0, x, c) : h < 300 ? (x, 0.0, c) : (c, 0.0, x);
        return $"#{(int)((r + m) * 255):X2}{(int)((g + m) * 255):X2}{(int)((b + m) * 255):X2}";
    }
}
