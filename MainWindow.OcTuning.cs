using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ResetExplode.Modular;

namespace ResetExplode;

/// <summary>Aba Overclock no estilo do MSI Afterburner: sliders, curva V/F com undervolt, curva de fans, perfis e monitoramento.</summary>
public partial class MainWindow
{
    private GpuOc? _oc;
    private OcProfile P => _settings.OcCurrent;
    private readonly DispatcherTimer _ocTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<OcSample> _ocSamples = new();
    private IReadOnlyList<(int Mv, int Mhz)>? _vfStock;
    private readonly Dictionary<string, (Slider Slider, TextBox Box)> _ocSliders = new();
    private readonly Dictionary<string, TextBlock> _ocGauges = new();
    private readonly List<(Canvas Canvas, TextBlock Value, Func<OcSample, double?> Get, Func<OcSample, double?>? Limit, string Unit, string Color)> _ocCharts = new();
    private CheckBox? _ocFanAuto;
    private bool _ocSyncing, _ocSaving, _ocFansTouched;
    private int _ocFanCurrent = -1;
    private int? _ocDrag;

    // ---------------- Montagem ----------------

    private void InitOc()
    {
        nint h = OcGpu;
        OcGpuTitle.Text = _gpus.Count > 0 ? "GPU · " + GameCatalog.ShortGpuName(_gpus[0].Name).ToUpperInvariant() : "GPU";
        if (h != IntPtr.Zero)
        {
            uint pci = Nvml.Devices().FirstOrDefault(d => d.Handle == h).Pci.PciDeviceId;
            _oc = GpuOc.Open(h, pci);
        }

        BuildOcGauges();
        BuildOcCharts();
        BuildOcProfiles();
        if (_oc == null)
        {
            OcGpuState.Text = "O overclock está disponível só para placas NVIDIA (driver oficial).";
            BtnOcApply.IsEnabled = BtnOcReset.IsEnabled = BtnOcSave.IsEnabled = false;
        }
        else
        {
            SyncOffsetsFromGpu();
            BuildOcSliders();
            OcApplyOnStart.IsChecked = _settings.OcApplyOnStart;
            OcFanCurveOn.IsChecked = P.FanCurveOn;
            OcFanCurveOn.IsEnabled = _oc.Fans > 0;
            OcFanHyst.Value = P.FanHysteresisC;
            OcGpuState.Text = OcStateText();
            ReadVfCurve();
            if (_settings.OcApplyOnStart && _settings.OcApplied is { } applied && IsAdmin())
            {
                _settings.OcCurrent = applied.Clone();
                OcGpuResult.Text = "Ajustes reaplicados ao abrir: " + string.Join(" ", _oc.Apply(P));
                ApplyFanMode();
                SyncOffsetsFromGpu();
                SyncOcSliders();
            }
            _ocTimer.Tick += (_, _) => OcTick();
            _ocTimer.Start();
            Closed += (_, _) => { _ocTimer.Stop(); if (_ocFansTouched) _oc?.FansAuto(); };
        }

        OcVfCanvas.SizeChanged += (_, _) => DrawVf();
        OcVfCanvas.MouseLeftButtonDown += OcVf_Down;
        OcVfCanvas.MouseMove += OcVf_Move;
        OcVfCanvas.MouseLeftButtonUp += (_, _) => { _ocDrag = null; OcVfCanvas.ReleaseMouseCapture(); _settings.Save(); };
        OcVfCanvas.MouseRightButtonDown += OcVf_Right;
        OcFanCanvas.SizeChanged += (_, _) => DrawFan();
        OcFanCanvas.MouseLeftButtonDown += OcFan_Down;
        OcFanCanvas.MouseMove += OcFan_Move;
        OcFanCanvas.MouseLeftButtonUp += (_, _) => { _ocDrag = null; OcFanCanvas.ReleaseMouseCapture(); _settings.Save(); };
        OcFanCanvas.MouseRightButtonDown += OcFan_Right;

        BuildOcGuide();
        UpdateOcHistory();
        UpdateRecentErrors();
    }

    private static bool IsAdmin() => new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent())
        .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

    /// <summary>Chamado ao abrir a aba: atualiza o resumo e relê a curva.</summary>
    private void UpdateOcGpuState()
    {
        if (_oc == null) return;
        OcGpuState.Text = OcStateText();
        if (_vfStock == null) ReadVfCurve();
    }

    private string OcStateText()
    {
        if (_oc == null) return "";
        var parts = new List<string>();
        if (_oc.Power is { } pw) parts.Add($"energia padrão {pw.DefaultW} W (faixa {pw.MinW}–{pw.MaxW} W)");
        parts.Add(_oc.Fans > 0 ? $"{_oc.Fans} fan(s) controláveis" : "fans sem controle pelo driver");
        parts.Add(_oc.CurveSupported ? "curva V/F disponível" : "curva V/F indisponível");
        return string.Join(" · ", parts) + (_oc.Problem is { } p ? ". " + p : ".");
    }

    private void SyncOffsetsFromGpu()
    {
        if (_oc == null) return;
        var (core, mem, volt) = _oc.ReadOffsets();
        P.CoreOffsetMhz = core; P.MemOffsetMhz = mem; P.VoltagePercent = volt;
        if (_oc.Power is { } pw && Nvml.PowerLimitMw(_oc.NvmlGpu) is uint lim)
            P.PowerPercent = (int)Math.Round(lim / 10.0 / pw.DefaultW);
        if (_oc.TempLimit is { } tl) P.TempLimitC ??= tl.Current;
    }

    private void BuildOcGauges()
    {
        OcGauges.Children.Clear();
        foreach (var (key, label) in new[] { ("core", "GPU CLOCK"), ("mem", "MEM CLOCK"), ("mv", "VOLTAGEM"), ("temp", "TEMPERATURA"), ("power", "CONSUMO"), ("fan", "FAN") })
        {
            var value = new TextBlock { Text = "—", FontSize = 24, FontWeight = FontWeights.SemiBold, FontFamily = (FontFamily)FindResource("Mono") };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("Label") });
            stack.Children.Add(value);
            OcGauges.Children.Add(new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 10, 0), Child = stack });
            _ocGauges[key] = value;
        }
    }

    private void BuildOcSliders()
    {
        if (_oc == null) return;
        OcSliders.Children.Clear();
        _ocSliders.Clear();
        var pw = _oc.Power;
        var tl = _oc.TempLimit;
        bool nvapi = _oc.NvApiGpu != IntPtr.Zero;
        Row("volt", "CORE VOLTAGE (%)", 0, 100, nvapi, v => P.VoltagePercent = v, () => P.VoltagePercent, signed: false);
        Row("power", "POWER LIMIT (%)", pw is { } a ? (int)Math.Ceiling(a.MinW * 100.0 / a.DefaultW) : 100, pw is { } b ? (int)(b.MaxW * 100.0 / b.DefaultW) : 100,
            pw != null, v => P.PowerPercent = v, () => P.PowerPercent, signed: false);
        Row("temp", "TEMP. LIMIT (°C)", tl?.Min ?? 0, tl?.Max ?? 100, tl != null, v => P.TempLimitC = v, () => P.TempLimitC ?? tl?.Current ?? 83, signed: false);
        Row("core", "CORE CLOCK (MHz)", _oc.CoreRangeMhz.Min, _oc.CoreRangeMhz.Max, nvapi, v => { P.CoreOffsetMhz = v; DrawVf(); }, () => P.CoreOffsetMhz, signed: true);
        Row("mem", "MEMORY CLOCK (MHz)", _oc.MemRangeMhz.Min, _oc.MemRangeMhz.Max, nvapi, v => P.MemOffsetMhz = v, () => P.MemOffsetMhz, signed: true);
        var fanRow = Row("fan", "FAN SPEED (%)", (int)_oc.FanRange.Min, (int)_oc.FanRange.Max, _oc.Fans > 0, v => P.FanFixedPercent = v, () => P.FanFixedPercent, signed: false);
        _ocFanAuto = new CheckBox { Content = "AUTO", IsChecked = P.FanAuto, IsEnabled = _oc.Fans > 0, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        _ocFanAuto.Checked += (_, _) => { P.FanAuto = true; UpdateFanSliderState(); };
        _ocFanAuto.Unchecked += (_, _) => { P.FanAuto = false; UpdateFanSliderState(); };
        Grid.SetColumn(_ocFanAuto, 3);
        fanRow.Children.Add(_ocFanAuto);
        UpdateFanSliderState();
        if (pw != null) _ocSliders["power"].Slider.ValueChanged += (_, _) => UpdatePowerHint();
        UpdatePowerHint();

        Grid Row(string key, string label, int min, int max, bool enabled, Action<int> set, Func<int> get, bool signed)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10), IsEnabled = enabled, Opacity = enabled ? 1 : 0.4 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            var name = new TextBlock { Text = label, Style = (Style)FindResource("Label"), VerticalAlignment = VerticalAlignment.Center };
            var slider = new Slider { Minimum = min, Maximum = Math.Max(min, max), TickFrequency = 1, IsSnapToTickEnabled = true, VerticalAlignment = VerticalAlignment.Center, Value = Math.Clamp(get(), min, Math.Max(min, max)) };
            var box = new TextBox { Margin = new Thickness(12, 0, 0, 0), FontFamily = (FontFamily)FindResource("Mono"), HorizontalContentAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            string Format(double v) => signed ? $"{v:+0;-0;0}" : $"{v:0}";
            box.Text = Format(slider.Value);
            slider.ValueChanged += (_, e) => { box.Text = Format(e.NewValue); if (!_ocSyncing) set((int)e.NewValue); };
            void Commit() { if (int.TryParse(box.Text.Replace("+", ""), out int v)) slider.Value = Math.Clamp(v, slider.Minimum, slider.Maximum); box.Text = Format(slider.Value); }
            box.LostFocus += (_, _) => Commit();
            box.KeyDown += (_, e) => { if (e.Key == Key.Enter) Commit(); };
            slider.MouseRightButtonUp += (_, _) => slider.Value = Math.Clamp(signed ? 0 : slider.Value, slider.Minimum, slider.Maximum);
            Grid.SetColumn(slider, 1); Grid.SetColumn(box, 2);
            grid.Children.Add(name); grid.Children.Add(slider); grid.Children.Add(box);
            OcSliders.Children.Add(grid);
            _ocSliders[key] = (slider, box);
            return grid;
        }
    }

    private void UpdatePowerHint()
    {
        if (_oc?.Power is not { } pw || !_ocSliders.TryGetValue("power", out var s)) return;
        s.Slider.ToolTip = $"{pw.DefaultW * s.Slider.Value / 100:0} W";
    }

    private void UpdateFanSliderState()
    {
        if (!_ocSliders.TryGetValue("fan", out var s)) return;
        bool manual = !P.FanAuto && !P.FanCurveOn;
        s.Slider.IsEnabled = s.Box.IsEnabled = manual;
        s.Slider.Opacity = manual ? 1 : 0.45;
        if (_ocFanAuto != null) _ocFanAuto.IsEnabled = _oc?.Fans > 0 && !P.FanCurveOn;
    }

    private void SyncOcSliders()
    {
        _ocSyncing = true;
        void Set(string key, int v) { if (_ocSliders.TryGetValue(key, out var s)) s.Slider.Value = Math.Clamp(v, s.Slider.Minimum, s.Slider.Maximum); }
        Set("volt", P.VoltagePercent); Set("power", P.PowerPercent); Set("temp", P.TempLimitC ?? 0);
        Set("core", P.CoreOffsetMhz); Set("mem", P.MemOffsetMhz); Set("fan", P.FanFixedPercent);
        _ocSyncing = false;
        if (_ocFanAuto != null) _ocFanAuto.IsChecked = P.FanAuto;
        OcFanCurveOn.IsChecked = P.FanCurveOn;
        OcFanHyst.Value = P.FanHysteresisC;
        OcUvMhz.Text = P.UvCapMhz?.ToString() ?? "";
        UpdateFanSliderState();
        DrawVf(); DrawFan();
    }

    // ---------------- Aplicar, reset e perfis ----------------

    private void BtnOcApply_Click(object sender, RoutedEventArgs e)
    {
        if (_oc == null || !RequireAdmin()) return;
        var report = _oc.Apply(P);
        report.Add(ApplyFanMode());
        _settings.OcApplied = P.Clone();
        _settings.Save();
        OcGpuResult.Text = string.Join(" ", report) + " Rode o verificador de estabilidade para confirmar.";
        ReadVfCurve();
    }

    private void BtnOcReset_Click(object sender, RoutedEventArgs e)
    {
        if (_oc == null || !RequireAdmin()) return;
        var report = _oc.Reset();
        _ocFansTouched = false; _ocFanCurrent = -1;
        _settings.OcCurrent = new OcProfile { FanCurve = P.FanCurve, FanHysteresisC = P.FanHysteresisC };
        _settings.OcApplied = null;
        SyncOffsetsFromGpu();
        _settings.Save();
        SyncOcSliders();
        ReadVfCurve();
        OcGpuResult.Text = "Padrão de fábrica: " + string.Join(" ", report);
    }

    /// <summary>Fans: curva (controlada no timer), fixo (manual) ou automático da placa.</summary>
    private string ApplyFanMode()
    {
        if (_oc == null || _oc.Fans == 0) return "";
        if (P.FanCurveOn) { _ocFanCurrent = -1; _ocFansTouched = true; return "Fans: curva ativa."; }
        if (!P.FanAuto)
        {
            _ocFansTouched = true;
            return Nvml.Explain(_oc.SetFans((uint)P.FanFixedPercent)) is { } err ? $"Fans: não aplicado ({err})." : $"Fans: {P.FanFixedPercent}% fixo.";
        }
        if (_ocFansTouched) { _oc.FansAuto(); _ocFansTouched = false; }
        return "Fans: automático.";
    }

    private void BuildOcProfiles()
    {
        OcProfilesPanel.Children.Clear();
        for (int i = 0; i < 5; i++)
        {
            int slot = i;
            bool used = _settings.OcProfiles.Length > slot && _settings.OcProfiles[slot] != null;
            var b = new Button
            {
                Content = (slot + 1).ToString(), Width = 38, Padding = new Thickness(0, 9, 0, 9), Margin = new Thickness(0, 0, 6, 0),
                Style = (Style)FindResource(used ? "PrimaryButton" : "OutlineButton"), IsEnabled = _oc != null,
                ToolTip = used ? $"Perfil {slot + 1}: clique para carregar e aplicar" : $"Perfil {slot + 1}: vazio",
            };
            b.Click += (_, _) => OcProfileClick(slot);
            OcProfilesPanel.Children.Add(b);
        }
    }

    private void BtnOcSave_Click(object sender, RoutedEventArgs e)
    {
        _ocSaving = !_ocSaving;
        BtnOcSave.Content = _ocSaving ? "ESCOLHA 1–5…" : "SALVAR";
    }

    private void OcProfileClick(int slot)
    {
        if (_settings.OcProfiles.Length < 5) { var grown = new OcProfile?[5]; _settings.OcProfiles.CopyTo(grown, 0); _settings.OcProfiles = grown; }
        if (_ocSaving)
        {
            _settings.OcProfiles[slot] = P.Clone();
            _settings.Save();
            _ocSaving = false; BtnOcSave.Content = "SALVAR";
            BuildOcProfiles();
            OcGpuResult.Text = $"Ajustes salvos no perfil {slot + 1}.";
            return;
        }
        if (_settings.OcProfiles[slot] is not { } saved) { OcGpuResult.Text = $"O perfil {slot + 1} está vazio. Use SALVAR e escolha o número."; return; }
        _settings.OcCurrent = saved.Clone();
        SyncOcSliders();
        BtnOcApply_Click(this, new RoutedEventArgs());
    }

    private void OcApplyOnStart_Changed(object sender, RoutedEventArgs e)
    {
        _settings.OcApplyOnStart = OcApplyOnStart.IsChecked == true;
        _settings.Save();
    }

    // ---------------- Monitoramento e controle dos fans (1 s) ----------------

    private void OcTick()
    {
        if (_oc == null) return;
        OcSample s;
        try { s = _oc.Sample(); } catch (Exception) { return; }
        _ocSamples.Add(s);
        if (_ocSamples.Count > 120) _ocSamples.RemoveAt(0);

        if (P.FanCurveOn && _oc.Fans > 0 && s.TempC is uint t)
        {
            int next = _ocFanCurrent < 0 ? FanCurveMath.SpeedAt(P.FanCurve, t) : FanCurveMath.Next(P.FanCurve, t, _ocFanCurrent, P.FanHysteresisC);
            if (next != _ocFanCurrent && _oc.SetFans((uint)next) == 0) { _ocFanCurrent = next; _ocFansTouched = true; }
        }

        if (PageOc.Visibility != Visibility.Visible) return;
        _ocGauges["core"].Text = s.CoreMhz is uint c ? $"{c} MHz" : "—";
        _ocGauges["mem"].Text = s.MemMhz is uint m ? $"{m} MHz" : "—";
        _ocGauges["mv"].Text = s.CoreMv is int mv ? $"{mv} mV" : "—";
        _ocGauges["temp"].Text = s.TempC is uint tc ? $"{tc} °C" : "—";
        _ocGauges["temp"].Foreground = s.TempC is uint tb ? TempBrush(tb) : Fg;
        _ocGauges["power"].Text = s.PowerW is double w ? $"{w:0} W" : "—";
        _ocGauges["fan"].Text = s.FanPercent is uint f ? $"{f}%" : "—";
        OcLimiter.Text = "Limitado por: " + GpuOc.Limiter(s.Limiters) + (s.UsagePercent is uint u ? $" · uso {u}%" : "");
        DrawOcCharts();
        DrawFan();
        if (_vfStock != null) DrawVf();
    }

    private void BuildOcCharts()
    {
        OcCharts.Children.Clear();
        _ocCharts.Clear();
        Add("GPU CLOCK", s => s.CoreMhz, null, "MHz", "Accent");
        Add("MEMORY CLOCK", s => s.MemMhz, null, "MHz", "RingVram");
        Add("TEMPERATURA", s => s.TempC, null, "°C", "TempHot");
        Add("CONSUMO × LIMITE (CURVA DE POTÊNCIA)", s => s.PowerW, s => s.PowerLimitW, "W", "ChartGpu");
        Add("FAN", s => s.FanPercent, null, "%", "RingRam");
        Add("VOLTAGEM", s => s.CoreMv, null, "mV", "TempWarn");

        void Add(string title, Func<OcSample, double?> get, Func<OcSample, double?>? limit, string unit, string color)
        {
            var canvas = new Canvas { Height = 80, ClipToBounds = true, Margin = new Thickness(0, 6, 0, 0) };
            var value = new TextBlock { FontFamily = (FontFamily)FindResource("Mono"), HorizontalAlignment = HorizontalAlignment.Right, Foreground = (Brush)FindResource(color) };
            var head = new Grid();
            head.Children.Add(new TextBlock { Text = title, Style = (Style)FindResource("Label") });
            head.Children.Add(value);
            var stack = new StackPanel();
            stack.Children.Add(head); stack.Children.Add(canvas);
            OcCharts.Children.Add(new Border { Child = stack, Margin = new Thickness(0, 0, 14, 14), Padding = new Thickness(12), Background = (Brush)FindResource("Bg"), CornerRadius = new CornerRadius(10) });
            _ocCharts.Add((canvas, value, get, limit, unit, color));
        }
    }

    private void DrawOcCharts()
    {
        foreach (var (canvas, value, get, limit, unit, color) in _ocCharts)
        {
            canvas.Children.Clear();
            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            var data = _ocSamples.Select(get).ToList();
            var lim = limit == null ? null : _ocSamples.Select(limit).ToList();
            var all = data.Concat(lim ?? new()).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            value.Text = data.LastOrDefault() is double last ? $"{last:0} {unit}" + (lim?.LastOrDefault() is double l ? $" / {l:0} {unit}" : "") : "—";
            if (w <= 0 || all.Count < 2) continue;
            double min = all.Min(), max = all.Max();
            if (max - min < 1) { min -= 1; max += 1; }
            double pad = (max - min) * 0.1; min -= pad; max += pad;
            Polyline Line(List<double?> vals, Brush brush, bool dashed)
            {
                var pl = new Polyline { Stroke = brush, StrokeThickness = dashed ? 1 : 1.8 };
                if (dashed) pl.StrokeDashArray = new DoubleCollection { 4, 3 };
                for (int i = 0; i < vals.Count; i++)
                    if (vals[i] is double v) pl.Points.Add(new Point(w - (vals.Count - 1 - i) * w / 119.0, h - (v - min) / (max - min) * h));
                return pl;
            }
            if (lim != null) canvas.Children.Add(Line(lim, (Brush)FindResource("TempCrit"), dashed: true));
            canvas.Children.Add(Line(data, (Brush)FindResource(color), dashed: false));
        }
    }

    // ---------------- Curva V/F ----------------

    private void BtnOcReadCurve_Click(object sender, RoutedEventArgs e) => ReadVfCurve();

    private void ReadVfCurve()
    {
        _vfStock = null;
        if (_oc?.CurveSupported == true)
        {
            try { _vfStock = _oc.ReadCurve()?.Stock; }
            catch (Exception ex) { OcVfInfo.Text = "Não foi possível ler a curva: " + ex.Message; }
        }
        DrawVf();
    }

    private (double X0, double X1, double Y0, double Y1) VfRange(int[] target)
    {
        var s = _vfStock!;
        double y0 = Math.Min(s.Min(p => p.Mhz), target.Min()) - 100, y1 = Math.Max(s.Max(p => p.Mhz), target.Max()) + 150;
        return (s[0].Mv - 10, s[^1].Mv + 10, Math.Max(0, y0), y1);
    }

    private Point VfPoint(double mv, double mhz, (double X0, double X1, double Y0, double Y1) r)
    {
        double w = OcVfCanvas.ActualWidth, h = OcVfCanvas.ActualHeight;
        return new Point(44 + (mv - r.X0) / (r.X1 - r.X0) * (w - 52), (h - 22) - (mhz - r.Y0) / (r.Y1 - r.Y0) * (h - 30));
    }

    private void DrawVf()
    {
        if (!IsLoaded || OcVfCanvas == null) return;
        OcVfCanvas.Children.Clear();
        if (_vfStock == null)
        {
            if (_oc?.CurveSupported == true && string.IsNullOrEmpty(OcVfInfo.Text))
                OcVfInfo.Text = "A curva não pôde ser lida agora (a placa estava trocando de estado de energia). Clique em RELER CURVA.";
            else if (_oc?.CurveSupported != true) OcVfInfo.Text = "Editor de curva indisponível nesta placa ou driver.";
            return;
        }
        if (OcVfCanvas.ActualWidth < 100) return;
        int[] target = VfCurveMath.TargetMhz(_vfStock, P);
        var r = VfRange(target);
        var faint = (Brush)FindResource("Line");
        var muted = (Brush)FindResource("Muted");
        for (int mv = (int)Math.Ceiling(r.X0 / 100) * 100; mv <= r.X1; mv += 100)
        {
            var a = VfPoint(mv, r.Y0, r); var b = VfPoint(mv, r.Y1, r);
            OcVfCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = faint });
            Label($"{mv}", a.X - 12, a.Y + 3);
        }
        for (int mhz = (int)Math.Ceiling(r.Y0 / 250) * 250; mhz <= r.Y1; mhz += 250)
        {
            var a = VfPoint(r.X0, mhz, r); var b = VfPoint(r.X1, mhz, r);
            OcVfCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = faint });
            Label($"{mhz}", 0, a.Y - 8);
        }
        var stock = new Polyline { Stroke = (Brush)FindResource("LineStrong"), StrokeThickness = 2 };
        var edit = new Polyline { Stroke = (Brush)FindResource("Accent"), StrokeThickness = 2 };
        for (int i = 0; i < _vfStock.Count; i++)
        {
            stock.Points.Add(VfPoint(_vfStock[i].Mv, _vfStock[i].Mhz, r));
            edit.Points.Add(VfPoint(_vfStock[i].Mv, target[i], r));
        }
        OcVfCanvas.Children.Add(stock);
        OcVfCanvas.Children.Add(edit);
        for (int i = 1; i < _vfStock.Count; i++)
        {
            var p = VfPoint(_vfStock[i].Mv, target[i], r);
            bool edited = P.PointOffsetsMhz.ContainsKey(i);
            var dot = new Ellipse { Width = edited ? 7 : 5, Height = edited ? 7 : 5, Fill = edited ? (Brush)FindResource("TempWarn") : (Brush)FindResource("Accent") };
            Canvas.SetLeft(dot, p.X - dot.Width / 2); Canvas.SetTop(dot, p.Y - dot.Height / 2);
            OcVfCanvas.Children.Add(dot);
        }
        if (P.UvCapMv is int cap)
        {
            int ci = VfCurveMath.NearestIndex(_vfStock, cap);
            var a = VfPoint(_vfStock[ci].Mv, r.Y0, r); var b = VfPoint(_vfStock[ci].Mv, r.Y1, r);
            OcVfCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = (Brush)FindResource("TempWarn"), StrokeDashArray = new DoubleCollection { 4, 3 } });
        }
        if (_ocSamples.LastOrDefault() is { CoreMv: int lmv, CoreMhz: uint lmhz } && lmv >= r.X0 && lmv <= r.X1)
        {
            var p = VfPoint(lmv, lmhz, r);
            var now = new Ellipse { Width = 10, Height = 10, Stroke = (Brush)FindResource("ChartGpu"), StrokeThickness = 2 };
            Canvas.SetLeft(now, p.X - 5); Canvas.SetTop(now, p.Y - 5);
            OcVfCanvas.Children.Add(now);
        }

        int capIdx = P.UvCapMv is int cm ? VfCurveMath.NearestIndex(_vfStock, cm) : -1;
        OcVfInfo.Text = (capIdx >= 0 ? $"Undervolt: teto em {_vfStock[capIdx].Mv} mV a {target[capIdx]} MHz (fábrica nessa tensão: {_vfStock[capIdx].Mhz} MHz)." : "Sem teto de undervolt.")
            + $" Pico: {target.Max()} MHz (fábrica {_vfStock.Max(p => p.Mhz)} MHz). Core {P.CoreOffsetMhz:+0;-0;0} MHz"
            + (P.PointOffsetsMhz.Count > 0 ? $", {P.PointOffsetsMhz.Count} ponto(s) editado(s)." : ".") + " Círculo verde = ponto em uso agora. Clique em APLICAR para gravar.";

        void Label(string text, double x, double y)
        {
            var t = new TextBlock { Text = text, FontSize = 10, Foreground = muted };
            Canvas.SetLeft(t, x); Canvas.SetTop(t, y);
            OcVfCanvas.Children.Add(t);
        }
    }

    private int? NearestVfPoint(Point at)
    {
        if (_vfStock == null) return null;
        int[] target = VfCurveMath.TargetMhz(_vfStock, P);
        var r = VfRange(target);
        int? best = null; double bestD = 14;
        for (int i = 1; i < _vfStock.Count; i++)
        {
            var p = VfPoint(_vfStock[i].Mv, target[i], r);
            double d = Math.Abs(p.X - at.X) + Math.Abs(p.Y - at.Y) * 0.3;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private void OcVf_Down(object sender, MouseButtonEventArgs e)
    {
        _ocDrag = NearestVfPoint(e.GetPosition(OcVfCanvas));
        if (_ocDrag != null) OcVfCanvas.CaptureMouse();
    }

    private void OcVf_Move(object sender, MouseEventArgs e)
    {
        if (_ocDrag is not int i || _vfStock == null || e.LeftButton != MouseButtonState.Pressed) return;
        var r = VfRange(VfCurveMath.TargetMhz(_vfStock, P));
        double h = OcVfCanvas.ActualHeight;
        int mhz = (int)(Math.Round((r.Y0 + ((h - 22) - e.GetPosition(OcVfCanvas).Y) / (h - 30) * (r.Y1 - r.Y0)) / 5) * 5);
        int extra = P.PointOffsetsMhz.TryGetValue(i, out int o) ? o : 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            int core = Math.Clamp(mhz - _vfStock[i].Mhz - extra, _oc?.CoreRangeMhz.Min ?? -1000, _oc?.CoreRangeMhz.Max ?? 1000);
            if (_ocSliders.TryGetValue("core", out var s)) s.Slider.Value = core; else P.CoreOffsetMhz = core;
        }
        else
        {
            int point = Math.Clamp(mhz - _vfStock[i].Mhz - P.CoreOffsetMhz, -VfCurveMath.MaxDeltaMhz, VfCurveMath.MaxDeltaMhz);
            if (point == 0) P.PointOffsetsMhz.Remove(i); else P.PointOffsetsMhz[i] = point;
        }
        DrawVf();
    }

    private void OcVf_Right(object sender, MouseButtonEventArgs e)
    {
        if (NearestVfPoint(e.GetPosition(OcVfCanvas)) is not int i || _vfStock == null) return;
        P.UvCapMv = _vfStock[i].Mv;
        P.UvCapMhz = int.TryParse(OcUvMhz.Text, out int mhz) ? mhz : null;
        _settings.Save();
        DrawVf();
    }

    private void OcUvMhz_Changed(object sender, RoutedEventArgs e)
    {
        P.UvCapMhz = int.TryParse(OcUvMhz.Text, out int mhz) && mhz is > 200 and < 4000 ? mhz : null;
        OcUvMhz.Text = P.UvCapMhz?.ToString() ?? "";
        _settings.Save();
        DrawVf();
    }

    private void OcUvMhz_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) OcUvMhz_Changed(sender, e); }

    private void BtnOcUvClear_Click(object sender, RoutedEventArgs e)
    {
        P.UvCapMv = null; P.UvCapMhz = null; OcUvMhz.Text = "";
        _settings.Save(); DrawVf();
    }

    private void BtnOcPointsClear_Click(object sender, RoutedEventArgs e)
    {
        P.PointOffsetsMhz.Clear();
        _settings.Save(); DrawVf();
    }

    // ---------------- Curva de fans ----------------

    private Point FanPt(double temp, double pct)
    {
        double w = OcFanCanvas.ActualWidth, h = OcFanCanvas.ActualHeight;
        return new Point(34 + (temp - 20) / 80 * (w - 42), (h - 20) - pct / 100 * (h - 28));
    }

    private (int Temp, int Pct) FanFromPoint(Point p)
    {
        double w = OcFanCanvas.ActualWidth, h = OcFanCanvas.ActualHeight;
        return ((int)Math.Round(Math.Clamp(20 + (p.X - 34) / (w - 42) * 80, 20, 100)),
                (int)Math.Round(Math.Clamp(((h - 20) - p.Y) / (h - 28) * 100, 0, 100)));
    }

    private void DrawFan()
    {
        if (!IsLoaded || OcFanCanvas.ActualWidth < 100) return;
        OcFanCanvas.Children.Clear();
        var faint = (Brush)FindResource("Line");
        var muted = (Brush)FindResource("Muted");
        for (int t = 20; t <= 100; t += 10)
        {
            var a = FanPt(t, 0); var b = FanPt(t, 100);
            OcFanCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = faint });
            Label($"{t}°", a.X - 8, a.Y + 3);
        }
        for (int pct = 0; pct <= 100; pct += 20)
        {
            var a = FanPt(20, pct); var b = FanPt(100, pct);
            OcFanCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = faint });
            Label($"{pct}%", 0, a.Y - 8);
        }
        var pts = P.FanCurve.OrderBy(p => p.TempC).ToList();
        var line = new Polyline { Stroke = (Brush)FindResource(P.FanCurveOn ? "Accent" : "LineStrong"), StrokeThickness = 2 };
        line.Points.Add(FanPt(20, pts[0].Percent));
        foreach (var p in pts) line.Points.Add(FanPt(p.TempC, p.Percent));
        line.Points.Add(FanPt(100, pts[^1].Percent));
        OcFanCanvas.Children.Add(line);
        foreach (var p in pts)
        {
            var c = FanPt(p.TempC, p.Percent);
            var dot = new Ellipse { Width = 10, Height = 10, Fill = (Brush)FindResource(P.FanCurveOn ? "Accent" : "Muted") };
            Canvas.SetLeft(dot, c.X - 5); Canvas.SetTop(dot, c.Y - 5);
            OcFanCanvas.Children.Add(dot);
        }
        var last = _ocSamples.LastOrDefault();
        if (last?.TempC is uint temp)
        {
            double tc = Math.Clamp(temp, 20, 100);
            var a = FanPt(tc, 0); var b = FanPt(tc, 100);
            OcFanCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = (Brush)FindResource("TempHot"), StrokeDashArray = new DoubleCollection { 4, 3 } });
            if (last.FanPercent is uint f)
            {
                var c = FanPt(tc, f);
                var now = new Ellipse { Width = 12, Height = 12, Stroke = (Brush)FindResource("ChartGpu"), StrokeThickness = 2 };
                Canvas.SetLeft(now, c.X - 6); Canvas.SetTop(now, c.Y - 6);
                OcFanCanvas.Children.Add(now);
            }
            OcFanInfo.Text = $"Agora: {temp} °C · fan {(last.FanPercent is uint fp ? fp + "%" : "—")}"
                + (P.FanCurveOn ? $" · a curva pede {FanCurveMath.SpeedAt(P.FanCurve, temp)}%" : " · curva desligada (fans no modo dos sliders)");
        }

        void Label(string text, double x, double y)
        {
            var t = new TextBlock { Text = text, FontSize = 10, Foreground = muted };
            Canvas.SetLeft(t, x); Canvas.SetTop(t, y);
            OcFanCanvas.Children.Add(t);
        }
    }

    private int? NearestFanPoint(Point at)
    {
        for (int i = 0; i < P.FanCurve.Count; i++)
        {
            var c = FanPt(P.FanCurve[i].TempC, P.FanCurve[i].Percent);
            if (Math.Abs(c.X - at.X) < 10 && Math.Abs(c.Y - at.Y) < 10) return i;
        }
        return null;
    }

    private void OcFan_Down(object sender, MouseButtonEventArgs e)
    {
        var at = e.GetPosition(OcFanCanvas);
        if (e.ClickCount == 2 && NearestFanPoint(at) == null)
        {
            var (t, pct) = FanFromPoint(at);
            if (P.FanCurve.All(p => p.TempC != t)) P.FanCurve = P.FanCurve.Append(new FanPoint(t, pct)).OrderBy(p => p.TempC).ToList();
            _settings.Save(); DrawFan();
            return;
        }
        _ocDrag = NearestFanPoint(at);
        if (_ocDrag != null) OcFanCanvas.CaptureMouse();
    }

    private void OcFan_Move(object sender, MouseEventArgs e)
    {
        if (_ocDrag is not int i || e.LeftButton != MouseButtonState.Pressed) return;
        var (t, pct) = FanFromPoint(e.GetPosition(OcFanCanvas));
        var sorted = P.FanCurve;
        // Cada ponto fica entre os vizinhos, para a curva nunca "voltar".
        int lo = i > 0 ? sorted[i - 1].TempC + 1 : 20, hi = i < sorted.Count - 1 ? sorted[i + 1].TempC - 1 : 100;
        sorted[i] = new FanPoint(Math.Clamp(t, lo, Math.Max(lo, hi)), pct);
        _ocFanCurrent = -1;
        DrawFan();
    }

    private void OcFan_Right(object sender, MouseButtonEventArgs e)
    {
        if (NearestFanPoint(e.GetPosition(OcFanCanvas)) is int i && P.FanCurve.Count > 2)
        {
            P.FanCurve.RemoveAt(i);
            _settings.Save(); DrawFan();
        }
    }

    private void OcFanCurveOn_Changed(object sender, RoutedEventArgs e)
    {
        if (_ocSyncing || _oc == null) return;
        bool on = OcFanCurveOn.IsChecked == true;
        if (on == P.FanCurveOn) return;
        if (on && !RequireAdmin()) { OcFanCurveOn.IsChecked = false; return; }
        P.FanCurveOn = on;
        _ocFanCurrent = -1;
        OcGpuResult.Text = ApplyFanMode();
        _settings.Save();
        UpdateFanSliderState();
        DrawFan();
    }

    private void OcFanHyst_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OcFanHystValue == null) return;
        OcFanHystValue.Text = $"{e.NewValue:0} °C";
        if (_ocSyncing || _oc == null) return;
        P.FanHysteresisC = (int)e.NewValue;
        _settings.Save();
    }

    private void BtnOcFanDefault_Click(object sender, RoutedEventArgs e)
    {
        P.FanCurve = FanCurveMath.Default();
        _ocFanCurrent = -1;
        _settings.Save(); DrawFan();
    }
}
