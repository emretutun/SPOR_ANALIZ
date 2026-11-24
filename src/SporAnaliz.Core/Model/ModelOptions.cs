namespace SporAnaliz.Core.Model;

/// <summary>
/// Güç modelinin ayarları. Varsayılanlar geçmiş sınamasıyla seçilmiştir; değiştirmeden önce
/// "tools ... tune" ve "tools ... backtest" ile ölçün.
/// </summary>
public sealed record ModelOptions
{
    /// <summary>Bir maçın ağırlığının yarıya düştüğü süre (gün).</summary>
    public double HalfLifeDays { get; init; } = 500;
    /// <summary>Az maçlı takımı grubunun seviyesine çeken sözde maç sayısı.</summary>
    public double PriorMatches { get; init; } = 8;
    /// <summary>Çekilen merkez: 1 = tamamen kendi lig/ülke ortalaması, 0 = genel ortalama.</summary>
    public double GroupPull { get; init; } = 0.85;
    /// <summary>Avrupa kupası maçlarının ağırlık çarpanı (ligleri birbirine bağlayan maçlar).</summary>
    public double EuropeanWeight { get; init; } = 2;
    /// <summary>
    /// Gücün ne kadarının şutlardan geleceği (0 = yalnızca goller, 1 = yalnızca şut tabanlı gol beklentisi).
    /// Gol az ve şansa açık bir veridir; isabetli şut takımın gerçek üretimini daha kararlı gösterir.
    /// </summary>
    public double ShotWeight { get; init; } = 0.3;
    public int Iterations { get; init; } = 40;
}
