# Validação do MVP 0.1

- Compilação Release e publicação win-x64 com runtime incluído: sucesso.
- 7 testes automatizados: passaram (catálogo sem sobreposição, restauração após recriar motor, rollback parcial, falha de recuperação com nova tentativa, divergência externa, backup corrompido e tipos de registro).
- Teste de registro restrito a chave aleatória Software\PcismoMaster.Tests; nenhuma configuração de produção alterada.
- Interface: abertura, leitura dos sete módulos, filtro Gráficos e prévia de MPO conferidos por automação desktop.
- 105 scripts PowerShell inventariados com SHA-256. Integração parcial de cinco scripts-fonte, desmembrados em sete módulos.
- O build mantém 8 avisos distintos herdados em MainWindow.Oc.cs e DiskHealth.cs, emitidos também na compilação WPF intermediária; zero erros.
- Não validado: efeito dos ajustes após reiniciar Windows, todas as edições/versões, benchmarks, drivers e demais ferramentas herdadas. O executável não é assinado digitalmente.
- Inicialização de QA realizada pela DLL através do runtime instalado, sem interação com a solicitação de elevação. O executável distribuído conserva o manifesto requireAdministrator.
- Busca textual por Widgets validada na versão final: retornou somente o módulo correspondente.
