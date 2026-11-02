using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using ResetExplode.Modular;

namespace ResetExplode;
public partial class MainWindow
{
    private ScrollViewer PageUltimate = new() { Name = "PageUltimate" };
    private readonly ModuleEngine updateEngine = new(new RegistryStore(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcismoMaster", "ModuleBackups"));
    private Module UpdateModule()
    {
        var now = DateTime.UtcNow;
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcismoMaster", "ModuleBackups", "updates-pause.json");
        if (File.Exists(file))
        {
            var saved = System.Text.Json.JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(file))!;
            return new("updates-pause", "Pausar atualizações", "Windows", "", "12 Updates Pause.ps1", saved.Settings);
        }
        return new("updates-pause", "Pausar atualizações", "Windows", "", "12 Updates Pause.ps1",
            new[] { "PauseUpdatesExpiryTime", "PauseFeatureUpdatesEndTime", "PauseFeatureUpdatesStartTime", "PauseQualityUpdatesEndTime", "PauseQualityUpdatesStartTime", "PauseUpdatesStartTime" }
            .Select(n => new Setting("HKLM", @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", n, 0, (n.Contains("Start") ? now : now.AddDays(365)).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))).ToArray());
    }
    private void InitUltimate()
    {
        var nav = new RadioButton { Content = "OTIMIZAÇÃO", Tag = "PageUltimate", Style = (Style)FindResource("NavItem") };
        nav.Checked += Nav_Checked;
        nav.Checked += (_, _) => TitlePage.Text = "OTIMIZAÇÃO";
        ((StackPanel)GrpMaker.Parent).Children.Add(nav);
        PageUltimate.Visibility = Visibility.Collapsed;
        PageUltimate.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ((Panel)PageDash.Parent).Children.Add(PageUltimate);
        var panel = new StackPanel { Margin = new Thickness(28) }; PageUltimate.Content = panel;
        panel.Children.Add(new TextBlock { Text = "OTIMIZAÇÃO", FontSize = 28, Margin = new Thickness(0,0,0,20) });
        panel.Children.Add(new TextBlock { Text = "Pausar Windows Update por 365 dias", FontSize = 20, Margin = new Thickness(0,0,0,12) });
        panel.Children.Add(new TextBlock { Text = "Implementação do Ultimate: grava seis datas de pausa no Windows Update. Inclui atualizações de recursos e de segurança. A versão do Windows pode limitar ou ignorar esse prazo. Nenhuma alteração é aplicada ao abrir esta aba.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,20) });
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,16) }; panel.Children.Add(status);
        void Refresh() { try { var m = UpdateModule(); status.Text = updateEngine.HasBackup(m) ? "Backup disponível. Você pode restaurar a configuração anterior." : "Nenhuma pausa aplicada pelo PcismoMaster."; } catch(Exception ex) { status.Text = ex.Message; } }
        foreach (var label in new[] { "Pausar por 365 dias", "Restaurar configuração anterior", "Abrir Windows Update" })
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,0,0,12), Padding = new Thickness(18,10,18,10) };
            panel.Children.Add(button);
            button.Click += (_, _) => {
                try {
                    if (label == "Abrir Windows Update") { Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true }); return; }
                    var m = UpdateModule();
                    if (MessageBox.Show(this, label + "? A pausa inclui correções de segurança. Restaurar recupera os valores anteriores, que também podem conter uma pausa.", "Windows Update", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
                    if (label.StartsWith("Pausar")) updateEngine.Enable(m); else updateEngine.Restore(m);
                    Refresh();
                } catch(Exception ex) { MessageBox.Show(this, ex.Message, "Operação não concluída"); }
            };
        }
        Refresh();
    }
}
