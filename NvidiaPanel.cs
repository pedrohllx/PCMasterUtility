namespace ResetExplode;

/// <summary>Uma opção da lista: o que ela grava (uma ou mais configurações do perfil, como o Painel faz com "Modo de baixa latência").</summary>
public sealed record PanelChoice(string Label, (uint Id, uint Value)[] Writes);

/// <summary>Uma linha de "Gerenciar as configurações em 3D" do Painel de Controle NVIDIA.</summary>
public sealed record PanelSetting(string Key, string Title, string Help, uint[] Ids, PanelChoice[] Choices, bool Fps = false, bool HybridOnly = false);

/// <summary>Valor atual de uma linha num perfil: a opção que bate (ou null) e se foi definida neste perfil.</summary>
public sealed record PanelState(PanelSetting Setting, PanelChoice? Current, bool SetHere, bool Predefined, uint? Raw);

/// <summary>
/// "Gerenciar as configurações em 3D" do Painel de Controle NVIDIA, com os mesmos nomes e opções, gravado direto nos perfis
/// do driver (o mesmo banco que o Painel e o Profile Inspector usam). Configurações globais = perfil base; de programa = perfil do jogo.
/// </summary>
public static class NvidiaPanel
{
    private static PanelChoice C(string label, uint id, uint value) => new(label, new[] { (id, value) });
    private static PanelSetting S(string key, string title, string help, uint id, params (string Label, uint Value)[] choices)
        => new(key, title, help, new[] { id }, choices.Select(c => C(c.Label, id, c.Value)).ToArray());

    private const uint AnisoMode = 0x10D2BB16, AnisoLevel = 0x101E61A9, TrMs = 0x10FC2D9C, TrSs = 0x10D48A85;
    private const uint LlCpl = 0x0005F543, LlUltra = 0x10835000, Prerender = 0x007BA09E, OptCompat = 0x10F9DC80, OptMode = 0x10F9DC81;
    public const uint MaxFps = 0x10835002, BgFps = 0x10835005;

    public static readonly PanelSetting[] All =
    {
        S("ao", "Oclusão de ambiente", "Sombras de contato mais realistas em jogos compatíveis; custa desempenho.",
            0x00667329, ("Desligado", 0), ("Desempenho", 1), ("Qualidade", 2)),
        new("aniso", "Filtragem anisotrópica", "Nitidez das texturas vistas em ângulo (chão, paredes). Custa pouco nas placas atuais.",
            new[] { AnisoMode, AnisoLevel },
            new[] { new PanelChoice("Controlado pelo aplicativo", new[] { (AnisoMode, 0u) }), new PanelChoice("Desligado", new[] { (AnisoMode, 1u), (AnisoLevel, 1u) }) }
                .Concat(new uint[] { 2, 4, 8, 16 }.Select(x => new PanelChoice($"{x}x", new[] { (AnisoMode, 1u), (AnisoLevel, x) }))).ToArray()),
        S("fxaa", "Suavização - FXAA", "Suavização rápida por pós-processamento; deixa a imagem um pouco borrada.", 0x1074C972, ("Desligado", 0), ("Ligado", 1)),
        S("aagamma", "Suavização - correção de gama", "Corrige o brilho das bordas suavizadas (OpenGL).", 0x107D639D, ("Desligado", 0), ("Ligado", 2)),
        S("aamode", "Suavização - modo", "Deixa o jogo decidir, substitui ou melhora a suavização do jogo (só jogos DirectX 9/OpenGL antigos).",
            0x107EFC5B, ("Controlado pelo aplicativo", 0), ("Substituir qualquer configuração de aplicativo", 1), ("Aprimorar a configuração do aplicativo", 2)),
        S("aaset", "Suavização - configuração", "Nível de suavização (MSAA) quando o modo acima substitui ou aprimora.",
            0x10D773D2, ("Controlado pelo aplicativo", 0), ("2x", 0x0E), ("4x", 0x10), ("8x", 0x25), ("8xQ", 0x26), ("16x", 0x27)),
        new("aatrans", "Suavização - transparência", "Suaviza folhas, grades e cercas (texturas transparentes). Supersampling pesa bastante.",
            new[] { TrMs, TrSs }, new[]
            {
                new PanelChoice("Desligado", new[] { (TrMs, 0u), (TrSs, 0u) }), new PanelChoice("Multiamostragem", new[] { (TrMs, 4u), (TrSs, 0u) }),
                new PanelChoice("2x (supersample)", new[] { (TrMs, 0u), (TrSs, 0x14u) }), new PanelChoice("4x (supersample)", new[] { (TrMs, 0u), (TrSs, 0x24u) }),
                new PanelChoice("8x (supersample)", new[] { (TrMs, 0u), (TrSs, 0x34u) }), new PanelChoice("2x (supersample de grade esparsa)", new[] { (TrMs, 0u), (TrSs, 0x18u) }),
                new PanelChoice("4x (supersample de grade esparsa)", new[] { (TrMs, 0u), (TrSs, 0x28u) }), new PanelChoice("8x (supersample de grade esparsa)", new[] { (TrMs, 0u), (TrSs, 0x38u) }),
            }),
        new("bgfps", "Taxa de quadros máx. de aplicativo em segundo plano", "Limita o FPS do jogo quando ele não está em primeiro plano (economiza energia e calor).",
            new[] { BgFps }, Array.Empty<PanelChoice>(), Fps: true),
        S("cudasys", "CUDA - política de fallback de memória do sistema", "O que fazer quando a memória da placa acaba em CUDA/IA: usar a RAM (mais lento) ou dar erro.",
            0x10ECECC9, ("Padrão do driver", 0), ("Preferir sem fallback de memória do sistema", 1), ("Preferir fallback de memória do sistema", 2)),
        new("latency", "Modo de baixa latência", "Menos quadros na fila = resposta mais rápida. Ultra envia o quadro bem na hora; jogos com NVIDIA Reflex usam o Reflex no lugar.",
            new[] { LlCpl, LlUltra, Prerender }, new[]
            {
                new PanelChoice("Desligado", new[] { (LlCpl, 0u), (LlUltra, 0u), (Prerender, 0u) }),
                new PanelChoice("Ligado", new[] { (LlCpl, 1u), (LlUltra, 0u), (Prerender, 1u) }),
                new PanelChoice("Ultra", new[] { (LlCpl, 2u), (LlUltra, 1u), (Prerender, 1u) }),
            }),
        new("maxfps", "Taxa de quadros máxima", "Limite de FPS do driver. Um pouco abaixo da taxa do monitor com G-SYNC dá a menor latência sem tearing.",
            new[] { MaxFps }, Array.Empty<PanelChoice>(), Fps: true),
        S("monitor", "Tecnologia do monitor", "G-SYNC (ou G-SYNC Compatible) ou taxa de atualização fixa.", 0x10A879CF, ("G-SYNC / G-SYNC Compatible", 0), ("Taxa de atualização fixa", 4)),
        S("mfaa", "Suavização de quadro de amostragem múltipla (MFAA)", "Deixa o MSAA do jogo mais barato alternando o padrão de amostras.", 0x0098C1AC, ("Desligado", 0), ("Ligado", 1)),
        S("gdi", "Compatibilidade de GDI do OpenGL", "Para programas OpenGL antigos que desenham com GDI.", 0x2072C5A3, ("Automático", 2), ("Preferir desempenho", 0), ("Preferir compatibilidade", 1)),
        S("power", "Modo de gerenciamento de energia", "Desempenho máximo segura o clock alto (menos oscilação, mais consumo e calor parado).",
            0x1057EB71, ("Normal", 5), ("Adaptável", 0), ("Preferir desempenho máximo", 1)),
        new("gpu", "Processador gráfico preferido", "Notebooks com gráfico híbrido: qual placa roda os programas.",
            new[] { OptCompat, OptMode }, new[]
            {
                new PanelChoice("Processador NVIDIA de alto desempenho", new[] { (OptCompat, 1u), (OptMode, 1u) }),
                new PanelChoice("Gráficos integrados", new[] { (OptCompat, 0u), (OptMode, 0u) }),
            }, HybridOnly: true),
        S("refresh", "Taxa de atualização preferida", "Força a maior taxa do monitor em jogos que pedem menos.", 0x0064B541, ("Controlado pelo aplicativo", 0), ("Mais alta disponível", 1)),
        S("shader", "Tamanho do cache de sombreador", "Espaço em disco para shaders compilados: cache maior = menos travadas ao abrir áreas novas.",
            0x00AC8497, ("Desabilitado", 0), ("5 GB", 0x1400), ("10 GB", 0x2800), ("100 GB", 0x19000), ("Ilimitado", 0xFFFFFFFF)),
        S("anisoopt", "Filtragem de textura - otimização de amostra anisotrópica", "Usa menos amostras na filtragem anisotrópica (mais rápido, um pouco menos nítido).",
            0x00E73211, ("Desligado", 0), ("Ligado", 1)),
        S("lod", "Filtragem de textura - tendência LOD negativa", "Fixar evita texturas tremulando quando o jogo pede LOD negativo.", 0x0019BB68, ("Permitir", 0), ("Fixar", 1)),
        S("texq", "Filtragem de textura - qualidade", "Qualidade da filtragem das texturas. Alto desempenho quase não muda a imagem nas placas atuais.",
            0x00CE2691, ("Alta qualidade", 0xFFFFFFF6), ("Qualidade", 0), ("Desempenho", 10), ("Alto desempenho", 20)),
        S("trilinear", "Filtragem de textura - otimização trilinear", "Filtragem trilinear mais barata (ignorada em Alta qualidade).", 0x002ECAF2, ("Ligado", 0), ("Desligado", 1)),
        S("threaded", "Otimização encadeada", "Usa mais núcleos do processador no driver (OpenGL e jogos antigos).", 0x20C1221E, ("Automático", 0), ("Ligado", 1), ("Desligado", 2)),
        S("triple", "Buffer triplo", "Só para OpenGL com sincronização vertical ligada.", 0x20FDD1F9, ("Desligado", 0), ("Ligado", 1)),
        S("vsync", "Sincronização vertical", "Tira o tearing travando o FPS na taxa do monitor; aumenta a latência. Rápida = sem trava e sem tearing (precisa de FPS bem acima da taxa).",
            0x00A879CF, ("Usar a configuração do aplicativo 3D", 0x60925292), ("Desligado", 0x08416747), ("Ligado", 0x47814940), ("Rápida", 0x18888888)),
        S("present", "Método de apresentação Vulkan/OpenGL", "Em camadas no DXGI permite G-SYNC, HDR automático e Frame Generation em jogos Vulkan/OpenGL em janela.",
            0x20D690F8, ("Automático", 2), ("Preferir nativo", 0), ("Preferir em camadas no DXGI Swapchain", 1)),
    };

    /// <summary>Opções comuns de limite de FPS (o valor atual entra na lista se for outro).</summary>
    public static readonly uint[] FpsPresets = { 30, 60, 75, 90, 100, 120, 141, 144, 157, 160, 165, 180, 200, 237, 240, 300, 360 };

    public static PanelChoice FpsChoice(uint id, uint fps) => C(fps == 0 ? "Desligado" : $"{fps} FPS", id, fps);

    /// <summary>Lê as linhas no perfil (global = perfil base). Rápido: ~30 leituras numa sessão.</summary>
    public static List<PanelState> Read(string profile, string baseName, IEnumerable<PanelSetting> settings)
        => NvDrs.Session(s =>
        {
            IntPtr p = profile == baseName ? NvDrs.BaseProfile(s) : NvDrs.FindProfile(s, profile);
            var list = new List<PanelState>();
            foreach (var st in settings)
            {
                var values = st.Ids.ToDictionary(id => id, id => { try { return NvDrs.Get(s, p, id); } catch { return null; } });
                bool here = values.Values.Any(v => v is { Location: DrsLocation.Current, IsPredefined: false });
                uint? raw = values[st.Ids[0]] is { } first ? (uint)first.Number : null;
                PanelChoice? current = st.Fps
                    ? raw is uint f ? FpsChoice(st.Ids[0], f) : null
                    : st.Choices.FirstOrDefault(c => c.Writes.All(w => values.GetValueOrDefault(w.Id) is { } v && (uint)v.Number == w.Value))
                      // compostas: a primeira configuração decide (ex.: "Modo de baixa latência" pelo estado do Painel)
                      ?? (st.Ids.Length > 1 && raw is uint r ? st.Choices.FirstOrDefault(c => c.Writes[0].Id == st.Ids[0] && c.Writes[0].Value == r) : null);
                bool predefined = !here && values.Values.Any(v => v is { Location: DrsLocation.Current, IsPredefined: true });
                list.Add(new PanelState(st, current, here, predefined, raw));
            }
            return list;
        });

    /// <summary>O que gravar: a opção escolhida, ou null = "usar global/padrão" (tira a configuração deste perfil).</summary>
    public static IEnumerable<DrsChange> Changes(PanelSetting st, PanelChoice? choice)
        => choice == null
            ? st.Ids.Select(id => new DrsChange(id, DrsType.Dword, 0, null, Reset: true))
            : choice.Writes.Select(w => new DrsChange(w.Id, DrsType.Dword, w.Value, null, Reset: false));
}
