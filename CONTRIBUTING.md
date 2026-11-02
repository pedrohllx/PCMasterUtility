# Colaboração

O repositório é privado. O proprietário deve convidar o outro desenvolvedor em **Settings → Collaborators → Add people**. Cada pessoa usa sua própria conta e credenciais.

Antes de trabalhar:

```powershell
git status
git fetch origin --prune --tags
git switch main
git pull --ff-only
git switch -c feat/nome-da-funcao
```

Troque de branch somente após preservar alterações locais. Confira os commits recentes e leia os arquivos afetados antes de implementar. Se houver divergência, integre com cuidado; não use push forçado.

Depois de implementar, rode os testes e compile, atualize CHANGELOG.md e publique sua branch:

```powershell
git add <arquivos-alterados>
git commit -m "feat: descreva a mudança"
git push -u origin feat/nome-da-funcao
```

Abra um pull request para main e peça revisão ao outro desenvolvedor. O workflow executa testes e gera um pacote portátil para download nos artifacts do GitHub Actions. Não altere configurações reais do Windows nos testes.

Para lançar uma versão: atualize Version no projeto e o changelog, integre na main e crie uma tag nova `vX.Y.Z`. Publique com `git push origin vX.Y.Z`. O código fica no Git; executáveis são gerados pelo workflow ou por `publicar.bat`.

O remoto origin pertence ao PcismoMaster; upstream mantém a referência do ResetExplode original. Preserve avisos de licença nas adaptações.
