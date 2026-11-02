using System.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace ResetExplode;

/// <summary>Linha do painel de sensores, com notificação de mudança para a UI.</summary>
public class SensorRow : INotifyPropertyChanged
{
    public string Id { get; init; } = "";
    public string Hardware { get; init; } = "";
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public SensorType Type { get; init; }
    public HardwareType HardwareType { get; init; }

    /// <summary>Posição do dispositivo e do sensor na ordem em que o hardware foi detectado (ordenação da tabela).</summary>
    public int HwIndex { get; init; }
    public int Index { get; init; }

    private float? _value, _min, _max;
    private double _sum;
    private long _count;
    public float? RawValue => _value;

    /// <summary>Ordem de exibição por tipo (clocks e temperaturas primeiro, como no GPU-Z).</summary>
    public int Order => Type switch
    {
        SensorType.Clock => 0, SensorType.Temperature => 1, SensorType.Load => 2, SensorType.Fan => 3,
        SensorType.Control => 4, SensorType.Power => 5, SensorType.Voltage => 6, SensorType.Current => 7,
        SensorType.SmallData => 8, SensorType.Data => 9, SensorType.Throughput => 10, _ => 11
    };
    public string Value => Format(_value);
    public string Min => Format(_min);
    public string Max => Format(_max);
    public string Average => Format(_count > 0 ? (float)(_sum / _count) : null);

    private static readonly PropertyChangedEventArgs ValueArgs = new(nameof(Value)), MinArgs = new(nameof(Min)),
        MaxArgs = new(nameof(Max)), AvgArgs = new(nameof(Average));

    /// <summary>Nova leitura: o app guarda mínimo, máximo e média desde a abertura (ou o último "zerar"), como o
    /// HWiNFO, e avisa a tela só do que mudou (centenas de sensores por segundo).</summary>
    public void Update(float? value)
    {
        if (_value != value) { _value = value; PropertyChanged?.Invoke(this, ValueArgs); }
        if (value is not float v || float.IsNaN(v)) return;
        if (_min is not float lo || v < lo) { _min = v; PropertyChanged?.Invoke(this, MinArgs); }
        if (_max is not float hi || v > hi) { _max = v; PropertyChanged?.Invoke(this, MaxArgs); }
        string before = Average;
        _sum += v; _count++;
        if (Average != before) PropertyChanged?.Invoke(this, AvgArgs);
    }

    /// <summary>Zera mínimo, máximo e média (o botão de relógio do HWiNFO).</summary>
    public void Reset()
    {
        _min = _max = _value; _sum = _value ?? 0; _count = _value.HasValue ? 1 : 0;
        PropertyChanged?.Invoke(this, MinArgs); PropertyChanged?.Invoke(this, MaxArgs); PropertyChanged?.Invoke(this, AvgArgs);
    }

    private string Format(float? v)
    {
        if (v == null) return "—";
        return Type switch
        {
            SensorType.Temperature => $"{v:0.#} °C",
            SensorType.Load or SensorType.Control or SensorType.Level or SensorType.Humidity => $"{v:0.#} %",
            SensorType.Clock => $"{v:0} MHz",
            SensorType.Power => $"{v:0.#} W",
            SensorType.Voltage => $"{v:0.###} V",
            SensorType.Current => $"{v:0.###} A",
            SensorType.Fan => $"{v:0} RPM",
            SensorType.Flow => $"{v:0.#} L/h",
            SensorType.Frequency => $"{v:0.#} Hz",
            SensorType.Data => $"{v:0.##} GB",
            SensorType.SmallData => $"{v:0.#} MB",
            SensorType.Throughput => $"{v / 1048576f:0.##} MB/s",
            SensorType.Energy => $"{v:0.#} mWh",
            _ => $"{v:0.##}"
        };
    }

    public static string CategoryName(SensorType t) => t switch
    {
        SensorType.Temperature => "Temperatura",
        SensorType.Load => "Carga",
        SensorType.Clock => "Clock",
        SensorType.Power => "Consumo",
        SensorType.Voltage => "Tensão",
        SensorType.Current => "Corrente",
        SensorType.Fan => "Ventoinhas",
        SensorType.Control => "Controle",
        SensorType.Level => "Nível",
        SensorType.Data or SensorType.SmallData => "Dados",
        SensorType.Throughput => "Taxa",
        SensorType.Frequency => "Frequência",
        SensorType.Energy => "Energia",
        SensorType.Flow => "Fluxo",
        SensorType.Humidity => "Umidade",
        SensorType.Factor => "Fator",
        _ => t.ToString()
    };

    public event PropertyChangedEventHandler? PropertyChanged;
}
