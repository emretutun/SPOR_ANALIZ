using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace SporAnaliz.Kumanda.Viz;

/// <summary>
/// Viz Engine komut portuna (varsayılan 6100) tek ve sürekli açık TCP bağlantısı.
/// Her komut "kimlik komut\0" biçiminde gider; -1 kimliği yanıt istemez. Yanıtlar kimliğe göre eşleşir.
/// </summary>
public sealed class VizBaglanti : IDisposable
{
    readonly object _kilit = new();
    readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _bekleyen = new();
    TcpClient? _tcp;
    NetworkStream? _ns;
    CancellationTokenSource? _iptal;
    int _sonKimlik;

    public string Host { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 6100;
    public bool Bagli => _tcp?.Connected == true && _ns != null;

    /// <summary>Bağlantı kurulunca ya da kopunca çağrılır (arka plan iş parçacığından gelebilir).</summary>
    public event Action<bool>? DurumDegisti;

    public async Task<bool> BaglanAsync(string host, int port)
    {
        Kes();
        Host = host;
        Port = port;
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            using var zaman = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await tcp.ConnectAsync(host, port, zaman.Token);
        }
        catch
        {
            tcp.Dispose();
            DurumDegisti?.Invoke(false);
            return false;
        }
        lock (_kilit)
        {
            _tcp = tcp;
            _ns = tcp.GetStream();
            _iptal = new CancellationTokenSource();
        }
        _ = Task.Run(() => OkumaDongusu(_ns, _iptal.Token));
        DurumDegisti?.Invoke(true);
        return true;
    }

    public void Kes()
    {
        lock (_kilit)
        {
            _iptal?.Cancel();
            _ns?.Dispose();
            _tcp?.Dispose();
            _ns = null;
            _tcp = null;
        }
        foreach (var b in _bekleyen.Values) b.TrySetResult("");
        _bekleyen.Clear();
    }

    /// <summary>Yanıt beklemeden komut gönderir. Bağlantı yoksa ya da yazılamazsa false döner.</summary>
    public bool Gonder(string komut) => Yaz("-1 " + komut);

    /// <summary>Komut gönderir ve yanıtını bekler; süre dolarsa null.</summary>
    public async Task<string?> SorAsync(string komut, int beklemeMs = 1500)
    {
        int kimlik = Interlocked.Increment(ref _sonKimlik) % 1_000_000 + 1;
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _bekleyen[kimlik] = tcs;
        if (!Yaz(kimlik + " " + komut)) { _bekleyen.TryRemove(kimlik, out _); return null; }
        var bitti = await Task.WhenAny(tcs.Task, Task.Delay(beklemeMs));
        _bekleyen.TryRemove(kimlik, out _);
        return bitti == tcs.Task ? tcs.Task.Result : null;
    }

    bool Yaz(string mesaj)
    {
        var veri = Encoding.UTF8.GetBytes(mesaj + "\0");
        lock (_kilit)
        {
            if (_ns == null) return false;
            try
            {
                _ns.Write(veri, 0, veri.Length);
                return true;
            }
            catch
            {
                Kopma();
                return false;
            }
        }
    }

    void Kopma()
    {
        bool vardi;
        lock (_kilit)
        {
            vardi = _ns != null;
            _ns?.Dispose();
            _tcp?.Dispose();
            _ns = null;
            _tcp = null;
        }
        if (vardi) DurumDegisti?.Invoke(false);
    }

    async Task OkumaDongusu(NetworkStream ns, CancellationToken iptal)
    {
        var tampon = new byte[8192];
        var birikmis = new List<byte>();
        try
        {
            while (!iptal.IsCancellationRequested)
            {
                int n = await ns.ReadAsync(tampon, iptal);
                if (n <= 0) break;
                for (int i = 0; i < n; i++)
                {
                    if (tampon[i] != 0) { birikmis.Add(tampon[i]); continue; }
                    var mesaj = Encoding.UTF8.GetString(birikmis.ToArray());
                    birikmis.Clear();
                    int bosluk = mesaj.IndexOf(' ');
                    if (bosluk > 0 && int.TryParse(mesaj[..bosluk], out int kimlik) && _bekleyen.TryRemove(kimlik, out var tcs))
                        tcs.TrySetResult(mesaj[(bosluk + 1)..]);
                }
            }
        }
        catch { /* bağlantı kapandı */ }
        if (!iptal.IsCancellationRequested) Kopma();
    }

    public void Dispose() => Kes();
}
