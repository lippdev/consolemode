# Console Mode

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, desligue as outras telas, mude o áudio e abra a interface de jogos. Saiu do jogo, a mesa volta sozinha.

[English](README.md) · Português (Brasil) · [Español](README.es-ES.md) · [Deutsch](README.de-DE.md) · [Français](README.fr-FR.md) · [Русский](README.ru-RU.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![Licença](https://img.shields.io/github/license/lippdev/consolemode)

<img src="assets/console-mode-1.6-short.gif" alt="Tour do Console Mode 1.6: um clique leva o jogo para a TV, o menu da sessão sobre o jogo com as janelas abertas, o explorador de arquivos ControlFS embutido, a interface Console com as abas Início, Sessão e Sistema e os atalhos do controle à sua escolha." width="800">

## Download

Baixe a **[1.7.0](https://github.com/lippdev/consolemode/releases/tag/v1.7.0)**, a versão estável mais recente:

- **`ConsoleMode-Setup-x64.exe`** (recomendado): instala por usuário, sem admin, e se atualiza sozinho.
- **`ConsoleMode-Portable-x64.exe`**: um único exe que guarda os dados ao lado dele.
- Ou pelo winget: `winget install lippdev.ConsoleMode`

Está na 1.6? O app oferece a atualização e mantém suas configurações.

Quer testar o que vem aí antes de todo mundo? Ligue Ajustes → "Receber versões de teste (alpha e beta)".

Windows 10 ou 11, com [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) ou o app Xbox. O [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) é opcional, para o limite de FPS e o contador de FPS.

## Como funciona

1. Abra o app e escolha a tela onde você joga. As outras desligam.
2. Clique em **Jogar agora**, ou use o atalho de 1 clique na Área de Trabalho. Na primeira vez numa tela nova, a TV pergunta "Está vendo esta tela?" e desfaz tudo sozinha se ninguém responder.
3. Saia do Big Picture ou do Playnite e tudo volta: telas, áudio, resolução.

Prefere o sofá? Com o app na bandeja, segure o seu atalho do controle e ele começa dali.

Os atalhos do controle são você quem escolhe: nenhum vem ligado, o app pergunta na primeira abertura (e dá para mudar em Ajustes). Nenhum botão pode servir para duas ações.

| Atalho no controle | Faz | Sugestão |
|---|---|---|
| Segurar o seu atalho de **abrir** | Entra no modo console a partir da bandeja | **Home** (Xbox / PS) |
| Segurar o seu atalho do **menu** | Menu sobre o jogo: janelas abertas, explorador de arquivos, volume, resolução, áudio, FPS, HDR, sair | **Select + Y** (Create + △) |
| Segurar o seu atalho de **voltar ao PC** | Volta para o PC | **Start + Select** (Options + Create) |

## Recursos

- Esconde as outras telas **desconectando**, com **cortinas pretas** ou por **DDC/CI**, tudo pelas APIs do próprio Windows: nenhuma ferramenta de terceiros para telas ou áudio vem embutida
- **Resolução e Hz** por tela, **HDR**, **VRR** e **limite de FPS** (RTSS) enquanto você joga
- **Contador de FPS** sobre o jogo (RTSS) num card em três tamanhos: apenas FPS, compacto (uso de CPU, GPU e RAM), detalhado (clocks, temperatura da GPU e memória) ou do seu jeito
- O áudio vai para a TV (ou qualquer saída) e volta depois
- [Liga a TV e troca para a entrada do PC](docs/GUIDE.pt-BR.md#controle-da-tv) (Google TV / Android TV pela rede), e pode colocá-la em espera no fim
- Abre **Steam Big Picture**, **Playnite em tela cheia**, o **Modo Xbox** ou o [JoyChromium](https://github.com/lippdev/joychromium) (a web pelo controle), e manda a Steam para a bandeja ao voltar ao PC
- **Menu da sessão** sobre o jogo: troque ou feche as janelas abertas, mude volume, resolução, áudio, FPS e HDR, esconda o cursor do mouse ou mova-o com o controle e navegue pelos seus arquivos com o [ControlFS](https://github.com/nextestudios/ControlFS)
- Duas interfaces: **Desktop** (mouse) e **Console** (tela cheia, controles Xbox e PlayStation, abas no LB/RB, um papel de parede próprio ou as capas da sua Steam como plano de fundo)
- **Perfis por jogo** para os jogos da Steam (resolução, HDR, limite de FPS e áudio), o modo **Aplicativo personalizado** para qualquer `.exe` e a bateria do controle na tela
- Devolve os **ícones da área de trabalho** e o **ponteiro do mouse** ao lugar, pode deixar o controle **acordar o PC** e restaura a mesa na próxima abertura se uma sessão for interrompida
- Automação com os links `consolemode://start` / `stop` / `menu` (Stream Deck, scripts) e uma [API de controle local](docs/GUIDE.pt-BR.md#api-de-controle-local)
- Português, inglês, espanhol, alemão, francês e russo

## Novidades da 1.7.0

- **Interface Console com cara de console de mesa:** seus iniciadores numa fileira com os logos oficiais (o foco escolhe o que abre, como num PS5), a fonte Inter, o fundo Aurora e a aba Sistema com categorias
- **JoyChromium integrado:** o navegador para a TV que se usa com o controle abre em tela cheia na tela de jogo e volta a ser janela quando você volta ao PC
- **Perfis por jogo:** cada jogo da Steam lembra a própria resolução, HDR, limite de FPS e áudio, salvos pelo menu da sessão
- **Aplicativo personalizado:** qualquer `.exe` (RetroArch, outro launcher…) como o que abre na TV
- **A mesa volta inteira:** os ícones da área de trabalho e o ponteiro do mouse voltam ao lugar e, se o app ou o PC cair no meio da sessão, a próxima abertura restaura tudo
- **Acordar o PC pelo controle**, a bateria do controle na tela e um card de Diagnóstico com relatório para copiar

## Novidades da 1.6

- **Interface Console redesenhada:** abas Início, Sessão e Sistema no LB/RB, blocos das telas, as capas da sua Steam como plano de fundo e sons da interface
- **Menu da sessão como o overlay do Steam:** tela cheia sobre o jogo, com as janelas abertas no meio (um Alt+Tab para o controle)
- **Explorador de arquivos embutido:** o ControlFS abre pelo menu da sessão, e é instalado dali mesmo se faltar
- **Contador de FPS** sobre o jogo pelo RTSS, com os estilos Compacto, Detalhado e Personalizado
- **Seus atalhos, seus botões:** escolha os botões do controle que abrem o Console Mode, o menu da sessão e a volta ao PC
- **Controle da TV:** liga a Google TV / Android TV e troca para a entrada do PC
- **Só código do projeto:** telas e áudio pelas APIs do Windows, sem as ferramentas da NirSoft
- A Steam vai para a bandeja ao voltar ao PC, o layout da mesa volta idêntico (monitores em retrato também) e o Playnite demorando para abrir não encerra mais a sessão

Todos os detalhes nas [notas da versão](https://github.com/lippdev/consolemode/releases/tag/v1.7.0) e no [changelog](CHANGELOG.md) · [notes in English](CHANGELOG.en-US.md).

## Roadmap

**Próximos:** o [Tako](https://github.com/lippdev/tako-launcher) como iniciador e uma API local para ele, Hydra e AnyFSE, um link de apoio ao projeto · **Depois:** gravar os últimos 30 s ([#77](https://github.com/lippdev/consolemode/issues/77)), mais TVs (LG webOS, Samsung, HDMI-CEC [#75](https://github.com/lippdev/consolemode/issues/75)) · **Explorando:** Linux e macOS ([#74](https://github.com/lippdev/consolemode/issues/74)), [e mais](https://github.com/lippdev/consolemode/issues?q=is%3Aopen+label%3Aroadmap). O plano completo está no [PRD](docs/PRD.md).

## Mais

- [Guia](docs/GUIDE.pt-BR.md): modos e restauração, API de controle, HDR/VRR/FPS, solução de problemas, compilar
- **Feedback:** o botão ao lado de Ajustes no app, ou [este formulário](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Uma ⭐ ajuda outros jogadores de sofá a encontrar o projeto.
- **Traduções:** corrija um texto ou traga o seu idioma pelo [guia de tradução](docs/TRANSLATING.md) (em inglês); não precisa de Windows.
- As versões ainda não são assinadas, então o Windows SmartScreen ou um antivírus pode avisar sobre o download; o código está todo aqui.

Licença [GNU Affero General Public License v3.0 only](LICENSE) · [Avisos de terceiros](THIRD_PARTY_NOTICES.md) · [Política de assinatura de código](docs/CODE_SIGNING.md)
