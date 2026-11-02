using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ResetExplode;

/// <summary>FERRAMENTAS · Drivers: instalar o driver NVIDIA em silêncio e as configurações recomendadas da placa de vídeo.</summary>
public partial class MainWindow
{
    private bool _gfxInit, _gfxBusy;
    private readonly Dictionary<string, CheckBox> _gfxToggles = new();

    // Interruptores que não são do registro: as configurações do perfil global (Profile Inspector)
    private const string DrsNip = "gfx-nv-drs", DrsGsync = "gfx-nv-gsync";

    private async void InitGfxTweaks()
    {
        if (_gfxInit) return;
        _gfxInit = true;
        GfxStatus.Text = "Lendo o que já está aplicado…";
        // Quais fabricantes existem neste PC (pelo código PCI, mesmo sem driver).
        var vendors = await Task.Run(() => DriverManager.ReadGpus().Select(g => g.Vendor.ToLowerInvariant()).Where(v => v.Length > 0).Distinct().ToList());
        bool nvidia = vendors.Contains("nvidia");
        GfxNvInstallRow.Visibility = nvidia ? Visibility.Visible : Visibility.Collapsed;
        if (DebloatRows.Children.Count == 0) InitDebloat(vendors);
        var applied = await Task.Run(() => GraphicsTweaks.All.ToDictionary(t => t.Id, GraphicsTweaks.IsApplied));
        bool firstTime = !applied.Values.Any(x => x) && !GraphicsTweaks.DrsApplied(DrsNip) && !GraphicsTweaks.DrsApplied(DrsGsync);

        void Section(string title)
            => GfxRows.Children.Add(new TextBlock { Text = title, Style = (Style)FindResource("Label"), Margin = new Thickness(0, 10, 0, 8) });

        foreach (var (vendor, title) in new[] { ("nvidia", "NVIDIA"), ("amd", "AMD"), ("intel", "INTEL"), ("all", "WINDOWS · TODAS AS PLACAS") })
        {
            if (vendor != "all" && !vendors.Contains(vendor)) continue;
            Section(title);
            if (vendor == "nvidia")
            {
                GfxRow(DrsNip, "Perfil global recomendado (Profile Inspector)", false,
                    "Low Latency Ultra, VSync forçado desligado, energia em desempenho máximo, filtragem em alto desempenho, cache de shader ilimitado, " +
                    "otimização em threads e mais (o .nip do script). Antes, o valor atual de cada um é guardado.", GraphicsTweaks.DrsApplied(DrsNip), firstTime);
                GfxRow(DrsGsync, "Desligar o G-SYNC", true,
                    "Desliga o G-SYNC em todos os jogos (parte do script). Deixe desligado se você usa monitor G-SYNC/FreeSync.", GraphicsTweaks.DrsApplied(DrsGsync), false);
            }
            foreach (var t in GraphicsTweaks.All.Where(t => t.Vendor == vendor))
                GfxRow(t.Id, t.Title, t.Advanced, t.Description, applied[t.Id], firstTime && t.DefaultOn);
        }
        GfxStatus.Text = applied.Values.Any(x => x) || !firstTime
            ? "Os marcados como APLICADO já estão valendo. Desmarque e clique em Aplicar para desfazer um ajuste."
            : "Os recomendados já vêm marcados (os AVANÇADOS ficam desligados). Clique em Aplicar.";
    }

    /// <summary>Linha com título, selo SEGURO/AVANÇADO (e APLICADO), descrição e o interruptor.</summary>
    private void GfxRow(string id, string title, bool advanced, string desc, bool isApplied, bool defaultOn, Panel? target = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var head = new WrapPanel();
        head.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        var badge = new Border { Style = (Style)FindResource("MaintBadge"), Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock { Text = advanced ? "AVANÇADO" : "SEGURO" } };
        if (advanced) { badge.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xCF, 0x4D)); TextElement.SetForeground(badge, (Brush)FindResource("TempWarn")); }
        head.Children.Add(badge);
        if (isApplied)
        {
            var on = new Border { Style = (Style)FindResource("MaintBadge"), Padding = new Thickness(7, 2, 7, 2),
                Background = new SolidColorBrush(Color.FromArgb(0x26, 0x4E, 0xD1, 0x7A)), Child = new TextBlock { Text = "APLICADO" } };
            TextElement.SetForeground(on, (Brush)FindResource("ChartGpu"));
            head.Children.Add(on);
        }
        var text = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
        text.Children.Add(head);
        text.Children.Add(new TextBlock { Text = desc, Foreground = (Brush)FindResource("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
        grid.Children.Add(text);
        var sw = new CheckBox { VerticalAlignment = VerticalAlignment.Center, IsChecked = isApplied || defaultOn, Tag = isApplied };
        Grid.SetColumn(sw, 1);
        grid.Children.Add(sw);
        (target ?? GfxRows).Children.Add(grid);
        _gfxToggles[id] = sw;
    }

    private void RebuildGfx() { GfxRows.Children.Clear(); _gfxToggles.Clear(); _gfxInit = false; InitGfxTweaks(); }

    private void BtnGfxDefaults_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (id, sw) in _gfxToggles)
            sw.IsChecked = id == DrsNip || GraphicsTweaks.All.FirstOrDefault(t => t.Id == id) is { DefaultOn: true };
    }

    private async void BtnGfxApply_Click(object sender, RoutedEventArgs e)
    {
        if (_gfxBusy || !RequireAdmin()) return;
        var turnOn = _gfxToggles.Where(kv => kv.Value.IsChecked == true && kv.Value.Tag is false).Select(kv => kv.Key).ToList();
        var turnOff = _gfxToggles.Where(kv => kv.Value.IsChecked != true && kv.Value.Tag is true).Select(kv => kv.Key).ToList();
        if (turnOn.Count + turnOff.Count == 0) { GfxStatus.Text = "Nada mudou: os marcados já estão aplicados."; return; }
        if (turnOn.Contains("gfx-nv-hdcp") && MessageBox.Show(this,
                "Desligar o HDCP faz Netflix, Disney+, Prime Video e Blu-ray pararem de tocar no PC (ou tocarem em baixa qualidade). Continuar?",
                "Desligar o HDCP", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        if (MessageBox.Show(this, $"Aplicar {turnOn.Count} ajuste(s) e desfazer {turnOff.Count}? O valor anterior de cada um é guardado.",
                "Configurações da placa de vídeo", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

        _gfxBusy = true;
        BtnGfxApply.IsEnabled = false;
        GfxStatus.Text = "Aplicando…";
        var errors = new List<string>();
        await Task.Run(() =>
        {
            foreach (var id in turnOff) Try(id, () =>
            {
                if (id == DrsNip || id == DrsGsync) GraphicsTweaks.RestoreDrs(id);
                else GraphicsTweaks.Engine.Restore(GraphicsTweaks.ModuleFor(GraphicsTweaks.All.First(t => t.Id == id)));
            });
            foreach (var id in turnOn) Try(id, () =>
            {
                if (id == DrsNip) GraphicsTweaks.ApplyDrs(id, GraphicsTweaks.NipSettings);
                else if (id == DrsGsync) GraphicsTweaks.ApplyDrs(id, GraphicsTweaks.GsyncOff);
                else
                {
                    var m = GraphicsTweaks.ModuleFor(GraphicsTweaks.All.First(t => t.Id == id));
                    if (m.Settings.Length == 0) throw new InvalidOperationException("nenhuma chave deste ajuste neste PC");
                    GraphicsTweaks.Engine.Enable(m);
                }
            });
        });
        void Try(string id, Action a) { try { a(); } catch (Exception ex) { lock (errors) errors.Add($"{id}: {ex.Message}"); } }

        _gfxBusy = false;
        BtnGfxApply.IsEnabled = true;
        BtnGfxReboot.Visibility = Visibility.Visible;
        RebuildGfx();
        await Task.Delay(300);
        GfxStatus.Text = (errors.Count == 0 ? $"Pronto: {turnOn.Count} aplicado(s), {turnOff.Count} desfeito(s)." : $"Concluído com {errors.Count} erro(s): " + string.Join(" · ", errors))
                         + " Alguns ajustes (modo MSI, P-state, HDCP) só valem depois de reiniciar.";
    }

    private void BtnGfxUndoAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var sw in _gfxToggles.Values) sw.IsChecked = false;
        BtnGfxApply_Click(sender, e);
    }

    private void BtnGfxReboot_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Reiniciar o PC agora? Salve e feche seus programas.", "Reiniciar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 5") { UseShellExecute = false, CreateNoWindow = true });
    }

    /// <summary>Driver NVIDIA oficial (assinatura conferida) instalado em silêncio e do zero; depois, Painel de Controle e as configurações.</summary>
    private async void BtnGfxNvInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_gfxBusy || !RequireAdmin()) return;
        var g = _drvGpus.FirstOrDefault(x => x.Vendor == "NVIDIA") ?? DriverManager.ReadGpus().FirstOrDefault(x => x.Vendor == "NVIDIA");
        if (g == null) { GfxStatus.Text = "Nenhuma placa NVIDIA encontrada."; return; }
        if (MessageBox.Show(this,
                "Instalar o driver NVIDIA mais recente em modo silencioso e limpo?\n\n" +
                "• Baixa do servidor oficial e confere a assinatura.\n• Instala sem telas; a imagem pisca algumas vezes.\n" +
                "• \"Limpo\" apaga as configurações antigas do driver (aplique as recomendadas depois).\n• Não reinicia sozinho.",
                "Driver NVIDIA", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        _gfxBusy = true;
        BtnGfxNvInstall.IsEnabled = false;
        try
        {
            if (g.DownloadUrl.Length == 0) { GfxStatus.Text = "Procurando o driver mais recente…"; await DriverManager.CheckNvidiaAsync(g, DrvStudio.IsChecked == true, CancellationToken.None); }
            if (g.DownloadUrl.Length == 0) { GfxStatus.Text = g.Message; return; }
            string file = await DriverManager.DownloadAsync(g, (t, _) => GfxStatus.Text = t, CancellationToken.None);
            GfxStatus.Text = $"Instalando o driver {g.Latest} em silêncio… (pode levar alguns minutos; a tela vai piscar)";
            int code = await GraphicsTweaks.InstallNvidiaSilentAsync(file, CancellationToken.None);
            string panel = "";
            if (!GraphicsTweaks.HasNvidiaControlPanel())
            {
                try { Process.Start(new ProcessStartInfo(GraphicsTweaks.NvidiaControlPanelStore) { UseShellExecute = true }); } catch { }
                panel = " A página do Painel de Controle NVIDIA na Microsoft Store foi aberta: clique em Obter.";
            }
            GfxStatus.Text = (code == 0 ? $"Driver {g.Latest} instalado." : $"O instalador terminou com o código {code}; confira se o driver aparece no Gerenciador de Dispositivos.")
                             + panel + " Agora aplique as configurações recomendadas e reinicie. " + GpuDriverStatus.NvidiaAppTips;
            BtnGfxReboot.Visibility = Visibility.Visible;
        }
        catch (Exception ex) { GfxStatus.Text = "Não foi possível instalar: " + ex.Message; }
        finally { _gfxBusy = false; BtnGfxNvInstall.IsEnabled = true; }
    }
}
