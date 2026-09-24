# Console Mode

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, desligue as outras telas, mude o áudio e abra a interface de jogos. Saiu do jogo, a mesa volta sozinha.

🇺🇸 [Read in English](README.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![License](https://img.shields.io/badge/licença-MIT-green)

<img src="assets/console-mode.gif" alt="Os monitores da mesa desligam para a TV no HDMI; ao sair do Big Picture, a mesa volta sozinha." width="800">

## Download

Baixe a última versão em [Releases](https://github.com/lippdev/consolemode/releases/latest):

- **`ConsoleMode-Setup-x64.exe`** (recomendado): instala por usuário, sem admin, e se atualiza sozinho.
- **`ConsoleMode-Portable-x64.exe`**: um único exe que guarda os dados ao lado dele.

Windows 10 ou 11, com [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) ou o app Xbox. O [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) é opcional, para o limite de FPS.

## Como funciona

1. Abra o app e escolha a tela onde você joga. As outras desligam.
2. Clique em **Jogar agora**, ou use o atalho de 1 clique na Área de Trabalho. Na primeira vez numa tela nova, a TV pergunta "Está vendo esta tela?" e desfaz tudo sozinha se ninguém responder.
3. Saia do Big Picture ou do Playnite e tudo volta: telas, áudio, resolução.

Prefere o sofá? Com o app na bandeja, segure o botão Home do controle e ele começa dali.

| Atalho no controle | Faz |
|---|---|
| Segurar **Home** (Xbox / PS) | Entra no modo console a partir da bandeja |
| Segurar **Select + Y** (Create + △) | Menu sobre o jogo: volume, resolução, áudio, FPS, HDR, sair |
| Segurar **Start + Select** (Options + Create) | Volta para o PC |

## Recursos

- Esconde as outras telas **desconectando**, com **cortinas pretas** ou por **DDC/CI**
- **Resolução e Hz** por tela, **HDR**, **VRR** e **limite de FPS** (RTSS) enquanto você joga
- O áudio vai para a TV (ou qualquer saída) e volta depois
- Abre **Steam Big Picture**, **Playnite em tela cheia** ou o **Modo Xbox**
- Duas interfaces: **Desktop** (mouse) e **Console** (tela cheia, controles Xbox e PlayStation)
- Automação com os links `consolemode://start` / `stop` / `menu` (Stream Deck, scripts) e uma [API de controle local](docs/GUIDE.pt-BR.md#api-de-controle-local)
- Português, inglês e espanhol

## Novidades da 1.5

- **Menu Select + Y** sobre o jogo, com blocos na horizontal, agora funcionando com controles de PlayStation
- Navegação pelo controle em todo o app e um aviso no canto quando o jogo começa
- Atualizações instalam pela interface Console e não esgotam mais o tempo

Detalhes no [changelog](CHANGELOG.md) · [notes in English](CHANGELOG.en-US.md).

## Roadmap

**Próximos:** overlay de FPS ([#66](https://github.com/lippdev/consolemode/issues/66)), gravar os últimos 30 s ([#77](https://github.com/lippdev/consolemode/issues/77)), posição dos ícones da área de trabalho ([#30](https://github.com/lippdev/consolemode/issues/30)) · **Depois:** perfis por jogo ([#69](https://github.com/lippdev/consolemode/issues/69)), winget ([#13](https://github.com/lippdev/consolemode/issues/13)), mais idiomas ([#14](https://github.com/lippdev/consolemode/issues/14)) · **Explorando:** HDMI-CEC ([#75](https://github.com/lippdev/consolemode/issues/75)), Linux e macOS ([#74](https://github.com/lippdev/consolemode/issues/74)), [e mais](https://github.com/lippdev/consolemode/issues?q=is%3Aopen+label%3Aroadmap).

## Mais

- [Guia](docs/GUIDE.pt-BR.md): modos e restauração, API de controle, HDR/VRR/FPS, solução de problemas, compilar
- **Feedback:** o botão ao lado de Ajustes no app, ou [este formulário](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Uma ⭐ ajuda outros jogadores de sofá a encontrar o projeto.
- Alguns antivírus podem acusar as ferramentas auxiliares incluídas; o código está todo aqui.

Licença MIT · [Avisos de terceiros](THIRD_PARTY_NOTICES.md)
