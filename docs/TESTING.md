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

## 4. Regressões

- [ ] Interface Desktop: mapa de telas, `Segmented`, chips, tour de 3 passos e Ajustes continuam como antes. Resultado: ______
- [ ] O tour **não** aparece na interface Console na primeira execução. Resultado: ______
- [ ] Prompt "Está vendo esta tela?" na TV continua respondendo a A/B do controle e a Esc. Resultado: ______
- [ ] Fechar a janela durante a sessão vai para a bandeja; fora da sessão fecha o app. Menu da bandeja: Mostrar, Entrar, Restaurar, Sair. Resultado: ______
- [ ] Atualização: com uma release mais nova no GitHub, o aviso aparece nas duas interfaces (na Console, como banner no topo). Resultado: ______
- [ ] Atalho "Console Mode 1 Click" e `--tray` no início do Windows continuam funcionando. Resultado: ______
