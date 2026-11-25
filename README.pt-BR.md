# Console Mode

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, desligue as outras telas, mude o áudio e abra a interface de jogos. Saiu do jogo, a mesa volta sozinha.

[English](README.md) · Português (Brasil) · [Italiano](README.it-IT.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![Licença](https://img.shields.io/github/license/lippdev/consolemode)

https://github.com/user-attachments/assets/9f244c06-76e5-4ccb-ab66-e637eae900ec

## Download

Baixe a **[1.7.1](https://github.com/lippdev/consolemode/releases/tag/v1.7.1)**, a versão estável mais recente:

- **`ConsoleMode-Setup-x64.exe`** (recomendado): instala por usuário, sem admin, e se atualiza sozinho.
- **`ConsoleMode-Portable-x64.exe`**: um único exe que guarda os dados ao lado dele.
- Ou pelo winget: `winget install lippdev.ConsoleMode`

Está na 1.6? O app oferece a atualização e mantém suas configurações. Prefere a interface antiga, com o modo Desktop? Baixe o **Console Mode Legacy** (`ConsoleModeLegacy-Setup-x64.exe` ou `ConsoleModeLegacy-Portable-x64.exe`) na [release 1.7.0](https://github.com/lippdev/consolemode/releases/tag/v1.7.0). O Legacy é baseado na 1.6.2 e compartilha os ajustes com a versão atual. Ele deixou de receber correções e atualizações.

Quer testar o que vem aí antes de todo mundo? Ligue Sistema → App → "Receber versões de teste (beta)".

Windows 10 ou 11, com [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) ou o app Xbox.

## Como funciona

1. Abra o app e escolha a tela onde você joga. As outras desligam.
2. Clique em **Jogar agora**, ou use o atalho de 1 clique na Área de Trabalho. Na primeira vez numa tela nova, a TV pergunta "Está vendo esta tela?" e desfaz tudo sozinha se ninguém responder.
3. Saia do Big Picture ou do Playnite e tudo volta: telas, áudio, resolução.

Prefere o sofá? Com o app na bandeja, segure o seu atalho do controle e ele começa dali.

Os atalhos do controle são você quem escolhe: nenhum vem ligado, o tour da primeira abertura mostra onde escolhê-los (Sistema → Controle). Nenhum botão pode servir para duas ações.

| Atalho no controle | Faz | Sugestão |
|---|---|---|
| Segurar o seu atalho de **abrir** | Entra no modo console a partir da bandeja | **Home** (Xbox / PS) |
| Segurar o seu atalho do **menu** | Menu sobre o jogo: janelas abertas, explorador de arquivos, volume, resolução, áudio, HDR, sair | **Select + Y** (Create + △) |
| Segurar o seu atalho de **voltar ao PC** | Volta para o PC | **Start + Select** (Options + Create) |

## Recursos

- Esconde as outras telas **desconectando**, ou com **cortinas pretas**, tudo pelas APIs do próprio Windows: nenhuma ferramenta de terceiros para telas ou áudio vem embutida
- **Resolução e Hz** por tela, **HDR**, e **VRR** enquanto você joga
- O áudio vai para a TV (ou qualquer saída) e volta depois
- Oculta os **ícones da área de trabalho** durante a sessão se você ativar a opção e restaura a visibilidade anterior ao voltar ao PC
- Abre **Steam Big Picture**, **Playnite em tela cheia** ou o **Modo Xbox**, e manda a Steam para a bandeja ao voltar ao PC
- **Menu da sessão** sobre o jogo: troque ou feche as janelas abertas, mude volume, resolução, áudio e HDR, esconda o cursor do mouse ou mova-o com o controle e navegue pelos seus arquivos com o [ControlFS](https://github.com/nextestudios/ControlFS) e pela web com o [JoyChromium](https://github.com/lippdev/joychromium)
- Uma interface no estilo dos consoles, feita para a TV, que também funciona com mouse e teclado (tela cheia, controles Xbox e PlayStation, abas no LB/RB, um papel de parede próprio ou as capas da sua Steam como plano de fundo), com um tour guiado na primeira vez que você abre
- **Seus aplicativos** como iniciadores: qualquer `.exe` (RetroArch, outro launcher…) vira um bloco no Início
- Devolve os **ícones da área de trabalho** e o **ponteiro do mouse** ao lugar, pode deixar o controle **acordar o PC** e restaura a mesa na próxima abertura se uma sessão for interrompida
- Português, inglês, espanhol, alemão, francês, italiano e russo

## Novidades da 1.7.1

- **Oculte os ícones da área de trabalho durante a sessão:** o app restaura a visibilidade anterior ao voltar ao PC.
- **Melhorias nos controles:** leitura de bateria pelo Windows para controles Bluetooth quando disponível, suporte a botões centrais HID e mais detalhes em **Testar controle**.
- **Correções na sessão:** a seleção do iniciador é mantida ao trocar de aba, conectar um controle volta a iniciar a sessão com o app na bandeja e os modos de TV a 59 e 59,94 Hz são tratados corretamente.
- O canal de testes do app oferece apenas betas.

## Novidades da 1.7.0

- **Uma interface só, com cara de console:** seus iniciadores numa fileira com os logos oficiais, a fonte Inter, o fundo Aurora, uma abertura com som e um tour guiado na primeira vez. Funciona com controle, mouse e teclado; a interface Desktop continua no Legacy
- **Seus aplicativos no Início:** adicione qualquer `.exe` pelo **+**, quantos quiser, e tire um deles pelo próprio bloco
- **Menu da sessão em qualquer lugar:** o atalho do menu abre sem iniciar uma sessão, para usar o PC do sofá, e um atalho novo abre a visão de tarefas do Windows
- **JoyChromium no menu da sessão:** o navegador para a TV que se usa com o controle abre por cima do jogo e se instala por ali mesmo se faltar
- **A mesa volta inteira:** os ícones da área de trabalho e o ponteiro do mouse voltam ao lugar e, se o app ou o PC cair no meio da sessão, a próxima abertura restaura tudo
- **Acordar o PC pelo controle**, a bateria do controle na tela e a mesma cara em qualquer escala do Windows, de 1080p a 4K
- **Console Mode Legacy** estreou na 1.7.0 para quem prefere a interface antiga

## Novidades da 1.6

- **Interface Console redesenhada:** abas Início, Sessão e Sistema no LB/RB, blocos das telas, as capas da sua Steam como plano de fundo e sons da interface
- **Menu da sessão como o overlay do Steam:** tela cheia sobre o jogo, com as janelas abertas no meio (um Alt+Tab para o controle)
- **Explorador de arquivos embutido:** o ControlFS abre pelo menu da sessão, e é instalado dali mesmo se faltar
- **Seus atalhos, seus botões:** escolha os botões do controle que abrem o Console Mode, o menu da sessão e a volta ao PC
- **Só código do projeto:** telas e áudio pelas APIs do Windows, sem as ferramentas da NirSoft
- A Steam vai para a bandeja ao voltar ao PC, o layout da mesa volta idêntico (monitores em retrato também) e o Playnite demorando para abrir não encerra mais a sessão

Todos os detalhes nas [notas da versão](https://github.com/lippdev/consolemode/releases/tag/v1.7.1) e no [changelog](CHANGELOG.md) · [notes in English](CHANGELOG.en-US.md).

## Mais

- [Guia](docs/GUIDE.pt-BR.md): modos e restauração, HDR/VRR, solução de problemas, compilar
- **Feedback:** Sistema → Atalho, ajuda e dados → Enviar feedback no app, ou [este formulário](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Uma ⭐ ajuda outros jogadores de sofá a encontrar o projeto.
- **Traduções:** corrija um texto ou traga o seu idioma pelo [guia de tradução](docs/TRANSLATING.md) (em inglês); não precisa de Windows.
- As versões ainda não são assinadas, então o Windows SmartScreen ou um antivírus pode avisar sobre o download; o código está todo aqui.

Licença [GNU Affero General Public License v3.0 only](LICENSE) · [Avisos de terceiros](THIRD_PARTY_NOTICES.md) · [Política de assinatura de código](docs/CODE_SIGNING.md)
