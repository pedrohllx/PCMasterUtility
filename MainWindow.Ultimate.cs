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
        var tabs = new TabControl { TabStripPlacement = Dock.Left, Margin = new Thickness(16) };
        PageUltimate.Content = tabs;
        var panel = new StackPanel { Margin = new Thickness(28) };
        tabs.Items.Add(new TabItem { Header = "Updates Pause", Content = panel });
        var background = new StackPanel { Margin = new Thickness(28) };
        tabs.Items.Add(new TabItem { Header = "Background Apps", Content = background });
        var store = new StackPanel { Margin = new Thickness(28) };
        tabs.Items.Add(new TabItem { Header = "Store Settings", Content = store });
        AddBackgroundTools(background);
        AddStoreTools(store);
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

    private static void ToolText(Panel panel, string text, double size = 14) => panel.Children.Add(new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,16) });
    private static Button ToolButton(Panel panel, string text, Action action)
    {
        var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(18,10,18,10), Margin = new Thickness(0,0,0,12) };
        button.Click += (_, _) => action(); panel.Children.Add(button); return button;
    }
    private void AddBackgroundTools(Panel panel)
    {
        ToolText(panel, "Background Apps", 24);
        ToolText(panel, "Limita aplicativos em segundo plano. Pode interromper notificações e sincronização. Não limita todos os programas desktop. Reinicie após alterar; a aplicação depende da versão do Windows.");
        var module = ModuleCatalog.All.Single(m => m.Id == "background");
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,16) }; panel.Children.Add(status);
        void Refresh() { status.Text = updateEngine.HasBackup(module) ? "Backup disponível para restauração." : "Nenhuma alteração salva por esta ferramenta."; }
        void Change(bool restore) {
            try {
                if (MessageBox.Show(this, restore ? "Restaurar os três valores anteriores?" : "Limitar apps em segundo plano? Notificações e sincronização podem parar.", "Background Apps", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
                if (restore) updateEngine.Restore(module); else updateEngine.Enable(module);
                Refresh();
            } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Operação não concluída"); }
        }
        ToolButton(panel, "Desativar apps em segundo plano", () => Change(false));
        ToolButton(panel, "Restaurar configuração anterior", () => Change(true));
        ToolButton(panel, "Abrir configurações", () => { try { Process.Start(new ProcessStartInfo("ms-settings:privacy-backgroundapps") { UseShellExecute = true }); } catch(Exception ex) { MessageBox.Show(this, ex.Message); } });
        Refresh();
    }
    private void AddStoreTools(Panel panel)
    {
        ToolText(panel, "Store Settings", 24);
        ToolText(panel, "Otimizar: desativa atualizações automáticas de apps, reprodução automática de vídeos, avisos de instalação e experiências personalizadas. A Store e seus processos auxiliares serão encerrados. A compatibilidade do arquivo interno de preferências depende da versão da Store.");
        ToolText(panel, "Padrão do script: remove configurações WindowsStore e executa wsreset. Essa opção redefine a Store; não recupera suas preferências anteriores. Confira o resultado nas configurações da Store ao terminar.");
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(status);
        bool running = false;
        async void Run(string action) {
            if (running) return;
            if (MessageBox.Show(this, action == "1" ? "Executar a otimização e encerrar os processos da Store?" : "Redefinir a Store conforme o script original? As preferências anteriores não serão restauradas.", "Store Settings", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            running = true; status.Text = "Executando script da Store…";
            try {
                var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe")) { UseShellExecute = false };
                foreach(var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(AppContext.BaseDirectory, "Scripts", "StoreSettings.ps1"), "-Action", action }) start.ArgumentList.Add(arg);
                using var process = Process.Start(start) ?? throw new IOException("Não foi possível iniciar o script.");
                await process.WaitForExitAsync();
                status.Text = process.ExitCode == 0 ? "Script encerrado. Confira as opções na Store: o script original pode ignorar falhas em preferências incompatíveis." : $"Script terminou com erro {process.ExitCode}. Confira a Store antes de tentar novamente.";
            } catch(Exception ex) { status.Text = ex.Message; }
            finally { running = false; }
        }
        ToolButton(panel, "Otimizar Store", () => Run("1"));
        ToolButton(panel, "Redefinir Store (padrão do script)", () => Run("2"));
    }
}
