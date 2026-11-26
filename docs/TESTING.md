# Roteiro de testes manuais

Tudo aqui precisa de hardware ou de olho humano e **não é coberto pelos testes automáticos** (`dotnet test`). Marque `[x]` e anote o resultado. Quando algo falhar, abra uma issue com o formulário de feedback (`consolemode://` não precisa; o botão de feedback já preenche o ambiente).

Como testar em build de desenvolvimento: `dotnet build src/ConsoleMode -c Debug -r win-x64` e rode `src/ConsoleMode/bin/Debug/net8.0-windows10.0.19041.0/win-x64/ConsoleMode.exe`. Os dados ficam em `ConsoleMode_Data` ao lado do exe (`config.json`, `consolemode.log`).

> **Desde a 1.7.0 o app só tem a interface Console.** Itens antigos deste roteiro que falam da interface Desktop, de "Ajustes" ou do "modo PC" valem para a aba **Sistema** (ou não se aplicam mais, como o botão "Modo desktop" e a opção Interface). O roteiro da interface única e do tour está em "Interface única e tour guiado", no fim.

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

- [ ] Padrão ("Aurora"): fundo azul-escuro com brilhos suaves verde (alto, à direita), azul (embaixo, à direita) e roxo (no alto), e três linhas finas em arco à direita; o lado esquerdo fica escuro e o texto continua legível. Sem faixas nem serrilhado nos degradês, na TV e no monitor. Resultado: ______
- [ ] Um `config.json` com `consoleBackground: "auto"` (o padrão das versões anteriores) abre com a Aurora; quem quiser as capas escolhe "Capas da Steam". Resultado: ______
- [ ] "Capas da Steam": colagem de capas dos jogos instalados sob o gradiente, o texto continua legível; sem Steam ou sem capas, só o gradiente. Resultado: ______
- [ ] Sistema → Plano de fundo alterna Aurora / Capas da Steam / Só gradiente / Minha imagem; "Minha imagem" abre o seletor de arquivo e usa a imagem escolhida; cancelar sem imagem volta ao modo anterior. Resultado: ______
- [ ] Na aba Sistema o fundo (Aurora, capas ou imagem) escurece com transição, e as linhas de opções ficam legíveis por cima de qualquer capa; ao voltar para Início ou Sessão, o fundo clareia de novo. Resultado: ______
- [ ] Aba Sistema numa TV larga (1920 px ou mais) e na janela pequena: a lista de opções começa logo ao lado das categorias, nenhuma linha passa da borda direita e o valor (em verde) fica todo visível; textos longos (alemão, russo) quebram em até duas linhas. A linha "Limpar" fica alinhada com as outras. Resultado: ______
- [ ] Aba Sistema com o mouse: clicar em cada categoria (de Tela de jogo a Outros apps) mostra as opções dela, também depois de entrar nas opções de outra categoria pelo controle; o clique só escolhe a categoria, sem pular para a primeira opção. Pelo controle, A numa categoria entra nas opções e ← volta para a categoria delas. Resultado: ______
- [ ] A escolha fica salva ao reabrir (`consoleBackground` e `consoleBackgroundImage` no `config.json`). Resultado: ______

### Sons

- [ ] Há um som ao mover o foco, outro ao confirmar (A ou clique) e outro ao voltar (B). Resultado: ______
- [ ] Trocar de aba com LB / RB (ou Page Up / Page Down) toca um som próprio, diferente do de mover. Resultado: ______
- [ ] Clicar com o mouse (numa aba, num bloco, num botão, numa linha de Sistema ou no menu da sessão) não faz som nenhum; o mesmo botão apertado pelo controle faz. Resultado: ______
- [ ] Os sons novos (vidro ao mover, confirmação, "minimizar" ao voltar e o cursor do Selene na aba) saem no mesmo volume, sem estalo no começo ou no fim. Resultado: ______
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

## 1b2. Acordar o PC pelo controle (issue #155)

Pré-condições: PC com adaptador sem fio Xbox, Bluetooth ou controle no cabo; a suspensão (não hibernação) habilitada.

- [ ] Ajustes → **Acordar o PC pelo controle**: a linha de status lista o adaptador/Bluetooth/controle e diz se já pode acordar o PC. Num PC sem nenhum deles, diz que nenhum dispositivo pode e a aba Sistema do modo Console mostra "Indisponível". Resultado: ______
- [ ] **Ativar** abre o pedido de administrador (UAC); aceitando, aparece "Pronto…" e o Gerenciador de Dispositivos → (adaptador/rádio Bluetooth) → Gerenciamento de Energia mostra *Permitir que este dispositivo ative o computador* marcado. Resultado: ______
- [ ] Recusar o UAC: aviso de que nada foi alterado; nenhum dispositivo muda. Resultado: ______
- [ ] Clicar em **Ativar**, desconectar o adaptador ou o controle com o UAC aberto e depois aceitar: aparece o aviso de que o dispositivo não foi encontrado (não "Pronto…"), e o log mostra o código 2. Resultado: ______
- [ ] Suspender o PC (Iniciar → Energia → Suspender) e apertar o botão Xbox/PS: o PC acorda. Anote a conexão (adaptador Xbox, Bluetooth, cabo) e o modelo do controle. Resultado: ______
- [ ] Com **Entrar ao conectar um controle** ligado e o app na bandeja: acordar pelo controle já entra no modo console. Resultado: ______
- [ ] Teclado e mouse não aparecem na lista e não são alterados. Resultado: ______
## 1b3. Perfis por jogo (issue #69)

Pré-condições: dois jogos da Steam instalados (de preferência um que aceite outra resolução sem reiniciar); RTSS instalado para o limite de FPS; sessão ativa.

- [ ] Sem jogo rodando, o menu da sessão **não** mostra a linha "Perfil do jogo". Com um jogo aberto (pelo Big Picture), em até ~5 s a linha aparece com "<jogo>: sem perfil". Resultado: ______
- [ ] No menu, trocar resolução, HDR, FPS e saída de áudio; depois **Perfil do jogo → Salvar os ajustes atuais**: a linha passa a "<jogo>: perfil salvo" e Ajustes → **Perfis por jogo** lista o jogo com o resumo certo. Resultado: ______
- [ ] Fechar o jogo: a resolução, o HDR, o FPS e o áudio voltam ao que eram quando o jogo abriu. O log mostra `Perfil por jogo: … fechou; voltando a tela de antes`. Resultado: ______
- [ ] Abrir o mesmo jogo de novo: o perfil é aplicado sozinho (log `aplicando o perfil de …`) e desfeito ao fechar. Resultado: ______
- [ ] Abrir **outro** jogo sem perfil: nada muda. Trocar direto de um jogo com perfil para outro: o primeiro é desfeito antes. Resultado: ______
- [ ] Jogo aberto pelo **Playnite** ou pelo **app Xbox** (jogo da Steam): o perfil também vale. Resultado: ______
- [ ] **Voltar ao PC** com o jogo ainda aberto e perfil aplicado: a mesa volta exatamente como antes da sessão (resolução, HDR, FPS, áudio). Resultado: ______
- [ ] A saída de áudio do perfil desconectada (fone desligado): o resto do perfil é aplicado e o log diz que a saída não está ativa. Resultado: ______
- [ ] Apagar: pelo menu da sessão (Perfil do jogo → Apagar), em Ajustes (botão **Apagar**) e na aba Sistema → Ao entrar no modo console → Perfis por jogo. Sem perfis, a linha da aba Sistema fica desativada. Resultado: ______

## 1c. Menu da sessão (atalho sugerido: Select + Y)

### Tela cheia estilo Steam (Shift+Tab), prévia e janelas

- [ ] O menu cobre a **tela inteira** do monitor do jogo (ou o principal, na prévia), com o jogo escurecido e desfocado por trás: painel lateral à esquerda com as opções e, no meio, o bloco "Janelas". Resultado: ______
- [ ] Com o atalho do menu definido e **sem** sessão ativa, segurar o atalho abre o menu (prévia), com o selo "Prévia · fora da sessão". O link `consolemode://menu` e o item da bandeja também abrem. Resultado: ______
- [ ] Na prévia: o FPS não aparece, "Voltar ao PC" vira "Fechar menu" e só fecha o menu, e "Sair do Console Mode" fecha o app sem restaurar nada. Volume, saída de áudio, resolução e HDR valem de verdade (não há sessão para desfazê-los). Resultado: ______
- [ ] O painel lateral entra deslizando pela esquerda com as linhas em cascata, as janelas sobem e aparecem, e ao fechar (B, Esc ou o atalho) tudo some em ~0,2 s. Com as animações do Windows desligadas, abre e fecha sem movimento. Resultado: ______
- [ ] A linha/card em foco ganha contorno branco e cresce um pouco; há som ao mover, confirmar e voltar. Resultado: ______
- [ ] Direcional entre o painel lateral e o grid de janelas (esquerda/direita) e dentro de cada um (cima/baixo) nunca fica "morto"; com o seletor (resolução, áudio, FPS) aberto o foco não escapa para trás. Resultado: ______
- [ ] **Janelas**: lista as janelas abertas (ícone, programa e título) em ordem de frente para trás. A traz a escolhida para a frente (restaura se estiver minimizada) e fecha o menu; X (□ no PlayStation) ou Delete pede para a janela fechar e o card some quando ela fecha; uma que pergunta "salvar?" mantém o card. Resultado: ______
- [ ] **Confirmação ao fechar janela**: pelo X do controle, Delete e botão de fechar no cartão, aparece um modal com o título correto; Cancelar ou B mantém a janela aberta e devolve o foco ao cartão; confirmar fecha só a janela selecionada. Enquanto o modal está aberto, direcional, A, X e B não acionam opções atrás dele. Resultado: ______
- [ ] **Cartões das janelas**: todos os cartões têm o mesmo tamanho, com título curto ou de duas linhas. Cada um mostra o ícone do próprio programa, nítido e do mesmo tamanho: um programa comum (Bloco de Notas, navegador), a Steam, uma pasta do Explorador e um app da Microsoft Store (Calculadora, Configurações). Só um programa sem ícone fica com o ícone genérico no círculo. Resultado: ______
- [ ] **Visual da confirmação**: o modal é um cartão centralizado com cantos arredondados, o ícone, o programa e o título da janela escolhida, "Cancelar" (em foco ao abrir) e "Fechar janela" em vermelho, lado a lado e da mesma largura; título longo quebra em até 2 linhas sem estourar o cartão. Resultado: ______
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

### Tour guiado do menu

Pré-condição para o convite: `SessionMenuTourDone` ausente ou `false` no `config.json` da pasta de dados.

- [ ] **Convite na primeira vez**: ao abrir o menu (sessão ou prévia), depois que as linhas terminam de entrar, aparece um cartão centralizado "Primeira vez neste menu?", com "Agora não" (B) e "Fazer o tour" (A, em foco). A dica do convite mostra o botão certo (Y no Xbox, △ no PlayStation). Resultado: ______
- [ ] **Agora não** (ou B): o cartão some, o foco volta para "Voltar ao jogo" e o convite **não** aparece mais ao reabrir o menu (`SessionMenuTourDone: true` no config). Resultado: ______
- [ ] **Fechar o menu com o convite aberto** (pelo atalho): o convite aparece de novo na próxima abertura. Resultado: ______
- [ ] **Paradas**: A avança por Voltar ao jogo → Explorador de arquivos → Volume → Tela e som → FPS (só com RTSS e fora da prévia) → Janelas → Voltar ao PC (ou "Fechar menu" na prévia) → Sair do Console Mode. Cada parada escurece o resto do menu, contorna na cor de destaque só as linhas certas (resolução e áudio juntas e o HDR à parte quando as linhas de FPS estão entre eles) e mostra "n de N". Resultado: ______
- [ ] **Balão**: fica à direita do painel lateral, na altura das linhas destacadas, sem sair da tela; na parada Janelas fica sobre o painel lateral. Os textos não ficam cortados a 1080p nem a 4K com escala. Resultado: ______
- [ ] **Rolagem**: numa tela em que o painel lateral precisa rolar (1080p com escala de 125%+), as paradas Voltar ao PC e Sair do Console Mode rolam o painel até as linhas e o destaque acompanha. Resultado: ______
- [ ] **Textos das saídas**: em sessão, "Voltar ao PC" explica que restaura e o app fica na bandeja, e "Sair do Console Mode" que também fecha o app; na prévia, "Fechar menu" diz que só fecha o menu. Resultado: ______
- [ ] **Controle no tour**: A e → avançam, ← volta uma parada (nunca para o convite), B sai; nenhum botão aciona a opção destacada nem mexe no menu por trás (X não fecha janela, Y não reinicia o tour). Na última parada o botão diz "Entendi" e encerra. Resultado: ______
- [ ] **Teclado e mouse no tour**: Enter/Espaço acionam o botão em foco no balão, Tab alterna entre os dois, ←/→ trocam de parada, Esc sai; clicar numa linha destacada não faz nada. Resultado: ______
- [ ] **Ver de novo**: Y (△) ou F1 com o menu aberto inicia o tour direto, sem convite; a barra de dicas mostra "Y Tour". Com o seletor ou a confirmação de fechar janela aberta, Y não faz nada. Resultado: ______
- [ ] **Select + Y não abre o tour**: com o atalho do menu em Select + Y, segurar o atalho para **fechar** o menu não mostra o tour antes de fechar. Resultado: ______
- [ ] **Pelas Configurações**: Ajustes → "Tour do menu da sessão" → "Ver o tour" (desktop) e Sistema → "Tour do menu da sessão" (interface Console) abrem o menu em prévia já com o tour, sem convite. Resultado: ______
- [ ] Ao sair do tour, o foco volta para a linha ou o card onde estava antes. Sem animações do Windows, o tour aparece e troca de parada sem movimento. Em alto contraste, o véu e o balão usam as cores do sistema. Resultado: ______

## 1d. Explorador de arquivos (ControlFS) no menu da sessão

Pré-condição: ControlFS instalado (https://github.com/nextestudios/ControlFS), exceto nos itens de instalação pelo menu.

- [ ] Select + Y: no topo da lateral esquerda, fora da lista que rola, há o painel "Explorador de arquivos (ControlFS)"; o restante das opções rola por baixo dele. Resultado: ______
- [ ] Direcional para cima a partir de "Voltar ao jogo" chega nele; A abre o ControlFS em tela cheia na TV, por cima do jogo/Big Picture, e o menu fecha. Resultado: ______
- [ ] O ControlFS abre **sempre em tela cheia** na tela da sessão (a TV), cobrindo-a por inteiro, mesmo que ele estivesse em janela ou maximizado; se já estava em tela cheia, nada muda. Resultado: ______
- [ ] Se outra aplicação permanecer em primeiro plano porque o Windows negou o foco ao ControlFS, o F11 não é enviado a ela; a janela de outra aplicação não muda de estado. Resultado: ______
- [ ] Abrir de novo com o ControlFS já aberto só o traz para a frente (continua um processo só no Gerenciador de Tarefas). Resultado: ______
- [ ] O ControlFS responde ao controle logo de cara (o foco do controle fica com ele, sem o Console Mode reagir aos botões). Resultado: ______
- [ ] Com o ControlFS já aberto: A só o traz para a frente (não abre uma segunda instância). Resultado: ______
- [ ] A sessão **não** termina nem restaura a mesa enquanto o ControlFS está na frente; ao sair dele (Menu do ControlFS → Sair) volta-se ao jogo. Resultado: ______
- [ ] Select + Y com o ControlFS na frente abre o menu por cima dele (saída de emergência). Resultado: ______
- [ ] **Instalar pelo menu** (desinstale o ControlFS antes): a linha diz "Não instalado · aperte para baixar e instalar". A começa o download: a linha mostra "Baixando… N%" com a barra enchendo, depois "Instalando…" com a barra em movimento, sem nenhuma janela de instalador nem pedido de administrador. Ao terminar, o menu continua aberto na frente e a linha diz "Instalado · aperte para abrir"; o ControlFS **não** abre sozinho. O A seguinte abre o ControlFS em tela cheia na tela da sessão, já na frente. Resultado: ______
- [ ] Durante o download o menu continua respondendo (direcional, volume); apertar A de novo na linha não começa um segundo download. Resultado: ______
- [ ] Fechar o menu durante a instalação: ela termina em segundo plano e o ControlFS **não** abre por cima do jogo; ao reabrir o menu a linha já diz "Navegue pelos seus arquivos com o controle". Resultado: ______
- [ ] Sem internet (cabo/Wi-Fi desligado): A na linha mostra "Não deu para instalar · aperte para abrir a página de download" e o log registra `ControlFS: instalar: ...`; o A seguinte abre a página de releases no navegador. Reabrindo o menu, a linha volta a oferecer a instalação. Resultado: ______
- [ ] Depois de instalado pelo menu, o ControlFS aparece em Configurações → Aplicativos instalados e nenhum `ControlFS-Setup-x64.exe` sobra em `%TEMP%ConsoleModeUpdate`. Resultado: ______
- [ ] Fora da sessão (prévia do menu) o painel funciona igual. Resultado: ______

## 1e. Cursor do mouse: ocultar e controlar pelo controle

Pré-condição: um emulador ou jogo que deixa o cursor visível (ex.: um emulador aberto pelo Playnite).

### Ocultar o cursor
- [ ] Menu da sessão: a linha "Ocultar o cursor do mouse" começa em Desativado, com a explicação embaixo inteira (sem cortar). Na prévia (fora da sessão) a linha não aparece. Resultado: ______
- [ ] A na linha: Ativado, e o cursor some da TV, também por cima do emulador. Resultado: ______
- [ ] Com ele oculto, deixe parado e depois mexa o mouse físico: o cursor **não** volta sozinho. Resultado: ______
- [ ] A na linha de novo: Desativado, e o cursor volta com o esquema de ponteiros do Windows de sempre (tamanho e cor de Configurações → Acessibilidade → Ponteiro do mouse). Resultado: ______
- [ ] Com o cursor oculto, ligar "Controlar o mouse com o controle" desliga "Ocultar o cursor" (o cursor aparece); ligar "Ocultar o cursor" desliga o controle do mouse. Resultado: ______
- [ ] Voltar ao PC com o cursor oculto: ele reaparece normal na mesa, e na próxima sessão a linha começa em Desativado. Resultado: ______
- [ ] Ajustes (PC e aba Sistema da interface Console) não têm mais a opção de ocultar o cursor. Resultado: ______
- [ ] Encerre o Console Mode à força (Gerenciador de Tarefas) com o cursor oculto: o cursor continua invisível até abrir o Console Mode de novo, que o restaura logo na abertura (log: `Cursor: restaurado após um encerramento inesperado`). Resultado: ______

### Controlar o mouse pelo controle (Xbox **e** PlayStation)
- [ ] Menu da sessão: a linha "Controlar o mouse com o controle" mostra Desativado e a ajuda com o botão certo do controle (A no Xbox, ✕ no PlayStation). Na prévia (fora da sessão) a linha não aparece. Resultado: ______
- [ ] A na linha: passa a Ativado. Enquanto o menu está aberto, os analógicos e o A continuam navegando no menu, sem mexer no cursor. Resultado: ______
- [ ] Fechar o menu (B ou "Voltar ao jogo"): o A que fechou o menu **não** vira clique. Resultado: ______
- [ ] Analógico esquerdo move o cursor com o jogo em primeiro plano; inclinação leve = movimento fino, inclinação total atravessa a TV em ~1 s, igual em 1080p e 4K. Resultado: ______
- [ ] Analógico direito para cima/baixo rola a página (navegador, lista do emulador); para os lados rola na horizontal onde houver. Resultado: ______
- [ ] A uma vez = clique esquerdo (com um pequeno atraso, ~¼ s); A duas vezes rápido = clique direito (abre o menu de contexto); A segurado = arrastar (solte para largar). Resultado: ______
- [ ] Com o DualSense/DualShock **sem** Steam Input (lido por HID) o cursor também se move e clica. Resultado: ______
- [ ] Voltar ao PC e iniciar outra sessão: a opção começa Desativada de novo. Resultado: ______
- [ ] Limitação esperada: o jogo também recebe o A e os analógicos. Em programas abertos como administrador o cursor não mexe (o Windows bloqueia a entrada vinda de um app comum). Resultado: ______

## 1f. Bateria do controle (issue #70)

Onde aparece: no cabeçalho do menu da sessão (depois de "controle Xbox detectado") e na barra de cima da interface Console, ao lado do relógio. Atualiza a cada 30 s, ao conectar um controle e ao abrir o menu. Com vários controles, mostra o mais descarregado.

- [ ] Controle Xbox sem fio (Bluetooth ou adaptador): ícone de bateria + porcentagem. Se só o XInput responder, o número vem com "~" (5/30/65/100 %). Compare com Configurações → Bluetooth do Windows. Resultado: ______
- [ ] DualSense por USB: porcentagem (de 10 em 10, ex.: 65 %) com o ícone de carregando. Resultado: ______
- [ ] DualSense por Bluetooth: porcentagem sem o ícone de carregando; desconecte e reconecte e o valor volta em até ~30 s. Resultado: ______
- [ ] DualShock 4 por USB e por Bluetooth: igual ao DualSense (por Bluetooth, só depois que o controle passa a mandar o relatório completo; com a Steam aberta isso é imediato). Resultado: ______
- [ ] Controle com fio sem bateria (Xbox com fio, genérico): nenhum indicador, nem no menu nem na barra. Resultado: ______
- [ ] Carregando: ligue o cabo num controle sem fio; em até ~30 s o ícone muda para o de carregando. Resultado: ______
- [ ] Bateria fraca na sessão: com um controle em 15 % ou menos, aparece um aviso no canto da TV ("Bateria do controle fraca"), sem tirar o foco do jogo; não repete a cada 30 s. Aparece de novo só em 5 % ou menos, ou depois de carregar. Fora da sessão não há aviso. Log: `Controle: bateria fraca (...)`. Resultado: ______
- [ ] Leitor de tela (Narrador) no indicador: lê "Bateria do controle: 65%" (e ", carregando"). Resultado: ______

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
- [ ] Em alemão, francês e russo (textos mais longos), percorrer Home, Ajustes, menu da sessão e tutorial nas duas interfaces: nenhum botão, cartão ou linha do menu corta o texto ou sobrepõe outro elemento. Resultado: ______
- [ ] Instalador em alemão, francês e russo: o diálogo de idioma lista os três, as opções de atalho e de iniciar com o Windows aparecem sem caracteres quebrados, e o app abre no idioma escolhido. Resultado: ______

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

## 3c. Mandar a Steam para a bandeja ao voltar ao PC

Pré-condições: lançador = Steam Big Picture; Ajustes → "Mandar a Steam para a bandeja ao voltar ao PC" **ligado** (padrão); nenhum jogo aberto.

- [ ] Voltar ao PC por cada caminho (botão "Voltar ao PC", menu da sessão, atalho do controle, `consolemode://stop`, tray → Restaurar): o **Big Picture fecha**, a janela principal some e a **Steam continua em execução na bandeja**. Resultado: ______
- [ ] Sair pelo próprio Big Picture (Sair → Sair do Big Picture): a mesa volta e a Steam permanece na bandeja. Resultado: ______
- [ ] O processo da Steam continua no Gerenciador de Tarefas; clicar no ícone da bandeja reabre o cliente sem login nem reinício. Resultado: ______
- [ ] Com um **jogo da Steam rodando**: a Steam fica aberta e o log mostra `Steam: mantida aberta (jogo em execução, id …)`; o Big Picture ainda é fechado. Resultado: ______
- [ ] Com um **jogo SDL em tela cheia sem bordas** aberto (ex.: CS2, Dota 2, Half-Life: Alyx ou um indie feito com SDL) e não salvo: voltar ao PC **não fecha o jogo**; o Big Picture fecha e, se o jogo passou por Big Picture na detecção, o log mostra `Fechar Big Picture: janela em tela cheia não é da Steam, mantida aberta`. Resultado: ______
- [ ] Com o ajuste **desligado**: o Big Picture fecha, a Steam fica aberta (`Steam: mantida aberta (desligado nos ajustes)`). Resultado: ______
- [ ] Desligar o ajuste, fechar e reabrir o Console Mode e iniciar uma sessão Big Picture: a preferência continua desligada e a Steam fica aberta. Resultado: ______
- [ ] Com o Steam aberto e o estado do jogo indisponível no Registro (valor `RunningAppID` ausente ou ilegível): a Steam fica aberta e o log informa que não foi possível verificar se há jogo em execução. Resultado: ______
- [ ] Steam **já fechada** antes de voltar: nada é aberto nem pedido (`Steam: não estava aberta`); a Steam não é iniciada de novo por engano. Resultado: ______
- [ ] Lançador Playnite ou Modo Xbox: a Steam não é tocada. Resultado: ______
- [ ] Janelas de jogo, chat/popups da Steam e outros aplicativos não recebem o pedido de fechamento da janela principal. Resultado: ______
- [ ] Se o Big Picture demorar a fechar (travado), a restauração da mesa continua e termina normalmente (o log mostra `Big Picture ainda aberto`). Resultado: ______

## 3c. Controle da TV: Google TV / Android TV (issue #75)

Pré-condições: TV Google TV / Android TV na mesma rede do PC, com **Depuração USB** (ou "Depuração pela rede") ativada nas Opções do desenvolvedor; Ajustes → TV → *Google TV / Android TV* com o IP e a entrada HDMI do PC.

- [ ] **Testar agora** na primeira vez: a TV pede "Permitir depuração"; com "Sempre permitir" + "Permitir", a TV acorda e vai para a entrada do PC. O status mostra sucesso. Resultado: ______
- [ ] **Testar agora** de novo: não pede mais permissão; com a TV em espera, ela liga e troca a entrada. Resultado: ______
- [ ] Recusar o pedido na TV (ou esperar 60 s): o status explica que a TV não autorizou este PC. Resultado: ______
- [ ] IP errado / TV fora da rede: o status diz que a TV não respondeu, em poucos segundos. Resultado: ______
- [ ] TV acessível pela rede, mas sem responder ao teste ADB: o status informa o limite de 90 s e o botão **Testar agora** volta a ficar disponível. Resultado: ______
- [ ] **Jogar agora** com a TV em espera: ela liga, troca para o PC e o modo console segue normal. O log tem `TV: ligar (androidTv) ok`. Resultado: ______
- [ ] Com a TV desligada da tomada: o modo console não trava; o log tem `TV: ligar ... falhou` e o fluxo segue (a TV não aparece, o app avisa como antes). Resultado: ______
- [ ] TV que some da rede em espera + **MAC** preenchido: o log mostra `enviando Wake-on-LAN` e a TV liga (com "Ligar pela rede" ativo na TV). Resultado: ______
- [ ] **Colocar a TV em espera ao restaurar** ligado: ao sair do Big Picture, a mesa volta e depois a TV entra em espera. Desligado (padrão): a TV continua ligada. Resultado: ______
- [ ] TV que ignora a tecla HDMI: preencher **Comando da entrada** faz a troca funcionar. Resultado: ______
- [ ] Escolher "Não controlar": os campos somem e nada é enviado à TV, e **Jogar agora** não demora mais do que antes (nenhuma linha `TV:` no log). Resultado: ______
- [ ] O arquivo `adbkey.pem` na pasta de dados começa com `dpapi:` (não com `-----BEGIN`). Copiar a pasta de dados para outro usuário do Windows: o log mostra "chave ADB de outro usuário/PC; criando outra" e a TV pede permissão de novo. Resultado: ______
- [ ] TV desligada da tomada: **Jogar agora** espera no máximo ~30 s pela TV e segue (o log mostra o tempo). Resultado: ______

## 3d. Configurações no modo Console

- [ ] Em Sistema, as configurações do modo PC também estão disponíveis: caminho do Playnite, fechar a Steam, Android TV, FPS personalizado, atalhos, diagnóstico do controle, tutorial e pasta de dados. Resultado: ______
- [ ] Configurar, alterar e remover os atalhos de abrir o app, menu da sessão e voltar ao PC; uma combinação em conflito mostra o mesmo erro do modo PC. Durante a captura, os botões não navegam nem iniciam uma sessão. Resultado: ______
- [ ] Na captura, apertar e soltar B/○ sozinho cancela sem alterar o atalho; B/○ de novo fecha o editor e devolve o foco à configuração. B/○ junto com outro botão ainda pode ser salvo como combinação, mas B/○ sozinho não é um atalho válido. Teclado e mouse também conseguem abrir e fechar o editor sem acionar opções atrás dele. Resultado: ______
- [ ] Alterar uma configuração no modo Console, trocar para PC e reabrir o app: o valor permanece igual nas duas interfaces. Resultado: ______
- [ ] Selecionar e limpar o caminho do Playnite; cancelar o seletor mantém o valor anterior. Resultado: ______
- [ ] Configurar a Android TV no modo Console: endereço, MAC opcional, entrada HDMI, comando da entrada e espera ao restaurar; Testar agora mostra o resultado e libera o botão após erro ou timeout. Resultado: ______
- [ ] Selecionar FPS personalizado e digitar o limite; o modo PC mostra o mesmo valor. Sem RTSS disponível, a configuração respeita a mesma indisponibilidade das duas interfaces. Resultado: ______
- [ ] Copiar o diagnóstico do controle, abrir a pasta de dados e rever o tutorial no modo Console; ao voltar, o foco continua em uma opção visível. Resultado: ______
- [ ] Passar o mouse sobre categorias, opções, abas e cards: o realce aparece e some suavemente (sem cinza brusco), e o texto não fica apagado ao clicar; clicar afunda o item de leve. Trocar de categoria acende a barra verde e o nome com transição. Resultado: ______
- [ ] Desativar uma opção (ex.: atalho de toque curto sem atalho Home) a deixa esmaecida e sem realce. Menu da sessão: as mesmas linhas e cards continuam legíveis no tema claro. Resultado: ______

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

## Restaurar a mesa: posições das telas

Pré-condições: pelo menos 3 telas (a TV desligada na mesa normal, mais duas telas empilhadas ou lado a lado, p. ex. uma ultrawide com outra logo abaixo, alinhadas à esquerda); anote o mapa em Configurações → Sistema → Tela antes de começar.

- [ ] Estratégia "Desconectar": **Jogar agora** e depois **Voltar ao PC** (botão, menu da sessão e tray): o mapa em Configurações → Sistema → Tela volta **idêntico** ao anotado (cada tela no mesmo lugar, mesma tela principal, a TV desligada como antes). Resultado: ______
- [ ] O `consolemode.log` mostra a ordem `Telas: desativar <TV>` **antes** de `Telas: layout restaurado => 0` e não tem `posições fora do backup`. Se tiver, o texto diz quais telas e em qual tentativa a restauração acertou. Resultado: ______
- [ ] Repetir 3 vezes seguidas: o layout não deriva a cada ciclo. Resultado: ______
- [ ] TV que já estava ligada (como tela estendida) antes da sessão: depois de voltar, ela continua ligada no mesmo lugar. Resultado: ______
- [ ] Se uma das telas originais não voltar (cabo solto): o log diz que a tela de jogo ficou ligada e o PC não fica sem imagem. Resultado: ______

## Ícones da área de trabalho (issue #30)

Pré-condições: área de trabalho com **Organizar ícones automaticamente** desligado (clique direito → Exibir) e ícones espalhados em lugares fáceis de reconhecer (alguns no meio da tela, um no canto direito); de preferência a TV não sendo a tela principal na mesa normal.

- [ ] Ajustes → **Devolver os ícones da área de trabalho ao lugar** ligado (padrão): **Jogar agora** e **Voltar ao PC**: os ícones voltam exatamente onde estavam, mesmo que o Windows os tenha embaralhado durante a sessão. O log mostra `Ícones da área de trabalho: N posições salvas` e depois `… M reposicionados`. Resultado: ______
- [ ] O retorno ao PC não fica mais lento: os ícones voltam alguns segundos depois da mesa, sem travar a janela. Resultado: ______
- [ ] Um atalho criado na área de trabalho durante a sessão fica onde o Windows o pôs; um apagado durante a sessão não causa erro. Resultado: ______
- [ ] Com a opção desligada (Ajustes e aba Sistema do modo Console): o log não mostra nada sobre ícones e nada é movido. Resultado: ______
- [ ] Com **Organizar ícones automaticamente** ligado: o log mostra `organização automática ligada; posições não salvas` e o Windows arruma os ícones sozinho. Resultado: ______
- [ ] Explorer reiniciado durante a sessão (Gerenciador de Tarefas → Reiniciar): voltar ao PC não trava nem dá erro. Resultado: ______

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

### Aplicativo personalizado

- [ ] RetroArch: em Ajustes → Ao entrar no modo console, escolher "Aplicativo personalizado", Procurar… e selecionar `retroarch.exe` (argumentos vazios). Iniciar: as telas trocam como nos outros modos, o RetroArch abre na TV e na frente. O resumo e a tela da sessão mostram "RetroArch". Resultado: ______
- [ ] Argumentos: com `--fullscreen` (ou o argumento equivalente do app), o app abre em tela cheia; a pasta de trabalho é a do exe (o RetroArch acha os cores e a configuração). Resultado: ______
- [ ] Fechar o app escolhido restaura a mesa sozinho em poucos segundos (log: `Loop: processo … fechou`). Resultado: ______
- [ ] Launcher intermediário que abre outro processo e fecha (ex.: um atalho/launcher de jogo): com o nome do processo real em "Processo a acompanhar", a sessão continua depois que o launcher fecha e só restaura quando o processo real fecha. Resultado: ______
- [ ] "Processo a acompanhar" com um nome que nunca aparece: depois de ~90 s o log mostra `não apareceu`, a mesa **não** volta sozinha, e segurar Start + Select (ou o menu da bandeja) restaura. Resultado: ______
- [ ] Caminho inválido (exe apagado ou renomeado depois de escolhido) ou nenhum app escolhido: Iniciar mostra o aviso e nada muda nas telas. Resultado: ______
- [ ] Na interface Console, a linha "Aplicativo a abrir" aparece só com esse modo escolhido e abre o seletor de arquivos. Resultado: ______

### Janelas e recuperação

- [ ] Big Picture ou Playnite abrindo na tela errada é movido para a TV. Resultado: ______
- [ ] Playnite lento para abrir (troca a janela de carregamento pela principal): o app não volta ao PC sozinho e só restaura a mesa quando o Playnite fecha. Resultado: ______
- [ ] Fechar o app no meio da sessão e abrir de novo: "Restaurar setup" traz a mesa de volta a partir do backup. Resultado: ______
- [ ] Um backup da mesa feito pela 1.5 (sessão iniciada na 1.5, app atualizado antes de restaurar) é restaurado pela 1.6. Resultado: ______

## Regressão: DualShock e navegação entre painéis do menu

- [ ] DualShock 4 por USB, sem DS4Windows/Steam Input: na tela Console, D-pad e analógico movem o foco; ✕ confirma, ○ volta e L1/R1 trocam abas. Resultado: ______
- [ ] Repetir por Bluetooth e com Steam Input/DS4Windows ligado: cada toque produz uma única ação, sem movimento em repouso. Resultado: ______
- [ ] Com um Xbox também conectado, repetir a navegação com o DualShock. Resultado: ______
- [ ] Reconectar o DualShock com a tela Console aberta: a navegação volta a responder. Resultado: ______
- [ ] **DualSense (PS5) por Bluetooth**, sem Steam Input/DS4Windows e com o jogo/Big Picture em foco: Select + Y (Create + △) abre o overlay e, nele, D-pad, ✕, ○ e □ respondem. Repetir por USB. Resultado: ______
- [ ] O `consolemode.log` tem, por controle Sony, uma linha `Controle: HID DualSense: relatório 0x.. com N bytes (descritor declara M)`. Se o menu não responder, essa linha (e `relatório 0x.. não reconhecido`, se houver) diz o motivo. Resultado: ______
- [ ] Durante o jogo, Share + △ (Select + Y) abre o overlay; → em uma opção lateral leva ao primeiro cartão de janela e ✕ ativa a janela escolhida. Resultado: ______
- [ ] No primeiro cartão de uma coluna, ← retorna à opção lateral de origem; ↑/↓ e ←/→ navegam pelos demais cartões, inclusive com rolagem. Resultado: ______
- [ ] Com seletor de sessão ou confirmação de fechamento aberto, as direções ficam dentro do diálogo. Sem janelas abertas, → não perde o foco. Resultado: ______
- [ ] Com **3 ou mais janelas** abertas (uma linha de cartões): → e ← percorrem a linha cartão a cartão; → no último cartão não faz nada (não pula para outro lugar); ← no primeiro volta à opção lateral de origem. Com várias linhas: → e ← ficam na linha do cartão. Resultado: ______
- [ ] Os cartões não exibem o botão X no canto. Quadrado/Delete continuam pedindo confirmação para fechar a janela selecionada. Resultado: ______
- [ ] A logo fornecida do ControlFS aparece no painel fixo do overlay, tanto instalado quanto ausente, inclusive no aplicativo publicado. Resultado: ______
- [ ] Com overlay aberto ou outro aplicativo em primeiro plano, a tela Console ao fundo não reage ao DualShock. Resultado: ______

## Ajustes da interface Console: controle e atalhos

### Layout dos botões

- [ ] Abra **Sistema > Controle > Layout dos botões** com um controle conectado.
- [ ] Em **Automático**, confirme que os símbolos acompanham o controle Xbox ou PlayStation conectado.
- [ ] Selecione **Xbox** e confirme que a prévia e as dicas usam A, B, X, Y, LB e RB.
- [ ] Selecione **PlayStation** e confirme que a prévia e as dicas usam ✕, ○, □, △, L1 e R1.
- [ ] Feche e abra o app; confirme que a escolha manual continua salva.
- [ ] Com dois tipos de controle disponíveis, troque o controle conectado em **Automático** e confirme que os símbolos acompanham o novo controle.
- [ ] Abra o menu da sessão e confirme que as dicas também respeitam o layout escolhido.

### Atalhos

- [ ] Abra cada um dos três atalhos com A e confirme que os cartões ficam legíveis em 1920×1080 e em uma janela de 960×760.
- [ ] Navegue entre **Definir/Alterar**, **Remover** e **Voltar** usando apenas o controle; confirme que o foco visível acompanha cada ação.
- [ ] Grave uma combinação, solte os botões e confirme que o valor aparece no cartão e na linha de Ajustes.
- [ ] Durante a gravação, pressione B/○ sozinho e confirme que a captura é cancelada sem alterar o atalho.
- [ ] Tente usar uma combinação já atribuída e confirme que a mensagem de conflito aparece inteira e não corta os botões.
- [ ] Reduza a altura da janela e confirme que o editor rola até **Voltar**, sem esconder os cartões.
- [ ] Feche o editor durante uma captura e confirme que ela para; reabra-o e confirme que nenhum botão fica preso em captura.

## Contador de FPS (RTSS)

Pré-requisito: RivaTuner Statistics Server instalado, com "Show On-Screen Display" ligado; MSI Afterburner aberto com o OSD dele ligado.

- [ ] Sem o RTSS instalado, a linha "Contador de FPS" não aparece no menu da sessão e o cartão em Ajustes fica desabilitado.
- [ ] Na prévia (menu aberto fora de uma sessão), a linha não aparece.
- [ ] Config nova: o estilo é "Afterburner (não mexer)" e o OSD do Afterburner aparece normal na sessão.
- [ ] Em sessão, A na linha "Contador de FPS" abre a lista de ajustes do contador. A em "Estilo" alterna Afterburner → Desligado → Apenas FPS → Compacto → Detalhado → Personalizado → Afterburner, e o jogo muda na hora; a lista continua aberta com o foco na mesma linha.
- [ ] Na lista do contador, Tamanho, Posição, Fundo em card e Cor mudam o contador na hora; com Personalizado aparecem "Tudo em uma linha" e os itens (com ✓ nos ligados), e A liga/desliga cada um. B fecha a lista e volta para a linha do contador. Os ajustes ficam salvos e aparecem iguais em Ajustes.
- [ ] Desligado: nada na tela, nem o nosso nem o do Afterburner (no máximo um piscar rápido do Afterburner de vez em quando).
- [ ] Compacto / Detalhado / Personalizado: só o nosso aparece; o do Afterburner fica escondido.
- [ ] Afterburner: o nosso some e o do Afterburner volta em até 1 s.
- [ ] Ajustes → Contador de FPS → Personalizado: marcar/desmarcar API, FPS e tempo de quadro, "Tudo em uma linha" e a cor muda o contador no jogo na hora (com a sessão ativa).
- [ ] Personalizado sem nenhum item marcado ainda mostra o FPS.
- [ ] Com o RTSS fechado, ligar Compacto abre o RTSS e o contador aparece.
- [ ] Voltar ao PC: o nosso some e o do Afterburner volta.
- [ ] A escolha e o layout ficam salvos para a próxima sessão.
- [ ] Com Compacto (ou outro estilo nosso) já salvo em Ajustes, iniciar a sessão: o contador aparece e o do Afterburner some sem precisar mexer na linha do menu.

### Card, tamanhos e dados do PC (1.6.1)

- [ ] Apenas FPS / Compacto / Detalhado aparecem dentro de um card escuro, translúcido e de cantos arredondados, com uma folga em volta do texto; o card acompanha o tamanho do texto (não cobre a tela toda nem o OSD de outro programa).
- [ ] Posição "A do RTSS": o card fica na posição do OSD escolhida no RTSS.
- [ ] Posição em cada um dos quatro cantos: o contador vai para aquele canto da tela, com e sem o card, e não sai da tela nos cantos da direita e de baixo.
- [ ] "Fundo em card" desligado: o texto volta a ficar direto sobre o jogo, sem fundo.
- [ ] Tamanho Pequeno / Médio / Grande muda o tamanho de todo o contador na hora; no Grande o texto continua nítido e as unidades (FPS, %, GHz) ficam alinhadas na base dos números.
- [ ] Compacto: uma linha com FPS, CPU %, GPU % e RAM %; os números não fazem o texto "pular" quando mudam de 2 para 3 dígitos.
- [ ] Detalhado: FPS, tempo de quadro e API na primeira linha; CPU (uso e clock), GPU (uso, clock e °C), VRAM (usada / total) e RAM (usada / total) em linhas alinhadas.
- [ ] Os valores batem com o Gerenciador de Tarefas (CPU %, velocidade, GPU %, memória dedicada, temperatura da GPU) e atualizam a cada ~1 s.
- [ ] No primeiro segundo os valores aparecem como "--" e depois são preenchidos.
- [ ] Notebook com GPU integrada + dedicada: os dados de GPU são da placa que está rodando o jogo.
- [ ] GPU cujo driver não informa temperatura ou clock: esses itens simplesmente não aparecem (sem "0 MHz").
- [ ] Personalizado: marcar/desmarcar cada item do PC (uso/clock da CPU, RAM, VRAM, uso/clock/temperatura da GPU) muda o contador na hora; com "Tudo em uma linha" ligado, fica tudo numa linha.
- [ ] Config salva na 1.6.0 com "Compacto": depois de atualizar, aparece "Apenas FPS" e o jogo mostra só o FPS.
- [ ] Voltar ao PC com Compacto/Detalhado: o contador some e não volta sozinho segundos depois.
- [ ] Uso de CPU do Console Mode com Detalhado ligado continua baixo (Gerenciador de Tarefas, < 1%).

## Minimizar para a bandeja

- [ ] Ajustes → "Minimizar para a bandeja" vem desligado; minimizar deixa a janela na barra de tarefas, como antes.
- [ ] Ligado: minimizar (botão da barra de título, Win+Down ou clique no ícone da barra de tarefas) tira o app da barra de tarefas e do Alt+Tab; o ícone continua nos ícones ocultos.
- [ ] Clique no ícone da bandeja (ou "Mostrar janela" no menu dele) traz a janela de volta, no mesmo tamanho e posição, com foco.
- [ ] Com a janela maximizada antes de minimizar, ela volta maximizada.
- [ ] Interface Console → Sistema: a linha "Minimizar para a bandeja" alterna Ligado/Desligado e o modo PC mostra o mesmo valor; o valor fica salvo ao reabrir o app.
- [ ] Com a sessão ativa e a opção ligada, minimizar e reabrir pela bandeja continua funcionando; o atalho do controle para abrir o app também traz a janela de volta.
- [ ] Fechar no X (sem sessão ativa) continua fechando o app, com a opção ligada ou desligada.

## Interface Console no estilo console (1.7.0)

### Início
- [ ] Os iniciadores aparecem numa fileira no topo (Steam Big Picture, Playnite se instalado, modo Xbox), cada um com sua cor e ícone; o que abre hoje tem o selo ✓ verde no canto e uma barra verde embaixo, e os outros ficam mais apagados.
- [ ] Logos oficiais nos blocos: Steam Big Picture com o logo da Steam, Playnite com o do Playnite e modo Xbox com o do app Xbox, nítidos na TV 4K (sem serrilhado nem borrão). Aplicativo personalizado mostra o ícone do `.exe` escolhido e troca ao escolher outro; Playnite troca ao escolher outra pasta. Sem a Steam ou o app Xbox instalado, o bloco mostra o ícone genérico de antes, sem bloco vazio. Resultado: ______
- [ ] Com o controle, subir do Jogar agora chega na fileira; o bloco focado cresce e ganha o contorno branco, sem cortar nos vizinhos nem no topo.
- [ ] Andar pela fileira já troca o iniciador, sem apertar A: o selo, a barra e o título grande embaixo seguem o foco, e a escolha aparece igual em Sistema → Ao entrar no modo console → Abrir e no modo PC.
- [ ] A num iniciador (ou um clique) abre direto aquele iniciador, como o Jogar agora. Sem tela de jogo escolhida, A só escolhe e não abre nada.
- [ ] Fonte Inter: os textos da interface Console e do menu da sessão usam a Inter (o "a" e o "g" de dois andares mudam de cara em relação à Segoe), em negrito de verdade no título e no Jogar agora, sem aparência borrada de negrito sintético. Conferir também no **portátil**: se a fonte não carregar, o app cai na Segoe UI sem quebrar. O modo PC continua com a Segoe UI.
- [ ] Russo: o cirílico aparece na Inter, sem misturar fontes no meio da palavra.
- [ ] A linha embaixo do título mostra a resolução da tela de jogo e depois áudio, FPS, HDR e VRR quando ligados, separados por "·", e atualiza ao mudar cada um.
- [ ] Sem tela de jogo escolhida, o texto pede para escolher a tela e Jogar agora fica desativado.
- [ ] O botão redondo ao lado de Jogar agora abre a aba Sessão.
- [ ] Com capas da Steam como fundo, as capas aparecem mais à direita e o lado esquerdo e a parte de baixo ficam escuros; o texto continua legível. Com imagem própria e sem fundo também.
- [ ] Nomes longos (alemão, russo) não estouram: o título corta com reticências numa linha.

### Barra do topo e dicas
- [ ] As abas ficam à esquerda, ao lado do ícone, com LB/RB (L1/R1) em volta; o relógio continua à direita.
- [ ] A barra de dicas embaixo não mostra mais LB/RB "Abas"; as outras dicas continuam.

### Sistema
- [ ] Ao abrir a aba, o foco fica na categoria em uso, com a barrinha verde ao lado e o nome aceso; as outras categorias ficam apagadas.
- [ ] Descer e subir pelas categorias troca as opções da direita na hora, e a lista volta para o topo.
- [ ] A ou direita entra na primeira opção; esquerda ou B volta para a categoria certa (a da barrinha, não a que estiver na mesma altura).
- [ ] Abrir uma escolha (por exemplo Idioma, em App) e voltar com B ou escolhendo: o foco volta para a mesma linha.
- [ ] Atalhos (Controle): abrir o editor e voltar devolve o foco para a linha do atalho.
- [ ] Teste do controle (Atalho, ajuda e dados): ao fechar, o foco volta para a linha.
- [ ] Fundo, imagem do fundo e sons da interface ficam em App; Fechar a Steam ao voltar ao PC fica em Ao entrar no modo console.
- [ ] Campos de texto da TV e do FPS personalizado continuam aceitando digitação e as setas movem o cursor neles.

### Visual geral
- [ ] Blocos e linhas sem borda fina, cantos mais arredondados, Jogar agora em formato de pílula; o contorno branco do foco aparece em tudo, inclusive no menu da sessão.
- [ ] Ajustes rápidos da Sessão aparecem como quadrados do mesmo tamanho, com o texto do valor inteiro (ou em duas linhas).
- [ ] Alto contraste do Windows: textos e foco continuam visíveis.
## Diagnóstico (Ajustes → Atalho, ajuda e dados)

- [ ] Abrir o card "Diagnóstico": o anel de carregamento aparece e, sem travar a janela, o relatório mostra versão e canal (instalado/portátil, estável/teste), Windows com build, e a versão do .NET.
- [ ] Telas: aparecem todas as que o app vê, com resolução @ Hz, HDR ligado/desligado/sem suporte; a tela de jogo vem marcada "game screen" e as que serão ocultadas, "hidden in console mode". Trocar a tela de jogo ou as ocultas e clicar em **Atualizar** muda as marcas.
- [ ] Ligar o HDR no Windows e clicar em **Atualizar**: a tela passa a "HDR on".
- [ ] Áudio: lista as saídas, com a padrão do Windows e a escolhida no app marcadas; com "Ao conectar", nenhuma vem como escolhida.
- [ ] Controles: com um Xbox e um DualSense ligados, os dois aparecem (Xbox / PlayStation); sem nenhum, "(none)".
- [ ] Programas: Steam, Playnite, RTSS e ControlFS com caminho e versão quando instalados, e "not found" quando não; "Launches" mostra o iniciador escolhido.
- [ ] Log: as últimas ~50 linhas do `consolemode.log` aparecem no fim.
- [ ] Nenhum caminho mostra o nome do usuário do Windows (`C:\Users\…\`), nem com Playnite instalado em `%LOCALAPPDATA%`.
- [ ] **Copiar relatório** mostra o aviso de copiado e o texto colado no Bloco de Notas é igual ao da tela.
- [ ] Com uma seção falhando (ex.: sem permissão de leitura do log), ela mostra "unavailable" e o resto do relatório continua aparecendo.

## JoyChromium no menu da sessão (1.7.0, #175, #181)

Pré-condições: um controle conectado; uma sessão em andamento (o menu também abre fora de sessão, como prévia).

- [ ] A Início e Ajustes → Ao entrar no modo console **não** mostram o JoyChromium como iniciador. Resultado: ______
- [ ] Menu da sessão: a linha **Navegador (JoyChromium)** fica fixa logo abaixo do ControlFS, com o ícone do JoyChromium (ou um globo, se ele não estiver instalado). O tour do menu destaca as duas linhas juntas. Resultado: ______
- [ ] Com o JoyChromium instalado, A na linha: o menu fecha e o JoyChromium abre em tela cheia **na tela de jogo** (não no monitor da mesa), e o controle navega nele. Resultado: ______
- [ ] Com o JoyChromium já aberto: A na linha traz o mesmo para a frente em tela cheia, sem abrir outro. Resultado: ______
- [ ] Com o JoyChromium aberto pelo menu, voltar ao PC: a mesa volta e o JoyChromium continua aberto **em janela**, com as mesmas abas. Um JoyChromium que você abriu sozinho na mesa, fora do menu, não é mexido. Resultado: ______
- [ ] Sem o JoyChromium instalado (desinstale antes): A na linha mostra "Baixando… N%", depois "Instalando…" e "Instalado · aperte para abrir"; o próximo A o abre. Sem internet: a linha diz que não deu para instalar e o próximo A abre a página de releases. Resultado: ______
- [ ] Menu da sessão (Select + Y) por cima do JoyChromium: abre, e **Voltar ao jogo** devolve o foco ao navegador. Resultado: ______

## Tirar o ponteiro do mouse do caminho (issue #172, #177)

Pré-condições: a opção **Tirar o ponteiro do mouse do caminho** ligada (é o padrão); mouse conectado.

- [ ] Deixar o ponteiro no meio do monitor da mesa e **Jogar agora** com o Steam Big Picture: quando o Big Picture aparece na TV, o ponteiro vai para o canto inferior direito da TV e continua lá depois que o Big Picture termina de abrir (o log mostra `Ponteiro: estacionado` duas vezes). Resultado: ______
- [ ] O mesmo com Playnite e Aplicativo personalizado. Com o modo Xbox, o ponteiro vai para o canto uns 4 s depois de abrir. Resultado: ______
- [ ] Voltar ao PC: o ponteiro volta exatamente para onde estava no monitor da mesa (log: `Ponteiro: de volta a`). Resultado: ______
- [ ] Desligar a opção (Ajustes ou Sistema → Ao entrar no modo console): o ponteiro não é movido nem ao começar nem ao voltar. Resultado: ______
- [ ] Com **Esconder o ponteiro do mouse** ligado no menu da sessão, nada muda: o ponteiro some e reaparece no canto quando a opção é desligada. Resultado: ______

## Restauração depois de uma queda (CORE-012, #178)

- [ ] Com a TV como tela de jogo e os monitores da mesa desligados pela sessão, fechar o Console Mode à força no Gerenciador de Tarefas (Finalizar tarefa) no meio da sessão. Abrir o Console Mode de novo: as telas da mesa voltam como estavam, o áudio volta, o ponteiro volta para onde estava e aparece o aviso de que a última sessão não terminou direito. Resultado: ______
- [ ] O mesmo, desligando o PC no botão (ou tirando da tomada) no meio da sessão e ligando de novo com o Console Mode iniciando com o Windows: a mesa volta ao entrar no Windows. Resultado: ______
- [ ] Uma sessão normal (voltar ao PC pelo atalho ou pelo menu) não deixa o arquivo `session.json` na pasta de dados, e abrir o app depois não mostra o aviso. Resultado: ______

## Correções da issue #149 (#180)

- [ ] Saída de áudio **A que aparecer ao conectar**, com a TV **já ligada** e o áudio HDMI dela já aparecendo no Windows antes de começar: uns 10 s depois de entrar no modo console, o áudio vai para a TV (log: `Áudio: nenhuma saída nova; usando a saída já ativa da tela de jogo`). Resultado: ______
- [ ] O mesmo com a TV **desligada** antes de começar: o áudio vai para a TV assim que ela liga, como antes. Resultado: ______
- [ ] Com uma soundbar ou receiver que liga junto com a TV: o áudio vai para a soundbar, não para a TV. Resultado: ______
- [ ] Controle que fazia o menu "subir sozinho": o menu fica parado; se o log mostrar `analógico fora do centro desde o início`, o direcional e os botões continuam navegando. Anote o modelo do controle. Resultado: ______
- [ ] Controle normal: o analógico navega como antes (na primeira leitura ele está no centro). Resultado: ______
- [ ] Aba Sessão em espanhol, francês e russo: as legendas dos ajustes rápidos aparecem inteiras, em até duas linhas. Resultado: ______

## Barra de título (#182)

- [ ] Interface Console: nenhuma faixa cinza no topo; o fundo (Aurora, capas ou imagem) vai até a borda da janela, e minimizar, maximizar e fechar aparecem brancos, com realce suave ao passar o mouse e mais forte ao clicar. Resultado: ______
- [ ] Interface Console: arrastar a janela pela área de cima (fora das abas e dos botões) funciona; as abas, o relógio e os botões redondos continuam clicáveis. Resultado: ______

## Playnite com jogo aberto e nomes das telas (#191)

- [ ] Playnite em tela cheia com "Minimizar após iniciar o jogo" ligado (Selene): abrir um jogo pela sessão e jogar mais de 1 minuto. A sessão continua (o log diz "Playnite escondeu a janela (jogo aberto); a sessão continua"); ao fechar o jogo o Playnite volta na TV ("voltou a mostrar a janela") e a sessão segue. Resultado: ______
- [ ] Fechar o Playnite depois disso encerra a sessão e a mesa volta como estava ("Restore: concluído com sucesso", sem "Não foi possível desconectar"). Resultado: ______
- [ ] Com a TV como tela de jogo e os dois monitores da mesa desligados pela sessão, abrir e encerrar várias sessões seguidas. Se o log mostrar "Telas: a tela de jogo passou de … para …", a resolução da TV é aplicada, o jogo abre na TV e a volta religa os monitores da mesa nas posições certas (o log da volta mostra "telas com outro nome desde o backup"). Resultado: ______

## Foco volta para a linha depois de uma lista (#196)

- [ ] Sistema, com o controle: descer até uma opção que não seja a primeira (ex.: Como desligar as outras telas), abrir com A, escolher outro valor com A. O foco fica nessa mesma opção. Fechar outra lista com B também volta para a linha dela. Resultado: ______
- [ ] Sessão: Saída de áudio e Resolução, escolhendo um valor ou fechando com B: o foco volta para o ajuste, não para a primeira tela. Resultado: ______
- [ ] Com o mouse: clicar numa opção, escolher um valor; a lista fecha e a página não pula. Resultado: ______

## Tour que avança sozinho (#194)

- [ ] Sem tocar em nada, o tour passa por todas as paradas sozinho: cada uma fica o tempo de ler (de 7 a 16 s), a barra embaixo do cartão enche nesse tempo, e a última fecha o tour e volta para o Início (reabrir o app não mostra o tour de novo). Resultado: ______
- [ ] A (ou ✕), Voltar e um clique em Próximo agem na hora, e a parada nova começa a barra do zero. B (ou ○) e Pular tutorial fecham o tour e a barra para. Resultado: ______
- [ ] Com o app em segundo plano (Alt + Tab para outra janela), a parada não avança; ao voltar, ela conta o tempo de novo. Resultado: ______
- [ ] Com controle de PS5: o tour avança sozinho mesmo que os botões não respondam nele. Anotar se ✕ e ○ funcionam no tour. Resultado: ______

## Controle da TV fora da 1.7 (#195)

- [ ] Sistema não mostra mais a categoria **TV**; o direcional e o mouse andam pelas outras categorias sem buracos. Resultado: ______
- [ ] Com uma Android TV que estava configurada na 1.6: uma sessão não liga a TV nem troca a entrada, e a volta não a coloca em espera (o log não tem linhas "TV: ligar" / "TV: desligar"). O `config.json` continua com o bloco `tv` como estava. Resultado: ______

## Remover um app pelo bloco da Início (#198)

- [ ] Com dois apps adicionados pelo **+**: foco num deles, **☰** (ou OPTIONS) abre o menu com o nome do app, **Remover da Início** e **Cancelar**. **Remover da Início** tira o bloco (e do `config.json`); **Cancelar** ou B fecham sem mudar nada. Resultado: ______
- [ ] O botão direito do mouse no bloco abre o mesmo menu. Resultado: ______
- [ ] Removendo o app que estava escolhido para abrir, o Steam Big Picture passa a ser o escolhido; removendo outro, o escolhido não muda. Resultado: ______
- [ ] **☰** num bloco da Steam, do Playnite ou do modo Xbox, ou no botão **Jogar agora**, continua começando a jogar. Com uma sessão ativa, o menu não abre. Resultado: ______
- [ ] Tour, parada dos iniciadores: o texto mostra o botão certo (☰ no Xbox, OPTIONS no PlayStation). Resultado: ______

## Legacy e rascunho da 1.7.0 (#200, #201, #202)

Com o rascunho da release aberto no GitHub (só o dono vê), baixar os quatro arquivos.

- [ ] Com a beta instalada e aberta: o app **não** oferece a 1.7.0 enquanto ela é rascunho (Sistema → App → Verificar agora diz que está na versão mais recente). Resultado: ______
- [ ] `ConsoleMode-Setup-x64.exe` por cima da beta: instala, abre como 1.7.0 e mantém todos os ajustes. Resultado: ______
- [ ] `ConsoleModeLegacy-Setup-x64.exe` por cima da 1.7.0: o instalador e a entrada em "Aplicativos instalados" dizem **Console Mode Legacy**; o app abre com o título "Console Mode Legacy", mostra o tutorial antigo uma vez, e telas, áudio, atalhos do controle e idioma estão como na 1.7.0. Ajustes mostra "1.7.0 Legacy" e não tem as linhas de atualização. Resultado: ______
- [ ] Na Legacy, com um aplicativo adicionado pelo **+** escolhido na 1.7.0: o iniciador mostrado é o Steam Big Picture. Uma sessão pela Legacy (TV, voltar ao PC) funciona como na 1.6.2. Resultado: ______
- [ ] Mudar um ajuste na Legacy (HDR, por exemplo), fechar, e instalar a 1.7.0 por cima: os aplicativos do **+**, os perfis por jogo, o estilo da abertura e o atalho da visão de tarefas estão como antes, o aplicativo escolhido continua escolhido e o HDR ficou como mudado na Legacy. Resultado: ______
- [ ] De volta à Legacy mais uma vez: o tutorial antigo não aparece de novo e a interface escolhida nela (Desktop ou Console) foi lembrada. Resultado: ______
- [ ] A Legacy portátil numa pasta própria abre com os ajustes padrão (dados na `ConsoleMode_Data` ao lado) e convive com a 1.7.0 instalada, uma aberta por vez. Resultado: ______
- [ ] Depois de publicar o rascunho: a tag `v1.7.0` aparece, a beta instalada oferece a 1.7.0 (nunca a Legacy) e o workflow **Winget** roda sozinho. Resultado: ______

## Interface única e tour guiado (1.7.0)

Pré-condições: um `config.json` sem `consoleTourDone` (vale para quem atualiza de uma versão anterior, mesmo tendo visto o tutorial antigo). Teste com controle, só com mouse e só com teclado.

### Uma interface só

- [ ] O app abre maximizado, direto na interface Console, com e sem controle conectado; não há mais o botão "Modo desktop" no topo nem a opção Interface em Sistema → App. Resultado: ______
- [ ] Restaurar a janela (botão do meio) deixa ela no tamanho antigo e tudo continua utilizável; maximizar volta. Resultado: ______
- [ ] Iniciado escondido (Iniciar com o Windows, `--start`, um controle conectando com o app na bandeja): a janela não aparece sozinha; ao abri-la pela bandeja, ela maximiza. Resultado: ______
- [ ] Só com mouse e teclado: clicar nas abas, Page Up / Page Down, setas, Enter e Esc navegam em tudo (Início, Sessão, Sistema, listas de opções, editor de atalhos). Resultado: ______
- [ ] Um `config.json` antigo com `"uiMode": "desktop"` abre normalmente na interface Console; a chave some na próxima gravação. Resultado: ______
- [ ] Sistema → Extras opcionais, contador de FPS: estilo, tamanho, posição, cor, cartão, itens do jogo e do PC e "uma linha" mudam o contador na sessão; sem RTSS a linha do estilo fica desabilitada, e tamanho/posição/cor só valem com um estilo que desenha, os itens só no estilo personalizado. Resultado: ______
- [ ] Sistema → Ao entrar no modo console, com Aplicativo personalizado: "Argumentos" e "Processo a acompanhar" aceitam texto (o teclado na tela não é necessário para o resto) e ficam salvos ao reabrir. Resultado: ______
- [ ] Sistema → Atalho, ajuda e dados: "Diagnóstico → Copiar relatório" põe o relatório na área de transferência e mostra o aviso; a versão do app aparece no fim da lista. Resultado: ______
- [ ] Sistema: com o foco numa linha, a descrição dela aparece embaixo da lista (até 3 linhas); no trilho das categorias, some. Em alemão, francês e russo o texto não estoura. Resultado: ______

- [ ] Aba Sessão: as telas aparecem como em Configurações → Sistema → Tela do Windows: na mesma posição relativa, na proporção real (um monitor girado aparece em pé) e com um espaço entre elas; uma tela desligada no Windows aparece tracejada à direita. Os textos encolhem para caber nas telas pequenas. Resultado: ______
- [ ] O direcional anda entre as telas pela posição delas no mapa (direita vai para a tela à direita etc.) e A abre o painel de papel da tela. Resultado: ______

### Abertura

- [ ] Ao abrir o app ("Liga o PC"): no escuro, a luz de energia da base acende e pulsa duas vezes, a base aparece, a tela se desenha e dá um clarão verde, o > e o _ são digitados (sem pontinhos antes), o cursor pisca três vezes e termina aceso, o nome aparece e toca o som de entrada; some com um fade em uns 3 s (ou quando o app termina de carregar, se demorar mais) e o Início aparece pronto (o tour, na primeira vez). Resultado: ______
- [ ] Um botão do controle, um clique ou uma tecla durante a abertura pula para a interface e não faz mais nada (não começa sessão, não troca de aba). Resultado: ______
- [ ] Sistema → App → "Estilo da abertura" → "Nenhuma (abre direto)": reabrir o app vai direto para a interface, sem som; "Som da abertura" e "Ver a abertura" ficam desabilitados. Escolher um estilo de novo religa a abertura. Com o som desligado: abertura muda; ligar o som toca o som. Uma config antiga com a abertura desligada aparece como "Nenhuma". Resultado: ______
- [ ] "Sons da interface" desligado não silencia a abertura (ela tem o próprio ajuste). Resultado: ______
- [ ] Sistema → App → "Estilo da abertura": Console Mode, "Inspirada no Xbox 360" (fundo claro, faixas verdes e prateadas girando em volta de um orbe que cresce num clarão, logo com o nome escuro) e "Inspirada no PS4" (azul, ondas de luz passando, partículas subindo, logo na luz), cada um com o seu som; a escolha fica salva e vale na próxima abertura do app. Resultado: ______
- [ ] Sem estilo escolhido (config sem `introStyle`): abrindo o app várias vezes, quase sempre vem a do Console Mode e de vez em quando (cerca de 1 em 8) a do Xbox 360 ou a do PS4, cada uma com o seu som. Escolher qualquer estilo em Sistema → App (até "Console Mode", já marcado) grava `introStyle` e, daí em diante, só vem aquele. Resultado: ______
- [ ] "Ver a abertura" toca o estilo escolhido na hora, por cima da interface, e some sozinha (ou com qualquer botão). Resultado: ______
- [ ] Iniciar com o Windows / `--start` / bandeja: nenhuma abertura nem som com a janela escondida. Com as animações do Windows desligadas, o logo aparece parado e some sem fade. Resultado: ______

### Atalho da visão de tarefas

- [ ] Numa instalação nova e numa que já existia, Sistema → Controle → **Atalho do controle para a visão de tarefas** já mostra Select + X. Noutra rodada, trocar para só o botão Xbox (com o atalho de abrir o Console Mode desligado). Fora da sessão e numa sessão, segurar o atalho abre a visão de tarefas do Windows. Resultado: ______
- [ ] Controle **Xbox**, com ela aberta: o próprio Windows move a seleção, cada toque anda **uma** casa só (sem repetir), **A** abre, **B** fecha; o atalho de novo também fecha. Com o controle como mouse ligado, o ponteiro não se mexe enquanto a visão está aberta. Resultado: ______
- [ ] Controle **PlayStation** (sem DS4Windows/Steam Input transformando em Xbox): direcional e analógico esquerdo movem a seleção (segurar repete), ✕ abre, ○ fecha. Fechando com o mouse, o controle volta ao jogo (✕ não vira Enter). Com **Navegar na visão de tarefas com controle PlayStation** desligado, o controle PlayStation não mexe na visão. Resultado: ______
- [ ] Com o menu da sessão aberto, o atalho fecha o menu e abre a visão de tarefas. Resultado: ______
- [ ] Nas três primeiras vezes, o aviso "Visão de tarefas" desliza no canto com os botões do controle em uso (A/B ou ✕/○) e a visão abre ~1,6 s depois; da quarta em diante abre na hora, sem aviso. O aviso não aparece como janela dentro da visão de tarefas. Resultado: ______

### Lista de Sistema e outros apps

- [ ] Aviso de início de sessão (com o atalho do menu definido) e de bateria fraca: entra deslizando da borda direita da TV, sem aparecer em outra tela ao lado; o fundo em volta do card é transparente (sem retângulo preto); a barrinha verde esvazia e o aviso sai deslizando para a direita. O jogo não perde o foco em nenhum momento. Resultado: ______

- [ ] Menu fora do modo console: com o atalho do menu definido e o Console Mode só na bandeja, segurar o atalho abre o Menu da sessão na tela de jogo (ou na principal, com a TV desligada), com o selo "Fora da sessão" e sem limite/contador de FPS. Resultado: ______
- [ ] Fora da sessão, **Controlar o mouse com o controle** move o ponteiro e clica; com a janela do Console Mode em primeiro plano o controle volta a navegar a interface (o mouse espera); com o menu aberto, também. **Ocultar o cursor** some com a seta. Começar uma sessão desliga os dois; fechar o Console Mode traz o cursor de volta. Resultado: ______
- [ ] Sistema → Controle → **Menu da sessão fora do modo console** desligado: o atalho não abre o menu fora da sessão (abre numa sessão), o ícone da bandeja continua abrindo; desligar com o mouse do controle ligado o desliga. Resultado: ______

- [ ] Escala de console: com o Windows em 100 %, 125 %, 150 % e 200 % (Configurações → Sistema → Tela → Escala), a Início, a Sessão, o Sistema e o Menu da sessão ocupam a mesma proporção da tela, sem nada cortado ou gigante. Na TV 4K, igual a uma TV 1080p. Com a janela restaurada e redimensionada, a interface encolhe junto, sem cortar. Resultado: ______

- [ ] Início: os ícones dos tiles (Steam, Playnite, Xbox e um app personalizado) têm bordas lisas, sem serrilhado, com o tile parado e com foco; repetir com a tela em 150 % de escala e arrastando a janela para uma tela de escala diferente. Resultado: ______

- [ ] Sistema, numa categoria longa (App, Extras opcionais): a borda de baixo esmaece no fundo enquanto há linhas abaixo, e a de cima quando já rolou; no fim e no começo da lista, a borda fica limpa. A barra fina à direita aparece só quando a lista rola e acompanha a posição (controle, roda do mouse e teclado). Ao entrar na aba Sistema e ao trocar de categoria (de uma curta, como Tela de jogo, para App), a borda de baixo já aparece esmaecida, sem mostrar a lista cortada por um instante. No menu da sessão, a lista do painel lateral faz o mesmo (com o "Fechar menu" fora da tela, a borda de baixo esmaece e a barra aparece). Resultado: ______
- [ ] Sistema → Outros apps do desenvolvedor: ControlFS (Alpha), Tako Launcher (Alpha fechado) e JoyChromium (Beta) em três cards lado a lado, com a arte em cima e nome, fase e descrição embaixo; o card com foco cresce sem ser cortado nas bordas, e esquerda/direita passam de um para outro; A abre a página do GitHub de cada um. Não há mais WakeOn nem Next Boost. Resultado: ______

### Vários aplicativos personalizados

- [ ] Início: depois dos lançadores aparece o bloco **+ Adicionar app**; A (ou clique) nele abre o seletor de arquivo; escolher um `.exe` cria um bloco com o ícone e o nome do programa (a descrição do exe), já selecionado. Passar o foco pelo "+" não troca o lançador. Resultado: ______
- [ ] Adicionar dois ou três apps: cada um tem o seu bloco; selecionar um mostra em Sistema → Ao entrar no modo console o .exe, os argumentos e o processo a acompanhar **dele**, e editar muda só ele (confira trocando de bloco e voltando). Resultado: ______
- [ ] "Remover este aplicativo" tira o bloco e o Steam Big Picture passa a abrir; os outros apps continuam. Reabrir o app mantém a lista, o selecionado e os campos de cada um. Resultado: ______
- [ ] Config antiga com um app personalizado: ele aparece como o primeiro bloco, selecionado se era o lançador. Resultado: ______
- [ ] Sessão com um app que é só um iniciador (abre outro programa e fecha), com "Processo a acompanhar" vazio: o log mostra "X abriu Y e fechou; a sessão passa a acompanhar Y", a janela de Y vai para a tela de jogo e a sessão termina uns 8 s depois que Y fecha. Com "Processo a acompanhar" preenchido, vale o processo escrito. Resultado: ______

### Tour guiado

- [ ] Primeira abertura: o tour abre sozinho em "Bem-vindo ao Console Mode", 1 de 9, com o cartão no meio da tela e o fundo escurecido. Resultado: ______
- [ ] Cada parada troca para a aba certa e o recorte (com contorno na cor de destaque) envolve: as abas; a fileira de iniciadores; Jogar agora e o botão redondo; as telas (Sessão); os ajustes rápidos (Sessão, rolando se precisar); as categorias (Sistema); as três linhas de atalho (Sistema → Controle). Resultado: ______
- [ ] O cartão nunca cobre o recorte nem sai da tela: em 1080p, em 4K com escala de 150% e com a janela restaurada (pequena). Redimensionar com o tour aberto reposiciona recorte e cartão. Resultado: ______
- [ ] Controle: A avança, B pula, as setas andam entre Próximo / Voltar / Pular; LB/RB, Start e Y não fazem nada durante o tour (nenhuma sessão começa). Mouse: os três botões funcionam e clicar no fundo escuro não faz nada. Teclado: Enter avança, Esc pula. Resultado: ______
- [ ] Os símbolos nos textos seguem o controle: LB/RB, A, ☰ e Y no Xbox; L1/R1, ✕, OPTIONS e △ no PlayStation (e mudam se trocar o layout em Sistema → Controle). Resultado: ______
- [ ] "Começar" (última parada) ou "Pular tutorial" volta para o Início com o foco em Jogar agora e grava `consoleTourDone: true` (e `shortcutsOnboardingDone`); reabrir o app não mostra o tour de novo. Resultado: ______
- [ ] Sistema → App → Tutorial abre o tour de novo, do começo. Resultado: ______
- [ ] O app iniciado escondido mostra o tour só quando a janela é aberta pela bandeja; com uma sessão ativa, nunca. Resultado: ______
- [ ] Alemão, francês e russo: título e texto do cartão quebram linha sem cortar; os botões cabem. Resultado: ______

## Console Mode 2 Alpha: enumeração somente leitura

- [ ] Executar `cargo run -p cm-platform --example hardware` e conferir telas ativas, desativadas e desconectadas conhecidas pelo Windows; dispositivos sem fonte GDI devem ter nome GDI vazio. Resultado: ______
- [ ] Comparar `stable_id` byte a byte com `focusMonitor`, `hideMonitors` e chaves de `monitorModes` do config da 1.7, sem alterar o arquivo. Resultado: ______
- [ ] Conferir primário, posição, resolução, frequência, modos únicos em ordem decrescente e HDR com as configurações do Windows. Campos desconhecidos de telas desconectadas devem aparecer como `None` ou lista vazia. Resultado: ______
- [ ] Conferir saídas de áudio ativas, desabilitadas e desconectadas, nomes e saída padrão; repetir com uma saída de captura para confirmar que não é listada. Resultado: ______
- [ ] Confirmar que a execução não altera telas, áudio, registro ou arquivos de configuração. Resultado: ______

## Console Mode 2 Alpha: desk real

Executar somente com autorização do dono, acesso local e uma forma alternativa de recuperar imagem; não executar enquanto o único acesso remoto depender de uma tela virtual.

- [ ] Guardar backup CCD; confirmar que o arquivo persiste após reiniciar o app e que um arquivo truncado/inválido é recusado antes de alterar as telas.
- [ ] Ativar uma TV conectada mas inativa; confirmar espera limitada, retorno de erro se indisponível e TV como primária, preservando as posições relativas das outras telas.
- [ ] Desconectar apenas as telas selecionadas (inclusive em múltiplas GPUs e clonagem); confirmar recusa ao tentar desconectar todas as telas ativas.
- [ ] Aplicar resolução/frequência suportada e conferir o modo efetivo; para modo indisponível, conferir seleção da resolução mais próxima e depois frequência mais próxima.
- [ ] Conferir screen_rect com coordenadas negativas, rotação e escala/DPI diferentes e posicionar o launcher no retângulo retornado pelo adaptador.
- [ ] Ativar HDR inicialmente desligado: hdr_on deve retornar true; repetir com HDR já ligado: false. Restaurar somente o HDR ligado pela sessão; verificar erro em tela sem suporte.
- [ ] Trocar áudio pelos IDs de endpoint e pelos IDs legados, incluindo saída desabilitada; conferir os três papéis (console, multimídia e comunicações) e restauração ao endpoint salvo.
- [ ] Restaurar backup após foco, desconexão e mudança de modo: conferir todas as telas originalmente ativas, telas originalmente inativas, resolução, frequência, rotação, posição e primária.
- [ ] Simular recuperação após reiniciar o app, com adaptador do engine instalado; se uma tela do backup estiver ausente ou o driver rejeitar a topologia, conferir erro e manutenção do diário/backup para nova tentativa.
- [ ] Confirmar que TV em standby físico exige despertar externo se o driver não oferecer um caminho disponível: este desk não usa DDC/CI, cortinas pretas nem fallback de estender todas as telas.

## Console Mode 2 Alpha: Home do modo Xbox no Windows 11

- [ ] Com a alpha selecionada como Home e o Steam Big Picture em modo janela, iniciar uma sessão com **Usar FSE**. Verificar no monitor de jogo as bordas e o foco. Conferir nos eventos de ativação do Windows se a Home da alpha foi iniciada e se houve uma abertura do Xbox. Ao sair, verificar a volta ao desktop.

- [ ] Com o Xbox selecionado como Home, tentar jogar com FSE pelo Steam ou Playnite. A alpha deve orientar a seleção da Home antes de alterar telas, áudio ou entrar na FSE. Repetir com a seleção de Home ausente.
- [ ] Registrar, selecionar e restaurar a Home com o controle. Conferir o indicador na linha acionada, mover o foco durante a operação e verificar que pressionar A duas vezes não repete a ação.

- [ ] Na Home da 2.0, abrir as opções do Playnite, do Steam Big Picture e de um app próprio. Ligar **Usar FSE** só no Playnite, reabrir a alpha e confirmar que a preferência ficou independente por launcher.
- [ ] A partir do desktop, abrir cada launcher com **Usar FSE** ligado. Confirmar a entrada no Windows, testar o controle e conferir que o launcher recebe o foco. Encerrar pelo menu da sessão e conferir a volta ao desktop.
- [ ] Repetir partindo de uma FSE que já estava ativa. Encerrar pelo menu da sessão e confirmar que a FSE continua ativa.
- [ ] Recusar a entrada no Windows e verificar se a sessão volta ao desktop e restaura as telas e o áudio. Em outra tentativa, encerrar à força com a FSE ativa e reabrir a alpha para validar a recuperação.

- [ ] Anotar a Home atual em Configurações do Windows → Jogos → Modo Xbox. Registrar a alpha e verificar que o Console Mode aparece no seletor e que a escolha anterior continua ativa.
- [ ] Selecionar o Console Mode como Home e definir o launcher da Home. Pelo Windows, entrar no modo Xbox com a alpha fechada e repetir com ela na bandeja. Verificar que o launcher escolhido abre uma única vez.
- [ ] Em um desktop que omite o seletor do Windows, usar **Selecionar o Console Mode como Home** na alpha. Conferir se o Windows mantém a escolha após a entrada na FSE. Usar **Restaurar a Home anterior** e verificar a seleção original, inclusive se antes não havia uma Home explícita.
- [ ] Abrir o Playnite pela Home, entrar em um jogo e encerrar o launcher. Verificar o retorno à Home. Em seguida, voltar ao PC e verificar tela principal, áudio e área de trabalho.
- [ ] Com entrada automática desativada, abrir a alpha normalmente no desktop. Ativar a preferência e repetir. Testar também a inicialização na bandeja.
- [ ] Restaurar a Home anterior no seletor do Windows, remover o registro da alpha e verificar que o executável continua abrindo como aplicativo de desktop.
- [ ] Testar em um Windows sem suporte à FSE ou sem permissão de registro. Conferir a mensagem no app e repetir a navegação normal pelo desktop.

## Console Mode 2 Alpha: app (bandeja, instância, inicialização)

- [ ] Na 2.0 em janela, arrastar pelo topo, maximizar/restaurar por duplo clique e pelos botões, minimizar e fechar. Conferir o fechamento com a bandeja habilitada e desabilitada e a ausência dos botões em tela cheia.

Executar apenas quando o dono puder testar localmente. Os itens abaixo não foram validados
fisicamente pelo agente; não iniciar sessão durante acesso remoto do dono.

- [ ] Com a 1.7 aberta, abrir a Alpha em janela normal e confirmar ícone próprio na janela e na bandeja, sem alterar a 1.7.
- [ ] Abrir a Alpha novamente: apenas uma janela/processo persistente; a primeira janela aparece, inclusive se estava oculta ou minimizada.
- [ ] Consultar `\\.\pipe\ConsoleMode2Alpha.Control` com uma linha JSON `{"cmd":"status"}`; conferir `ok`, `active`, `launcher` e `focusScreenId` (id configurado). Fora de sessão, `launcher` é vazio.
- [ ] Enviar `{"cmd":"show"}`; a janela deve aparecer. JSON inválido, comando desconhecido ou `exit` pelo pipe devem responder `ok:false`, sem executar ações.
- [ ] Em Sistema → Inicialização, ativar Começar na bandeja: fechar a janela deve ocultá-la; Abrir no ícone deve restaurá-la. Desativado, fechar deve encerrar o app.
- [ ] Abrir com `--tray`: não deve permanecer janela visível nem botão na barra de tarefas; Abrir restaura a janela. Sair remove ícone e processo.
- [ ] Reiniciar o Explorer: o ícone deve reaparecer. Se a bandeja não puder ser criada, o app deve manter a janela acessível.
- [ ] Somente com autorização para gravar o registro: alternar Iniciar com o Windows e conferir exclusivamente o valor `ConsoleMode2Alpha` em HKCU Run, com executável entre aspas e `--tray`; desativar remove somente esse valor.
- [ ] Somente com autorização para gravar o registro: ativar Registrar links consolemode2alpha, conferir HKCU\Software\Classes\consolemode2alpha e testar `consolemode2alpha://show` com Alpha aberta e fechada; desativar remove somente essa classe. Testar executável em caminho com espaços.
- [ ] Confirmar que nenhuma chave, pipe, mutex, protocolo ou ícone da 1.7 foi substituído. Apenas alternar opções explicitamente grava o registro; abrir a Alpha não registra nada.
- [ ] Em janela local segura, com sessão configurada e dono presente: `start`/`consolemode2alpha://start` devem jogar o card focado, inclusive fora da aba Início; nova requisição não cria outra sessão.
- [ ] Durante sessão local segura: `menu`/link/menu da bandeja devem colocar o menu sobre o jogo; `stop`/link/Encerrar sessão devem restaurar o desktop. Fora de sessão, os dois itens de sessão da bandeja ficam desabilitados.
- [ ] Durante sessão local segura: Sair (ou fechar sem Começar na bandeja) deve pedir restauração e aguardar o término antes de sair. Não matar o processo para este teste.

O pipe usa uma linha JSON UTF-8 de até 4096 bytes e uma resposta JSON. As respostas de
`start` e `stop` confirmam o recebimento pela UI; consultar `status` depois para observar
a transição assíncrona da sessão. Os links aceitam show/start/stop/menu; status é só pelo pipe.
A ACL do pipe permite apenas o usuário atual e LocalSystem, rejeitando clientes remotos.


## Console Mode 2 Alpha: apps próprios

- [ ] Preparar uma pasta temporária com `CM2_DATA_DIR` e usar `CM2_SIMULATED_DESK=1` em todos os testes; nunca iniciar sessão no desktop real. Fechar o app ao terminar.
- [ ] Importar uma cópia da config 1.7: conferir apps, argumentos, processo acompanhado e `customAppId`; salvar/reabrir sem perder campos desconhecidos. Não alterar a config instalada da 1.7.
- [ ] Conferir apps após os iniciadores e antes de “Adicionar app”, com ícone do executável e descrição do arquivo (ou nome sem extensão), sobre fundo neutro.
- [ ] A/clique em “Adicionar app” abre o seletor Windows filtrado para `.exe`. Cancelar não altera a fileira; adicionar salva e foca o novo card.
- [ ] Selecionar arquivo ausente ou não `.exe`: mostrar erro sem adicionar. Tentar jogar um app removido do disco: bloquear antes da sessão.
- [ ] Y/clique direito abre o menu do app com Jogar, Argumentos, Processo a acompanhar, Mover na fileira e Remover da Início, com o mesmo painel do menu de jogo.
- [ ] Editar argumentos e processo com teclado e com o teclado na tela (controle/mouse); salvar e reabrir, cancelar sem alterar. Vazio acompanha o próprio executável.
- [ ] Mover apps entre si, concluir, reabrir e conferir ordem. Os apps não atravessam os iniciadores nem “Adicionar app”.
- [ ] Remover sem confirmação: ouvir som de voltar e conferir foco no card anterior. Remover o último e reiniciar: não ressuscitar pelo campo legado. Adicionar novamente recupera o app.
- [ ] Com desktop simulado, abrir um executável inofensivo com argumentos; conferir que o processo indicado mantém a sessão e que fechar respeita a tolerância de saída. Nunca mudar telas/áudio/HDR reais.
- [ ] Simular falha ao salvar a config: mostrar erro e manter o app/edição/remoção anterior.

## Console Mode 2 Alpha: entrega (atualização, instalador)

Usar uma máquina de teste, ou o PC do dono com cuidado: o instalador da 2.0 **não** pode tocar na 1.7. Nada disto roda sem o dono pedir; não há release da 2.0 publicada ainda.

- [ ] `.\build\Publish-ConsoleMode2.ps1` gera `dist\v2\ConsoleMode2.exe`, `ConsoleMode2Alpha-Setup-x64.exe` e o `.sha256` (e só o exe, com aviso, se o Inno Setup não estiver instalado). Resultado: ______
- [ ] O `ConsoleMode2.exe` abre sem janela de console; no Explorer (Propriedades → Detalhes) mostra o ícone, "Console Mode 2 Alpha", versão `2.0.0` e copyright. Resultado: ______
- [ ] Instalar o `Setup` sem a 1.7 no PC: instala em `%LOCALAPPDATA%\Programs\Console Mode 2 Alpha`, sem pedir administrador, com "Console Mode 2 Alpha" no menu Iniciar e `LICENSE.txt` + `THIRD_PARTY_NOTICES.md` na pasta. Resultado: ______
- [ ] Instalar com a 1.7 já instalada e em uso: a 1.7 continua intacta (pasta, atalho "Console Mode", entrada em Aplicativos instalados, valor de iniciar com o Windows, `consolemode://start` ainda abre a 1.7, `%LOCALAPPDATA%\ConsoleMode`). As duas aparecem separadas em Aplicativos instalados. Resultado: ______
- [ ] Desinstalar a 2.0 não remove nada da 1.7, e `%LOCALAPPDATA%\ConsoleMode2` (config) continua lá. Desinstalar a 1.7 não remove a 2.0. Resultado: ______
- [ ] Sistema → Atualizações mostra a versão instalada, o canal "Alpha" e "Procurar atualização". Sem release `v2.*` publicada: "Procurar atualização" responde "Você já está na versão mais recente", sem erro. Resultado: ______
- [ ] Sem internet: "Procurar atualização" mostra o aviso de que não conseguiu, sem travar a interface; a verificação ao abrir não mostra nada. Resultado: ______
- [ ] "Procurar atualizações ao abrir" desligado: abrir o app não faz nenhuma conexão (conferir no Monitor de recursos / firewall). Ligado: uma consulta rápida ao abrir, sem aviso quando não há nada. Resultado: ______
- [ ] Com uma pre-release `v2.*` de teste (versão maior, com `ConsoleMode2Alpha-Setup-x64.exe` e `.sha256`): o aviso aparece, a linha vira "Instalar e reiniciar", o instalador roda em modo silencioso, fecha o app e o abre de novo na versão nova. Resultado: ______
- [ ] Mesma release com o `.sha256` de outro arquivo (ou o `.exe` adulterado): a atualização é recusada ("não passou na verificação"), nada é instalado e o arquivo baixado é apagado. Resultado: ______
- [ ] Release `v1.*` ou `v2.*` que não está marcada como pre-release, ou sem o `.sha256`: nunca é oferecida pela 2.0. Resultado: ______
- [ ] Com a 1.7 (com a opção de pré-lançamentos ligada) e uma release `v2.*`: conferir o aviso da seção "Aviso sobre a 1.7" de `docs/v2/RELEASE.md` e que a 1.7 não baixa nada da 2.0. Resultado: ______
- [ ] Durante uma sessão ativa, "Instalar e reiniciar" recusa ("Termine a sessão antes de atualizar") e não fecha o app. Resultado: ______

## Console Mode 2 Alpha: extras da sessão

Nada abaixo foi executado de verdade na 2.0: o código compila e as regras puras têm testes, mas RTSS, ponteiro, ícones, Steam e perfis por jogo só se confirmam numa sessão real. Executar somente com autorização do dono e com acesso local, com a 1.7 sem sessão ativa. Antes de cada item, abrir o `ConsoleMode2` e acompanhar o log (`startup.log`).

**RTSS: limite e contador de FPS**
- [ ] Com RTSS instalado e `rtss-cli.exe` em `ConsoleMode2\tools` (ou na pasta da 1.7): anotar limite e limitador do RTSS, iniciar a sessão com "Limite de FPS" em 60 e conferir no RTSS: 60 e limitador ligado. Ao encerrar, limite e limitador voltam exatamente ao anotado. Resultado: ______
- [ ] Mesmo teste com o limitador do RTSS desligado e limite 144 antes da sessão: depois da sessão continua desligado, com 144. Resultado: ______
- [ ] Sem RTSS instalado, ou sem `rtss-cli.exe`: a sessão abre normalmente e o log diz que o limite foi ignorado (nada de erro na tela). Resultado: ______
- [ ] RTSS fechado antes da sessão: ele é aberto na bandeja, o limite aplicado e, no fim, restaurado. Resultado: ______
- [ ] "Mostrar FPS na tela" ligado (estilo `compact`): o contador "NN FPS" aparece dentro do jogo e some ao encerrar; o OSD do Afterburner fica escondido durante a sessão e volta depois. Estilo `external`: nada nosso aparece e o Afterburner não é mexido. Resultado: ______
- [ ] Estilos `stats`, `detailed` e `custom`: CPU e RAM atualizam a cada segundo. **Diferença conhecida da 1.7:** GPU, VRAM, clocks e temperatura ainda não aparecem na 2.0 (a linha some em vez de mostrar valores). Resultado: ______
- [ ] O `fpsOverlayLayout` do config da 1.7 (posição no canto, cartão, cor, tamanho) é respeitado. Resultado: ______
- [ ] Matar o `ConsoleMode2` durante a sessão (limite e contador ligados) e abri-lo de novo: a recuperação devolve o limite do RTSS ao de antes e apaga o contador. Resultado: ______

**Ponteiro**
- [ ] "Estacionar o ponteiro" ligado: quando o iniciador abre, o ponteiro vai para o canto inferior direito da tela de jogo (duas vezes, com ~3 s de intervalo) e, ao encerrar, volta ao lugar de antes. Testar com Big Picture, Playnite, app personalizado e Xbox (neste, ~4 s depois de abrir). Resultado: ______
- [ ] Tela de jogo à esquerda ou acima da principal (coordenadas negativas): o ponteiro cai dentro dela. Resultado: ______
- [ ] Desligado nos ajustes: o ponteiro não se mexe. Resultado: ______
- [ ] Sessão encerrada sem nunca estacionar (iniciador que não abriu): o ponteiro não é movido na volta. Resultado: ______

**Ícones da área de trabalho**
- [ ] Anotar a disposição dos ícones (organização automática desligada), iniciar uma sessão que desliga a tela principal e encerrar: os ícones voltam aos lugares em até ~8 s, sem atrasar a volta. Resultado: ______
- [ ] Com a organização automática ligada: o log diz que as posições não foram salvas e nada é mexido. Resultado: ______
- [ ] Ícones criados ou apagados durante a sessão não são movidos nem recriados. Resultado: ______
- [ ] "Devolver os ícones da área de trabalho ao lugar" desligado: nada é lido nem movido. Resultado: ______
- [ ] Matar o app no meio da sessão e reabrir: a recuperação também recoloca os ícones. Resultado: ______

**Steam na volta**
- [ ] Big Picture como iniciador, "Fechar a Steam ao voltar" ligado, nenhum jogo rodando: ao encerrar, o Big Picture fecha e a janela da Steam vai para a bandeja (o processo continua). Resultado: ______
- [ ] Mesmo teste com um jogo ainda aberto (RunningAppID diferente de 0): a Steam fica aberta. Resultado: ______
- [ ] Opção desligada: o Big Picture fecha, a janela da Steam não é tocada. Resultado: ______
- [ ] Outro iniciador (Playnite, Xbox, app personalizado): a Steam não é tocada. Resultado: ______
- [ ] **Mudança em relação ao alpha anterior:** o Big Picture agora fecha ao encerrar mesmo com "Fechar a Steam ao voltar" desligado (como na 1.7); só a janela do cliente depende da opção. Resultado: ______

**Perfil por jogo durante a sessão**
- [ ] Sessão iniciada pelo Big Picture; abrir pela Steam um jogo com perfil (resolução, HDR, FPS, áudio): ao abrir, o perfil é aplicado; ao fechar o jogo, a tela volta como estava quando ele abriu. Resultado: ______
- [ ] Trocar de um jogo com perfil para outro: o segundo perfil entra e, ao fechar, volta ao estado de antes do segundo jogo. Resultado: ______
- [ ] Jogo sem perfil (ou perfil vazio): nada muda, o log diz "sem perfil". Resultado: ______
- [ ] Jogo aberto pelo card da Início (a sessão já começa com o perfil): ao fechar o jogo, volta para a resolução, HDR e FPS da sessão (config), não para os do perfil. **Limitação:** se a resolução da sessão está em "Não mudar", a resolução do perfil não é desfeita ao fechar o jogo. Resultado: ______
- [ ] HDR do perfil ligando um HDR que estava desligado antes da sessão: ao encerrar a sessão (ou após um crash e a recuperação) o HDR volta a desligado. Resultado: ______
- [ ] Limite de FPS do perfil: o backup do RTSS continua sendo o do usuário (feito uma vez); no fim da sessão volta o limite original, não o do perfil. Resultado: ______
- [ ] Encerrar a sessão com o jogo ainda aberto: o perfil é desfeito antes de restaurar as telas. Resultado: ______

**Game Bar**
- [ ] A 1.7 só usa a Game Bar na tela de ajustes (botão Xbox abre a Game Bar: `HKCU\Software\Microsoft\GameBar\UseNexusForGameBarEnabled`), nunca durante a sessão. A 2.0 tem o módulo (`cm_platform::gamebar`), mas ainda não há ajuste do botão Home para ligá-lo. Quando houver: desligar a opção grava 0, ligar grava 1, e valor ausente conta como ligado. Resultado: ______

## Console Mode 2 Alpha: menu da sessão, tour, sons e Sobre

- [ ] Segurar ☰ por ~1 s durante um jogo abre o menu da sessão por cima, em tela cheia; B fecha e o jogo volta à frente.
- [ ] Janelas: lista as janelas abertas; A traz uma para a frente; X fecha a selecionada.
- [ ] Tela e som: troca a saída de áudio; o som passa a sair no aparelho escolhido.
- [ ] Volume: ← → muda o volume real do Windows (confira no ícone de som da barra de tarefas).
- [ ] FPS: trocar entre Sem limite / 30 / 60 / 120 / 144 aplica o limite no RTSS na hora; ao encerrar, o limite antigo do RTSS volta.
- [ ] Visão de tarefas abre a Visão de tarefas do Windows.
- [ ] Primeira abertura (pasta de dados vazia): o tour aparece, ← → navega, B pula; não aparece de novo na próxima abertura.
- [ ] Sistema → Sobre → "Ver o tour de novo" mostra o tour outra vez.
- [ ] Som de abertura toca ao abrir o app (não toca com `--tray` nem quando o "som de abertura" do 1.7 estava desligado).
- [ ] Sistema → Som: estilo "Suave" troca os sons por tons curtos; o volume dos sons muda de verdade; desligar os sons silencia tudo.
- [ ] Sistema → Sobre → "Relatar um problema" abre uma issue nova no GitHub já com a versão.
- [ ] Sistema → Sobre → "Abrir a pasta de registros" abre `%LOCALAPPDATA%\ConsoleMode2` no Explorer, com `consolemode2.log` dentro e horários nas linhas.
- [ ] Sistema → Sobre → "Licenças" abre o `THIRD_PARTY_NOTICES.md` instalado (sem instalar, abre `docs/v2/THIRD_PARTY_NOTICES.md` do repositório).

## Console Mode 2 Alpha: controle (atalhos, visão de tarefas, mouse)

Não dá para provar no CI: precisa de um controle de verdade (Xbox e PlayStation), de um PC com a 2.0 aberta e de uma sessão. O CI cobre as regras puras (`cargo test --workspace`). A combinação é sempre segurada com **um** controle: botões de controles diferentes não se somam. Para trocar os atalhos: Sistema → Atalhos.

**Atalhos que funcionam com o app na bandeja ou atrás de outras janelas**
- [ ] Sistema → Atalhos: as quatro linhas (Abrir o Console Mode, Menu da sessão, Voltar ao PC, Visão de tarefas) trocam entre "Desligado" (no menu, "Segurar ☰ (padrão)") e as combinações; cada combinação aparece com os nomes do Xbox e do PlayStation. Resultado: ______
- [ ] Uma combinação que já é de outro atalho (ou que contém a dele) é pulada ao trocar; nunca dois atalhos ficam com a mesma. Resultado: ______
- [ ] Um `config.json` copiado da 1.7 mostra os atalhos que ela tinha (`homeShortcut`, `menuShortcut`, `exitShortcut`, `taskViewShortcut`), e salvar no app mantém os mesmos números no arquivo. Resultado: ______
- [ ] "Abrir o Console Mode" = botão Xbox/PS, app na bandeja (fora de sessão): segurar ~1 s traz a janela para a frente; soltar antes de ~1 s não faz nada. Resultado: ______
- [ ] O mesmo com o app aberto mas atrás de outra janela: a janela vem para a frente. Com o app já na frente: nada acontece. Resultado: ______
- [ ] Com o atalho "Desligado": segurar a combinação não abre nada. Resultado: ______
- [ ] Em sessão, "Menu da sessão" no padrão: segurar ☰ por ~0,8 s abre o menu sobre o jogo; um toque rápido continua sendo do jogo. Segurar Select + Start (ou outra combinação com ☰) **não** abre o menu. Resultado: ______
- [ ] Em sessão, "Menu da sessão" com uma combinação (ex.: Select + Y): segurar a combinação por ~0,25 s abre o menu; ☰ sozinho deixa de abrir. Resultado: ______
- [ ] Em sessão, "Voltar ao PC" com uma combinação: segurar por ~1 s encerra a sessão e devolve as telas (igual a "Encerrar sessão"). Fora de sessão não faz nada. Resultado: ______
- [ ] Segurar uma combinação já apertada antes da sessão começar não dispara nada até soltar e apertar de novo. Resultado: ______
- [ ] Os atalhos de Abrir e de Voltar não disparam ao mesmo tempo (um só vale fora da sessão, o outro dentro). Resultado: ______
- [ ] Mexer nos ajustes de atalho com o app na frente: o novo atalho vale em até ~1 s, sem reabrir o app. Resultado: ______

**Visão de tarefas com o controle**
- [ ] "Visão de tarefas" em Select + X (padrão, como na 1.7): segurar a combinação abre a Visão de tarefas do Windows, também em um jogo e com o app na bandeja. Resultado: ______
- [ ] Controle **Xbox**: a Visão de tarefas se move sozinha com o direcional (o Windows já entende); o app **não** manda teclas por cima (cada movimento anda uma casa só). Resultado: ______
- [ ] Controle **PlayStation** com "Visão de tarefas no controle PlayStation" ligado: direcional e analógico esquerdo movem a seleção (segurar repete), A abre a janela escolhida, B fecha. Resultado: ______
- [ ] Os botões ainda apertados da combinação não contam: só depois de soltar tudo o controle começa a mexer. Resultado: ______
- [ ] A opção desligada (ou combinação desligada): o controle PlayStation não manda teclas. Resultado: ______
- [ ] Segurar a combinação de novo com a Visão de tarefas aberta pelo app fecha a Visão de tarefas. Resultado: ______
- [ ] Fechar a Visão de tarefas pelo mouse, teclado ou Windows + Tab: o controle larga a Visão de tarefas na hora (log: "controle solto (fechada)"). Resultado: ______
- [ ] Se a Visão de tarefas não abrir (não ficou em primeiro plano): o controle não manda teclas para o jogo atrás (log: "não ficou em primeiro plano"). Resultado: ______
- [ ] Depois de ~2 min aberta, o controle larga sozinho. Resultado: ______
- [ ] Enquanto a Visão de tarefas está sendo guiada, o app (se estiver aberto) não reage aos botões. Resultado: ______

**Controle como mouse** (Sistema → Controles → "Usar o controle como mouse")
- [ ] Desligado (padrão): nenhum controle mexe no ponteiro. Resultado: ______
- [ ] Ligado, fora de sessão, com o app **atrás** de outra janela ou na bandeja: o analógico esquerdo move o ponteiro, A clica (segurar arrasta, dois toques dão clique duplo), B clica com o botão direito, o analógico direito rola (para cima/baixo e para os lados). Resultado: ______
- [ ] A velocidade (Lenta, Média, Rápida) muda o quanto o ponteiro anda; na TV 4K e no monitor Full HD o ponteiro cruza a tela em tempo parecido. Resultado: ______
- [ ] Um analógico gasto, parado um pouco fora do centro, não mexe o ponteiro (zona morta). Resultado: ______
- [ ] Com o app **na frente**: o controle navega no app e o ponteiro fica parado; A não clica. Ao tirar o app da frente, o ponteiro volta a responder. Resultado: ______
- [ ] Com o app na bandeja: apertar A/B/direcional **não** abre cards, menus nem inicia nada no app escondido. Resultado: ______
- [ ] Em sessão, o controle nunca mexe o ponteiro (é do jogo), mesmo com a opção ligada. Resultado: ______
- [ ] Um botão já apertado ao ligar o modo (o A que acabou de tirar o app da frente) não clica até ser solto. Resultado: ______
- [ ] Segurar A (arrastando) e trazer o app para a frente: o botão do mouse é solto, nunca fica preso. Resultado: ______
- [ ] Janela de programa rodando como administrador: o ponteiro não responde ali (UIPI); o app não trava. Resultado: ______
- [ ] Ao fechar o app com a opção ligada, o ponteiro volta a funcionar só com o mouse físico. Resultado: ______

**Controle conectado entra no modo console** (Sistema → Controles → "Entrar ao conectar o controle")
- [ ] Ligado, app na bandeja, fora de sessão: ligar um controle (ou acordar um sem fio) traz o app para a frente. Resultado: ______
- [ ] Desligado (padrão): ligar o controle não faz nada. Resultado: ______
- [ ] Nos primeiros 15 s depois de abrir o app, controles já ligados não disparam nada. Resultado: ______
- [ ] Depois de uma sessão encerrar, controles sem fio que reconectam sozinhos em até 30 s não trazem o app de volta. Resultado: ______
- [ ] Com a janela aberta (mesmo atrás de outras) ou durante uma sessão: ligar um controle não faz nada. Resultado: ______
- [ ] O log (`%TEMP%\ConsoleMode2-startup.log`) registra "Controle conectado; entrar no modo console: sim/não". Resultado: ______

## Console Mode 2 Alpha: idiomas

Seis idiomas: pt-BR, en-US, es-ES, de-DE, fr-FR, ru-RU (`crates/cm-app/locales`). Sempre com `CM2_DATA_DIR` numa pasta de teste e `CM2_SIMULATED_DESK=1`.

**Escolha do idioma**
- [ ] Config sem `appLanguage` (vazio) e Windows em português: a interface abre em português. Com o Windows em inglês, alemão, francês, espanhol ou russo: abre no idioma do Windows. Com um idioma que não temos (ex.: japonês): abre em português. Resultado: ______
- [ ] `appLanguage` com um código salvo (ex.: `ru-RU`) vale mais que o idioma do Windows. Resultado: ______
- [ ] Sistema → Idioma: cada toque em A passa ao idioma seguinte e **toda** a interface muda na hora, sem reiniciar (abas, hints da barra de baixo, Sessão, Sistema, menu do card, menu da sessão). Resultado: ______
- [ ] O nome de cada idioma continua na própria língua (Português (Brasil), English, Español, Deutsch, Français, Русский). Resultado: ______
- [ ] Fechar e abrir o app: o idioma escolhido continua, e `config.json` tem o `appLanguage` certo. Resultado: ______
- [ ] A lógica não depende do idioma: em cada idioma, "Ver o tour de novo", "Relatar um problema", "Abrir a pasta de registros", "Licenças", Tema, Estilo/Volume dos sons, Limite de FPS, Velocidade e os atalhos do controle fazem o mesmo que em português. Resultado: ______

**Textos**
- [ ] Início, Sessão e Sistema (todas as categorias) em inglês e em russo: nada em português, o cirílico aparece (sem quadradinhos). Resultado: ______
- [ ] Alemão e russo (textos mais longos): nenhuma descrição passa da borda direita, nada se sobrepõe aos cards da Início nem às dicas da barra de baixo. Resultado: ______
- [ ] Tour (Sistema → Sobre → Ver o tour de novo), card menu (Y num jogo ou app próprio), editor de argumentos, tela "Está vendo esta tela?" e menu da sessão: tudo traduzido, botões legíveis. Resultado: ______
- [ ] Mensagens (toasts): sessão encerrada, "<app> fechou", atualização (procurar, baixar), erros ao salvar apps. Resultado: ______
- [ ] "Última vez" e "Steam · última vez" em dias e meses (em russo: 1/21 dia, 2–4 dias, 5+ dias; 1 mês, 2–4 meses, 5+ meses). Resultado: ______
- [ ] Bandeja (botão direito no ícone): Abrir, Menu da sessão, Encerrar sessão e Sair no idioma escolhido, e mudam ao trocar o idioma sem reiniciar. Resultado: ______
- [ ] O log (`consolemode2.log`) continua em português em qualquer idioma. Resultado: ______
- [ ] Revisão por nativos de francês e russo (traduzidas por IA; ver o relatório da PR): termos como "Affichage des tâches", "Представление задач", "сеанс" e o tom (vous / вы). Resultado: ______

## Console Mode 2 Alpha: o que faltava da 1.7

Itens portados depois do primeiro corte da 2.0. Sempre com `CM2_DATA_DIR` numa pasta de teste e `CM2_SIMULATED_DESK=1`, a menos que o item diga o contrário. Cada bloco diz o que foi visto num teste ao vivo com controle virtual (ViGEm) e o que só dá para conferir com hardware de verdade.

**Bateria do controle** (barra de cima, menu da sessão e aviso de bateria fraca)
- [ ] Controle sem fio de verdade (DualSense e Xbox): o nível aparece na barra de cima, ao lado do relógio, em até 30 s depois de conectar, e bate com o que o Windows mostra. Resultado: ______
- [ ] Ligado por cabo, ou controle sem bateria: nada aparece. Resultado: ______
- [ ] Dois controles com níveis diferentes: aparece o mais baixo. Resultado: ______
- [ ] No cabo, carregando: o ícone vira o de carregamento. Resultado: ______
- [ ] Menu da sessão (segurar ☰ no jogo): o mesmo nível aparece ao lado do relógio. Resultado: ______
- [ ] Durante uma sessão, com a bateria em 15 % ou menos e sem carregar: um aviso aparece no canto de cima, à direita, da **tela do jogo**, por cima do jogo, sem roubar o foco do jogo. De novo uma vez só em 5 %. Voltar a subir a 16 % e cair de novo não repete o aviso; carregar ou passar de 25 % libera o aviso de novo. Resultado: ______
- [ ] Fora de uma sessão não aparece aviso (só o ícone vermelho na barra). Resultado: ______
- [ ] Teste sem controle com bateria fraca: `CM2_FAKE_BATTERY=12` (ou `12,charging`) faz todo controle ligado informar esse nível. Resultado: ______

**Tour do menu da sessão** (visto ao vivo com controle virtual, menu em prévia; o resto precisa de uma sessão de verdade)
- [ ] Config sem `sessionMenuTourDone`: a primeira vez que o menu abre (segurar ☰ no jogo, ou ☰ na Início), aparece o convite "Primeira vez neste menu?". A faz o tour, B ("Agora não") recusa; depois de recusar ou concluir o convite não volta (`sessionMenuTourDone: true` no `config.json`). Resultado: ______
- [ ] Cada parada seleciona o botão de que fala (Voltar ao jogo, Janelas, Tela e som, Volume, FPS, Visão de tarefas, Encerrar sessão), o resto do menu escurece e o cartão mostra "n de 7". ◀ volta uma parada (nunca ao convite), A avança, B pula. Resultado: ______
- [ ] Durante o tour os painéis de Janelas/Volume/FPS não aparecem e clicar nos botões do menu não faz nada. Resultado: ______
- [ ] Fechar o menu com o tour aberto (☰ de novo, sessão acabando) guarda o tour: ele é oferecido de novo na próxima abertura. Resultado: ______
- [ ] Y no menu abre o tour direto (sem convite), em qualquer abertura. Dentro de uma sessão com "Menu da sessão" = Select + Y: apertar o Y do atalho para fechar o menu **não** abre o tour. Resultado: ______
- [ ] Sistema → Sobre → "Tour do menu da sessão" abre o menu numa prévia (fora de uma sessão) com o tour já rodando; as paradas "Tela e som" e "Fechar menu" explicam o que acontece fora de uma sessão. Dentro de uma sessão, o texto de "Encerrar sessão" diz que as telas voltam. Resultado: ______
- [ ] Em inglês, alemão e russo: os textos do tour cabem no cartão. Resultado: ______

**Abertura (logo sobre a interface ao abrir o app)** (os três estilos foram vistos ao vivo, em janela; o som e a TV só com você)
- [ ] Ao abrir o app (janela, não bandeja): a abertura aparece por cima da Início por 3 s e some com um fade de 0,4 s. Qualquer botão do controle, clique ou tecla pula na hora. Resultado: ______
- [ ] Estilo Console Mode ("Liga o PC"): a luz de energia acende e pulsa, a base aparece, a tela se desenha e dá um clarão verde, o `>_` é digitado, o cursor pisca 3 vezes, o nome entra. Resultado: ______
- [ ] Estilos inspirados no Xbox 360 (fita verde girando num orbe, clarão branco, logo escuro sobre fundo claro) e no PS4 (azul profundo, ondas à deriva, partículas subindo, logo claro). Resultado: ______
- [ ] Sem estilo escolhido (`introStyle` ausente), de vez em quando (~1 em 8) abre de surpresa um dos dois inspirados; escolher qualquer estilo em Sistema → Aparência → Abertura fixa aquele e acaba com as surpresas. Resultado: ______
- [ ] "Nenhuma (abre direto)" desliga a abertura (`introAnimation: false`, o estilo continua guardado) e o app abre direto na Início. Resultado: ______
- [ ] Som da abertura: cada estilo tem o seu som, que toca junto com a abertura, mesmo com "Sons de navegação" desligado; "Som da abertura" desligado = abertura muda; sem abertura, sem som. Ligar o som da abertura nas opções toca o som para você ouvir. Resultado: ______
- [ ] "Ver a abertura" (Sistema → Aparência → Abertura) toca agora o estilo escolhido, com o som, por cima da tela de configurações. Resultado: ______
- [ ] Com os efeitos de animação do Windows desligados (Configurações → Acessibilidade → Efeitos visuais): a abertura aparece já pronta (logo inteiro, sem os efeitos de movimento) pelo mesmo tempo. Resultado: ______
- [ ] App aberto na bandeja (`--tray` ou "Começar na bandeja"): nada aparece e nada toca; ao abrir pela bandeja não há abertura. Resultado: ______
- [ ] Primeira abertura do app (`tourDone` falso): o tour já está por baixo e aparece quando a abertura sai. Resultado: ______

**Novidades depois de atualizar** (o cartão foi visto ao vivo com um `whatsnew.json` e `lastRunVersion` de teste; a atualização de verdade só com um release)
- [ ] Atualizar pelo app (Sistema → Atualizações): antes de abrir o instalador o app grava `whatsnew.json` na pasta de dados com as notas da versão baixada. Resultado: ______
- [ ] Na primeira abertura da versão nova (o instalador reabre o app): depois da abertura, com o app na frente e na Início, aparece o cartão "Atualizado para a versão X" com o primeiro item das notas no idioma da interface (português: o resumo oculto `<!-- summary pt-BR: … -->` ou a seção em português; outros idiomas: o texto em inglês). A abre a página da versão no navegador, B fecha. Resultado: ______
- [ ] O cartão aparece uma vez só: fechar e abrir de novo o app não mostra nada (`lastRunVersion` no `config.json` é a versão atual e o `whatsnew.json` some). Resultado: ______
- [ ] Atualizar sem o app ter baixado (instalador rodado à mão, sem `whatsnew.json`): o cartão diz só "Atualizado para a versão X" e "Veja o que mudou nas notas desta versão." com o link da página da versão. Resultado: ______
- [ ] Primeira execução da 2.0 (sem `lastRunVersion`) e voltar para uma versão mais antiga: nenhum cartão. Resultado: ______
- [ ] App aberto na bandeja: o cartão espera a janela ficar visível; durante uma sessão ou com a abertura/tour na tela ele espera também. Resultado: ______

**VRR e HDR pelo menu da sessão** (visto ao vivo com a mesa simulada: backup no diário, aplicar, restaurar e o HDR pelo menu; o registro e a tela de verdade só com você)
- [ ] Sessão → Imagem → VRR ligado, sessão de verdade: ao iniciar, `HKCU\Software\Microsoft\DirectX\UserGpuPreferences` → `DirectXUserGlobalSettings` passa a ter `VRROptimizeEnable=1;` (e o resto do texto continua igual); o log diz "VRR: ligado para a sessão". Em Configurações do Windows → Sistema → Tela → Gráficos, "Taxa de atualização variável" aparece ligada. Resultado: ______
- [ ] Ao encerrar, o valor volta **exatamente** ao que era (o texto de antes, ou nenhum valor se não existia), e o log diz "VRR: de volta ao que era". Se já estava ligado antes, o app não mexe e não desfaz. Resultado: ______
- [ ] Matar o app no meio da sessão (Gerenciador de Tarefas) e abrir de novo: a recuperação devolve o VRR junto com as telas (o `session.json` guarda `vrr.previous`). Resultado: ______
- [ ] VRR desligado em Sessão → Imagem: o valor do registro não muda em nenhum momento. A Início mostra "· VRR" ao lado do HDR na linha da imagem quando está ligado. Resultado: ______
- [ ] Menu da sessão → Tela e som: com uma sessão e HDR disponível na tela de jogo, a linha "HDR" mostra Ligado/Desligado e o X troca na hora; ao encerrar, o HDR volta ao que era antes da sessão (se estava ligado antes e o menu desligou, volta ligado; se o app ligou e o menu desligou, fica desligado). Resultado: ______
- [ ] Mesmo teste do HDR com a recuperação depois de um travamento no meio da sessão (o `session.json` guarda `hdrTurnedOff`). Resultado: ______

**Acordar o PC pelo controle** (Sistema → Controles; só a **leitura** foi vista ao vivo, sem permissão de administrador: nesta máquina nenhum adaptador de controle foi achado e a linha mostrou "Indisponível". Ativar de verdade pede o UAC e **não foi testado**)
- [ ] Abrir Sistema → Controles: a linha "Acordar o PC pelo controle" mostra "Verificando os dispositivos…" por alguns segundos e depois "Indisponível" (nenhum adaptador de controle nem rádio Bluetooth capaz de acordar o PC), "Ativar" (algum ainda não pode) ou "Ligado" (todos podem). Resultado: ______
- [ ] Com o adaptador sem fio Xbox, o Bluetooth do PC ou um controle no cabo conectado, e "Permitir que este dispositivo acorde o computador" desligado no Gerenciador de Dispositivos: a linha mostra "Ativar". Resultado: ______
- [ ] Apertar A na linha: o Windows pede permissão de administrador (UAC). Aceitar: o app lê de novo, confirma que os dispositivos agora podem acordar o PC e mostra "Pronto. Suspenda o PC e aperte o botão central do controle para testar."; a linha vira "Ligado" e o Gerenciador de Dispositivos mostra a caixa marcada (aba Gerenciamento de energia). Resultado: ______
- [ ] Recusar o UAC: aparece "O Windows não recebeu a permissão; nada foi alterado." e nada muda. Resultado: ______
- [ ] Desconectar o adaptador com o UAC aberto e aceitar: "O dispositivo não foi encontrado na hora de ativar…", sem dizer que deu certo. Resultado: ______
- [ ] Suspender o PC e apertar o botão central do controle: o PC acorda (com "Entrar ao conectar o controle" ligado, no modo console). Resultado: ______
- [ ] O log (`consolemode2.log`) registra "Acordar pelo controle: …" com os nomes dos dispositivos e o código de saída. Resultado: ______

**Relatório de diagnóstico ("Relatar um problema")** (o relatório foi gerado e lido ao vivo com `CM2_NO_OPEN=1`, que não abre o navegador; a página da issue só com você)
- [ ] Sistema → Sobre → "Relatar um problema": aparece "Reunindo as informações do relatório…", em poucos segundos o navegador abre uma nova issue em `lippdev/consolemode` com o título "[2.0 Alpha] " e o corpo com a versão, o Windows e o idioma, e a mensagem diz que o relatório foi copiado e onde foi salvo. **Nada é enviado sozinho**: você revisa e envia na página. Resultado: ______
- [ ] O relatório está na área de transferência (colar no Bloco de Notas) e em `diagnostico.txt` na pasta de dados, com as seções: Console Mode (versão, Windows, tempo de execução, idioma), Displays (modo, HDR, marcas "game screen", "hidden in console mode", "primary", "off"), Audio outputs (só as ativas, com "Windows default" e "chosen in Console Mode"), Controllers (tipo, VID:PID e a bateria quando há), Programs (Steam, Playnite, RTSS e o iniciador padrão) e as últimas 50 linhas do log. Resultado: ______
- [ ] Dados privados fora: nenhum caminho mostra o nome do usuário do Windows (`C:\Users\…\AppData…`) e nenhuma chave, token ou senha aparece no log colado. Resultado: ______
- [ ] Com um DualSense e um controle Xbox conectados, cada um aparece com seu tipo; com bateria, "battery NN%". Resultado: ______
- [ ] Apertar o botão de novo enquanto o relatório está sendo reunido: "Aguarde, ainda estou trabalhando nisso." Resultado: ______
- [ ] Sem internet ou sem navegador padrão o relatório ainda é copiado e salvo. Resultado: ______

**Visão de tarefas pelo menu da sessão, guiada pelo controle** (visto ao vivo numa sessão simulada com um DualShock 4 virtual: a Visão de tarefas abriu pelo botão do menu e o controle escolheu a janela e fechou; Xbox e DualSense de verdade só com você)
- [ ] Segurar ☰ no jogo → botão "Visão de tarefas" → A: o menu fecha, o app sai da frente e a Visão de tarefas do Windows abre por cima do jogo (como o atalho Select + X). Resultado: ______
- [ ] Com um controle PlayStation (e "Visão de tarefas no controle PlayStation" ligado): o direcional e o analógico escolhem a janela, A abre e B fecha, **sem** o Console Mode reagir aos botões; terminando, o controle volta ao app/jogo (o log diz "Visão de tarefas: aberta pelo controle" e "controle solto"). Resultado: ______
- [ ] Com um controle Xbox: o Windows já move a Visão de tarefas sozinho; nada do app atrapalha. Resultado: ______
- [ ] "Visão de tarefas no controle PlayStation" desligado (DS4Windows/Steam): a Visão de tarefas abre pelo menu, mas o controle não é guiado pelo app. Resultado: ______
- [ ] Abrir pelo menu e fechar com B/Esc volta ao jogo sem deixar o app em primeiro plano. Resultado: ______

**Tela de confirmação em tela cheia na tela do jogo** (vista ao vivo com `CM2_SIMULATED_DESK=1 CM2_SIMULATED_CONFIRM=1`: a tela "Está vendo esta tela?" cobriu a tela inteira, acima da barra de tarefas, e depois de A o app saiu da frente; a troca de tela e a TV de verdade só com você — **cuidado**: só com um setup em que o Windows não pode ficar sem tela, como na 1.7)
- [ ] Setup novo (ainda não confirmado, ou outra tela/resolução): ao iniciar a sessão com a TV como tela de jogo e a janela do app na outra tela, a pergunta "Está vendo esta tela?" aparece **na TV**, em tela cheia e por cima de tudo, e não na janela do app. Resultado: ______
- [ ] Com as outras telas ligadas ("Deixar ligadas"), a pergunta vai mesmo assim para a tela de jogo, não para a que o app estava. Resultado: ______
- [ ] A ("Sim, estou vendo"): a pergunta some, o iniciador abre na TV e o app sai da frente (minimizado), como antes. B ("Não, voltar") ou 15 s sem resposta: as telas voltam como estavam e o app volta para a frente, como janela normal. Resultado: ______
- [ ] App aberto com `--fullscreen`: a pergunta também vai para a tela de jogo e, depois da sessão, o app volta em tela cheia na tela de antes. Resultado: ______
- [ ] App na bandeja (escondido): ao iniciar pelo link/atalho, a pergunta aparece na tela de jogo, e depois o app volta para a bandeja/fica escondido como antes. Resultado: ______
- [ ] O controle responde à pergunta (A/B) sem precisar tocar na janela; o mouse também funciona nos botões. Resultado: ______

## Console Mode 2 Alpha: mapa das telas e listas de opções

- [ ] Sessão → Telas mostra as telas na posição e proporção reais (como nas configurações de tela do Windows); uma tela desligada no Windows aparece à direita, com "Desligada no Windows".
- [ ] A numa tela abre a lista: Tela de jogo / Desligar na sessão / Deixar ligada; a tela muda de cor na hora (jogo em destaque, desligar tracejada e apagada).
- [ ] Trocar a tela de jogo: a antiga fica com o papel que a nova tinha; a tela de jogo não pode ser só desligada ou mantida (opções apagadas).
- [ ] Setas andam entre as telas pela posição; ↓ na última linha de telas vai para "Confirmar a imagem ao iniciar".
- [ ] Iniciar uma sessão de verdade e conferir que apagou só as telas marcadas para desligar.
- [ ] Qualquer opção com várias escolhas (Sessão e Sistema) abre uma lista no canto direito da linha: ↑↓ escolhe, A confirma, B fecha; com o mouse, clicar fora fecha. Listas longas rolam.
- [ ] Atalhos: as combinações que outro atalho já usa aparecem apagadas e não podem ser escolhidas.
- [ ] Ao andar pelos cards da Início com o controle, os outros cards não tremem.

### 2.0: refino visual (profundidade, fontes e ícones)
- [ ] Texto sem franjas coloridas (antialias em tons de cinza), na TV e no monitor
- [ ] Textos pequenos (dicas, notas dos cards, rótulos do Início) mais firmes que antes; títulos e relógio com o corte Display
- [ ] Ícones dos cards (Steam, Playnite, Xbox, apps próprios) com borda lisa, sem serrilhado, também em 4K / escala 150 %
- [ ] Cards com sombra e um brilho suave em cima; o focado com sombra maior e um halo
- [ ] Botões A/B/LB/RB, pílula das abas e chave liga/desliga com relevo (sombra e brilho)
- [ ] Linha focada em Sessão/Sistema e item focado das listas com relevo; listas e cartões com sombra
- [ ] Sessão/Sistema: luz suave da cor do tema no canto de cima; cantos escurecendo (vinheta) em todas as telas com fundo
- [ ] Texto sobre a arte (Início) legível em capas claras, com sombra suave
- [ ] Desempenho: navegar pelos cards continua liso (sem queda visível)

### 2.0: Início no estilo PS5 (proposta B)
- [ ] Abas como palavras à esquerda (a atual em branco), LB/RB pequenos; à direita controle/bateria, botões Sessão, Sistema e menu (☰), relógio
- [ ] Fileira de quadrados arredondados logo abaixo das abas; o focado cresce com anel branco e o nome (e a nota) embaixo
- [ ] Andar pela fileira: os cards seguintes não tremem; a fileira acompanha o foco
- [ ] Capas de jogos da Steam (retrato) recortadas no quadrado, com cantos arredondados
- [ ] Destaque: logo/nome grande, informações, botão "Jogar" (abre) e "…" (opções, só em jogos e apps próprios)
- [ ] Opções do card (Y) abrem ao lado do card focado
- [ ] Mouse: clique nas abas, nos botões redondos, no "Jogar", no "…"; setas da fileira e roda do mouse
- [ ] Dentro de uma categoria do Sistema, clicar na aba "Sistema" (ou em "Sistema ›" no título) volta para a lista

Teste lado a lado com a sua cópia: `CM2_INSTANCE=teste` dá ao app de teste mutex e pipe próprios, e
`CM2_WINDOW_AT=10000,10000` abre a janela fora da tela (capturável com PrintWindow).

### 2.0: aberturas PS1, PS2, PS3, Xbox e Xbox One
- [ ] Sistema → Aparência → Abertura lista as 8 opções (Console Mode, PS1–PS4, Xbox, Xbox 360, Xbox One, Nenhuma); "Ver a abertura" mostra a escolhida
- [ ] PS1: a tela sobe do preto ao branco passando pelo cinza; a logo entra grande em degradê laranja-amarelo, o prompt dela logo depois, o nome em azul embaixo; parado com o som, depois corte seco para o preto e a logo menor em vermelho/amarelo com o nome branco
- [ ] PS2: torres de cubos de vidro 3D crescendo bloco a bloco na névoa azul, câmera girando devagar; no fim só a logo do Console Mode
- [ ] PS3: a onda de fios finos aparece no preto, torcendo e brilhando onde os fios se juntam; o fundo vai a um roxo profundo; logo e nome pequenos à direita do meio, em cima da onda
- [ ] Xbox: fitas grossas de energia verde girando em 3D em volta da esfera preta, estouro, logo do Console Mode verde na esfera e "CONSOLE MODE" espaçado embaixo
- [ ] Xbox One: preto, depois tudo branco (a esfera colada na câmera); ela recua sobre o fundo verde com chão mais claro, rola revelando "Console Mode" letra a letra e gira mostrando a logo verde na face, com uma sombra no chão
- [ ] Xbox 360: sala clara, faixas largas e translúcidas verdes/amarelas/brancas girando em 3D em volta de um orbe prateado, fechando até o clarão branco no último golpe do som; depois fundo verde-acinzentado, anel de luz se abrindo, logo no orbe prateado e "Console" verde + "Mode" cinza embaixo
- [ ] Cada estilo reproduz o som de abertura do Console Mode no volume configurado. B ou um clique pula a animação e interrompe o som.
- [ ] Sem estilo escolhido, de vez em quando o app abre de surpresa com uma das 7 inspiradas

### 2.0: menu da sessão — Janelas em miniaturas, Tela com HDR/VRR, Volume com a saída
- [ ] Janelas: com o botão em foco aparecem as miniaturas das janelas abertas (como Win + Tab), com nome do programa e título
- [ ] Com o botão Janelas em foco, ↑↓ escolhem a miniatura (anel branco); A traz a janela para a frente; X pede para fechar; ←→ continuam trocando de botão
- [ ] Miniaturas ao vivo (DWM, como o Alt + Tab): mexem junto com a janela; minimizada mostra o nome; jogo em DirectX aparece; abrir e fechar o menu muitas vezes não aumenta a memória
- [ ] Tela (numa sessão): tela de jogo e resolução; ↑↓ escolhem HDR ou VRR e A liga/desliga na hora
- [ ] VRR mudado pelo menu volta ao que era ao encerrar a sessão (e depois de um travamento, pela recuperação)
- [ ] Tela (fora de sessão): só mostra como está, com o aviso de que HDR e VRR mudam durante uma sessão
- [ ] Volume: ↑↓ ajusta; X troca a saída de áudio (também clicando na linha); ao encerrar, o áudio volta ao que era

### 2.0: ControlFS e JoyChromium no menu da sessão (botão Apps)
- [ ] Menu da sessão → Apps (penúltimo botão): duas linhas, "Explorador de arquivos (ControlFS)" e "Navegador (JoyChromium)"; ↑↓ escolhem, A abre
- [ ] Instalado: a linha diz "Navegue pelos seus arquivos/pela web com o controle"; A fecha o menu e o app abre por cima do jogo, na tela de jogo
- [ ] ControlFS: abre em tela cheia na tela de jogo (F11 enviado pelo Console Mode; se não pegar, fica maximizado); já aberto, só vem para a frente, sem segunda cópia
- [ ] JoyChromium: abre em tela cheia (`--tv`) e é levado para a tela de jogo se o Windows abrir em outra
- [ ] Ao encerrar a sessão com o JoyChromium aberto pelo menu, ele sai da tela cheia e fica aberto em janela (abas mantidas); um JoyChromium aberto por conta própria não é tocado
- [ ] Nenhum dos dois é fechado pelo Console Mode
- [ ] Não instalado (desinstale um deles para testar): a linha diz "Não instalado · aperte para baixar e instalar"; nada é baixado sem apertar
- [ ] A na linha: "Baixando… N%" com a barra, depois "Instalando…", depois "Instalado · aperte para abrir"; o próximo A abre
- [ ] Sem internet (ou instalação que falha): "Não deu para instalar · aperte para abrir a página de download"; o próximo A abre a página de releases no navegador; reabrindo o menu, a instalação é oferecida de novo
- [ ] O log registra o download verificado (SHA-256) e o resultado, em português
- [ ] Sistema → Sobre → Relatar um problema lista ControlFS e JoyChromium em Programas
- [ ] Tour do menu da sessão tem a parada "Apps"

### 2.0: atalho personalizado do controle (Gravar combinação…)
- [ ] Sistema → Atalhos: cada atalho tem "Gravar combinação…" no fim da lista
- [ ] Escolher com A abre o cartão "Segure os botões e depois solte…"; o A que escolheu (ainda apertado) não conta
- [ ] Segurar botões mostra a combinação ao vivo ("Select + Y (Create + △)"); soltar salva e a linha mostra a combinação nova (antes de "Gravar combinação…")
- [ ] O atalho gravado funciona (segurar dispara) e fica no config.json nas chaves da 1.7 (`homeShortcut`, `menuShortcut`, `exitShortcut`, `taskViewShortcut`)
- [ ] B sozinho cancela; Esc/Backspace no teclado e o botão "Cancelar" também; nada muda
- [ ] Um botão sozinho (ex.: A) é recusado com o aviso de usar dois ou mais botões (ou só o Xbox/PS)
- [ ] Combinação de outro atalho (ou que contém/está contida nela) é recusada com "… já é o atalho de …"
- [ ] Dois controles: só o primeiro que apertar é gravado; as combinações não se misturam
- [ ] Durante a gravação nenhum atalho dispara e o controle não mexe no app; depois, o atalho recém-gravado só dispara depois de soltar e segurar de novo
## Console Mode 2 Alpha: controle da TV (Google TV / Android TV, como na 1.7.1/1.8)

Pré-condições: TV Google TV / Android TV na mesma rede, com **Depuração USB** (ou "Depuração pela rede") nas Opções do desenvolvedor. Sistema → TV → *Google TV / Android TV (rede)*, endereço IP e entrada HDMI do PC. Testado sem TV: protocolo ADB contra uma TV falsa em 127.0.0.1, chave RSA (CNG) e DPAPI, telas de Sistema → TV fora da tela.

- [ ] Sistema → TV aparece entre Aparência e Inicialização, com "Controle da TV" e "Ao voltar ao PC"; um `config.json` da 1.7.1 com o bloco `tv` chega com os mesmos valores (provedor, IP, MAC, HDMI, comandos, espera).
- [ ] Endereço, MAC e os dois comandos abrem o teclado na tela com o valor atual; Enter/Salvar grava, Esc/B cancela sem mudar nada; comandos longos aparecem cortados com "…" ao lado da linha.
- [ ] **Testar a TV** na primeira vez: a TV pede "Permitir depuração" (o nome mostrado é `ConsoleMode2@<PC>`); com "Sempre permitir" + "Permitir", a TV acorda e vai para a entrada do PC; o aviso diz que a TV respondeu. A linha mostra "Testando…" enquanto isso. Resultado: ______
- [ ] **Testar a TV** de novo: não pede permissão; com a TV em espera, ela liga e troca a entrada. Resultado: ______
- [ ] Recusar na TV (ou esperar 60 s): o aviso diz que a TV não autorizou este PC. IP errado: o aviso diz que a TV não respondeu, em poucos segundos. Resultado: ______
- [ ] TV só com "Depuração sem fio" (pareamento por código): o aviso pede para ativar a Depuração USB / pela rede. Resultado: ______
- [ ] O arquivo `adbkey` na pasta de dados da 2.0 começa com `dpapi:`; nenhuma linha do log mostra a chave. Copiado para outro usuário do Windows: o log diz "chave ADB de outro usuário/PC; criando outra" e a TV pede permissão de novo. Resultado: ______
- [ ] **Jogar** com a TV em espera: ela liga e troca para o PC antes de a tela de jogo ser ligada; o log tem `TV: ligar (androidTv) ok em …s`. Resultado: ______
- [ ] TV fora da tomada: a sessão espera no máximo ~30 s pela TV e segue (log `TV: ligar ... falhou` ou `sem resposta em 30s`). Resultado: ______
- [ ] TV que some da rede em espera + MAC preenchido: o log mostra `enviando Wake-on-LAN` e a TV liga ("Ligar pela rede" ativo na TV). Resultado: ______
- [ ] TV que ignora a tecla HDMI: o **Comando da entrada** (ex.: o `am start …` da Skyworth) troca para o PC. Resultado: ______
- [ ] **Colocar a TV em espera ao restaurar** ligado: ao encerrar, as telas voltam e depois a TV entra em espera. Com **Comando ao voltar ao PC** = `input keyevent 3`: a TV vai para a tela inicial antes da espera (ou fica ligada, com a espera desligada). Log: `TV: voltar ao PC (androidTv) ok`. Resultado: ______
- [ ] Nada para enviar ao voltar (comando vazio, espera desligada): nenhuma linha `TV: voltar ao PC` no log; "Não controlar": nenhuma linha `TV:` e a sessão não demora mais. Resultado: ______
- [ ] Travamento no meio da sessão com espera ligada: na próxima abertura, a recuperação devolve as telas e depois coloca a TV em espera (o passo está no `session.json` como `tv`). Resultado: ______
- [ ] Sessão com `CM2_SIMULATED_DESK=1`: o log diz só "TV ligaria" / "TV ao voltar ao PC", nada é enviado à TV. Resultado: ______

### 2.0: app parado quase sem CPU (quadros sob demanda, som sob demanda)
- [ ] Gerenciador de Tarefas com o app parado na Início: CPU em 0%
- [ ] Animações continuam lisas: abertura, troca de card, troca de aba, listas, menu da sessão, passar o mouse
- [ ] Redimensionar, maximizar, minimizar e voltar: a janela redesenha certo
- [ ] Depois de ~30 s sem som, o próximo som de navegação ainda toca (a saída é reaberta na hora)
- [ ] Controle: o primeiro botão depois de uma pausa responde rápido; segurar e navegar continua imediato
- [ ] Bandeja e `consolemode2alpha://` respondem (podem levar até ¼ s)
