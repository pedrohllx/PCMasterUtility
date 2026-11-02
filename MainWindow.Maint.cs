using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ResetExplode;

/// <summary>Aba MANUTENÇÃO · Reparos do Windows: revisão semanal e ferramentas de reparo/reinício, uma por vez.</summary>
public partial class MainWindow
{
    private bool _maintInit;
    private CancellationTokenSource? _maintCts;
    private readonly List<Button> _maintButtons = new();

    private void InitMaint()
    {
        UpdateMaintWeekly();
        if (_maintInit) return;
        _maintInit = true;
        MaintWeeklyDesc.Text = Maintenance.Weekly.Description;
        _maintButtons.Add(BtnMaintWeekly);
        foreach (var tool in Maintenance.Tools) MaintTools.Children.Add(MaintCard(tool));
    }

    private void UpdateMaintWeekly()
    {
        var last = _settings.MaintWeeklyLast;
        MaintWeeklyLast.Text = last is DateTime d ? $"Última revisão: {d:dd/MM/yyyy HH:mm}" : "Nenhuma revisão feita pelo PcismoMaster ainda.";
        MaintWeeklyBadge.Text = last is DateTime l && (DateTime.Now - l).TotalDays < 7 ? "Em dia" : "Recomendado 1 vez por semana";
    }

    /// <summary>Cartão no estilo do exemplo: título, descrição, selo de ponto de restauração e botão Executar.</summary>
    private Border MaintCard(MaintTool tool)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(new TextBlock { Text = tool.Title, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        var desc = new TextBlock { Text = tool.Description + (tool.NeedsReboot ? " Exige reiniciar o PC." : ""), Foreground = (Brush)FindResource("Muted"),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(desc, 1); grid.Children.Add(desc);
        var bottom = new Grid { MinHeight = 32 };
        if (tool.RestorePoint)
            bottom.Children.Add(new Border { Style = (Style)FindResource("MaintBadge"), VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "Cria ponto de restauração" } });
        var run = new Button { Content = "Executar", Style = (Style)FindResource("OutlineButton"), Padding = new Thickness(16, 6, 16, 6), FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right };
        run.Click += (_, _) => RunMaint(tool);
        _maintButtons.Add(run);
        bottom.Children.Add(run);
        Grid.SetRow(bottom, 2); grid.Children.Add(bottom);
        return new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 12, 12), Child = grid };
    }

    private void BtnMaintWeekly_Click(object sender, RoutedEventArgs e) => RunMaint(Maintenance.Weekly);

    private void BtnMaintCancel_Click(object sender, RoutedEventArgs e) => _maintCts?.Cancel();

    private async void RunMaint(MaintTool tool)
    {
        if (_maintCts != null || !RequireAdmin()) return;
        string ask = tool.Title + "?\n\n" + tool.Description
            + (tool.RestorePoint ? "\n\nAntes, o PcismoMaster cria um ponto de restauração do Windows." : "")
            + (tool.NeedsReboot ? "\n\nDepois será preciso reiniciar o PC." : "");
        if (MessageBox.Show(this, ask, "Manutenção", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

        _maintCts = new CancellationTokenSource();
        foreach (var b in _maintButtons) b.IsEnabled = false;
        MaintRunCard.Visibility = Visibility.Visible;
        MaintRunTitle.Text = tool.Title;
        MaintRunStatus.Text = "Rodando… pode continuar usando o PC.";
        MaintLog.Clear();
        BtnMaintCancel.IsEnabled = true;
        var log = new Progress<string>(line =>
        {
            MaintLog.AppendText(line + Environment.NewLine);
            if (MaintLog.LineCount > 3000) MaintLog.Text = MaintLog.Text[^60000..];
            MaintLog.ScrollToEnd();
        });
        var ctx = new Maintenance.Ctx(log, _maintCts.Token);
        var started = DateTime.Now;
        string result;
        try
        {
            if (tool.RestorePoint && await Maintenance.CreateRestorePoint(ctx, tool.Title) is string why
                && MessageBox.Show(this, $"Não foi possível criar o ponto de restauração: {why}.\n\nContinuar mesmo assim?", "Manutenção",
                                   MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                result = "Cancelado antes de começar (sem ponto de restauração).";
            }
            else
            {
                await Task.Run(() => tool.Run(ctx));
                result = ctx.Failures == 0
                    ? $"Concluído em {DateTime.Now - started:mm\\:ss}." + (tool.NeedsReboot ? " Reinicie o PC para terminar." : "")
                    : $"Terminou em {DateTime.Now - started:mm\\:ss} com {ctx.Failures} etapa(s) com erro. Veja a saída acima.";
                if (tool == Maintenance.Weekly && ctx.Failures == 0) { _settings.MaintWeeklyLast = DateTime.Now; _settings.Save(); }
            }
        }
        catch (OperationCanceledException) { result = "Cancelado. O que já tinha sido feito continua aplicado."; }
        catch (Exception ex) { result = "Erro: " + ex.Message; }

        MaintRunStatus.Text = result;
        AddEvent("", "MANUTENÇÃO", tool.Title, ctx.Failures == 0 ? "concluído" : "com erros");
        BtnMaintCancel.IsEnabled = false;
        foreach (var b in _maintButtons) b.IsEnabled = true;
        _maintCts.Dispose();
        _maintCts = null;
        UpdateMaintWeekly();
    }
}
