# Console Mode

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, desligue as outras telas, mude o áudio e abra a interface de jogos. Saiu do jogo, a mesa volta sozinha.

🇺🇸 [Read in English](README.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![Licença](https://img.shields.io/github/license/lippdev/consolemode)

<img src="assets/features.gif" alt="Tour do Console Mode 1.5: resolução e HDR ajustados para a TV, Big Picture, Playnite ou Xbox como lançador, as telas da mesa e o áudio voltando ao sair do jogo, segurar Home para começar e Start + Select para voltar ao PC." width="800">

## Download

Baixe a **[1.5.1](https://github.com/lippdev/consolemode/releases/tag/v1.5.1)**, a versão estável mais recente:

- **`ConsoleMode-Setup-x64.exe`** (recomendado): instala por usuário, sem admin, e se atualiza sozinho.
- **`ConsoleMode-Portable-x64.exe`**: um único exe que guarda os dados ao lado dele.

Quer ajudar a testar o que vem aí? A **[1.6.0-alpha.3](https://github.com/lippdev/consolemode/releases/tag/v1.6.0-alpha.3)** é uma alpha da 1.6 que não embute mais nenhuma ferramenta de terceiros para telas e áudio. Ela ainda não foi validada em muitos setups: fique na 1.5.1 se precisa que funcione sem surpresas, ou ligue Ajustes → "Receber versões de teste" na 1.5.1 para recebê-la pelo app.

Está na 1.4.0? O app oferece a atualização. Se ela falhar por tempo esgotado, baixe o instalador uma vez; as configurações são mantidas.

Windows 10 ou 11, com [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) ou o app Xbox. O [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) é opcional, para o limite de FPS.

## Como funciona

1. Abra o app e escolha a tela onde você joga. As outras desligam.
2. Clique em **Jogar agora**, ou use o atalho de 1 clique na Área de Trabalho. Na primeira vez numa tela nova, a TV pergunta "Está vendo esta tela?" e desfaz tudo sozinha se ninguém responder.
3. Saia do Big Picture ou do Playnite e tudo volta: telas, áudio, resolução.

Prefere o sofá? Com o app na bandeja, segure o seu atalho do controle e ele começa dali.

Os atalhos do controle são você quem escolhe: nenhum vem ligado, o app pergunta na primeira abertura (e dá para mudar em Ajustes). Nenhum botão pode servir para duas ações.

| Atalho no controle | Faz | Sugestão |
|---|---|---|
| Segurar o seu atalho de **abrir** | Entra no modo console a partir da bandeja | **Home** (Xbox / PS) |
| Segurar o seu atalho do **menu** | Menu sobre o jogo: volume, resolução, áudio, FPS, HDR, sair | **Select + Y** (Create + △) |
| Segurar o seu atalho de **voltar ao PC** | Volta para o PC | **Start + Select** (Options + Create) |

## Recursos

- Esconde as outras telas **desconectando**, com **cortinas pretas** ou por **DDC/CI**
- **Resolução e Hz** por tela, **HDR**, **VRR** e **limite de FPS** (RTSS) enquanto você joga
- O áudio vai para a TV (ou qualquer saída) e volta depois
- [Liga a TV e troca para a entrada do PC](docs/GUIDE.pt-BR.md#controle-da-tv) (Google TV / Android TV ou LG webOS pela rede), e pode colocá-la em espera no fim
- Abre **Steam Big Picture**, **Playnite em tela cheia** ou o **Modo Xbox**
- Duas interfaces: **Desktop** (mouse) e **Console** (tela cheia, controles Xbox e PlayStation)
- Automação com os links `consolemode://start` / `stop` / `menu` (Stream Deck, scripts) e uma [API de controle local](docs/GUIDE.pt-BR.md#api-de-controle-local)
- Português, inglês e espanhol

## Novidades da 1.5.0

- **Interface Console:** tela inicial e Ajustes em tela cheia feitos para o controle, com controles Xbox e PlayStation
- **Menu Select + Y** sobre o jogo: volume, resolução, áudio, FPS, HDR e a saída, em blocos na horizontal
- **Atalhos do sofá** que funcionam também com DualSense e DualShock 4, sem precisar de Steam Input
- **Automação:** links `consolemode://`, `--stop` e uma API de controle local
- Teste de controle nos Ajustes, botão de feedback, interface em espanhol e atualização direto do sofá

Todos os detalhes nas [notas da versão](https://github.com/lippdev/consolemode/releases/tag/v1.5.0) e no [changelog](CHANGELOG.md) · [notes in English](CHANGELOG.en-US.md).

## Roadmap

**Próximos:** overlay de FPS ([#66](https://github.com/lippdev/consolemode/issues/66)), gravar os últimos 30 s ([#77](https://github.com/lippdev/consolemode/issues/77)), posição dos ícones da área de trabalho ([#30](https://github.com/lippdev/consolemode/issues/30)) · **Depois:** perfis por jogo ([#69](https://github.com/lippdev/consolemode/issues/69)), winget ([#13](https://github.com/lippdev/consolemode/issues/13)), mais idiomas ([#14](https://github.com/lippdev/consolemode/issues/14)) · **Explorando:** HDMI-CEC ([#75](https://github.com/lippdev/consolemode/issues/75)), Linux e macOS ([#74](https://github.com/lippdev/consolemode/issues/74)), [e mais](https://github.com/lippdev/consolemode/issues?q=is%3Aopen+label%3Aroadmap).

## Mais

- [Guia](docs/GUIDE.pt-BR.md): modos e restauração, API de controle, HDR/VRR/FPS, solução de problemas, compilar
- **Feedback:** o botão ao lado de Ajustes no app, ou [este formulário](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Uma ⭐ ajuda outros jogadores de sofá a encontrar o projeto.
- Alguns antivírus podem acusar as ferramentas auxiliares incluídas; o código está todo aqui.

Licença [GNU Affero General Public License v3.0 only](LICENSE) · [Avisos de terceiros](THIRD_PARTY_NOTICES.md) · [Política de assinatura de código](docs/CODE_SIGNING.md)
