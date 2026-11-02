namespace ResetExplode.Modular;

/// <summary>Um ponto da curva de fan: temperatura da GPU (°C) → velocidade (%).</summary>
public sealed record FanPoint(int TempC, int Percent);

/// <summary>Perfil de overclock no estilo Afterburner. Tudo o que a aba Overclock aplica numa vez.</summary>
public sealed class OcProfile
{
    public string Name { get; set; } = "";
    public int CoreOffsetMhz { get; set; }
    public int MemOffsetMhz { get; set; }
    public int PowerPercent { get; set; } = 100;
    public int? TempLimitC { get; set; }
    public int VoltagePercent { get; set; }

    /// <summary>Offsets extras por ponto da curva V/F (índice do ponto → MHz), editados no gráfico.</summary>
    public Dictionary<int, int> PointOffsetsMhz { get; set; } = new();

    /// <summary>Undervolt: a curva fica plana a partir desta tensão (mV). Null = sem teto.</summary>
    public int? UvCapMv { get; set; }
    /// <summary>Clock no teto do undervolt (MHz). Null = o clock que a curva já tem nessa tensão.</summary>
    public int? UvCapMhz { get; set; }

    public bool FanAuto { get; set; } = true;
    public int FanFixedPercent { get; set; } = 60;
    public bool FanCurveOn { get; set; }
    public List<FanPoint> FanCurve { get; set; } = FanCurveMath.Default();
    public int FanHysteresisC { get; set; } = 3;

    public OcProfile Clone() => new()
    {
        Name = Name, CoreOffsetMhz = CoreOffsetMhz, MemOffsetMhz = MemOffsetMhz, PowerPercent = PowerPercent,
        TempLimitC = TempLimitC, VoltagePercent = VoltagePercent, PointOffsetsMhz = new(PointOffsetsMhz),
        UvCapMv = UvCapMv, UvCapMhz = UvCapMhz, FanAuto = FanAuto, FanFixedPercent = FanFixedPercent,
        FanCurveOn = FanCurveOn, FanCurve = FanCurve.ToList(), FanHysteresisC = FanHysteresisC,
    };
}

/// <summary>Contas da curva V/F: do perfil (offset global, offsets por ponto e teto de undervolt) aos deltas que o driver recebe.</summary>
public static class VfCurveMath
{
    /// <summary>Limite de offset por ponto aceito pelo driver (±1000 MHz).</summary>
    public const int MaxDeltaMhz = 1000;

    /// <summary>Clock final de cada ponto (MHz) a partir da curva de fábrica.</summary>
    public static int[] TargetMhz(IReadOnlyList<(int Mv, int Mhz)> stock, OcProfile p)
    {
        var target = new int[stock.Count];
        for (int i = 0; i < stock.Count; i++)
        {
            int extra = p.PointOffsetsMhz.TryGetValue(i, out int o) ? o : 0;
            target[i] = stock[i].Mhz + Clamp(p.CoreOffsetMhz + extra);
        }

        if (p.UvCapMv is int capMv && stock.Count > 0)
        {
            int cap = NearestIndex(stock, capMv);
            int capMhz = p.UvCapMhz ?? target[cap];
            for (int i = 0; i < target.Length; i++)
                target[i] = i >= cap ? capMhz : Math.Min(target[i], capMhz);
        }

        // Cada ponto fica dentro de ±1000 MHz do original. Depois do teto não: lá a placa deixa a curva plana sozinha (DeltasKhz).
        int capIdx = CapIndex(stock, p);
        for (int i = 0; i < target.Length; i++)
            if (capIdx < 0 || i <= capIdx) target[i] = stock[i].Mhz + Clamp(target[i] - stock[i].Mhz);
        return target;
    }

    /// <summary>Deltas em kHz por ponto, alinhados com a curva (o que o driver grava). Como o MSI Afterburner, todos os
    /// pontos depois do teto recebem o mesmo offset bem negativo: eles ficam abaixo do teto e o driver, que não deixa
    /// a curva descer, os segura no clock do teto. Assim a curva fica plana até o fim e a placa não sobe a tensão.</summary>
    public static int[] DeltasKhz(IReadOnlyList<(int Mv, int Mhz)> stock, OcProfile p)
    {
        int[] target = TargetMhz(stock, p);
        var deltas = new int[stock.Count];
        int capIdx = CapIndex(stock, p);
        int end = capIdx < 0 ? stock.Count : capIdx + 1;
        for (int i = 1; i < end; i++) deltas[i] = (target[i] - stock[i].Mhz) * 1000;
        if (capIdx >= 0 && end < stock.Count)
        {
            int below = Enumerable.Range(end, stock.Count - end).Min(i => target[capIdx] - stock[i].Mhz) - BelowCapMarginMhz;
            int flat = Math.Max(below, -MaxBelowCapMhz) * 1000;
            for (int i = end; i < stock.Count; i++) deltas[i] = flat;
        }
        return deltas;
    }

    /// <summary>Folga para os pontos depois do teto ficarem com certeza abaixo dele.</summary>
    public const int BelowCapMarginMhz = 200;

    /// <summary>O offset mais negativo usado depois do teto (o Afterburner grava cerca de −2017 MHz ali).</summary>
    public const int MaxBelowCapMhz = 2000;

    private static int CapIndex(IReadOnlyList<(int Mv, int Mhz)> stock, OcProfile p)
        => p.UvCapMv is int mv && stock.Count > 0 ? NearestIndex(stock, mv) : -1;

    /// <summary>Índice do ponto com a tensão mais próxima.</summary>
    public static int NearestIndex(IReadOnlyList<(int Mv, int Mhz)> curve, int mv)
    {
        int best = 0;
        for (int i = 1; i < curve.Count; i++)
            if (Math.Abs(curve[i].Mv - mv) < Math.Abs(curve[best].Mv - mv)) best = i;
        return best;
    }

    /// <summary>Onde a curva efetiva fica plana até o fim (teto de undervolt), ou null se ela sobe até o último ponto.
    /// Exige um platô de pelo menos 3 pontos para não confundir com o topo natural da curva.</summary>
    public static int? FlatStart(IReadOnlyList<(int Mv, int Mhz)> curve)
    {
        if (curve.Count < 4) return null;
        int top = curve[^1].Mhz, f = curve.Count - 1;
        while (f > 0 && Math.Abs(curve[f - 1].Mhz - top) <= 2) f--;
        return curve.Count - f >= 3 && curve.Take(f).All(p => p.Mhz < top) ? f : null;
    }

    /// <summary>Curva de fábrica = curva atual menos os deltas aplicados.</summary>
    public static IReadOnlyList<(int Mv, int Mhz)> SubtractDeltas(IReadOnlyList<(int Mv, int Mhz)> live, int[] deltasKhz)
        => live.Select((p, i) => (p.Mv, p.Mhz - (i < deltasKhz.Length ? (int)Math.Round(deltasKhz[i] / 1000.0) : 0))).ToList();

    /// <summary>A coluna de clock é legível (não está no meio de uma troca de estado de energia).</summary>
    public static bool Readable(IReadOnlyList<(int Mv, int Mhz)> curve)
    {
        if (curve.Count < 16) return false;
        for (int i = 0; i < curve.Count; i++)
        {
            if (curve[i].Mhz < 100 || curve[i].Mhz > 4000) return false;
            if (i > 0 && curve[i].Mhz < curve[i - 1].Mhz - 25) return false;
        }
        return curve.Max(c => c.Mhz) >= 1200;
    }

    private static int Clamp(int mhz) => Math.Clamp(mhz, -MaxDeltaMhz, MaxDeltaMhz);
}

/// <summary>Curva de fan por software (como no Afterburner): interpolação e histerese.</summary>
public static class FanCurveMath
{
    public static List<FanPoint> Default() =>
        [new(30, 30), new(50, 40), new(60, 50), new(70, 65), new(80, 85), new(90, 100)];

    /// <summary>Velocidade (%) para a temperatura, por interpolação linear entre os pontos.</summary>
    public static int SpeedAt(IReadOnlyList<FanPoint> points, double tempC)
    {
        if (points.Count == 0) return 100;
        var sorted = points.OrderBy(p => p.TempC).ToList();
        if (tempC <= sorted[0].TempC) return sorted[0].Percent;
        if (tempC >= sorted[^1].TempC) return sorted[^1].Percent;
        for (int i = 1; i < sorted.Count; i++)
        {
            var (a, b) = (sorted[i - 1], sorted[i]);
            if (tempC <= b.TempC)
            {
                double t = b.TempC == a.TempC ? 1 : (tempC - a.TempC) / (b.TempC - a.TempC);
                return (int)Math.Round(a.Percent + t * (b.Percent - a.Percent));
            }
        }
        return sorted[^1].Percent;
    }

    /// <summary>Próxima velocidade com histerese: sobe na hora, mas só desce quando a temperatura cai
    /// <paramref name="hysteresisC"/> graus abaixo do ponto que pediu a velocidade atual (evita o fan "serrando").</summary>
    public static int Next(IReadOnlyList<FanPoint> points, double tempC, int current, int hysteresisC)
    {
        int wanted = SpeedAt(points, tempC);
        if (wanted >= current) return wanted;
        int relaxed = SpeedAt(points, tempC + hysteresisC);
        return relaxed < current ? relaxed : current;
    }
}
