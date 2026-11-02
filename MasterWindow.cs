using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ResetExplode.Modular;

namespace ResetExplode;

public sealed class MasterWindow : Window
{
    private readonly ModuleEngine engine = new(new RegistryStore(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcismoMaster", "ModuleBackups"));
    private readonly StackPanel cards = new();
    private readonly TextBlock summary = new();
    private readonly TextBox search = new() { Margin = new Thickness(0, 16, 0, 12), Height = 44, Padding = new Thickness(12, 4, 12, 4), Tag = "Buscar módulos…", ToolTip = "Buscar módulos por nome ou categoria" };
    private string category = "Todos";
    private MainWindow? hardware;
    private static Brush Color(string hex) => new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));
    public MasterWindow()
    {
        Title = "PcismoMaster"; Width = 1180; Height = 820; MinWidth = 800; MinHeight = 600;
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/app.ico"));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Color("#10131C"); Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI");
        var layout = new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) }); layout.ColumnDefinitions.Add(new ColumnDefinition());
        Content = layout;
        var sidebar = new StackPanel { Margin = new Thickness(24, 32, 20, 24) }; layout.Children.Add(sidebar);
        sidebar.Children.Add(Text("P / M", 30, "#A9A0FF"));
        sidebar.Children.Add(Text("PcismoMaster", 22));
        sidebar.Children.Add(Text("SEU PC. SEU CONTROLE.", 10, "#A6AEC4"));
        sidebar.Children.Add(Text("\nMVP 0.1 · WINDOWS", 11, "#A6AEC4"));
        foreach (var name in new[] { "Todos", "Aparência", "Windows", "Gráficos" })
            sidebar.Children.Add(Button(name, () => { category = name; Render(); }));
        sidebar.Children.Add(Text("\nFERRAMENTAS", 11, "#A6AEC4"));
        sidebar.Children.Add(Button("Hardware e diagnóstico ↗", () =>
        {
            if (hardware == null) { hardware = new MainWindow(); hardware.Closed += (_, _) => hardware = null; hardware.Show(); }
            else hardware.Activate();
        }));
        sidebar.Children.Add(Button("Guia e créditos ↗", () => Open("README.md")));
        sidebar.Children.Add(Button("Atualizar estados", Render));
        sidebar.Children.Add(Text("\nBase: ResetExplode\nMódulos: Ultimate / FR33THY\nLicenças MIT preservadas", 11, "#A6AEC4"));
        var main = new DockPanel { Margin = new Thickness(20, 30, 30, 20) }; Grid.SetColumn(main, 1); layout.Children.Add(main);
        var heading = new StackPanel(); DockPanel.SetDock(heading, Dock.Top); main.Children.Add(heading);
        heading.Children.Add(Text("CENTRAL MODULAR", 11, "#A9A0FF"));
        heading.Children.Add(Text("Ajustes sob seu controle", 30));
        heading.Children.Add(Text("Ative individualmente. Desative restaurando o estado anterior.", 14, "#A6AEC4"));
        summary.Margin = new Thickness(0, 16, 0, 0); summary.TextWrapping = TextWrapping.Wrap; heading.Children.Add(summary);
        heading.Children.Add(search); search.TextChanged += (_, _) => Render();
        main.Children.Add(new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Render();
    }
    private void Open(string file)
    {
        try { Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, file)) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível abrir"); }
    }
    private static TextBlock Text(string value, double size = 14, string hex = "#F3F4FA") => new() { Text = value, FontSize = size, Foreground = Color(hex), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
    private static Button Button(string label, Action action)
    {
        var button = new Button { Content = label, Padding = new Thickness(12, 9, 12, 9), Margin = new Thickness(0, 5, 8, 5), HorizontalContentAlignment = HorizontalAlignment.Left, Background = Color("#28283E"), Foreground = Brushes.White };
        button.Click += (_, _) => action(); return button;
    }
    private void Render()
    {
        cards.Children.Clear();
        summary.Text = $"{ModuleCatalog.All.Length} módulos disponíveis · {ModuleCatalog.All.Count(engine.HasBackup)} backups de restauração\nNenhum ajuste é aplicado ao abrir. Reinicie o Windows após alterar políticas ou gráficos.";
        foreach (var module in ModuleCatalog.All.Where(m => (category == "Todos" || m.Category == category) && (m.Title + m.Description + m.Category).Contains(search.Text, StringComparison.OrdinalIgnoreCase)))
        {
            var panel = new StackPanel();
            var card = new Border { Child = panel, Background = Color("#191E2C"), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 12) }; cards.Children.Add(card);
            bool backup = engine.HasBackup(module); bool matches = false; string? error = null;
            try { matches = engine.Matches(module); } catch (Exception ex) { error = ex.Message; }
            panel.Children.Add(Text(module.Category.ToUpperInvariant() + "  /  " + (error != null ? "LEITURA INDISPONÍVEL" : backup ? matches ? "APLICADO · BACKUP SALVO" : "DIVERGÊNCIA OU RECUPERAÇÃO PENDENTE" : matches ? "JÁ CONFIGURADO · SEM BACKUP" : "NÃO APLICADO PELO APP"), 10, "#B7AEFF"));
            panel.Children.Add(Text(module.Title, 20));
            panel.Children.Add(Text(error ?? module.Description, 13, "#B8C0D3"));
            panel.Children.Add(Text("Origem: Ultimate / " + module.Source, 10, "#A6AEC4"));
            var actions = new WrapPanel(); panel.Children.Add(actions);
            actions.Children.Add(Button("Ver alterações", () => { try { MessageBox.Show(this, engine.Preview(module), module.Title); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } }));
            var toggle = Button(backup ? "Desativar / restaurar" : "Ativar módulo", () => Change(module, backup));
            toggle.IsEnabled = error == null && (backup || !matches); actions.Children.Add(toggle);
        }
        if (cards.Children.Count == 0) cards.Children.Add(Text("Nenhum módulo encontrado para esta busca."));
    }
    private void Change(Module module, bool restore)
    {
        try
        {
            var detail = restore ? "Restaurar os valores anteriores salvos? Se outro programa alterou esses mesmos valores, eles serão substituídos pelo backup." : module.Description + "\n\n" + engine.Preview(module);
            if (MessageBox.Show(this, detail, restore ? "Desativar módulo" : "Ativar módulo", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;
            if (restore) engine.Restore(module); else engine.Enable(module);
            MessageBox.Show(this, "Valores verificados. Reinicie ou encerre a sessão conforme a descrição do módulo.", "Concluído");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message + "\nOs backups existentes foram preservados quando a recuperação não pôde ser concluída.", "Operação não concluída", MessageBoxButton.OK, MessageBoxImage.Error); }
        Render();
    }
}
