# Roteiro de testes manuais

Tudo aqui precisa de hardware ou de olho humano e **não é coberto pelos testes automáticos** (`dotnet test`). Marque `[x]` e anote o resultado. Quando algo falhar, abra uma issue com o formulário de feedback (`consolemode://` não precisa; o botão de feedback já preenche o ambiente).

Como testar em build de desenvolvimento: `dotnet build src/ConsoleMode -c Debug -r win-x64` e rode `src/ConsoleMode/bin/Debug/net8.0-windows10.0.19041.0/win-x64/ConsoleMode.exe`. Os dados ficam em `ConsoleMode_Data` ao lado do exe (`config.json`, `consolemode.log`).

## 0. Atalhos do controle configuráveis e primeira configuração

Pré-condições: config sem `shortcutsOnboardingDone` (instalação nova, ou apague a chave/`config.json`); controle Xbox e, se tiver, um PlayStation. Nenhum atalho vem ligado.

### Primeira abertura da versão

- [ ] Ao abrir o app (janela visível, interface desktop), aparece "Configure os atalhos do controle" com as três ações (Abrir o Console Mode, Menu da sessão, Voltar ao PC), todas "Não definido". Resultado: ______
- [ ] Com o **tour** de primeiro uso pendente, o tour vem primeiro e a configuração dos atalhos aparece ao terminá-lo ou pulá-lo. Resultado: ______
- [ ] Iniciado com `--tray` (janela oculta): nada aparece; ao abrir a janela pela bandeja, a configuração aparece. Resultado: ______
- [ ] "Usar os atalhos sugeridos" preenche só os vazios (Xbox, Select + Y, Select + Start). Resultado: ______
- [ ] Fechar a janela em "Concluir", com atalhos vazios, e reabrir o app: a configuração **não** volta a aparecer. Resultado: ______
- [ ] Antes de concluir, nenhum atalho dispara (segurar Home, Select + Y, Start + Select não fazem nada). Resultado: ______

### Escolher um botão

- [ ] "Definir": mostra "Segure os botões e depois solte…" e, ao segurar, os nomes dos botões aparecem ao vivo. Ao soltar, o atalho é gravado. Resultado: ______
- [ ] O botão A que clicou em "Definir" (navegação por controle) **não** é gravado como atalho e a tela não navega enquanto captura. Resultado: ______
- [ ] Um botão sozinho (A, B, X, Y, Start, Select) é recusado com a explicação; só o botão Home pode ficar sozinho. Resultado: ______
- [ ] O direcional e o analógico não entram na captura. Resultado: ______
- [ ] Repetir a mesma combinação em outra ação é recusado ("… já é o atalho de …"); uma combinação que **contém** a outra (Select + Start + Y vs Select + Start) também. Resultado: ______
- [ ] Trocar o atalho de uma ação para a combinação que ela já tinha é aceito. Resultado: ______
- [ ] "Remover" deixa a ação sem atalho: ela não dispara mais. Resultado: ______
- [ ] Controle **PlayStation**: os nomes aparecem como Create/Options/△✕○□ e o atalho gravado funciona na sessão (o app lê o controle por HID). Resultado: ______
- [ ] Enquanto captura, um atalho já existente **não** dispara (ex.: capturar o Home com o Home já definido não abre o modo console). Resultado: ______
- [ ] Os atalhos ficam salvos ao fechar e abrir o app (`homeShortcut`, `menuShortcut`, `exitShortcut` no `config.json`). Resultado: ______
- [ ] Ajustes → cards de Home, menu da sessão e voltar ao PC mostram o mesmo estado; o "Toque curto" só fica ativo com o atalho Home definido. Resultado: ______
- [ ] Interface Console → Ajustes: A numa linha de atalho liga com o padrão sugerido ou desliga; a dica "Ou segure … no controle" na sessão mostra o atalho de voltar (e some se não houver). Resultado: ______
- [ ] O toast "Menu do Console Mode" no início da sessão mostra o atalho escolhido e **não** aparece se o menu não tiver atalho. Resultado: ______
- [ ] Um `config.json` de versão anterior (com `homeButtonLaunch: true`) abre sem atalhos ligados e mostra a configuração. Resultado: ______

## 0b. Interface Console: abas, fundo e sons

Pré-condições: interface Console ativa (Ajustes → Interface → Console, ou controle conectado no modo Automático); controle Xbox e, se tiver, PlayStation.

### Abas (LB / RB)

- [ ] Três abas no topo: Início, Sessão e Sistema. A ativa fica destacada e sublinhada; as outras, mais apagadas. Resultado: ______
- [ ] **RB** vai para a próxima aba e **LB** para a anterior; nas pontas não dá a volta. Com **L1/R1** no PlayStation (aparece L1/R1 nas dicas). Resultado: ______
- [ ] PageUp/PageDown no teclado e o clique do mouse nas abas também trocam. Resultado: ______
- [ ] A página nova entra deslizando e aparecendo: RB entra pela direita e LB pela esquerda, em cerca de 0,25 s; trocar rápido várias vezes não trava nem deixa a página pela metade. Com as animações do Windows desligadas (Acessibilidade → Efeitos visuais), a troca é instantânea. Resultado: ______
- [ ] Ao trocar de aba o foco vai para o primeiro item dela (Jogar agora, a primeira tela, a primeira linha). Resultado: ______
- [ ] LB/RB não trocam a aba com um painel aberto por cima (papel da tela, seletor, teste do controle) nem durante a sessão. Resultado: ______
- [ ] **B** em Sessão ou Sistema volta para Início; **Y** vai para Sistema e volta. Resultado: ______
- [ ] **Início** mostra o banner com "Jogar agora" e, abaixo, "Setup atual" com quatro atalhos (tela de jogo, áudio, iniciador e HDR/VRR): os três primeiros abrem a escolha e o quarto leva à aba Sessão. O texto do banner cabe em até 2 linhas, também na janela pequena. Resultado: ______
- [ ] O fundo de capas cobre a janela toda, sem faixa sem gradiente no topo (sob a barra de título). Resultado: ______
- [ ] **Sessão** mostra as telas (papéis) e os ajustes rápidos; **Sistema** tem todas as linhas de ajustes (atalhos, sons, interface, fundo, atualizações). Resultado: ______
- [ ] Janela pequena (960×760): nada some; Sessão rola e Sistema também. Resultado: ______

### Navegação pelo controle (direcional e analógico)

- [ ] Em Sessão, com o foco num ajuste rápido à direita (ex.: "Limite de FPS"), **cima** sobe para uma tela (a mais próxima), não fica parado. Resultado: ______
- [ ] Em Sessão, **baixo** das telas vai para o ajuste rápido mais próximo; esquerda/direita andam só dentro da própria fileira e param nas pontas. Resultado: ______
- [ ] Ao trocar de aba com LB/RB o foco cai no primeiro item (Início: Jogar agora; Sessão: primeira tela; Sistema: primeira linha), com o anel branco visível. Resultado: ______
- [ ] Em Início, baixo de Jogar agora vai para "Setup atual"; cima volta; cima de Jogar agora vai ao botão Modo desktop do topo. Resultado: ______
- [ ] Em Sistema, cima/baixo percorrem todas as linhas, a lista rola e o anel nunca some; nenhuma tecla "não faz nada" no meio da lista. Resultado: ______
- [ ] Segurar cima/baixo repete sem pular nem travar; uma diagonal do analógico anda na vertical. Resultado: ______
- [ ] O log (`consolemode.log`) não mostra `Controles: navegação: Parâmetro incorreto`. Resultado: ______

### Blocos e foco

- [ ] Cada tela é desenhada como um monitor: verde com ▶ para "Jogar aqui", escuro com ⏻ para "Desligar", azul para "Manter ligada"; a pílula do papel e a resolução aparecem. Trocar o papel atualiza o desenho. Resultado: ______
- [ ] O bloco em foco cresce um pouco e ganha contorno branco; volta ao normal ao sair. As linhas de Ajustes não crescem. Resultado: ______

### Plano de fundo

- [ ] Padrão ("Capas da Steam"): colagem de capas dos jogos instalados sob o gradiente, o texto continua legível; sem Steam ou sem capas, só o gradiente. Resultado: ______
- [ ] Sistema → Plano de fundo alterna Capas da Steam / Só gradiente / Minha imagem; "Minha imagem" abre o seletor de arquivo e usa a imagem escolhida; cancelar sem imagem volta ao modo anterior. Resultado: ______
- [ ] A escolha fica salva ao reabrir (`consoleBackground` e `consoleBackgroundImage` no `config.json`). Resultado: ______

### Sons

- [ ] Há um som ao mover o foco, outro ao confirmar (A ou clique) e outro ao voltar (B); trocar de aba toca o de mover. Resultado: ______
- [ ] Sistema/Ajustes → "Sons da interface" desligado: silêncio total. Resultado: ______
- [ ] Segurar o direcional não acumula sons (um novo interrompe o anterior). Resultado: ______

- [ ] **Segurar o botão Xbox por 1 s** entra no modo console (mesmo caminho do atalho "1 Click"). Resultado: ______
- [ ] Ao configurar um atalho, deixe um controle segurando um botão e use outro para montar o atalho; a captura aceita o segundo sem esperar o primeiro soltar. Resultado: ______
- [ ] Com dois controles conectados, pressione partes diferentes do atalho em cada um, inclusive soltando um antes de pressionar o outro; a captura não combina os controles e só aceita a combinação feita em um único controle. Resultado: ______
- [ ] Durante a captura, desconecte o controle depois de pressionar parte da combinação e continue no segundo controle; o primeiro encerra sua captura e os botões do segundo não são acrescentados a ela. Resultado: ______
## 1. Atalho Home do controle (PR #27)

Pré-condições: atalho de "Abrir o Console Mode" definido (o padrão sugerido é o botão Xbox, controle **Xbox** (XInput)); app na bandeja (minimizado ou aberto com `--tray`); sessão inativa.

- [ ] **Segurar o atalho por 1 s** entra no modo console (mesmo caminho do atalho "1 Click"). Resultado: ______
- [ ] **Toque curto** no botão Xbox não faz nada no app (só a Game Bar abre, se o atalho dela estiver ligado). Resultado: ______
- [ ] Com a sessão **ativa** (Big Picture aberto), segurar o atalho de abrir **não** reinicia nada. Resultado: ______
- [ ] **Segurar o atalho de voltar ao PC por 1 s** durante a sessão restaura a mesa (igual a "Restaurar agora"). Resultado: ______
- [ ] Remover o atalho de abrir em Ajustes: ele para de funcionar, e o de voltar ao PC continua. Resultado: ______
- [ ] O log (`consolemode.log`) mostra `Controle: atalho Home` / `Controle: atalho Voltar ao PC` / `Controle: atalho Menu da sessão` a cada disparo. Resultado: ______
- [ ] Controle **PlayStation**: o botão PS como atalho de abrir na bandeja só funciona com a leitura HID ativa; confirmar o comportamento e que o app não registra erro. Resultado: ______

### Toque curto (desliga o atalho da Game Bar)

- [ ] Ligar "Toque curto no botão Xbox": `HKCU\Software\Microsoft\GameBar\UseNexusForGameBarEnabled` vira `0`; um toque curto entra no modo console. Resultado: ______
- [ ] Desligar o toque curto: a chave volta para `1` e a Game Bar volta a abrir no toque. Resultado: ______
- [ ] Com a **Steam aberta** e o toque curto ligado, o card mostra a dica sobre "Botão Guide foca a Steam". Com a Steam fechada, mostra a descrição normal. Resultado: ______
- [ ] Com a Steam aberta e a opção da Steam ligada: o toque curto abre a Steam em vez do app (esperado; a dica cobre isso). Resultado: ______

## 1b. Entrar ao conectar um controle (issue #29)

Pré-condições: Ajustes → "Entrar ao conectar um controle" **ligado**; app na bandeja (janela oculta); sessão inativa; passaram 15 s desde que o app abriu.

- [ ] Ligar um controle sem fio (ou conectar por USB): a sessão inicia sozinha. Resultado: ______
- [ ] Com a **janela aberta**, conectar o controle **não** inicia nada. Resultado: ______
- [ ] Conectar nos **primeiros 15 s** após abrir o app: nada acontece (controles já pareados se anunciam nesse momento). Resultado: ______
- [ ] Restaurar a mesa e, em menos de 30 s, o controle reconectar sozinho: nada acontece. Após 30 s, conectar de novo inicia. Resultado: ______
- [ ] Durante a sessão, um controle reconectando não reinicia nada. Resultado: ______
- [ ] O log mostra `Controle conectado: <nome>; auto-start sim/não` a cada conexão. Resultado: ______
- [ ] Com a opção **desligada** (padrão), nada disso acontece. Resultado: ______

## 1c. Menu da sessão (atalho sugerido: Select + Y)

### Tela cheia estilo Steam (Shift+Tab), prévia e janelas

- [ ] O menu cobre a **tela inteira** do monitor do jogo (ou o principal, na prévia), com o jogo escurecido e desfocado por trás: painel lateral à esquerda com as opções e, no meio, o bloco "Janelas". Resultado: ______
- [ ] Com o atalho do menu definido e **sem** sessão ativa, segurar o atalho abre o menu (prévia), com o selo "Prévia · fora da sessão". O link `consolemode://menu` e o item da bandeja também abrem. Resultado: ______
- [ ] Na prévia: o FPS não aparece, "Voltar ao PC" vira "Fechar menu" e só fecha o menu, e "Sair do Console Mode" fecha o app sem restaurar nada. Volume, saída de áudio, resolução e HDR valem de verdade (não há sessão para desfazê-los). Resultado: ______
- [ ] O painel lateral entra deslizando pela esquerda com as linhas em cascata, as janelas sobem e aparecem, e ao fechar (B, Esc ou o atalho) tudo some em ~0,2 s. Com as animações do Windows desligadas, abre e fecha sem movimento. Resultado: ______
- [ ] A linha/card em foco ganha contorno branco e cresce um pouco; há som ao mover, confirmar e voltar. Resultado: ______
- [ ] Direcional entre o painel lateral e o grid de janelas (esquerda/direita) e dentro de cada um (cima/baixo) nunca fica "morto"; com o seletor (resolução, áudio, FPS) aberto o foco não escapa para trás. Resultado: ______
- [ ] **Janelas**: lista as janelas abertas (ícone, programa e título) em ordem de frente para trás. A traz a escolhida para a frente (restaura se estiver minimizada) e fecha o menu; X (□ no PlayStation) ou Delete pede para a janela fechar e o card some quando ela fecha; uma que pergunta "salvar?" mantém o card. Resultado: ______
- [ ] **Falha ao ativar uma janela**: abra o menu com o Bloco de Notas aberto, feche o Bloco de Notas por outro meio depois de a lista carregar (por exemplo, `taskkill /IM notepad.exe /F`) e escolha o card obsoleto com A. O menu continua aberto, o foco permanece no card e o log mostra `Janelas: trocar ... => não conseguiu`; uma falha do Windows ao trazer uma janela válida para frente também deve manter o menu aberto. Resultado: ______
- [ ] A lista não mostra o próprio menu, a barra de tarefas, a área de trabalho nem apps UWP suspensos; sem janelas mostra "Nenhuma janela aberta". Resultado: ______
- [ ] Em sessão (Big Picture/Playnite aberto): trocar para outra janela **não** restaura a mesa, e voltar ao Big Picture pelo grid funciona; uma janela aberta na tela de jogo aparece na lista. Resultado: ______
- [ ] Monitor com escala de 150% ou 200% (a TV 4K): nada fica cortado nem minúsculo. Resultado: ______
- [ ] **Sem moldura**: nenhuma borda clara em volta da tela (antes havia um quadro de ~3 px do Windows); o fundo vai até as quatro bordas. Resultado: ______
- [ ] **Vidro condicional**: com Configurações → Personalização → Cores → "Efeitos de transparência" **ligado**, o jogo aparece desfocado e escurecido atrás; **desligado**, o painel fica escuro e sólido, com o mesmo contraste; trocar a configuração com o menu aberto atualiza na hora. Resultado: ______
- [ ] **Marcador de seleção**: um contorno branco de 2 px encostado na borda da linha ou do card (sem vão de fundo entre os dois e sem ser cortado na ponta da lista); as linhas do painel não mudam de tamanho e os cards de janela crescem ~4%. O modo de ajuste do volume mostra ◀ ▶ e o contorno acompanha. Resultado: ______
- [ ] **Controle**: esquerda, vinda do grid, volta à linha do painel de onde você saiu; cima/baixo param nas pontas do painel e do grid (não pulam para o outro lado nem para o card ao lado); o seletor (resolução, áudio, FPS) devolve o foco à linha que o abriu; ao fechar uma janela (X) o foco vai para o card seguinte, ou para "Voltar ao jogo" se não sobrar nenhum. Resultado: ______
- [ ] **Volume**: A liga o modo de ajuste, ◀/▶ mudam de 5 em 5, cima/baixo não fazem nada nele, X (□) muda o mudo, B (ou sair da linha) sai do ajuste sem fechar o menu. Resultado: ______
- [ ] **Legibilidade a 3 m**: rótulos (16 px), dicas (17 px) e valores (19 px) legíveis no sofá, a 1080p e a 4K com escala; em modo de alto contraste do Windows os textos e fundos usam as cores do sistema. Resultado: ______

Pré-condições: sessão ativa com Big Picture (ou jogo borderless) na tela; controle Xbox ou PlayStation; atalhos do menu e de voltar ao PC definidos. Nos passos abaixo, "Select + Y" e "Start + Select" são os atalhos sugeridos: use os que você escolheu.

- [ ] Segurar **Select + Y** por ~0,3 s abre o menu centralizado na tela de jogo, por cima do Big Picture, com foco em "Voltar ao jogo". Resultado: ______
- [ ] Select + Y de novo (ou B, ou Esc) fecha o menu e o jogo continua onde estava. Resultado: ______
- [ ] O aviso "Menu do Console Mode" do início da sessão some sozinho em cerca de 7 s, em várias sessões seguidas, e nunca fica preso na tela. Resultado: ______
- [ ] Cabeçalho mostra o relógio, "Jogando há X min" e o controle detectado. Resultado: ______
- [ ] **Volume:** ◀/▶ na linha muda de 5 em 5 e o Windows reflete; A alterna mudo. O valor inicial é o atual do Windows. Resultado: ______
- [ ] **Resolução:** abre a lista com a atual marcada; escolher outra aplica na TV, o Big Picture continua aberto e a sessão não se encerra sozinha. Ao restaurar no fim, a resolução original volta. Resultado: ______
- [ ] **Saída de áudio:** lista só saídas ativas; escolher troca o som na hora e o "ao conectar" não desfaz. Resultado: ______
- [ ] **Limite de FPS** (só com RTSS): trocar o limite vale no jogo; "Sem limite" desliga o limitador; ao restaurar, o RTSS volta ao que era antes da sessão. Resultado: ______
- [ ] **HDR:** alternar liga/desliga na TV; ao restaurar, o HDR volta ao estado de antes da sessão (inclusive se você desligou um que estava ligado). Resultado: ______
- [ ] "Gravar os últimos 30 s" aparece desativado com "em breve". Resultado: ______
- [ ] **Voltar ao PC** restaura e o app fica na bandeja; **Sair do Console Mode** restaura e fecha o app. Resultado: ______
- [ ] Start + Select continua restaurando direto, sem abrir o menu. Resultado: ______
- [ ] Com **PlayStation**: D-pad/✕/○ funcionam no menu (a janela toma o primeiro plano). Resultado: ______
- [ ] Jogo em **tela cheia exclusiva**: o menu não aparece por cima (limitação documentada); Select + Y não quebra nada. Resultado: ______
- [ ] `start consolemode://menu` no cmd abre o menu; fora da sessão, não faz nada. Resultado: ______

## 1d. Explorador de arquivos (ControlFS) no menu da sessão

Pré-condição: ControlFS instalado (https://github.com/nextestudios/ControlFS).

- [ ] Select + Y: no topo da lateral esquerda, fora da lista que rola, há o painel "Explorador de arquivos (ControlFS)"; o restante das opções rola por baixo dele. Resultado: ______
- [ ] Direcional para cima a partir de "Voltar ao jogo" chega nele; A abre o ControlFS em tela cheia na TV, por cima do jogo/Big Picture, e o menu fecha. Resultado: ______
- [ ] O ControlFS abre **sempre em tela cheia** na tela da sessão (a TV), cobrindo-a por inteiro, mesmo que ele estivesse em janela ou maximizado; se já estava em tela cheia, nada muda. Resultado: ______
- [ ] Se outra aplicação permanecer em primeiro plano porque o Windows negou o foco ao ControlFS, o F11 não é enviado a ela; a janela de outra aplicação não muda de estado. Resultado: ______
- [ ] Abrir de novo com o ControlFS já aberto só o traz para a frente (continua um processo só no Gerenciador de Tarefas). Resultado: ______
- [ ] O ControlFS responde ao controle logo de cara (o foco do controle fica com ele, sem o Console Mode reagir aos botões). Resultado: ______
- [ ] Com o ControlFS já aberto: A só o traz para a frente (não abre uma segunda instância). Resultado: ______
- [ ] A sessão **não** termina nem restaura a mesa enquanto o ControlFS está na frente; ao sair dele (Menu do ControlFS → Sair) volta-se ao jogo. Resultado: ______
- [ ] Select + Y com o ControlFS na frente abre o menu por cima dele (saída de emergência). Resultado: ______
- [ ] Sem o ControlFS instalado: a linha diz "Não instalado · aperte para baixar" e A abre a página de releases no navegador. Resultado: ______
- [ ] Fora da sessão (prévia do menu) o painel funciona igual. Resultado: ______

## 2. Links `consolemode://`

- [ ] `start consolemode://start` no `cmd` com o app **fechado**: abre e entra no modo console. Resultado: ______
- [ ] `consolemode://start` com o app **aberto**: entra no modo console sem abrir segunda instância (só um `ConsoleMode.exe` no Gerenciador de Tarefas). Resultado: ______
- [ ] `consolemode://stop` durante a sessão: restaura a mesa. Com sessão inativa: só mostra a janela. Resultado: ______
- [ ] `consolemode://show`: traz a janela para frente (da bandeja também). Resultado: ______
- [ ] Versão **instalada**: o instalador registra o protocolo; desinstalar remove `HKCU\Software\Classes\consolemode`. Resultado: ______
- [ ] Versão **portátil** movida de pasta: ao abrir, o registro passa a apontar para o novo caminho (log `Protocolo: consolemode:// registrado`). Resultado: ______

## 3. Interface Console

Pré-condições: Ajustes → Interface = **Automático** (padrão).

### Abertura e troca
- [ ] Com um controle **PlayStation** (ou Xbox por Bluetooth) já ligado antes de abrir o app: abre em Console mesmo assim (re-detecção em 1,5 s e 4 s). Resultado: ______
- [ ] Abrir o app em Desktop (sem controle) e então ligar um controle: aparece o chip "Controle detectado · aperte A para o modo console" no rodapé e, em modo Automático, a interface troca sozinha. Resultado: ______
- [ ] Em Desktop com o chip visível, apertar A ou Start no controle troca para Console. Com a janela em segundo plano, apertar A **não** troca. Resultado: ______
- [ ] Ao entrar na interface Console, o controle responde **imediatamente**, sem precisar trocar de tela. Resultado: ______
- [ ] Com um controle conectado, o app abre na interface Console, **maximizado**. Sem controle, abre na Desktop. Resultado: ______
- [ ] Aberto pelo botão Home (segurar) sem outro controle detectável: abre em Console. Resultado: ______
- [ ] Botão "Modo desktop" (canto superior direito) volta à Home atual, a janela volta ao tamanho anterior, e o `config.json` grava `"uiMode": "desktop"`. Resultado: ______
- [ ] Botão de controle no rodapé da Home desktop (ao lado do feedback) leva à interface Console e grava `"uiMode": "console"`. Resultado: ______
- [ ] Ajustes → Interface: as três opções funcionam e sobrevivem a fechar e abrir o app. Resultado: ______
- [ ] Trocar o idioma em Ajustes e voltar para Console: todos os textos, incluindo os cartões, trocam. Resultado: ______

### Navegação por controle (Xbox **e** PlayStation; a janela precisa estar em primeiro plano)
- [ ] **Interface Desktop:** D-pad move o foco entre tiles, o seletor de papel, os cards de Ajustes e os ComboBoxes; A abre um ComboBox, D-pad percorre, A escolhe, B fecha; Start abre e fecha Ajustes; Y vai para a interface Console; no tutorial, A avança e B pula. Resultado: ______
- [ ] Desconectar o controle no meio do uso e reconectar: a navegação volta a funcionar sem reiniciar o app (log mostra no máximo uma linha `Controles: ...`). Resultado: ______
- [ ] O primeiro toque após um clique de mouse só mostra o anel de foco; o segundo age. Resultado: ______
- [ ] Ao abrir, o foco está em **Jogar agora** (anel grosso). Resultado: ______
- [ ] D-pad e analógico esquerdo movem o foco: Jogar → cartões de ajustes → cartões de telas → "Modo desktop", e de volta. Segurar uma direção repete. Resultado: ______
- [ ] **A** num cartão de tela abre o painel "O que a tela X faz?" com foco em "Jogar aqui"; A escolhe; **B** fecha sem mudar. Resultado: ______
- [ ] **A** num cartão de ajuste rápido cicla o valor (Abrir, Áudio, Resolução, HDR, VRR, FPS) e o resumo no topo acompanha. Resultado: ______
- [ ] **Start/Options** com foco em qualquer lugar inicia a sessão; **Y/△** abre "Todos os ajustes" (interface Desktop). Resultado: ______
- [ ] As dicas do rodapé mostram A/B/☰/Y com Xbox e ✕/○/OPTIONS/△ com PlayStation. Resultado: ______
- [ ] Teclado: setas, Enter e Esc fazem o mesmo que D-pad, A e B. Resultado: ______
- [ ] Com o **Big Picture em primeiro plano** (sessão ativa), apertar A no controle **não** aciona nada na janela do app. Resultado: ______
- [ ] Voltar ao app (Alt+Tab) durante a sessão: a tela "Modo console ativo" tem foco em **Voltar ao PC**; A restaura. Resultado: ______

### Visual a 3 m da TV
- [ ] Textos e anel de foco legíveis no sofá; nenhum cartão cortado em 1080p e em 4K com escala 150 %. Resultado: ______
- [ ] Com 3+ monitores, os cartões de telas rolam horizontalmente ao mover o foco. Resultado: ______

## 3b. DualSense / HID (controle aparece mas não responde)

- [ ] Ajustes → "Testar controle" (nas duas interfaces): com o DualSense por **USB**, apertar ✕, ○, D-pad e mexer o analógico mostra os índices (Cross=1, Circle=2), o hat e os eixos na "Leitura ao vivo", e a linha "→" mostra Confirm/Back/Up… Resultado: ______
- [ ] O mesmo por **Bluetooth**. Resultado: ______
- [ ] Com o **Steam aberto** e suporte a PlayStation ligado: se a leitura fica vazia, fechar o Steam faz voltar (é o Steam Input capturando o controle). Resultado: ______
- [ ] "Copiar diagnóstico" cola dispositivos + amostra; o `consolemode.log` tem a linha `Controles: …` de abertura com o DualSense listado. Resultado: ______
- [ ] Desconectar e reconectar durante o teste: a lista atualiza e a leitura continua. Resultado: ______
- [ ] Um controle HID que manda leitura impossível (ex.: Switch Pro pelo driver genérico, com 6 ou mais botões "apertados" sozinhos) é ignorado mesmo quando também aparece em Windows.Gaming.Input: o app não navega nem inicia a sessão sozinho, e o log mostra `Controles: … ignorado: leitura inválida`. Reabrir o app tenta de novo. Resultado: ______
- [ ] Com um controle HID defeituoso e outro controle normal conectados, o defeituoso não gera ações pelo HID nem pela projeção Windows.Gaming.Input, e o controle normal continua navegando. Resultado: ______

## 3c. Fechar a Steam ao voltar ao PC

Pré-condições: lançador = Steam Big Picture; Ajustes → "Fechar a Steam ao voltar ao PC" **ligado** (padrão); nenhum jogo aberto.

- [ ] Voltar ao PC por cada caminho (botão "Voltar ao PC", menu da sessão, atalho do controle, `consolemode://stop`, tray → Restaurar): o **Big Picture fecha** e some da mesa, e a **Steam fecha sozinha** em alguns segundos (o ícone sai da bandeja, sem janela de erro). Resultado: ______
- [ ] Sair pelo próprio Big Picture (Sair → Sair do Big Picture): a mesa volta e a Steam também fecha. Resultado: ______
- [ ] A Steam **não é encerrada à força**: no Gerenciador de Tarefas ela some sem o aviso "o programa não está respondendo", e o log mostra `Steam: pedindo para fechar (sair normal)`. Resultado: ______
- [ ] Com um **jogo da Steam rodando**: a Steam fica aberta e o log mostra `Steam: mantida aberta (jogo em execução, id …)`; o Big Picture ainda é fechado. Resultado: ______
- [ ] Com o ajuste **desligado**: o Big Picture fecha, a Steam fica aberta (`Steam: mantida aberta (desligado nos ajustes)`). Resultado: ______
- [ ] Desligar o ajuste, fechar e reabrir o Console Mode e iniciar uma sessão Big Picture: a preferência continua desligada e a Steam fica aberta. Resultado: ______
- [ ] Com o Steam aberto e o estado do jogo indisponível no Registro (valor `RunningAppID` ausente ou ilegível): a Steam fica aberta e o log informa que não foi possível verificar se há jogo em execução. Resultado: ______
- [ ] Steam **já fechada** antes de voltar: nada é aberto nem pedido (`Steam: não estava aberta`); a Steam não é iniciada de novo por engano. Resultado: ______
- [ ] Lançador Playnite ou Modo Xbox: a Steam não é tocada. Resultado: ______
- [ ] Se o Big Picture demorar a fechar (travado), a restauração da mesa continua e termina normalmente (o log mostra `Big Picture ainda aberto`). Resultado: ______

## 3c. Controle da TV: Google TV / Android TV (issue #75)

Pré-condições: TV Google TV / Android TV na mesma rede do PC, com **Depuração USB** (ou "Depuração pela rede") ativada nas Opções do desenvolvedor; Ajustes → TV → *Google TV / Android TV* com o IP e a entrada HDMI do PC.

- [ ] **Testar agora** na primeira vez: a TV pede "Permitir depuração"; com "Sempre permitir" + "Permitir", a TV acorda e vai para a entrada do PC. O status mostra sucesso. Resultado: ______
- [ ] **Testar agora** de novo: não pede mais permissão; com a TV em espera, ela liga e troca a entrada. Resultado: ______
- [ ] Recusar o pedido na TV (ou esperar 60 s): o status explica que a TV não autorizou este PC. Resultado: ______
- [ ] IP errado / TV fora da rede: o status diz que a TV não respondeu, em poucos segundos. Resultado: ______
- [ ] **Jogar agora** com a TV em espera: ela liga, troca para o PC e o modo console segue normal. O log tem `TV: ligar (androidTv) ok`. Resultado: ______
- [ ] Com a TV desligada da tomada: o modo console não trava; o log tem `TV: ligar ... falhou` e o fluxo segue (a TV não aparece, o app avisa como antes). Resultado: ______
- [ ] TV que some da rede em espera + **MAC** preenchido: o log mostra `enviando Wake-on-LAN` e a TV liga (com "Ligar pela rede" ativo na TV). Resultado: ______
- [ ] **Colocar a TV em espera ao restaurar** ligado: ao sair do Big Picture, a mesa volta e depois a TV entra em espera. Desligado (padrão): a TV continua ligada. Resultado: ______
- [ ] TV que ignora a tecla HDMI: preencher **Comando da entrada** faz a troca funcionar. Resultado: ______
- [ ] Escolher "Não controlar": os campos somem e nada é enviado à TV, e **Jogar agora** não demora mais do que antes (nenhuma linha `TV:` no log). Resultado: ______
- [ ] O arquivo `adbkey.pem` na pasta de dados começa com `dpapi:` (não com `-----BEGIN`). Copiar a pasta de dados para outro usuário do Windows: o log mostra "chave ADB de outro usuário/PC; criando outra" e a TV pede permissão de novo. Resultado: ______
- [ ] TV desligada da tomada: **Jogar agora** espera no máximo ~30 s pela TV e segue (o log mostra o tempo). Resultado: ______

## 4. Regressões

- [ ] Interface Desktop: mapa de telas, `Segmented`, chips, tour de 3 passos e Ajustes continuam como antes. Resultado: ______
- [ ] O tour **não** aparece na interface Console na primeira execução. Resultado: ______
- [ ] Prompt "Está vendo esta tela?" na TV continua respondendo a A/B do controle e a Esc. Resultado: ______
- [ ] Fechar a janela durante a sessão vai para a bandeja; fora da sessão fecha o app. Menu da bandeja: Mostrar, Entrar, Restaurar, Sair. Resultado: ______
- [ ] Atualização: com uma release mais nova no GitHub, o aviso aparece nas duas interfaces (na Console, como banner no topo). Resultado: ______
- [ ] Atalho "Console Mode 1 Click" e `--tray` no início do Windows continuam funcionando. Resultado: ______

## Áudio com código próprio (issue #91)

Pré-condições: 1.6 instalada por cima da 1.5, com a configuração da 1.5 (saída de áudio escolhida).

- [ ] Ajustes → Saída de áudio lista as mesmas saídas que na 1.5, com os mesmos nomes, e as desativadas aparecem com o sufixo "[Desabilitado]". Resultado: ______
- [ ] A saída que já estava escolhida na 1.5 continua selecionada depois de atualizar (o ID salvo é o mesmo). Resultado: ______
- [ ] Entrar no modo console com uma saída fixa (ex.: HDMI da TV): o som passa para ela, inclusive em apps de chamada (papel "comunicações"). O log mostra `Áudio: saída padrão = …`. Resultado: ______
- [ ] Se a troca de saída falhar (ex.: o dispositivo some no meio), a sessão continua e o log mostra `Áudio: não foi possível trocar para …`; a mesa não é desfeita. Resultado: ______
- [ ] "A que aparecer ao conectar (TV)" com a TV começando **desligada**: quando a TV liga, o som vai para o HDMI dela. Resultado: ______
- [ ] Uma saída **desabilitada** em Configurações → Som: escolhê-la liga a saída e o som vai para ela. Resultado: ______
- [ ] Menu da sessão (Select + Y): o volume inicial é o do Windows; ◀/▶ muda de 5 em 5 e tira o mudo; A alterna o mudo. Resultado: ______
- [ ] Ao restaurar a mesa, o som volta para a saída de antes do modo console. Resultado: ______

## Telas com código próprio (issue #91)

Pré-condições: 1.6 instalada por cima da 1.5, com a configuração da 1.5 (tela de jogo e telas a esconder escolhidas). Anote antes, na 1.5, como o mapa de telas aparece, para comparar.

### Lista e identidade

- [ ] O mapa de telas mostra os mesmos monitores que na 1.5, com os mesmos nomes, resoluções e posições, inclusive a TV **desligada/desconectada**. Resultado: ______
- [ ] A tela de jogo e as telas a esconder que já estavam salvas continuam marcadas (o ID estável é o mesmo). Resultado: ______
- [ ] A lista de resoluções de cada tela continua igual. Resultado: ______
- [ ] Com telas **clonadas** (duas no mesmo ponto 0,0), só a principal do Windows aparece como principal. Resultado: ______

### Sessão com "Desconectar"

- [ ] TV começando **desligada**: "Jogar agora" liga a TV, ela vira a principal e as telas da mesa desligam. O log mostra `Telas: ativar …` e `Telas: desativar …` com `=> 0`. Resultado: ______
- [ ] Resolução/Hz salvos para a TV são aplicados. Resultado: ______
- [ ] Ao sair do Big Picture/Playnite, a mesa volta **exatamente** como estava: mesmas telas, principal, posições e resoluções. O log mostra `Telas: layout restaurado => 0`. Resultado: ______
- [ ] Monitor **girado (retrato)**: depois de restaurar, ele volta girado, na mesma posição e resolução (ex.: 1080×1920 a 90°). O `backup_monitores.cfg` guarda `DisplayOrientation=1` para ele. Resultado: ______
- [ ] Repetir a sessão 3 vezes seguidas: nenhuma tela troca de nome (\\.\DISPLAYn) nem de posição entre uma sessão e outra. Resultado: ______
- [ ] Com a TV já **ligada** antes de começar: mesmo resultado. Resultado: ______

### Outras estratégias

- [ ] "Cortinas pretas": as telas da mesa ficam pretas e voltam ao restaurar. Resultado: ______
- [ ] "Desligar por DDC/CI" (monitor com DDC/CI ligado no menu dele): as telas da mesa apagam e voltam a acender ao restaurar. Resultado: ______

### Janelas e recuperação

- [ ] Big Picture ou Playnite abrindo na tela errada é movido para a TV. Resultado: ______
- [ ] Playnite lento para abrir (troca a janela de carregamento pela principal): o app não volta ao PC sozinho e só restaura a mesa quando o Playnite fecha. Resultado: ______
- [ ] Fechar o app no meio da sessão e abrir de novo: "Restaurar setup" traz a mesa de volta a partir do backup. Resultado: ______
- [ ] Um backup da mesa feito pela 1.5 (sessão iniciada na 1.5, app atualizado antes de restaurar) é restaurado pela 1.6. Resultado: ______
