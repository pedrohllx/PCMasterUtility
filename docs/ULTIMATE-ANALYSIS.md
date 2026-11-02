# Análise de integração · Ultimate

Revisão estrutural do checkout a0a33a57078434201f582b0db5107dcbb502292a. O inventário inclui todos os scripts encontrados; isso não significa auditoria linha a linha de cada script. Os scripts-fonte dos sete módulos foram examinados diretamente.

| Grupo | Conteúdo | Tratamento no MVP |
| --- | --- | --- |
| 1 Check | BIOS, armazenamento, RAM, CPU e GPU | Diagnósticos herdados do ResetExplode; ferramentas externas ficam para etapa posterior |
| 2 Refresh | Reset, reinstalação, conta local, drivers, BIOS | Fora da execução modular; são operações de manutenção, algumas destrutivas |
| 3 Setup | BitLocker, memória, ativação, apps, atualizações | Integrado somente o módulo de apps em segundo plano |
| 4 Installers | Instaladores e MSI Afterburner | Pendente: origem, assinatura, licença e versões verificáveis |
| 5 Graphics | Drivers, runtimes, configurações GPU | Pendente: suporte por fabricante e rollback de drivers |
| 6 Windows | Shell, temas, apps, energia, rede, segurança | Integrados os recortes de tema, Widgets e política de Copilot |
| 7 Hardware | Mouse, controle, monitor, rede e guias | Pendente: configuração por dispositivo e testes físicos |
| 8 Advanced | Segurança, memória, serviços, GPU e drivers | Integrado apenas o valor de MPO, com aviso de diagnóstico |

## Critérios adotados

O Ultimate contém menus interativos, downloads, execução elevada, encerramento de processos, remoção de apps e operações de registro. Seus caminhos “Default” frequentemente impõem valores ou removem chaves inteiras; não equivalem a recuperar o estado individual de um PC. Por isso a integração usa operações tipadas locais e backup anterior à escrita, sem executar os scripts originais.

O MVP não inclui os módulos para desligar Defender, Firewall, DEP, mitigação Spectre/Meltdown, UAC, isolamento de núcleo ou verificações de drivers. Também não integra ativação do Windows ou conversão de edição. Estes itens têm efeitos distintos de otimização e exigem escopo e validação próprios.

## Etapas seguintes

1. Validar os sete módulos em máquinas virtuais Windows 10/11 e nas edições suportadas, incluindo reinicialização e efeito observado.
2. Unificar os ajustes herdados do ResetExplode no motor de backup e detectar conflitos entre módulos.
3. Integrar planos de energia, memória e ajustes por dispositivo com captura do estado original e testes específicos.
4. Gerenciar instaladores e drivers com confirmação por ação, procedência e recuperação documentadas.
5. Criar instalador assinado, atualização própria e pacote de distribuição com auditoria completa das licenças de componentes.

Nenhuma alteração de desempenho foi aplicada ao computador durante a construção. Testes usam armazenamento simulado e uma chave HKCU exclusiva e descartável para verificar serialização do registro.
