# PcismoMaster

Versão 0.2.1. Consulte [CHANGELOG.md](CHANGELOG.md), [CONTRIBUTING.md](CONTRIBUTING.md) e [AGENTS.md](AGENTS.md). Antes de implementar, consulte as atualizações do origin e compare o histórico remoto.

Abra dist\PcismoMaster.exe. A interface principal é a base ResetExplode. A nova aba OTIMIZAÇÃO fica abaixo de MAKER e permite pausar Windows Update por 365 dias, restaurar os seis valores anteriores e abrir as configurações do Windows Update. A pausa inclui correções de segurança e pode ser limitada pelo Windows. Nenhum ajuste é aplicado automaticamente.

A central separada foi retirada do fluxo. Licenças e créditos preservados em LICENSE e licenses/Ultimate-LICENSE.txt. O motor mantém backups em %LOCALAPPDATA%\PcismoMaster\ModuleBackups.

OTIMIZAÇÃO contém Updates Pause, Background Apps e Store Settings. Background Apps restaura valores anteriores; Store Settings oferece a redefinição do script original, sem backup de preferências. Scripts/StoreSettings.ps1 deriva do Ultimate (licença MIT em licenses).
