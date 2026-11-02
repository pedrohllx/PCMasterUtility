namespace ResetExplode.Modular;

public static class ModuleCatalog
{
    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    public static readonly Module[] All =
    [
        new("dark-apps", "Aplicativos em modo escuro", "Aparência", "Ativa o tema escuro dos aplicativos compatíveis. Pode exigir reabrir os aplicativos.", "6 Windows/5 Theme Black.ps1", [new("HKCU", Personalize, "AppsUseLightTheme", 0)]),
        new("dark-system", "Windows em modo escuro", "Aparência", "Altera o tema do sistema. Encerre a sessão para atualizar todos os componentes.", "6 Windows/5 Theme Black.ps1", [new("HKCU", Personalize, "SystemUsesLightTheme", 0)]),
        new("transparency", "Reduzir transparência", "Aparência", "Desativa efeitos de transparência do Windows. Sem promessa de ganho de FPS.", "6 Windows/5 Theme Black.ps1", [new("HKCU", Personalize, "EnableTransparency", 0)]),
        new("widgets", "Desativar Widgets", "Windows", "Define as políticas de Widgets. Requer reinicialização; a aplicação depende da edição e versão do Windows.", "6 Windows/8 Widgets.ps1", [new("HKLM", @"SOFTWARE\Microsoft\PolicyManager\default\NewsAndInterests\AllowNewsAndInterests", "value", 0), new("HKLM", @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0)]),
        new("background", "Limitar apps em segundo plano", "Windows", "Pode interromper notificações e sincronização de apps da Store. Reinicie para aplicar. Não limita todos os programas desktop.", "3 Setup/9 Background Apps.ps1", [new("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2), new("HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", 0), new("HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1)]),
        new("copilot-policy", "Desativar Copilot legado", "Windows", "Aplica somente a política do Copilot integrado antigo. Não remove o app Copilot atual; pode não ter efeito em versões recentes. Reinicie para aplicar.", "6 Windows/9 Copilot.ps1", [new("HKCU", @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1), new("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1)]),
        new("mpo", "Desativar Multiplane Overlay", "Gráficos", "Ajuste de diagnóstico para falhas gráficas. Pode piorar consumo e desempenho. Adaptação parcial do script, sem mudar preferências globais de jogos. Reinicialização necessária.", "8 Advanced/11 Mpo.ps1", [new("HKLM", @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5)])
    ];
}
