# Roteiro de testes manuais

Tudo aqui precisa de hardware ou de olho humano e **não é coberto pelos testes automáticos** (`dotnet test`). Marque `[x]` e anote o resultado. Quando algo falhar, abra uma issue com o formulário de feedback (`consolemode://` não precisa; o botão de feedback já preenche o ambiente).

Como testar em build de desenvolvimento: `dotnet build src/ConsoleMode -c Debug -r win-x64` e rode `src/ConsoleMode/bin/Debug/net8.0-windows10.0.19041.0/win-x64/ConsoleMode.exe`. Os dados ficam em `ConsoleMode_Data` ao lado do exe (`config.json`, `consolemode.log`).

## 1. Botão Home do controle (PR #27)

Pré-condições: controle **Xbox** (XInput) conectado; app na bandeja (minimizado ou aberto com `--tray`); sessão inativa.

- [ ] **Segurar o botão Xbox por 1 s** entra no modo console (mesmo caminho do atalho "1 Click"). Resultado: ______
- [ ] **Toque curto** no botão Xbox não faz nada no app (só a Game Bar abre, se o atalho dela estiver ligado). Resultado: ______
- [ ] Com a sessão **ativa** (Big Picture aberto), segurar o botão Xbox **não** reinicia nada. Resultado: ______
- [ ] **Segurar Start + Select por 1 s** durante a sessão restaura a mesa (igual a "Restaurar agora"). Resultado: ______
- [ ] Desligar "Abrir com o botão Home do controle" em Ajustes: nenhum dos gestos acima funciona. Resultado: ______
- [ ] O log (`consolemode.log`) mostra `Controle: botão Home` / `Controle: Start + Back` a cada disparo. Resultado: ______
- [ ] Controle **PlayStation**: confirmar que o Home **não** funciona na bandeja (limitação documentada) e que o app não registra erro. Resultado: ______

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

## 1c. Menu da sessão (Select + Y)

Pré-condições: sessão ativa com Big Picture (ou jogo borderless) na tela; controle Xbox ou PlayStation.

- [ ] Segurar **Select + Y** por ~0,3 s abre o menu centralizado na tela de jogo, por cima do Big Picture, com foco em "Voltar ao jogo". Resultado: ______
- [ ] Select + Y de novo (ou B, ou Esc) fecha o menu e o jogo continua onde estava. Resultado: ______
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

## 4. Regressões

- [ ] Interface Desktop: mapa de telas, `Segmented`, chips, tour de 3 passos e Ajustes continuam como antes. Resultado: ______
- [ ] O tour **não** aparece na interface Console na primeira execução. Resultado: ______
- [ ] Prompt "Está vendo esta tela?" na TV continua respondendo a A/B do controle e a Esc. Resultado: ______
- [ ] Fechar a janela durante a sessão vai para a bandeja; fora da sessão fecha o app. Menu da bandeja: Mostrar, Entrar, Restaurar, Sair. Resultado: ______
- [ ] Atualização: com uma release mais nova no GitHub, o aviso aparece nas duas interfaces (na Console, como banner no topo). Resultado: ______
- [ ] Atalho "Console Mode 1 Click" e `--tray` no início do Windows continuam funcionando. Resultado: ______
