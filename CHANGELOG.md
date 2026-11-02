# Histórico de versões

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
