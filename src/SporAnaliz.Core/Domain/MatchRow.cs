namespace SporAnaliz.Core.Domain;

/// <summary>Güç hesabına giren tamamlanmış maç.</summary>
public sealed class MatchRow
{
    public int Id { get; set; }
    public DateTime MatchDate { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    /// <summary>Maçın güç hesabındaki ağırlık çarpanı (Avrupa maçları ligleri birbirine bağladığı için daha ağır sayılabilir).</summary>
    public double Weight { get; set; } = 1;
    public int CompetitionId { get; set; }
    /// <summary>Avrupa kupası maçı mı (ligleri birbirine bağlayan maçlar; modelde ayrı ağırlığı vardır).</summary>
    public bool European { get; set; }
    // Maç istatistiği varsa şutlar (yoksa null): güç hesabında gollerle birlikte kullanılabilir
    public int? HomeShotsOn { get; set; }
    public int? HomeShotsOff { get; set; }
    public int? AwayShotsOn { get; set; }
    public int? AwayShotsOff { get; set; }
}
