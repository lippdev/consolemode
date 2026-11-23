# Console Mode 2 Alpha: como gerar e publicar um alpha

A 2.0 é **outro produto** em alpha, separado da 1.7 (ver [PLANO.md](PLANO.md)). Este arquivo descreve
os nomes que o atualizador da 2.0 procura e como preparar uma build interna. O workflow da 2.0
compila e testa a branch `v2-config`. O instalador para uso interno é gerado localmente e anexado ao rascunho.

## O que o app procura (canal alpha)

O código está em `crates/cm-core/src/update.rs` (regras, testadas) e `crates/cm-platform/src/update.rs`
(rede e disco). O app olha as releases de `lippdev/consolemode` e só considera uma release que tenha
**tudo** isto:

| Regra | Valor |
|-------|-------|
| Tag | `v2.X.Y-alpha.N` (também aceita `-beta.N`, `-rc.N`): sempre `v` + semver, **major 2** e com sufixo de pré-lançamento |
| Pré-lançamento | a release precisa estar marcada como **pre-release** e não pode ser rascunho |
| Instalador | asset `ConsoleMode2Alpha-Setup-x64.exe` |
| Checksum | asset `ConsoleMode2Alpha-Setup-x64.exe.sha256` |
| Origem | os dois assets vêm de `https://github.com/lippdev/consolemode/releases/download/...` |

- Tags `v1.*` (e qualquer outra que não seja `v2.*`) **nunca** são consideradas: a 2.0 não oferece a 1.7.
- A versão em execução (`CARGO_PKG_VERSION`) é comparada por semver com a tag: só uma versão maior é oferecida
  (`2.0.0-alpha.1` < `2.0.0-alpha.10` < `2.0.0-beta.1`).
- O arquivo `.sha256` tem o formato do `sha256sum`: `<64 hex>  ConsoleMode2Alpha-Setup-x64.exe`. Se o GitHub
  também publicar o `digest` (`sha256:...`) do asset, os dois precisam ser iguais; qualquer divergência
  recusa a atualização.
- O instalador só roda depois que o SHA-256 do arquivo baixado confere. Se não conferir, o arquivo é apagado
  e nada é instalado. Roda com `/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS` e o app sai
  em seguida; o instalador reabre o app no fim (instalação silenciosa).
- A rede só é usada quando o usuário aperta **Sistema › Atualizações › Procurar atualização** (e de novo
  para instalar) ou na verificação silenciosa ao abrir o app, se **Procurar atualizações ao abrir** estiver
  ligado (config `checkUpdates`). Se não achar nada, o app não mostra nada.
- A arquitetura está no nome (`x64`). Quando houver ARM64, vira outro par de assets com `arm64`.

### Aviso sobre a 1.7

O atualizador da **1.7** (`UpdateService.cs`) olha as mesmas releases e, para quem **optou por pré-lançamentos**
ou usa uma build de teste, vê `v2.0.0-alpha.N` como "versão mais nova". Como o asset da 2.0 não começa com
`ConsoleMode-Setup`/`ConsoleMode-Portable`, a 1.7 não baixa nem instala nada, mas mostraria o aviso com o
link da release. Antes do primeiro alpha publicado, decidir entre: (a) uma 1.7.x que ignora tags `v2.*`;
(b) publicar os alphas da 2.0 em outro repositório (basta trocar `REPOSITORY` em `cm-core/src/update.rs`).
Quem está na 1.7 estável (sem a opção de pré-lançamentos) não é afetado.

## Gerar os arquivos (no PC do dono)

### Home do modo Xbox no Windows 11

O empacotamento local acrescenta a identidade da alpha ao ativador `ConsoleMode2Home.exe` com `mt.exe` do Windows SDK e copia o manifesto AppX, o descritor de desenvolvimento e os ícones para junto dele. O instalador leva esses arquivos. O registro é opcional, por usuário, em **Sistema → Inicialização → Experiência de tela inteira para jogos**.

Para preparar uma cópia de debug, copie `ConsoleMode2.exe` e `ConsoleMode2Home.exe` para uma pasta de teste e rode `build\Stage-ConsoleMode2Home.ps1 -InstallRoot <pasta>`. O `ConsoleMode2.exe` dessa pasta aceita `--fse-status`, `--fse-register` e `--fse-remove`. Mantenha-o no mesmo local enquanto estiver registrado. Para desfazer o teste, restaure a Home anterior nos Ajustes do Windows, caso tenha escolhido a alpha, e remova o registro antes de apagar a pasta.

O pacote local usa o Modo de Desenvolvedor e um descritor restrito à identidade da alpha. Uma distribuição da capacidade especial fora desse modo exige um descritor autorizado. A disponibilidade do seletor de Home também depende da edição e configuração do Windows. [Capacidades personalizadas no Windows](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations#custom-capabilities) · [AnyFSE, referência para a declaração de Gaming Home](https://github.com/ashpynov/AnyFSE/blob/main/AppxManifest.xml)

### Executável e instalador

Precisa de Rust, CMake (o do Visual Studio serve; o script acha sozinho) e, para o instalador,
o [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
.\build\Publish-ConsoleMode2.ps1             # exe + instalador + .sha256
.\build\Publish-ConsoleMode2.ps1 -Target Exe # só o exe
```

Saída em `dist\v2\` (ignorada pelo git):

- `ConsoleMode2.exe`: release, sem janela de console, com ícone e versão embutidos (`crates/cm-app/build.rs`);
- `ConsoleMode2Alpha-Setup-x64.exe`: instalador por usuário em `%LOCALAPPDATA%\Programs\Console Mode 2 Alpha`,
  com atalho próprio no menu Iniciar, `LICENSE` e `THIRD_PARTY_NOTICES.md`;
- `ConsoleMode2Alpha-Setup-x64.exe.sha256`.

A versão vem de `[workspace.package] version` no `Cargo.toml` da raiz. O script recusa versões fora de `2.x.y`.
Se o Inno Setup não estiver instalado, só o exe é gerado, com um aviso.

O instalador tem **AppId próprio** (`{388223BE-BE29-42B4-AC17-4394D1A5F7FF}`; nunca mude), pasta, atalho e
entrada de desinstalação próprios. Não grava `consolemode://`, o valor de iniciar com o Windows nem nada
em `Software\ConsoleMode`. Desinstalar mantém os dados em `%LOCALAPPDATA%\ConsoleMode2`.

### Ícone

`crates/cm-app/assets/ConsoleMode2.ico` é o `src/ConsoleMode/Assets/Intro/logo.png` (256×256) convertido
para um `.ico` com 16, 24, 32, 48, 64, 128 e 256 px (Pillow: `Image.open(png).save(ico, sizes=[...])`).

### Notices de terceiros

`docs/v2/THIRD_PARTY_NOTICES.md` tem uma tabela gerada do `cargo metadata`. Depois de mudar dependências:
`.\build\Generate-Notices2.ps1` (e `-Check` para só conferir; o Publish já confere).

## Guardar uma alpha como rascunho

1. Faça o build no Windows a partir do commit testado pelo workflow **Console Mode 2 Alpha Build**.
2. Instale o Setup em um PC de teste e siga o checklist de entrega em [../TESTING.md](../TESTING.md).
3. Em **Releases**, crie um rascunho apontando para o SHA exato do commit da build e use como tag proposta
   a versão do `Cargo.toml` com prefixo `v` (por exemplo, `v2.0.0-alpha.0`). Marque a opção de pré-lançamento.
4. Anexe `ConsoleMode2Alpha-Setup-x64.exe` e `ConsoleMode2Alpha-Setup-x64.exe.sha256` e salve com
   **Save draft**. O rascunho é visível para quem tem acesso de escrita e fica fora dos atualizadores públicos.

Para consultar o que o atualizador oferece atualmente, rode
`cargo run -p cm-platform --example update_check [versão]`.
