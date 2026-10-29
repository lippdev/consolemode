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

- [ ] Padrão ("Aurora"): fundo azul-escuro com brilhos suaves verde (alto, à direita), azul (embaixo, à direita) e roxo (no alto), e três linhas finas em arco à direita; o lado esquerdo fica escuro e o texto continua legível. Sem faixas nem serrilhado nos degradês, na TV e no monitor. Resultado: ______
- [ ] Um `config.json` com `consoleBackground: "auto"` (o padrão das versões anteriores) abre com a Aurora; quem quiser as capas escolhe "Capas da Steam". Resultado: ______
- [ ] "Capas da Steam": colagem de capas dos jogos instalados sob o gradiente, o texto continua legível; sem Steam ou sem capas, só o gradiente. Resultado: ______
- [ ] Sistema → Plano de fundo alterna Aurora / Capas da Steam / Só gradiente / Minha imagem; "Minha imagem" abre o seletor de arquivo e usa a imagem escolhida; cancelar sem imagem volta ao modo anterior. Resultado: ______
- [ ] Na aba Sistema o fundo (Aurora, capas ou imagem) escurece com transição, e as linhas de opções ficam legíveis por cima de qualquer capa; ao voltar para Início ou Sessão, o fundo clareia de novo. Resultado: ______
- [ ] Aba Sistema numa TV larga (1920 px ou mais) e na janela pequena: a lista de opções começa logo ao lado das categorias, nenhuma linha passa da borda direita e o valor (em verde) fica todo visível; textos longos (alemão, russo) quebram em até duas linhas. A linha "Limpar" fica alinhada com as outras. Resultado: ______
- [ ] Aba Sistema com o mouse: clicar em cada categoria (de Tela de jogo a Outros apps) mostra as opções dela, também depois de entrar nas opções de outra categoria pelo controle; o clique só escolhe a categoria, sem pular para a primeira opção. Pelo controle, A numa categoria entra nas opções e ← volta para a categoria delas. Resultado: ______
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
- [ ] Interface Desktop: ícone e "Console Mode" à esquerda, botões de janela altos; em Ajustes, o botão de voltar aparece e funciona. Tema claro e escuro do Windows: os botões seguem o tema. Resultado: ______
- [ ] Trocar entre Desktop e Console (botão ou Y): a barra muda junto, sem sobrar faixa nem botões brancos no tema claro. Resultado: ______
