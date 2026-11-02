# Histórico de versões

A numeração segue **SemVer** (`MAJOR.MINOR.PATCH`). Regras e processo em [docs/projeto/ATUALIZACOES.md](docs/projeto/ATUALIZACOES.md).

## Em desenvolvimento

- **Drivers · Instalação enxuta do driver (debloat)** (o "5 Graphics/4 Driver Debloat Install & Settings" do Ultimate; os ajustes do script já estão no cartão de configurações recomendadas):
  - NVIDIA: abre o pacote com o 7-Zip, tira os componentes marcados (telemetria, NVIDIA App, ShadowPlay, containers, nView, FrameView…) e instala em silêncio e limpo. Baixa o mais recente (assinatura conferida) ou usa um instalador seu.
  - AMD e Intel: instalador escolhido por você (assinatura da AMD/Intel conferida, fabricante detectado pelo pacote), extras desligados no pacote e limpezas depois de instalar.
  - **Cada componente é opcional** e o que quebra algo vem desmarcado com o motivo: áudio HDMI/DisplayPort, PhysX, DLDSR, Dynamic Boost de notebooks, microfone/caixas de notebooks AMD, HDCP e firmware da Intel.
  - Serviços são **desativados com backup** em vez de apagados (`sc delete` do script) e entradas de inicialização saem com backup: botão **Desfazer as limpezas**.
  - Sem 7-Zip, oferece instalar pelo winget. Painel de Controle NVIDIA continua vindo da Microsoft Store. Sem reinício automático.
- **Drivers · Instalar driver e configurações recomendadas** (o "5 Graphics/3 Driver Updated Install & Settings" do Ultimate):
  - **Instalar driver NVIDIA (silencioso e limpo)**: download oficial com assinatura conferida, instalação sem telas (`-s -noreboot -noeula -clean`), sem 7-Zip; se faltar o Painel de Controle NVIDIA, abre a página dele na Microsoft Store.
  - **Configurações recomendadas** em interruptores com selo SEGURO/AVANÇADO, só dos fabricantes presentes no PC, **cada uma com backup e desfazer** (o script não guardava nada): NVIDIA (PhysX na GPU, menu de desenvolvedor, contadores de desempenho, sem ícone na bandeja, nitidez clássica), AMD (Anti-Lag, VSync off, texturas em desempenho, tesselação, Vari-Bright, Adrenalin sem notificações), Intel (VSync off, baixa latência, VRR global off) e Windows (gerenciamento automático de cor off, modo MSI, todos os ícones da bandeja).
  - **Perfil global recomendado** do script aplicado pelo nosso Profile Inspector, guardando o valor anterior de cada configuração; **G-SYNC desligado** virou opção separada.
  - Desligados por padrão (AVANÇADO): P-state dinâmico off (mais consumo em repouso), **HDCP off** (quebra Netflix/Disney+/Blu-ray, pede confirmação) e G-SYNC off. Fora do app: aceitar o EULA da AMD por você e o "Preferred OpenGL GPU" fixo de uma RTX 4090.
  - Sem reinício automático: botão **Reiniciar agora** quando precisa.
- **Drivers · Instalar driver atualizado** (o "5 Graphics/2 Driver Updated Install" do Ultimate, aproveitando o que a aba já fazia):
  - A placa de vídeo é reconhecida **mesmo sem driver** (pelo código PCI do fabricante), como logo depois da limpeza com o DDU; antes ela sumia da lista e não dava para instalar.
  - Novos botões **Instalar o driver mais recente de: NVIDIA · AMD · Intel** para escolher o fabricante à mão.
  - NVIDIA: se o modelo não for identificado, consulta o driver GeForce unificado mais recente (a mesma consulta do Ultimate); download oficial com assinatura conferida.
  - AMD: sem modelo reconhecido, procura o instalador que detecta a placa sozinho; se o site não entregar o link, oferece a página oficial.
  - Intel: abre a lista de drivers gráficos para Windows 11. Depois de instalar o driver NVIDIA, dicas do NVIDIA App (desligar Estatísticas, Filtros de jogo e otimização automática).
- **Drivers · Limpeza completa do driver de vídeo (DDU)** (versão do PcismoMaster do "5 Graphics/1 Driver Clean" do Ultimate):
  - Modos **Automático** (o DDU limpa sozinho no modo de segurança e o PC reinicia normal) e **Manual** (abre o DDU no modo de segurança); escolha da placa (NVIDIA, AMD, Intel ou todas).
  - Baixa o instalador oficial do DDU e **confere a assinatura da Wagnardsoft**; extrai sem precisar do 7-Zip (o script original falhava sem ele). Também aceita um DDU baixado por você.
  - Opcional e recomendado: **baixar antes o driver NVIDIA mais recente**; depois da limpeza aparece o botão para instalá-lo, mesmo sem internet.
  - Ponto de restauração, **tudo preparado e conferido antes de mexer no boot**, atalho de emergência "Sair do modo de segurança" na Área de Trabalho e reinício com **contagem regressiva e Cancelar** (desfaz o modo de segurança).
  - Bloqueio do driver automático do Windows Update **com botão para desfazer** (o script deixava permanente). Remover áudio Realtek/Sound Blaster virou opção, **desligada por padrão**.
- **FERRAMENTAS · Drivers ganhou sub-abas**: **Drivers** (o que já existia, sem mudanças) e o novo **Profile Inspector**.
- **Profile Inspector** (no estilo do NVIDIA Profile Inspector): lê e grava os perfis do driver NVIDIA (DRS da NVAPI), o mesmo banco do Painel de Controle NVIDIA, inclusive o que o painel esconde.
  - Lista de todos os perfis do driver (perfil global + milhares de jogos), com busca.
  - Configurações agrupadas como no Profile Inspector (sincronia, antisserrilhado, texturas, DLSS/upscaling, energia etc.), com nome, valor editável (lista de valores conhecidos ou número em hexadecimal/decimal) e a origem de cada valor: **você**, **NVIDIA** (predefinido para o jogo), **global** (herdado) ou **padrão** do driver. Passe o mouse para ver a descrição e o ID.
  - Alterações ficam pendentes (em amarelo) até **Aplicar alterações**; **Desfazer** descarta; ↺ tira a configuração do perfil (volta a herdar).
  - **Restaurar padrão do perfil**, **Novo perfil…**, **Excluir perfil**, executáveis do perfil (adicionar/tirar) e **Exportar/Importar .nip** compatível com o Profile Inspector.
  - Filtros: buscar configuração, **Só alteradas** e **Mostrar tudo** (configurações escondidas e as que o driver conhece sem nome).
  - Catálogo de configurações do nvidiaProfileInspector (Orbmu2k, licença MIT, em `licenses/`).
- Novo ComboBox escuro no tema do app.

## 0.5.0

- **JOGOS · Biblioteca com novo visual**: cabeçalho com busca, **Adicionar jogo (.exe)…** e **Procurar de novo**; "Meus jogos" em **cartões com a capa** de cada jogo (a mesma da biblioteca da Steam; jogos sem capa ganham as iniciais no degradê do ícone), nome, loja e os botões **Preparar e jogar** e **Ajustes**.
- **Janela "Ajustes do jogo"** no estilo do exemplo, com interruptores e selos **SEGURO/AVANÇADO**; só grava ao clicar em **Salvar ajustes** (Cancelar descarta):
  - **Novo: Liberar o núcleo 0** (o jogo fica fora do núcleo físico 0, o mais usado pelo Windows e pelos drivers).
  - **Novo: Só núcleos físicos (sem SMT/Hyper-Threading)**, uma thread por núcleo.
  - **Novo: Só núcleos de desempenho (sem E-cores)** em CPUs Intel híbridas (desativado quando a CPU não tem E-cores).
  - Prioridade alta, plano de energia Alto desempenho, placa de vídeo dedicada, desativar otimizações de tela cheia e o executável do jogo com **Trocar .exe**.
  - **Opções avançadas** mantidas: argumentos de inicialização (modo de janela, resolução, Hz, engine, extras), aplicar nas opções da Steam e programas auxiliares.
- Os núcleos escolhidos são aplicados ao processo do jogo em "Preparar e jogar" (afinidade do Windows; nada muda nos arquivos do jogo) e aparecem no status, por exemplo "núcleos: 14 de 16 threads".
- **Exibição em cartões ou em lista** (botões Cartões / Lista; a escolha fica salva). Na lista, cada jogo mostra o banner, nome, loja e pasta, com os mesmos botões.
- Capas encontradas também no formato novo da Steam (subpastas com hash, `library_capsule.jpg` / `library_header.jpg`); se não houver no disco, a arte é baixada uma vez do servidor de imagens da Steam e guardada em `%LocalAppData%PcismoMastervers`.
- Capas carregadas fora da thread da janela e já reduzidas ao tamanho do quadro, para a aba abrir leve.
- Versão atualizada para 0.5.0.

## 0.4.0

- **Nova aba MANUTENÇÃO** (abaixo de MASTER) · **Reparos do Windows**:
  - **Revisão semanal**: verificar o disco (CHKDSK), os arquivos protegidos (SFC) e reparar a imagem do Windows (DISM), com ponto de restauração antes; mostra a data da última revisão.
  - **Ferramentas**: reparo completo (DISM + SFC), SFC, DISM, CHKDSK, limpar cache de DNS, renovar IP, redefinir a rede, redefinir o Windows Update, destravar a impressora, reiniciar áudio, Bluetooth e Windows Explorer, reconstruir o cache de ícones, reiniciar o driver de vídeo (Win+Ctrl+Shift+B), procurar alterações de hardware e reparar Microsoft Store e menu Iniciar.
  - Cada ferramenta pede confirmação, cria ponto de restauração quando mexe no sistema, mostra a saída ao vivo e pode ser cancelada. Uma por vez.
- **Novo ícone do app** (engrenagem com chave no degradê roxo → ciano), no executável, na barra de título e nos relatórios.
- **Cores no estilo do ícone**: fundo escuro levemente arroxeado; destaque roxo (#A647FE) e degradê roxo → ciano (#29B4D9) nos botões principais, chips selecionados, sliders, interruptores e numa barrinha que marca a aba aberta; hover das abas em ciano; anéis de RAM/VRAM/Disco nos tons do ícone. O modo claro ganhou os mesmos tons, em versão clara.
- **Aba Sensores no formato do HWiNFO**: um bloco por dispositivo (CPU, GPU, memória, placa-mãe, discos, rede) na ordem de detecção, com cabeçalho que recolhe e expande; colunas **Sensor · Atual · Mínimo · Máximo · Média**; linhas compactas; botões **Zerar mín./máx./média**, **Recolher tudo** e **Expandir tudo**. A busca procura por sensor, dispositivo ou tipo (ex.: "temperatura").
- Mínimo, máximo e média passam a ser calculados pelo app desde a abertura (ou o último "zerar"), como no HWiNFO.
- Corrigida a aba Sensores sem linhas nem títulos de coluna (bug da 0.3.1): a virtualização com agrupamento deixava todas as colunas com largura mínima quando a aba começava escondida. A tabela agora rola suave, pixel a pixel.
- Versão atualizada para 0.4.0.

## 0.3.1

- **Desempenho e consumo** (o app fica aberto junto com o jogo):
  - A leitura dos sensores (LibreHardwareMonitor) saiu da thread da janela: a tela não engasga mais uma vez por segundo, e a tabela só é avisada dos valores que mudaram (antes eram ~1000 avisos por segundo).
  - Tabela de sensores virtualizada: só as linhas na tela são desenhadas.
  - **Minimizado**, o app lê os sensores a cada 3 s e não desenha nada (a não ser durante um teste ou análise), e o monitoramento do Overclock só segue se a curva de fans estiver ligada.
  - O painel de gargalo e o limitador da GPU só consultam o driver quando suas abas estão abertas; a escuta de FPS (ETW) só liga quando o Gargalo ou o overlay precisam dela, e esquece processos que pararam de desenhar.
  - Registro de eventos do Windows, varredura de portas COM, placa-mãe (WMI) e lista de programas abertos rodam fora da thread da janela; a checagem de saúde da Visão geral fica guardada por 60 s.
  - Overclock: APLICAR, RESET e a leitura da curva rodam em segundo plano (a janela não congela mais até 3 s), e o monitoramento pausa durante os benchmarks de GPU.
  - Vazamentos corrigidos: um token do Windows por segundo (checagem de administrador), handles de processos e uma chave de registro.
  - Controladoras HID (hubs de fan/RGB) não são mais varridas a cada segundo; isso também evita disputa com o iCUE.
- Versão atualizada para 0.3.1.

## 0.3.0

- **Aba Overclock refeita no estilo do MSI Afterburner** (placas NVIDIA):
  - **Sliders nos blocos do Afterburner**: **Clock** (Mem e Core, lado a lado), **Voltage** (Core % e o botão **Curve Editor**) e **Fan** (Fan speed com Auto; Temp Limit e Power Limit). Cada slider mostra o valor numa caixa à esquerda (clicar seleciona o valor para digitar por cima); botão direito no slider zera o offset. Botões **RESET** (padrão de fábrica) e **APLICAR**.
  - **Curva V/F (Ctrl+F do Afterburner)**: gráfico de tensão × frequência com a curva de fábrica (cinza) e a editada (azul). Overclock + undervolt juntos como no Afterburner: Shift + arrastar (ou o slider Core Clock) sobe a curva; clique num ponto e aperte **L** (ou botão direito) para travar o teto ali; arraste o ponto travado ou use ↑/↓ para escolher o clock do teto. Destaque do teto com "mV · MHz", ponto em uso agora e botão **LER DA PLACA**, que traz o ajuste aplicado (inclusive pelo MSI Afterburner).
  - **Editor de curva em janela própria**: Ctrl+F (ou o botão **CURVE EDITOR**) abre o "Editor da Curva Tensão/Frequência" como no Afterburner, sincronizado com o gráfico da aba. Visual igual ao do Afterburner: escala começando onde a curva sai do piso (ex.: 700–1250 mV, 500–3500 MHz), grade de 25 mV × 100 MHz, títulos "Frequência, MHz" e "Tensão, mV", curva de fábrica em linha fina e a editada com quadradinhos; o ponto selecionado mostra "mV · MHz". **Ctrl+D** volta a curva ao padrão. Um clique só seleciona o ponto; ele muda quando é arrastado.
  - Depois de aplicar, o app confirma no driver os pontos gravados e mantém a curva de fábrica na tela.
  - **Undervolt aplicado como no Afterburner**: os pontos depois do teto recebem um único offset bem negativo e o driver os segura no clock do teto, então a curva aplicada fica reta até o fim (sem subir clock nem tensão), mesmo quando o teto está mais de 1000 MHz abaixo da curva de fábrica. Com curva V/F o Core Clock não é mais gravado também na P0 (isso somava o offset duas vezes e levantava a parte plana).
  - **Curva de fans** controlada pelo app: pontos arrastáveis, duplo clique cria ponto, botão direito apaga, histerese ajustável, temperatura e fan atuais no gráfico. Ao fechar o app, os fans voltam para o automático.
  - **Monitoramento** dos últimos 2 minutos: GPU clock, memory clock, temperatura, consumo × limite (curva de potência), fan e voltagem, mais o que está limitando o clock (energia, temperatura etc.).
  - **5 perfis** (SALVAR e escolher 1–5; clicar num perfil carrega e aplica) e opção de reaplicar os últimos ajustes ao abrir o PcismoMaster.
- A camada NVAPI (curva V/F, offsets de clock e memória, tensão) foi portada do projeto **simple-nvidia-undervolt** (licença MIT, créditos em `licenses/`). Energia, temperatura e fans usam a NVML oficial.
- Memory Clock: a tabela da curva V/F também guarda a entrada da memória; o app limpa a curva antes de gravar a memória e grava só os pontos do core depois, para o offset valer de fato. Depois de aplicar, o app confere o clock real e avisa se o driver não ativou.
- O verificador de estabilidade e o guia de overclock na BIOS continuam na aba, abaixo dos novos controles.
- Novos testes automáticos da curva V/F, do undervolt, da curva de fans e dos perfis (sem tocar na GPU).
- Versão atualizada para 0.3.0.

## 0.2.5

- Visão geral com mais cor:
  - **CPU · TEMPERATURA**: chips de seleção (período e métrica) e a legenda da CPU no nosso azul; **curva da CPU em azul** e **curva da GPU em verde**.
  - O **valor da temperatura muda de cor por faixa**: azul abaixo de 60 °C, amarelo de 60 a 79, laranja de 80 a 89 e vermelho a partir de 90 °C.
  - **Memória e disco**: a quantidade de RAM em uso (GB) no nosso azul; os anéis de **RAM, VRAM e Disco** em tonalidades de roxo indo para o nosso azul.
  - As **sub-abas** (itens dentro das categorias) também acendem no nosso azul ao passar o mouse, como as abas principais.
- Versão atualizada para 0.2.5.

## 0.2.4

- Abas de navegação (MASTER, JOGOS, DESEMPENHO, FERRAMENTAS, MAKER, OTIMIZAÇÃO) com fonte nova em **negrito**, clean e legível no estilo de apps de tecnologia (Inter, com queda para Segoe UI Variable Display e Segoe UI).
- Ao passar o mouse, a aba fica em **azul claro iluminado** (com leve brilho).
- Abas padronizadas em CAIXA ALTA; a aba **PcismoMaster** passou a se chamar **MASTER**.
- O rótulo de versão no menu lateral agora é lido do próprio assembly, então nunca mais fica desatualizado.
- Documentação do projeto em `docs/projeto/` (fluxo de colaboração, planejamento e padrão de versionamento SemVer) para Bryan, Pedro, Claude e Codex seguirem o mesmo padrão.
- Versão atualizada para 0.2.4.

## 0.2.3

- Corrigido o erro "Could not find a part of the path ...\ModuleBackups\updates-pause.json" ao clicar em **Restaurar configuração anterior** no Windows Update sem ter pausado antes. Agora aparece um aviso de que não há nada para restaurar, e nenhum valor é alterado.
- O motor de backup recusa restaurar sem backup com uma mensagem clara, coberta por um novo teste automático.
- Versão atualizada para 0.2.3.

## 0.2.2

- Removidas as ferramentas Background Apps e Store Settings da aba OTIMIZAÇÃO, junto com o script `Scripts/StoreSettings.ps1`. Fica somente a pausa do Windows Update.
- "Updates Pause" passa a se chamar **Windows Update**.
- OTIMIZAÇÃO agora é um grupo da navegação com a mesma fonte e o mesmo estilo de JOGOS, DESEMPENHO, FERRAMENTAS e MAKER, e não mais um item avulso em maiúsculas.
- Nova sub-aba **Inicialização do Windows** dentro de OTIMIZAÇÃO, onde fica o Windows Update.
- Layout do Windows Update refeito no padrão das outras páginas: título, subtítulo, cartão, rótulos e botões (principal e contorno) iguais aos de MAKER e FERRAMENTAS.
- Como não há mais scripts externos, o `.exe` avulso da Release volta a ter todas as funções.
- Versão atualizada para 0.2.2.

## 0.2.1

- Executável único: `PcismoMaster.exe` agora é publicado como arquivo único autocontido (não precisa instalar o .NET), com as bibliotecas nativas embutidas.
- Lançamento automático: cada tag `vX.Y.Z` compila, testa e publica uma Release no GitHub com o `.exe`, um `.zip` (exe + Scripts, licenças e documentação) e as notas desta seção do CHANGELOG.
- `publicar.bat` passa a gerar o mesmo executável único da Release.
- Versão atualizada para 0.2.1.

Download recomendado: o `.zip`. O `.exe` avulso funciona, mas Store Settings precisa da pasta `Scripts` que vem no `.zip`.

## 0.2.0

- Sub-abas verticais Updates Pause, Background Apps e Store Settings dentro de OTIMIZAÇÃO.
- Background Apps com backup e restauração individual.
- Store Settings executa o script Ultimate com seleção pela interface; redefinição não equivale a backup. Efeitos na Store ainda requerem validação manual.

## 0.1.0

- Primeira versão PcismoMaster derivada do ResetExplode, com identidade própria e licenças preservadas.
- Abertura direta na interface de hardware e diagnóstico da base.
- Remoção do prefixo RESET nos nomes da navegação.
- Nova aba OTIMIZAÇÃO abaixo de MAKER: pausa do Windows Update por 365 dias, restauração dos valores anteriores e acesso às configurações do Windows Update.
- Motor de backup e recuperação de valores do registro com testes de falhas.
- Testes e compilação automatizados no GitHub Actions.

A pausa depende da versão do Windows e inclui correções de segurança. O efeito após reinicialização e os demais recursos herdados ainda precisam de validação específica.
