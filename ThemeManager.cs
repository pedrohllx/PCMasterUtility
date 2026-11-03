using System.Windows;
using System.Windows.Media;

namespace ResetExplode;

/// <summary>
/// Troca entre modo escuro e claro substituindo os pincéis da paleta (Theme.xaml). O XAML os referencia com DynamicResource,
/// então a troca vale na hora, sem recarregar janelas. Overlay e aviso de dispositivo ficam sempre escuros (aparecem sobre jogos).
/// </summary>
public static class ThemeManager
{
    public static bool IsLight { get; private set; }

    private static readonly (object Key, string Dark, string Light)[] Palette =
    {
        // Paleta laranja e preto: neutros com um toque quente; o degradê laranja → âmbar fica no Theme.xaml (igual nos dois modos).
        ("Bg", "#050505", "#FFFFFF"),
        ("Panel", "#0A0A0A", "#FBF7F3"),
        ("Card", "#121110", "#FFFFFF"),
        ("Hover", "#1E1813", "#FCEBDC"),
        ("Line", "#2A231C", "#EDDDCC"),
        ("LineStrong", "#4D3D2E", "#B8987A"),
        ("Fg", "#FFF7F0", "#140E08"),
        ("Muted", "#B3A698", "#5A4A3C"),
        ("Faint", "#5E544A", "#7A6A5C"),
        ("Ink", "#050505", "#FFFFFF"),
        ("ThumbBrush", "#2E2620", "#E8D5C2"),
        ("ThumbHover", "#7A5434", "#C9A584"),
        ("ThumbDrag", "#C2773A", "#C2773A"),
        ("Accent", "#FF8A2B", "#D95500"),
        ("NavHover", "#FF9A3C", "#C24E00"), // hover das categorias: laranja claro no escuro, um laranja mais fechado no modo claro
        (SystemColors.ControlBrushKey, "#050505", "#FFFFFF"),
        (SystemColors.HighlightBrushKey, "#1E1813", "#FCEBDC"),
        (SystemColors.InactiveSelectionHighlightBrushKey, "#1E1813", "#FCEBDC"),
    };

    public static void Apply(bool light)
    {
        IsLight = light;
        var res = Application.Current.Resources;
        foreach (var (key, dark, lightHex) in Palette)
        {
            var color = (Color)ColorConverter.ConvertFromString(light ? lightHex : dark);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            res[key] = brush; // o XAML usa DynamicResource: trocar o pincel atualiza todos os controles na hora
        }
    }
}
