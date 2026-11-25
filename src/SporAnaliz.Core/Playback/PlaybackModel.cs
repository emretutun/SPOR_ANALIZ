namespace SporAnaliz.Core.Playback;

/// <summary>Sahadaki bir nokta: 0..1 arası. x=0 sol kale çizgisi, x=1 sağ; y=0 üst kenar, y=1 alt kenar.</summary>
public readonly record struct Vec(double X, double Y);

/// <summary>Sahadaki bir oyuncu dairesi. Değişiklikte kimliği (Id, numara, ad) giren oyuncuya geçer.</summary>
public sealed class PitchDot
{
    /// <summary>0 ev sahibi (sağa hücum eder), 1 deplasman.</summary>
    public int Side { get; init; }
    /// <summary>Takım içindeki sıra; 0 kaleci.</summary>
    public int Idx { get; init; }
    public double BaseX { get; init; }
    public double BaseY { get; init; }
    public bool Wide { get; init; }
    /// <summary>Oyuncuya özgü küçük sapma: hatlar cetvelle çizilmiş gibi durmasın.</summary>
    public double Ox { get; init; }
    public double Oy { get; init; }

    public int? Id { get; set; }
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Kırmızı kart gördü; sahadan kalktı.</summary>
    public bool Off { get; set; }

    public string SideName => Side == 0 ? "home" : "away";
}

public enum BallMoveKind { Pass, Fade, Shot }

/// <summary>Topun bir hareketi: bir oyuncuya (ToDot) ya da sabit bir noktaya (To).</summary>
public sealed class BallMove
{
    public double Depart { get; init; }
    public double Dur { get; init; }
    public double Arrive => Depart + Dur;
    public int? ToDot { get; init; }
    public Vec To { get; init; }
    public BallMoveKind Kind { get; init; }
}

/// <summary>Oyunun oynandığı bölge: takım blokları buraya doğru kayar.</summary>
public readonly record struct Zone(double T, double X, double Y);

/// <summary>Bir oyuncunun şut noktasına koşusu.</summary>
public readonly record struct RunOrder(int Dot, double T0, double T1, double X, double Y);

/// <summary>Bir maç olayının ekrana düşme anı.</summary>
public readonly record struct EventFire(double T, Analysis.PlayEvent Event);

/// <summary>Temsili maçın sahadaki kurgusu. Zaman birimi "dilim": olaysız bir dakika 1 dilim sürer.</summary>
public sealed class MatchTimeline
{
    public required List<BallMove> Moves { get; init; }
    public required List<Zone> Zones { get; init; }
    public required List<RunOrder> Orders { get; init; }
    public required List<EventFire> Fires { get; init; }
    /// <summary>Her maç dakikasının (97 dilim: 1..45, 45+1..45+2, 46..90, 90+1..90+5) başladığı an.</summary>
    public required List<double> SlotStart { get; init; }
    public required double Total { get; init; }
}

/// <summary>Topun o anki durumu.</summary>
public readonly record struct BallState(Vec Pos, double Alpha, int Holder, bool Flying);
