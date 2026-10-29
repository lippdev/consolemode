# Console Mode — PRD e backlog

**Versão:** 0.3 (inventário completo, inclusive o que ficou pelo caminho)  
**Data:** 29/10/2026 (v0.2 e v0.1 do mesmo dia)  
**Status:** em vigor. Primeiro PRD do projeto: até a 1.7.0-alpha.2 o escopo vivia no roadmap do README, nas issues e nos changelogs. A v0.2 registra as decisões que já valiam na prática (§2), a fronteira com o Tako (§3), tudo o que foi entregue desde 07/07/2026 (§9, itens `[x]`) e o que vem depois da 1.7.0. A v0.3 acrescenta o que foi descartado ou revertido (§2.3) e os bugs conhecidos sem correção (épico BUG).  
**Plataformas:** Windows 10 (19041 ou mais novo) e Windows 11, x64. Linux e macOS só no backlog (PLAT, D-18).  
**Licença:** AGPL-3.0-only ([LICENSE](../LICENSE))

---

## 0. Como usar este documento

Este PRD descreve **o que o Console Mode é, o que já entregou e o que precisa ter daqui para a frente**. O detalhe de
cada mudança fica nos changelogs ([pt-BR](../CHANGELOG.md), [en-US](../CHANGELOG.en-US.md)); a validação manual
fica em [TESTING.md](TESTING.md).

Para quem for implementar, inclusive agentes:

1. **Ordem de verdade:** este PRD, depois o [`AGENTS.md`](../AGENTS.md), depois o [GUIDE](GUIDE.md). Se o código
   e o PRD discordarem sobre comportamento já lançado, o changelog da versão decide e o PRD é corrigido.
2. **Uma história = um PR**, com título em Conventional Commits e o item do changelog terminando no número do PR.
   Marque `[x]` neste arquivo no mesmo PR em que a história entra na `main`.
3. **Não decida sozinho o que este PRD marca como decisão (§2).** Se uma decisão se mostrar errada na prática,
   leve a evidência ao mantenedor antes de mudar o rumo.
4. **Release, tag e o workflow `Release` com "publish" só com pedido explícito do mantenedor.**

---

## 1. Visão

Um gesto (um botão do controle, um atalho ou um link) e o PC vira um console: a TV passa a ser a tela de jogo, as
outras telas apagam, o áudio vai para a TV, o launcher abre em tela cheia e tudo se controla do sofá. Ao terminar,
a mesa volta **exatamente** como estava.

**Proposta de valor:** o mesmo PC serve de mesa de trabalho e de console, sem cabo trocado, sem configuração
refeita e sem caminho de volta perdido.

### 1.1 Objetivos

1. Entrar e sair do modo console com um gesto, do sofá, sem tocar no mouse.
2. Restaurar telas, áudio, ícones e ponteiro ao estado anterior, inclusive depois de uma queda do app.
3. Funcionar com os launchers que o usuário já usa (Steam Big Picture, Playnite, modo Xbox, qualquer `.exe`) e,
   em especial, com o **Tako** (§3).
4. Dar ferramentas de sessão por cima do jogo: volume, resolução, áudio, FPS, HDR, janelas e arquivos.
5. Controlar a TV (ligar, entrada HDMI, standby) quando ela permitir.
6. Ser leve, gratuito, aberto e traduzido.

### 1.2 Público-alvo

- Quem joga no PC ligado à TV na sala ou no quarto.
- Quem usa o mesmo PC como mesa de trabalho (dois ou mais monitores) e como console.
- Usuários de Steam Big Picture, Playnite em tela cheia, modo Xbox e, a partir da integração, do Tako.
- Colaboradores: tradutores, testadores de setups de TV e monitor, autores de PR.

### 1.3 Princípios

- **Controller-first:** toda jornada principal funciona só com o controle, Xbox ou PlayStation.
- **Recovery-first:** nenhuma sessão deixa o usuário sem imagem, sem áudio ou sem caminho de volta à mesa.
  Toda troca é salva em disco antes de acontecer.
- **Código próprio:** telas e áudio usam as APIs do Windows; nada de ferramentas de terceiros embutidas, exceto o
  `rtss-cli` (MIT) (D-02, D-03).
- **Nada ligado de surpresa:** atalhos de controle, opções que mudam o sistema e versões de teste começam
  desligados (D-07, D-05).
- **Honesto sobre o que não dá para testar:** o que depende de TV, monitor, áudio ou controle vai para o
  `TESTING.md` e é validado no hardware antes da estável.
- **O Console Mode não é o Tako:** o Console Mode prepara e devolve o PC; o Tako lança e organiza jogos (§3).

---

## 2. Decisões

| # | Decisão | Situação |
|---|---|---|
| D-01 | **C# / .NET 8 com WinUI 3 (Windows App SDK)**, MVVM com CommunityToolkit.Mvvm. Substitui o app em PowerShell / WinForms do protótipo, que foi para a branch `legacy`. | Decidido (23/09/2026, #3, #8, #9) |
| D-02 | **Telas e áudio pelas APIs do Windows** (CCD / `SetDisplayConfig`, Core Audio). Sem MultiMonitorTool e SoundVolumeView, sem caminho de volta para eles; os arquivos antigos da pasta `tools` não são mais apagados pelo app. | Decidido (1.6.0-alpha.1 e alpha.2, #91, #97 a #101, #119) |
| D-03 | O pacote embute **só o `rtss-cli` (MIT)**. O RTSS em si é do usuário. | Decidido (#110) |
| D-04 | **Instalador por usuário, sem administrador**, e versão portátil. O app se atualiza sozinho nas duas formas, conferindo o SHA-256 do arquivo. | Decidido (1.3.0, #3) |
| D-05 | **Canais estável e de teste (alpha, beta)**, numerados (`1.7.0-alpha.N`, `1.7.0-beta.N`, `1.7.0`). Versões de teste só chegam a quem liga "Receber versões de teste". | Decidido (1.5.1, #103) |
| D-06 | **Publicação pelo workflow `Release`**, por tag `v*` ou à mão com "publish". A estável abre o PR no winget (`lippdev.ConsoleMode`). Tag e publicação **só com pedido do mantenedor**. | Decidido (#106, #123) |
| D-07 | **Atalhos de controle escolhidos pelo usuário.** Nenhum vem ligado; a primeira execução mostra o setup com sugestões (Home entra, Select + Y abre o menu, Start + Select volta). | Decidido (1.6.0, #116) |
| D-08 | **Duas interfaces:** Desktop (mouse) e Console (tela cheia, controle). O modo Automático escolhe a Console quando há controle conectado. A interface Console e o menu da sessão usam sempre o tema escuro. | Decidido (1.5.0, #32; tema escuro desde #117) |
| D-09 | **Interface Console no estilo de console doméstico:** abas Início, Sessão e Sistema com LB/RB, lançadores em fileira, Sistema com categorias à esquerda, fonte Inter embutida, fundo Aurora por padrão. | Decidido (1.6.0 #117; 1.7.0 #162, #164, #165) |
| D-10 | **Menu da sessão no estilo do overlay da Steam** (Shift + Tab): tela cheia, jogo escurecido atrás, coluna de opções e janelas abertas no meio. | Decidido (1.6.0, #120) |
| D-11 | **Steam ao voltar ao PC:** fecha o Big Picture e manda só a janela da Steam para a bandeja, com a Steam rodando; nada acontece com um jogo aberto. | Decidido (#121, #131, #140) |
| D-12 | **Esconder o ponteiro é manual**, pela linha do menu da sessão; o app não esconde nada sozinho. Estacionar o ponteiro no canto ao abrir o launcher é outra função (#172) e vem ligada (D-21). | Decidido (1.6.1, #145) |
| D-13 | **Seis idiomas** (pt-BR, en-US, es-ES, de-DE, fr-FR, ru-RU). Toda string nova entra nos seis catálogos, traduzida; os testes reprovam chave faltando, placeholder diferente ou frase em inglês copiada. | Decidido (#86, #151, #156) |
| D-14 | **Changelogs em pt-BR e en-US**, seção `[Unreleased]` em cada PR. O aviso de atualização mostra o destaque no idioma do app quando há changelog nele. | Decidido (#4, #50, #158) |
| D-15 | **Testes:** projeto `net8.0` que compila só os arquivos de lógica pura do app; tudo que toca Win32 / WinUI fica fora e vai para o `TESTING.md`. CI no Windows a cada PR, com CodeQL e Dependabot. | Decidido (#84, #86, #87) |
| D-16 | **Fronteira com o Tako** como na [ADR-0025 do Tako](https://github.com/lippdev/tako-launcher/blob/main/docs/adr/0025-fronteira-com-o-console-mode.md): TV, FPS, janelas, ControlFS, sessão de console e atalho de volta são do Console Mode; resolução, áudio e cursor por jogo são do Tako (§3). | Decidido (ADR-0025 do Tako, aceita em 16/10/2026) |
| D-17 | **API local v1 no contrato da ADR-0025** (pipe por usuário, `hello` com versão e capacidades, `call` / `result` / `event`). A API atual (`\\.\pipe\ConsoleMode.Control`, uma linha JSON) continua funcionando como legado, sem funções novas. | Proposta. Falta o mantenedor aprovar (épico API). |
| D-18 | **Linux e macOS só depois de separar o núcleo** (sessão, perfis, API) da camada Windows e da interface. Até lá, nenhum código específico desses SOs. | Proposta (P3, épico PLAT) |
| D-19 | **Lançadores de terceiros** (Hydra e outros): o Console Mode só abre, acompanha e fecha o aplicativo. Não lê nem mexe em catálogo, downloads ou conteúdo dele. | Proposta |
| D-20 | **Perfis por jogo com o Tako:** quando a sessão vem do Tako, quem aplica resolução e áudio do jogo é o Tako (D-16); os perfis do Console Mode (#170) valem só nas sessões que não vêm dele. | A decidir com o Tako (CMI) |
| D-21 | **Estacionar o ponteiro** no canto da tela de jogo quando o launcher aparece vem **ligado por padrão** e devolve o ponteiro para onde estava ao voltar. | Decidido (resposta à issue #172; implementação no INPUT-014) |
| D-22 | **Restauração durável:** tudo o que a sessão muda (telas, áudio, ícones, ponteiro, comandos de fim) é gravado em disco antes da mudança e desfeito no próximo início se o app cair. | Decidido como meta; telas e áudio já cumprem, o resto está no CORE-012 |

### 2.1 Como chegamos aqui

Primeiro commit em 07/07/2026. Até 29/10/2026: **316 commits, 131 PRs mesclados, 34 versões publicadas**
(1.3.0-beta.1 a 1.7.0-alpha.2) e seis idiomas.

| Versão | Data | Marco |
|---|---|---|
| Protótipo | 07/07 a 13/07 | Script em PowerShell e GUI WinForms que chamavam ferramentas externas para trocar de tela. Hoje na branch `legacy`. |
| **1.3.0** | 23/09 | App nativo (WinUI 3): escolher a tela de jogo e o que fazer com as outras, disposição pela posição real, navegação por controle, tutorial, confirmação para não ficar sem imagem; resolução, taxa, áudio, HDR, VRR e limite de FPS; instalador por usuário e portátil; avisos e atualização pelo GitHub. |
| **1.4.0** | 23/09 | Interface em inglês, com troca de idioma sem reiniciar. |
| **1.5.0** | 25/09, 11 betas | Interface Console; menu da sessão por cima do jogo; atalhos de sofá (Home entra, Start + Select volta), também com DualSense e DualShock por HID; links `consolemode://`, `--stop` e API local por pipe (de @nextestudios); "Testar controle"; entrar quando um controle conecta; pasta manual do Playnite; espanhol; botão de feedback. |
| 1.5.1 | 28/09 | Opção para receber versões de teste. |
| **1.6.0** | 06/10, 5 alphas e 8 betas | Telas e áudio com código próprio (adeus NirSoft); interface Console com abas e LB/RB, fundo com capas da Steam e sons; menu da sessão estilo overlay da Steam com troca de janelas; ControlFS no menu (com instalação); contador de FPS pelo RTSS; atalhos configuráveis; TV Google TV / Android TV pela rede; Steam na bandeja; restauração exata da mesa; winget. |
| 1.6.1 | 10/10 | Tour do menu da sessão; cartão do contador de FPS com tamanhos, posição e estilos; esconder o ponteiro e controlar o mouse pelo controle. |
| 1.6.2 | 17/10 | Minimizar para a bandeja; alemão (u/Skinner99), francês e russo. |
| **1.7.0-alpha.1** | 19/10 | Interface Console no estilo de console doméstico; fonte Inter; o foco escolhe o lançador; modo Aplicativo personalizado; bateria do controle; Diagnóstico; resumo da atualização no idioma do app; README e guia em seis idiomas. |
| 1.7.0-alpha.2 | 19/10 | Fundo Aurora; lista de ajustes corrigida; hover e clique suaves. |

### 2.2 Estado da 1.7.0 (29/10/2026)

Na `main` e nas alphas: tudo da tabela acima até a alpha.2. Abertos com CI verde e sem conflito: #167 (ícones da
área de trabalho), #168 (acordar o PC pelo controle), #169 (logos oficiais), #170 (perfis por jogo), #171
(protótipo HTML), #174 (este PRD). Em discussão: #173, de um colaborador, que pedimos para dividir em três PRs
(ponteiro, scripts de início e fim, taxa NTSC). Ordem para fechar a 1.7.0 na §9, "Fase 1.7".

### 2.3 O que ficou pelo caminho

Ideias, PRs e comportamentos que entraram e saíram, ou nunca entraram. Ficam aqui para não serem refeitos sem
saber por quê, e para que o código guardado possa ser reaproveitado. "Onde está" aponta para o que o git ou o
GitHub ainda guardam.

| Item | O que era | O que aconteceu | Onde está |
|---|---|---|---|
| App em PowerShell / WinForms | Protótipo de julho, chamando ferramentas externas | Substituído pelo app WinUI 3 na 1.3.0 (D-01) | Branch `legacy`; #8, #9 |
| MultiMonitorTool e SoundVolumeView (NirSoft) | Troca de telas e áudio até a 1.5.x | Trocados por código próprio na 1.6.0, sem opção de voltar (D-02). Motivo: abrir caminho para assinar o executável e reduzir falsos positivos | #91, #97 a #101, #119 |
| Notas de release só em português | Changelog da release em pt-BR | Viraram notas só em inglês com links de PR (#50); o destaque da atualização voltou a sair no idioma do app na 1.7.0 (#158) | #4, #50, #158; branch local `docs/pt-br-release-notes` |
| README só em inglês | Um README em inglês | Virou par en / pt-BR e depois seis idiomas | #55, #157 |
| Controle da TV do colaborador (@nextestudios) | Android TV por ADB e Wake-on-LAN | Mesclado em 30/09 e **revertido** no mesmo dia; o motivo não ficou registrado no PR. A função voltou com implementação nossa | #93, revert #125, refeito em #126 |
| `stop` fecha o launcher (@nextestudios) | `consolemode://stop` e Start + Select fechavam Big Picture e Playnite | Revertido junto do #93; o comportamento voltou pelo #121 | #45, #125, #121 |
| **TV LG webOS** (@nextestudios) | Wake-on-LAN + SSAP por WebSocket, chave guardada com DPAPI | Fechado sem merge em 06/10, depois que o #93 (base dele) foi revertido. Código completo e revisado no PR | #94 → base para o TV-002 |
| **Home Assistant** (@nextestudios) | Rodar um script, cena ou `media_player` por REST para ligar a TV | Fechado sem merge em 06/10, mesmo motivo | #95 → TV-005 |
| **HDMI-CEC por USB** (@nextestudios) | Adaptador Pulse-Eight pelo `cec-client` da libCEC, opcional e não embutido | Fechado sem merge em 06/10, mesmo motivo | #96 → base para o TV-004 |
| Fechar a Steam inteira ao voltar | `steam.exe -shutdown` ao voltar ao PC | Trocado por mandar só a janela para a bandeja, com a Steam rodando (D-11) | #121 → #131, #140 |
| Esconder o ponteiro sozinho | Ponteiro some depois de 3 s parado, ligado por padrão | O mantenedor decidiu que nunca é automático: só pela linha do menu (D-12) | #145 |
| Linha "Gravar" no menu da sessão | Linha provisória sem função | Removida; a ideia segue no MENU-008 (#77) | #147 |
| Tiles que crescem e brilham | Lançadores da Início crescendo e acendendo no lugar do anel de foco | Revertido para contorno, selo ✓ e barra | commit `5b61315`, revert `79e66a3` |
| Sons e arte do Xbox | Usar os sons e a arte da Microsoft na interface Console | Descartado: repositório público com AGPL. Sons gerados pelo próprio app; layout só inspirado no Xbox | Decisão de 30/09 (#117) |
| Contribuições substituídas | Navegação por controle (#17), `--stop` (#23), API por pipe (#24), interface Console (#31) | Portadas ou refeitas em PRs nossos, com crédito no changelog | #47, #51, #32 |
| Três ideias da 1.6.1 | O mantenedor planejou quatro funções; só o cartão de FPS foi especificado antes de lançar | As outras três não chegaram a ser escritas | _Pergunta ao mantenedor_ |
| Controle Switch Pro por HID genérico | Suporte "de graça" pelo HID | Manda leituras impossíveis e passou a ser ignorado (#115). Sem suporte hoje | INPUT-016 |
| Branches paradas | `fix/steam-tray-and-focus-on-restore` (03/10, coberta pelo #131), `test/1.6.1-preview`, `pr23`, `pr24`, worktrees antigas | Sobras locais, sem nada que não esteja na `main` (conferir antes de apagar) | Repositório local |

---

## 3. Fronteira com o Tako

O [Tako](https://github.com/lippdev/tako-launcher) é o launcher de jogos do mesmo mantenedor. Nas alphas em C#
ele absorveu boa parte do Console Mode; a ADR-0025 do Tako desfez isso. Vale para os dois repositórios:

| É do Console Mode | É do Tako |
|---|---|
| Entrar e sair do modo console: tela de jogo, apagar as outras telas, áudio da sessão, restaurar a mesa | Biblioteca, lojas, metadados, capas e temas |
| Controle de TV (Android TV por ADB hoje; LG webOS e Samsung Tizen no backlog, TV-002 e TV-003) | Abrir jogos e acompanhar a sessão de jogo |
| Menu da sessão: volume, resolução, HDR, janelas, ControlFS | Troca de resolução e taxa **por jogo** |
| Contador e limite de FPS | Troca da saída de áudio **por jogo** |
| Steam na bandeja ao voltar | Esconder o cursor e cursor pelo controle **durante o jogo do Tako** |
| Atalho global de volta ao PC e "abrir direto na TV" | |
| Ícones da área de trabalho, ponteiro estacionado, acordar o PC | |

Notas:
- **O Tako não depende do Console Mode** e o Console Mode não depende do Tako. Quando os dois estão instalados,
  conversam pela API local v1 (D-17, épico API) e cada um oferece as funções do outro **delegando**.
- **Perfis por jogo** existem nos dois. A regra de quem aplica fica na D-20.
- **Tako como launcher do Console Mode** (CMI-004 do Tako, LAUNCH-010 aqui): o Console Mode abre o Tako em tela
  cheia na tela de jogo e o Tako avisa pela API quando a sessão termina.
- As funções de TV LG e Samsung existiam no código C# do Tako; o porte para cá aproveita esse histórico.

---

## 4. Stack

| Camada | Escolha |
|---|---|
| Linguagem e runtime | C# sobre .NET 8 (`net8.0-windows10.0.19041.0`), x64 |
| Interface | WinUI 3 (Windows App SDK 1.7, self-contained), XAML com `x:Bind` |
| MVVM e controles | CommunityToolkit.Mvvm 8.4; CommunityToolkit.WinUI (Segmented, SettingsControls); H.NotifyIcon.WinUI (bandeja) |
| Telas | CCD (`QueryDisplayConfig` / `SetDisplayConfig`) por P/Invoke, identidade estável por caminho do monitor |
| Áudio | Core Audio (COM) por P/Invoke |
| Controles | `Windows.Gaming.Input` mais leitura HID própria para DualSense e DualShock (USB e Bluetooth) |
| TV | ADB pela rede (Google TV / Android TV), Wake-on-LAN |
| FPS | RTSS pelo `rtss-cli` (MIT); estatísticas do PC por contadores do Windows e do driver (D3DKMT) |
| Segredos | DPAPI (`ProtectedData`) para chaves guardadas |
| Fonte | Inter (OFL), embutida |
| Testes | xUnit 2.9, `net8.0`, 38 arquivos e cerca de 250 casos |
| Instalador | Inno Setup (`build/ConsoleMode.iss`), publicação por `build/Publish-ConsoleMode.ps1` |
| CI | GitHub Actions: `ci.yml` (build e testes no Windows), `codeql.yml`, `release.yml`, Pullfrog (revisão automática) |

---

## 5. Arquitetura

### 5.1 Pastas

| Pasta | Conteúdo |
|---|---|
| `src/ConsoleMode/Services` | Motor da sessão (`ConsoleEngine`), telas (`MonitorService`, `DisplayBackend`), áudio, lançadores (`LaunchService`, `SteamSession`, `PlayniteExitPolicy`, `CustomApp`), controles e atalhos, TV (`Tv/`), RTSS, ControlFS, atualizações, idiomas, diagnóstico, bandeja |
| `src/ConsoleMode/Native` | P/Invoke: CCD, Core Audio, janelas e ícones, cortina preta, contadores de desempenho |
| `src/ConsoleMode/Views`, `ViewModels`, `Controls` | Interface Desktop, interface Console, menu da sessão (MVVM) |
| `src/ConsoleMode/Resources` | Catálogos de strings dos seis idiomas |
| `tests/ConsoleMode.Tests` | Testes da lógica pura, ligada por arquivo no `.csproj` |
| `build/` | Publicação, instalador, `rtss-cli` |
| `docs/` | Guia (seis idiomas), `TESTING.md`, `TRANSLATING.md`, `CODE_SIGNING.md`, este PRD, protótipo da interface |

### 5.2 Processos e ciclo de vida

- **Uma instância só.** Abrir de novo, um link `consolemode://` ou um comando no pipe chegam à instância que já roda.
- **Sessão** (`ConsoleEngine`): salvar a mesa em disco → ligar a TV e trocar a entrada (se configurado) → ligar a
  tela de jogo e esperar ela acender → apagar ou esconder as outras → trocar o áudio → aplicar resolução, HDR, VRR,
  FPS → abrir o launcher na tela de jogo → acompanhar o processo → restaurar tudo ao fim.
- **Fim da sessão:** o launcher fecha (Big Picture, Playnite, app personalizado), o atalho de volta, o menu da
  sessão, `consolemode://stop`, `--stop` ou o comando `stop` no pipe. O modo Xbox não avisa quando sai da tela
  cheia; a volta é manual.
- **Bandeja:** com a janela fechada (ou minimizada para a bandeja), o app escuta os atalhos do controle e a API.
- **Janelas:** principal (Desktop ou Console), menu da sessão por cima do jogo, aviso do atalho no início, tela de
  confirmação na TV ("você está vendo a imagem?") na primeira vez com uma tela de jogo nova, que reverte sozinha.

### 5.3 Dados

Em `%LOCALAPPDATA%\ConsoleMode`: `config.json` (configurações), `backup_monitores.cfg` e
`backup_monitores_meta.json` (mesa antes da sessão), `backup_audio.txt`, `monitor_modes_cache.json`,
`consolemode.log` (log com rotação), `tools\` (restos de versões antigas, não usados). Segredos ficam cifrados
com DPAPI. Nada vai para servidor próprio; a única rede é o GitHub (atualizações, ControlFS) e a TV.

---

## 6. Escopo funcional (o que o app faz hoje)

| Área | O que faz | Desde |
|---|---|---|
| Sessão | Escolher a tela de jogo; o que fazer com as outras (desligar, cortinas pretas, só a TV); resolução, taxa, HDR, VRR, limite de FPS; restaurar exatamente a mesa (posições, retrato, tela principal, TV desligada) | 1.3.0, refinada até 1.6.0 |
| Lançadores | Steam Big Picture, Playnite em tela cheia (inclusive portátil), modo Xbox (experimental), Aplicativo personalizado (qualquer `.exe`, com processo a vigiar) | 1.3.0 a 1.7.0 |
| Steam | Fecha o Big Picture e manda a Steam para a bandeja ao voltar; capas dos jogos como fundo | 1.6.0 |
| Controles | Xbox, DualSense e DualShock (USB e Bluetooth, também por HID); atalhos configuráveis; entrar quando o controle conecta; testar controle; bateria; mouse pelo controle; esconder o ponteiro | 1.5.0 a 1.7.0 |
| Interface Console | Abas Início, Sessão e Sistema; lançadores em fileira; Jogar agora; telas e ajustes rápidos; Sistema com categorias; fundos Aurora, capas da Steam ou imagem própria; sons; fonte Inter | 1.5.0, redesenhada na 1.6.0 e na 1.7.0 |
| Menu da sessão | Volume, resolução, áudio, FPS, HDR; janelas abertas com Alt + Tab interno e fechar com confirmação; ControlFS; contador de FPS; ponteiro; voltar ao PC e sair; tour guiado | 1.5.0, redesenhado na 1.6.0 |
| TV | Google TV / Android TV: ligar pela rede, trocar para a entrada HDMI do PC, standby ao voltar | 1.6.0 |
| FPS | Contador pelo RTSS em estilos (só FPS, compacto, detalhado, personalizado), tamanhos, posição e cartão; estatísticas do PC | 1.6.0 e 1.6.1 |
| Automação | `consolemode://start|stop|show|menu`, `--stop`, API local por pipe (`status`, `start`, `stop`, `show`) | 1.5.0 |
| Atualizações | Aviso, download e instalação na versão instalada e na portátil; canal de teste; resumo no idioma do app; winget | 1.3.0 a 1.7.0 |
| Diagnóstico | Feedback com issue pré-preenchida; Diagnóstico com relatório copiável sem dados pessoais; log | 1.5.0 e 1.7.0 |
| Idiomas | pt-BR, en-US, es-ES, de-DE, fr-FR, ru-RU, com escolha no instalador | 1.4.0 a 1.6.2 |
| Bandeja | Ícone com menu, iniciar com o Windows, minimizar para a bandeja (1.6.2) | 1.3.0 |

---

## 7. Interfaces e design

- **Desktop:** janela WinUI com Mica, para mouse e teclado. Configura tudo, inclusive o que a Console não mostra.
- **Console:** 10-foot, tema escuro fixo, foco com contorno branco, cards sem bordas, fonte Inter, transições
  curtas (140 a 260 ms) que respeitam "Efeitos de animação" do Windows. Referências: PS5, Xbox e o overlay da Steam.
- **Menu da sessão:** tela cheia por cima do jogo, vidro ou fundo sólido conforme a transparência do Windows,
  tokens próprios com variante de alto contraste.
- **Protótipo:** o HTML da interface Console (#171) é a referência visual para mudanças grandes.
- **Regra:** nenhuma tela nova sem caminho completo pelo controle; nenhum texto sem os seis idiomas.

---

## 8. Requisitos não funcionais

### 8.1 Compatibilidade

- Windows 10 19041+ e Windows 11, x64. Sem administrador, exceto onde o próprio Windows pede (acordar pelo
  controle, #168).
- Controles: Xbox (com e sem adaptador), DualSense e DualShock 4 por USB e Bluetooth, sem Steam Input.
- Qualquer combinação de monitores e TV que o Windows aceite, inclusive retrato e telas clonadas.

### 8.2 Segurança e privacidade

- Atualizações e o instalador do ControlFS são conferidos por SHA-256 antes de rodar.
- A API local só aceita conexões do mesmo usuário; não há porta de rede aberta pelo app.
- Segredos cifrados com DPAPI; o relatório de diagnóstico tira nome de usuário de caminhos e não leva chaves.
- O app só lê o que o Windows, a Steam e os launchers já expõem (capas que a Steam baixou, caminhos de instalação).
- Sem telemetria. Se um dia existir, desligada por padrão e documentada (DATA-001).

### 8.3 Recuperação

- A mesa é salva antes de qualquer troca; com uma tela de jogo nova, a confirmação na TV reverte sozinha se ninguém responder.
- O resultado da restauração é conferido contra o backup e refeito se precisar (#134).
- Meta (D-22): queda do app no meio da sessão é desfeita no próximo início, para tudo o que a sessão mudou.

### 8.4 Desempenho (metas iniciais, a medir no PERF-001)

| Métrica | Meta |
|---|---|
| Do gesto até a imagem na TV (sem ligar a TV) | ≤ 5 s |
| Restauração da mesa | ≤ 5 s |
| Memória com um jogo aberto, app na bandeja | ≤ 150 MB |
| CPU com um jogo aberto, app na bandeja | ≈ 0 % em média |

### 8.5 Qualidade

- CI verde (build e testes no Windows, CodeQL) em todo PR.
- Lógica nova sem Win32 / WinUI vai para arquivo testado.
- Checklist de hardware no `TESTING.md` para cada recurso de tela, áudio, TV ou controle, validado antes da estável.

---

## 9. Backlog

Legenda: **P0** = necessário para a 1.7.0; **P1** = próxima versão; **P2** = depois; **P3** = explorar.
`[x]` = na `main` (com o PR e a versão). Itens sem ID antigo receberam um agora.

**Ordem atual (29/10/2026):** Fase 1.7 → épicos CMI e API (Tako) → LAUNCH (Hydra, AnyFSE) → FUND → UX
→ resto por prioridade.

### Prioridade MoSCoW da 1.7.0

| | Itens | Por quê |
|---|---|---|
| **Must** | DESK-001 (#167), INPUT-013 (#168), LAUNCH-007 (#169), PROF-001 (#170), LAUNCH-011 (#175), REL-020 | já estão em PR com CI verde; fecham a versão |
| **Should** | INPUT-014 (ponteiro, #172), DOC-010 (este PRD), DOC-011 (#171) | pedido de usuário com resposta prometida; documentação |
| **Could** | CORE-012 (restauração durável), LAUNCH-012 (scripts de início e fim) | ajudam a recuperação; dependem da divisão do #173 |
| **Won't (agora)** | CORE-013 (taxa NTSC), tudo de P2 e P3 | precisa de hardware para validar e de PR separado |

### Fase 1.7 — Fechar a 1.7.0 (P0)

- [ ] **REL-020 — Validar no hardware** os PRs #167 a #170 pelo `TESTING.md` e corrigir o que falhar. _Estado: a branch local `local/1.7-teste` junta os quatro._
- [ ] **INPUT-013 — Ajuste do #168:** se o script de ativação não encontrar nenhum dispositivo (por exemplo, desconectado com o UAC aberto), mostrar erro em vez de "Feito", ou conferir o estado depois.
- [ ] **REL-021 — Mesclar** #167 a #170 e o #175.
- [ ] **REL-022 — Preparar a 1.7.0:** `[Unreleased]` vira `## [1.7.0]` nos dois changelogs; README, GUIDE e traduções enxutas; roadmap do README revisado.
- [ ] **REL-023 — Sincronizar o fork do winget** (`lippdev/winget-pkgs`) antes da estável; sem isso o job falha com `CreateRef`.
- [ ] **REL-024 — Publicar a 1.7.0**, só com pedido do mantenedor (D-06).

### Épico CORE — Sessão e restauração

- [x] **CORE-001 —** Escolher a tela de jogo e o que fazer com as outras (desligar, cortinas pretas, só a TV), pela posição real das telas. (#3, 1.3.0)
- [x] **CORE-002 —** Resolução, taxa, HDR, VRR e limite de FPS da sessão. (#3, 1.3.0)
- [x] **CORE-003 —** Confirmação na TV na primeira vez com uma tela de jogo nova, com reversão automática se ninguém responder. (#3, 1.3.0)
- [x] **CORE-004 —** Esperar a tela de jogo acender antes de apagar as outras. (#19, 1.5.0)
- [x] **CORE-005 —** Telas pelo CCD e áudio pelo Core Audio, sem NirSoft. (#97 a #101, #119, 1.6.0)
- [x] **CORE-006 —** Guardar a rotação (retrato) no backup. (#108, 1.6.0)
- [x] **CORE-007 —** Tela principal pela marca do próprio Windows (telas clonadas). (#110, 1.6.0)
- [x] **CORE-008 —** Falha ao trocar o áudio não desfaz a sessão. (#109, 1.6.0)
- [x] **CORE-009 —** Restaurar a mesa exata, com a TV desligada primeiro e conferência contra o backup. (#134, 1.6.0)
- [x] **CORE-010 —** Identidade estável das telas, que sobrevive à renumeração `\\.\DISPLAYn`. (1.6.0)
- [x] **CORE-011 —** Cache dos modos de cada tela (resolução e taxa) para listar sem ligar a tela. (até a 1.4.0)
- [ ] **CORE-012 (P1) —** Restauração durável (D-22): gravar em disco ícones, ponteiro e comandos de fim, e desfazer no próximo início após uma queda. Aceite: matar o processo no meio da sessão e reabrir restaura tudo.
- [ ] **CORE-013 (P2) —** Taxa exata NTSC (119,88 / 59,94 Hz), opcional, aplicada antes da confirmação. Vem do #173, em PR próprio; não tratar 59 Hz reais como 59,94.
- [ ] **CORE-014 (P2) —** Pré-checagem antes de começar: TV ligada, entrada certa, launcher instalado; aponta o problema em vez de falhar no meio.

### Épico LAUNCH — Lançadores

- [x] **LAUNCH-001 —** Steam Big Picture, com volta automática ao fechar. (#3, #20, 1.3.0 e 1.5.0)
- [x] **LAUNCH-002 —** Playnite em tela cheia, com pasta manual para a versão portátil e início lento tolerado. (#36, #83, 1.5.0 e 1.6.0)
- [x] **LAUNCH-003 —** Modo Xbox (experimental, volta manual). (1.3.0)
- [x] **LAUNCH-004 —** `stop` e Start + Select fecham o Big Picture e o Playnite antes de restaurar. (#121, 1.6.0; a primeira versão, #45, foi revertida no #125, §2.3)
- [x] **LAUNCH-005 —** Steam: fechar o Big Picture e mandar a Steam para a bandeja, só nas janelas da Steam e nunca com jogo aberto. (#121, #131, #140, 1.6.0)
- [x] **LAUNCH-006 —** Aplicativo personalizado: qualquer `.exe` com argumentos e processo a vigiar. (#161, 1.7.0-alpha.1; issue #111)
- [ ] **LAUNCH-007 (P0) —** Logos oficiais dos lançadores nos tiles da Início, lidos dos apps instalados. (#169)
- [ ] **LAUNCH-010 (P1) — Tako Launcher.** Tile na Início com logo, abrir o Tako em tela cheia na tela de jogo (`tako --console-session`), restaurar quando o Tako avisar o fim pela API (API-002), detectar a instalação. Aceite: entrar, jogar e voltar ao PC sem mouse, com o Tako. Depende de CMI-004 do Tako.
- [x] **LAUNCH-011 — JoyChromium.** Tile na Início quando instalado, abre em tela cheia (`--tv`) na tela de jogo, restaura ao fechar e, ao voltar ao PC com ele aberto, o deixa em janela (`--window`). Segue o `docs/INTEGRATIONS.md` do JoyChromium. (#175, 1.7.0)
- [ ] **LAUNCH-012 (P2) —** Scripts próprios ao entrar e ao voltar (comando, `.bat` ou PowerShell), com tempo-limite e o comando de fim gravado em disco (CORE-012). Vem do #173, em PR próprio.
- [ ] **LAUNCH-013 (P2) — Hydra Launcher.** Detectar a instalação, abrir em tela cheia na tela de jogo, restaurar ao fechar, logo oficial (D-19). _A definir: caminho, processo real e modo tela cheia._
- [ ] **LAUNCH-014 (P2) — AnyFSE.** Ferramenta que põe qualquer app como tela inicial da experiência de tela cheia do modo Xbox. Níveis: (a) detectar e conviver, sem briga com o modo Xbox do Console Mode; (b) mostrar o estado e abrir a configuração; (c) configurar o app de destino (Tako, Hydra, Playnite). Aceite mínimo: com o AnyFSE ativo, entrar e sair restaura a mesa. O Tako já tem o lado dele (PLAT-007 do Tako).
- [ ] **LAUNCH-015 (P2) —** Detectar o fim da tela cheia do modo Xbox, para a volta deixar de ser manual.
- [ ] **LAUNCH-016 (P3) — Jogos nativos, sem launcher externo.** Abrir jogos instalados direto pela interface Console. **Antes, decidir com o Tako (§3):** a biblioteca é do Tako; o caminho natural é o Console Mode abrir o jogo pelo Tako, não ter biblioteca própria.

### Épico INPUT — Controles e ponteiro

- [x] **INPUT-001 —** Navegação por controle na interface. (#3, 1.3.0)
- [x] **INPUT-002 —** Botão Home entra no modo console com o app na bandeja; Start + Select volta. (#27, #61, 1.5.0)
- [x] **INPUT-003 —** Entrar quando um controle conecta. (#38, 1.5.0)
- [x] **INPUT-004 —** Detecção tardia de controles e um controle com falha não silencia os outros. (#41, 1.5.0)
- [x] **INPUT-005 —** Diagnóstico de controles ("Testar controle") e leitura HID resiliente. (#52, 1.5.0)
- [x] **INPUT-006 —** DualSense e DualShock por HID, inclusive Bluetooth com qualquer preenchimento. (#61, #130, #132, 1.5.0 e 1.6.0)
- [x] **INPUT-007 —** Toque curto do botão Xbox: desliga o atalho da Game Bar e avisa se a Steam também usa o botão. (1.5.0)
- [x] **INPUT-008 —** Ignorar controles HID com leituras impossíveis. (#115, 1.6.0)
- [x] **INPUT-009 —** Atalhos configuráveis com setup na primeira execução (D-07). (#116, 1.6.0)
- [x] **INPUT-010 —** Layout de botões Automático, Xbox ou PlayStation, com prévia. (#131, 1.6.0)
- [x] **INPUT-011 —** Esconder o ponteiro e mouse pelo controle no menu da sessão (D-12). (#145, 1.6.1)
- [x] **INPUT-012 —** Bateria do controle no menu e na interface Console, com avisos em 15 % e 5 %. (#160, 1.7.0-alpha.1)
- [ ] **INPUT-013 (P0) —** Acordar o PC pelo controle (adaptador Xbox, Bluetooth, cabo). (#168; ajuste na Fase 1.7)
- [ ] **INPUT-014 (P1) —** Estacionar o ponteiro no canto da tela de jogo quando o launcher aparece, ligado por padrão, devolvendo a posição ao voltar (D-21). Issue #172; base no #173.
- [ ] **INPUT-015 (P3) —** Atalhos por voz e assistente (Alexa, MCP). Issue #12.

### Épico UI — Interface Console e Desktop

- [x] **UI-001 —** Interface Console em tela cheia, guiada pelo controle, com seleção de opções por lista. (#32, #42, 1.5.0)
- [x] **UI-002 —** Correções de navegação nos Ajustes da Console e anel de foco visível. (#47, #60, #73, 1.5.0)
- [x] **UI-003 —** Redesenho estilo Xbox: abas com LB/RB, fundo com capas da Steam, sons da interface. (#117, 1.6.0)
- [x] **UI-004 —** Ajustes do Desktop dentro da interface Console. (#128, 1.6.0)
- [x] **UI-005 —** Minimizar para a bandeja. (#150, 1.6.2)
- [x] **UI-006 —** Redesenho estilo console doméstico: lançadores em fileira, Sistema com categorias. (#162, 1.7.0-alpha.1)
- [x] **UI-007 —** Fonte Inter e o foco escolhe o lançador. (#164, 1.7.0-alpha.1)
- [x] **UI-008 —** Fundo Aurora, lista de ajustes dentro da tela, hover e clique suaves. (#165, 1.7.0-alpha.2)
- [ ] **UI-009 (P1) —** Primeira execução guiada: escolher a tela de jogo, testar, configurar o atalho. Hoje o setup só cobre os atalhos.
- [ ] **UI-010 (P2) —** Acessibilidade: leitor de tela nas duas interfaces, alto contraste na interface Console, tamanho do texto, redução de animações própria.
- [ ] **UI-011 (P2) —** Polimento contínuo: legibilidade em TV com sobrevarredura, escala do texto, consistência de foco e hover entre Desktop, Console e menu.

### Épico MENU — Menu da sessão

- [x] **MENU-001 —** Menu por cima do jogo (Select + Y) com volume, resolução, áudio, FPS, HDR, voltar e sair. (#46, 1.5.0)
- [x] **MENU-002 —** Funciona com controles PlayStation; layout horizontal; aviso do atalho no início da sessão. (#64, 1.5.0)
- [x] **MENU-003 —** Estilo overlay da Steam com janelas abertas (Alt + Tab interno). (#120, 1.6.0)
- [x] **MENU-004 —** Fechar janela com confirmação; navegação lateral entre os cards; ícones reais. (#127, #133, #135, 1.6.0)
- [x] **MENU-005 —** O aviso do atalho não fica preso na tela. (#114, 1.6.0)
- [x] **MENU-006 —** Tour guiado do menu. (#144, 1.6.1)
- [x] **MENU-007 —** Sai a linha provisória de gravação. (#147, 1.6.1)
- [ ] **MENU-008 (P2) —** Gravar os últimos 30 s pelo menu (NVIDIA Instant Replay / Highlights). Issue #77.
- [ ] **MENU-009 (P2) —** Linha "Perfil do jogo" ligada ao Tako quando a sessão vem dele (D-20).

### Épico FILES — ControlFS

- [x] **FILES-001 —** ControlFS fixado no menu da sessão, em tela cheia na tela de jogo. (#122, 1.6.0)
- [x] **FILES-002 —** Instalar o ControlFS pelo menu, com SHA-256 conferido, e manter o menu na frente depois. (#136, #137, 1.6.0)

### Épico FPS — Contador e limite

- [x] **FPS-001 —** Contador pelo RTSS com estilos. (#138, 1.6.0)
- [x] **FPS-002 —** Aplicar o contador salvo ao começar a sessão. (#139, 1.6.0)
- [x] **FPS-003 —** Cartão com tamanhos, posição, cor e estatísticas do PC (CPU, GPU, RAM, VRAM, temperaturas). (#146, 1.6.1)

### Épico TV — Controle da TV

- [x] **TV-001 —** Google TV / Android TV pela rede (ADB): ligar, trocar a entrada HDMI, standby ao voltar. (#126, 1.6.0; a primeira versão, #93, foi revertida no #125, §2.3)
- [ ] **TV-002 (P2) —** LG webOS. Partir do #94 (fechado, já revisado) e do código C# antigo do Tako (§3).
- [ ] **TV-003 (P2) —** Samsung Tizen, a partir do código C# antigo do Tako.
- [ ] **TV-004 (P3) —** HDMI-CEC com adaptador USB, para TVs sem rede. Issue #75; partir do #96.
- [ ] **TV-005 (P3) —** Home Assistant: ligar a TV por um script, cena ou `media_player`. Partir do #95.

### Épico DESK — Mesa

- [ ] **DESK-001 (P0) —** Devolver os ícones da área de trabalho ao lugar depois da sessão. (#167; issue #30)
- [ ] **DESK-002 (P2) —** Perfis de setup (sala, quarto, simulador) escolhidos ao entrar.
- [ ] **DESK-003 (P2) —** Várias telas de jogo (simuladores de corrida e voo). Issue #112.

### Épico PROF — Perfis por jogo

- [ ] **PROF-001 (P0) —** Perfis por jogo da Steam: resolução, HDR, FPS e áudio, salvos pelo menu e aplicados ao abrir o jogo. (#170; issue #69)
- [ ] **PROF-002 (P1) —** Regra de convivência com o Tako (D-20).

### Épico API — Automação e API local

- [x] **API-001 —** Links `consolemode://start|stop|show|menu`, `--stop` e pipe `ConsoleMode.Control` (`status`, `start`, `stop`, `show`). (#27, #51, 1.5.0; de @nextestudios)
- [ ] **API-002 (P1) — API v1 no contrato da ADR-0025** (D-17): pipe `\\.\pipe\consolemode-api-<usuário>`, mensagens JSON com prefixo de tamanho, `hello` com versão e capacidades (`tv`, `fps`, `windows`, `console-session`), métodos `tv.power`, `tv.input`, `fps.overlay.set`, `fps.limit.set`, `windows.list`, `windows.activate`, `windows.close`, `session.console.start`, `session.console.stop` e eventos `tv.state`, `session.console.changed`. Aceite: o CMI-002 e o CMI-003 do Tako funcionam contra ela. Lógica de protocolo em arquivo testado.
- [ ] **API-003 (P2) —** Controle remoto pelo celular sobre a API. Issue #72.
- [ ] **API-004 (P3) —** Widget da Xbox Game Bar para entrar e sair. Issue #11.
- [ ] **API-005 (P2) —** Integração com o WakeOn: acordar o PC já em modo console. Issue #71.

### Épico CMI — Integração com o Tako (P1, depende de API-002)

- [ ] **CMI-001 —** API v1 (é o API-002 deste PRD; o Tako referencia por este número).
- [ ] **CMI-002 —** Detectar o Tako instalado e a versão.
- [ ] **CMI-003 —** Funções do Console Mode (TV, FPS, janelas) chamáveis pelo Tako, com o resultado mostrado lá.
- [ ] **CMI-004 —** Tako como launcher do Console Mode (LAUNCH-010).

### Épico I18N — Idiomas

- [x] **I18N-001 —** Inglês, troca sem reiniciar. (#5, 1.4.0)
- [x] **I18N-002 —** Idioma do instalador respeitado na primeira execução. (#7, #35, 1.4.0 e 1.5.0)
- [x] **I18N-003 —** Espanhol e lista de idiomas. (#40, 1.5.0)
- [x] **I18N-004 —** Teste de placeholders. (#86, 1.5.0)
- [x] **I18N-005 —** Alemão (u/Skinner99), francês e russo. (#151, 1.6.2)
- [x] **I18N-006 —** Teste que reprova frase em inglês copiada. (#156, 1.7.0-alpha.1)
- [ ] **I18N-007 (P2) —** Revisão humana do francês e do russo (hoje traduzidos por máquina).

### Épico REL — Distribuição e atualização

- [x] **REL-001 —** Instalador por usuário, portátil e atualização pelo GitHub. (#3, 1.3.0)
- [x] **REL-002 —** Notas de release bilíngues e escolha de idioma no instalador. (#6, #7, #50, 1.4.0 e 1.5.0)
- [x] **REL-003 —** Download da atualização sem estourar o tempo em conexão lenta; a interface Console instala. (#53, #58, 1.5.0)
- [x] **REL-004 —** Canal de teste opcional. (#103, 1.5.1)
- [x] **REL-005 —** PR automático no winget na estável. (#106, 1.6.0)
- [x] **REL-006 —** Publicar pelo workflow `Release`, não só por tag. (#123, 1.6.0)
- [x] **REL-007 —** Destaque da atualização no idioma do app. (#158, 1.7.0-alpha.1)
- [x] **REL-008 —** Política de assinatura de código e passos preparados (SignPath). (#90)
- [ ] **REL-009 (P1) —** Assinar os executáveis (SignPath), para reduzir o SmartScreen e os falsos positivos.
- [ ] **REL-010 (P2) —** Scoop e Chocolatey.

### Épico DIAG — Diagnóstico e suporte

- [x] **DIAG-001 —** Botão de feedback com issue pré-preenchida. (#25, 1.5.0)
- [x] **DIAG-002 —** "Mais apps do desenvolvedor" (WakeOn, Next Boost). (#63, 1.5.0)
- [x] **DIAG-003 —** Diagnóstico com relatório copiável, sem dados pessoais. (#159, 1.7.0-alpha.1)
- [ ] **DIAG-004 (contínuo) —** Coletar setups que funcionam ou quebram (issue #15) e levar os casos ao Diagnóstico.

### Épico QA — Qualidade e processo

- [x] **QA-001 —** CI no Windows em todo PR; Pullfrog. (#84)
- [x] **QA-002 —** Dependabot e CodeQL. (#87)
- [x] **QA-003 —** Licença AGPL-3.0 com o texto literal. (#107)
- [x] **QA-004 —** `AGENTS.md` com as regras para agentes.
- [ ] **PERF-001 (P2) —** Medir as metas da §8.4 e registrar em `docs/`.

### Épico DOC — Documentação

- [x] **DOC-001 —** README enxuto com detalhes no GUIDE, animações de novidades e roadmap. (#55, #67, #68, #79, #105)
- [x] **DOC-002 —** Guia em português sincronizado. (#85)
- [x] **DOC-003 —** README e guia em seis idiomas e guia de tradução. (#157)
- [ ] **DOC-010 (P0) —** Este PRD. (#174)
- [ ] **DOC-011 (P1) —** Protótipo HTML da interface Console. (#171)

### Épico FUND — Apoio ao projeto (P1)

- [ ] **FUND-001 —** Link de apoio (Ko-fi ou BuyMeACoffee) em Ajustes, no README e no `FUNDING.yml` do GitHub. Sem pop-up, sem bloquear função. Seis idiomas. _A decidir: a plataforma._

### Épico PLAT — Outras plataformas (P3, D-18)

- [ ] **PLAT-001 —** Separar o núcleo (sessão, perfis, API, protocolo) numa biblioteca sem WinUI, testável.
- [ ] **PLAT-002 —** Linux (KDE Wayland, gamescope): telas pelo compositor, áudio pelo PipeWire. Issue #74.
- [ ] **PLAT-003 —** macOS. Issue #74.

### Épico BUG — Problemas conhecidos sem correção

- [ ] **BUG-001 (P1) — Menu "fica subindo".** Relato de usuário (feedback do #150): no menu, o Cima se repete sozinho. Diagnóstico de 17/10: o `ControllerInput` junta todas as fontes com OU, e um eixo HID que não é de gamepad parado em 0 (ou um relatório Sony mal lido) vira um Cima permanente com repetição. Falta o modelo do controle e a saída do "Testar controle".
- [ ] **BUG-002 (P1) — Áudio "Ao conectar" não pega a TV já ativa.** Issue #149: a troca automática só reage a saídas que ficam ativas depois do início da sessão (linha de base em `ConsoleEngine.InitializeAudioWatch`), então uma saída da TV já ativa nunca é escolhida. Correção provável: escolhê-la também quando já está ativa no início. Falta o `consolemode.log` do usuário.
- [ ] **BUG-003 (P2) —** Na aba Sessão da interface Console, os tiles de ajuste rápido cortam legendas longas ("Resolução e taxa de atualização", "VRR") com reticências em es, fr e ru (visto na 1.6.2; conferir na interface da 1.7). É limite de layout, não de tradução.
- [ ] **INPUT-016 (P3) —** Controle Switch Pro: hoje é ignorado por mandar leituras impossíveis pelo HID genérico (#115). Suporte exige ler o protocolo dele.

### Épico DATA (P3)

- [ ] **DATA-001 —** Telemetria opcional e anônima de falhas de restauração, desligada por padrão. Decisão de privacidade do mantenedor.

---

## 10. Critérios de aceite

**CA-01 — Sessão completa.** Com a TV e um monitor ligados, o usuário segura o atalho do controle com o app na
bandeja; a TV vira a tela de jogo, o monitor apaga, o áudio vai para a TV e o launcher abre. Ao fechar o launcher
(ou segurar o atalho de volta), a mesa volta igual: posições, retrato, tela principal, áudio e TV desligada.

**CA-02 — Sem imagem, sem susto.** Se uma tela de jogo nova não mostrar imagem, a confirmação reverte sozinha e o usuário
volta à mesa sem tocar em nada.

**CA-03 — Queda.** Matar o processo no meio da sessão e abrir o app de novo restaura telas e áudio (hoje) e, depois
do CORE-012, ícones, ponteiro e comandos de fim.

**CA-04 — Controle.** Com um controle Xbox ou PlayStation, o usuário entra, configura, usa o menu da sessão e volta
ao PC sem mouse e sem teclado.

**CA-05 — Tako.** Com o Tako instalado, o tile dele abre o Tako na TV; ao sair do Tako a mesa volta; o Tako mostra
as funções de TV e FPS do Console Mode quando ele está rodando.

**CA-06 — Atualização.** A versão instalada e a portátil acham a próxima versão do seu canal, conferem o SHA-256 e
atualizam mantendo configurações, telas salvas e áudio.

**CA-07 — Idiomas.** Toda tela aparece completa nos seis idiomas; os testes de localização passam.

**CA-08 — Leve.** Com um jogo aberto e o app na bandeja, ele fica dentro das metas da §8.4.

---

## 11. Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Restauração falhar e deixar a mesa trocada | Alto | Backup antes de cada troca, conferência pós-restauração (#134), CORE-012 para quedas |
| Setups de TV e monitor que ninguém testou | Alto | `TESTING.md` por recurso, Diagnóstico copiável, coleta de setups (#15) |
| Só o mantenedor testa no hardware | Médio | Checklists curtas; versões de teste opcionais para a comunidade |
| API v1 e Tako evoluírem fora de sincronia | Médio | Contrato único na ADR-0025, versão maior no `hello`, testes do protocolo nos dois lados |
| Sobreposição de perfis por jogo com o Tako | Médio | D-20 decidida antes do PROF-002 |
| Mudanças nos launchers (Steam, Playnite, Hydra, AnyFSE) | Médio | Detecção tolerante, falha de um launcher não derruba a sessão, teste a cada versão |
| Crescimento de escopo (biblioteca própria, Linux e macOS) | Médio | §3 e D-18: biblioteca é do Tako; outras plataformas só depois do núcleo separado |
| SmartScreen e antivírus afastarem usuários novos | Médio | Código próprio sem NirSoft (D-02); assinatura (REL-009) |
| PRs grandes e misturados de colaboradores | Baixo | Pedir um assunto por PR; `AGENTS.md` como guia |
| Traduções por máquina com erro | Baixo | Teste de frases copiadas; revisão humana (I18N-007) |

---

## 12. Definition of Done

- Implementado, revisado e com testes automatizados para a lógica pura.
- CI verde (build, testes e CodeQL no Windows).
- Recursos de tela, áudio, TV ou controle com checklist no `TESTING.md`, executada ou marcada como pendente no PR.
- Erros com tratamento e caminho de volta; mensagens do `AppLog` em português.
- Textos visíveis nos seis idiomas.
- Changelog (`## [Unreleased]` nos dois arquivos, com o número do PR) e documentação em par atualizados.
- Item marcado `[x]` neste PRD no mesmo PR.

---

## 13. Referências

- Tako Launcher: https://github.com/lippdev/tako-launcher ([PRD](https://github.com/lippdev/tako-launcher/blob/main/docs/PRD.md), [ADR-0025](https://github.com/lippdev/tako-launcher/blob/main/docs/adr/0025-fronteira-com-o-console-mode.md))
- AnyFSE: https://github.com/ashpynov/AnyFSE
- ControlFS: gerenciador de arquivos para controle, do mesmo time
- RivaTuner Statistics Server: https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html
- Windows App SDK / WinUI 3: https://learn.microsoft.com/windows/apps/windows-app-sdk/
- CCD (Connecting and Configuring Displays): https://learn.microsoft.com/windows-hardware/drivers/display/ccd-apis
- Core Audio: https://learn.microsoft.com/windows/win32/coreaudio/core-audio-apis-in-windows-vista
- Microsoft — Game launchers and handhelds: https://learn.microsoft.com/en-us/xbox/gdk/docs/gdk-dev/pc-dev/handheld/handheld-launchers
- Issues do roadmap: https://github.com/lippdev/consolemode/issues?q=is%3Aopen+label%3Aroadmap
