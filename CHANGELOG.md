# Histórico de alterações

Notas em português do Brasil; a versão em inglês (Estados Unidos) fica em `CHANGELOG.en-US.md`. Antes de publicar uma versão, adicione uma seção `## [VERSÃO]` com o changelog daquela versão **nos dois arquivos**. O workflow publica as duas seções correspondentes à tag na mesma release e falha se faltar alguma.

## [Unreleased]
### Novidades
- Ajustes → "Testar controle": mostra ao vivo o que o Windows entrega de cada controle (origem, botões por índice, D-pad, analógicos) e copia um diagnóstico para a issue. O log de abertura lista os controles. Para quem o controle aparece mas não responde (DualSense com Steam aberto, por exemplo).

### Correções
- Controle: um dispositivo que falha na leitura não silencia mais os outros; controles que o Windows expõe como Gamepad sem XInput responder passam a ser lidos (antes eram pulados); DualSense/DualShock via HID caem no mesmo caminho quando o XInput está vazio.
- API de controle local: com o app aberto, o named pipe `\.pipeConsoleMode.Control` recebe uma linha JSON (`status`, `start`, `stop`, `show`) e responde com o estado (ativo, restaurando, modo, versão). Só o usuário logado e o LocalSystem conectam. Por @nextestudios. (#24)
- `ConsoleMode.exe --stop` restaura a mesa pela linha de comando, como o `consolemode://stop`. Por @nextestudios. (#23)

## [1.5.0-beta.6]
### Novidades
- Interface Desktop navegável pelo controle: D-pad/analógico movem, A ativa (listas, interruptores, cards), B fecha a lista ou volta de Ajustes, Start abre/fecha Ajustes, Y vai para a interface Console; no tutorial, A avança e B pula. Baseado no PR #17 de @nextestudios.

### Correções
- O controle deixava de responder de vez se um controle desconectasse no meio de uma leitura; agora a leitura só pula a amostra. Também vindo do PR #17. (#47)
- Navegação por controle: o primeiro toque só mostra o anel de foco; em listas roladas o foco cai na ordem de tabulação quando a busca espacial não acha nada; diagonais no D-pad contam como vertical; a leitura só vale com a janela em primeiro plano (verificação por janela, não por evento).
- Menu da sessão: com o jogo aberto, segure Select + Y para abrir um menu sobre o jogo com volume, resolução, saída de áudio, limite de FPS, HDR, "Voltar ao PC" e "Sair do Console Mode". Também por `consolemode://menu`.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.5]
### Novidades
- Interface Console: os ajustes de lista (abrir em, áudio, resolução, FPS, como desligar, idioma, interface) abrem um seletor com todas as opções, com foco na atual; A escolhe, B volta. Antes era preciso ir apertando A para ciclar.
- Na interface Desktop, com um controle detectado, aparece "aperte A para o modo console"; A ou Start trocam a interface.
- Interface em espanhol. Sem escolha salva, o app segue o idioma do Windows (português, espanhol ou inglês). Adicionar um idioma agora é só incluir um `Strings.<código>.json`.

### Correções
- O modo Automático não via controles que o Windows lista com atraso (PlayStation, Bluetooth) e abria em Desktop. Agora o app detecta de novo quando um controle aparece e também 1,5 s e 4 s após abrir.
- Na interface Console, o controle às vezes não respondia até trocar de tela: o listener não ligava na primeira vez.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.4]
### Novidades
- Opção "Entrar ao conectar um controle" (desligada por padrão): com o app na bandeja, ligar ou conectar um controle entra no modo console. Ignora os primeiros 15 s após abrir e 30 s após restaurar.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.3]
### Novidades
- Ajustes → "Pasta do Playnite": escolha o Playnite.FullscreenApp.exe manualmente para instalações portáteis, que a detecção automática não encontra.

### Correções
- O idioma escolhido no instalador agora vale para o aplicativo. Sem escolha salva, o app segue o idioma do Windows (português → pt-BR; qualquer outro → inglês) em vez de abrir sempre em português.

### Versão beta
- Pré-release: quem usa a 1.5.0-beta.1 ou beta.2 recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.2]
### Novidades
- Segurar o botão Xbox do controle por 1 segundo, com o app na bandeja, entra no modo console sem tocar no PC. Durante a sessão, segurar Start + Select por 1 segundo volta ao PC. Só controles Xbox (XInput); pode ser desligado em Ajustes.
- Opção "Toque curto no botão Xbox": o app desliga o atalho do controle para a Game Bar e avisa se a Steam também estiver usando o botão.
- Interface Console: cartões grandes em tela cheia, navegados pelo controle (D-pad ou analógico, A, B, Start, Y) ou pelo teclado, com ajustes rápidos que trocam de valor ao apertar A. Em Ajustes → Interface escolha Automático (Console quando há controle), Desktop ou Console; um botão na tela troca na hora.
- Links `consolemode://start`, `consolemode://stop` e `consolemode://show` para automação (Stream Deck, launchers, scripts).

### Versão beta
- Pré-release: quem usa a 1.5.0-beta.1 recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.1]
### Novidades
- Botão de feedback na tela inicial: abre uma issue no GitHub já preenchida com a versão do aplicativo e do Windows, para enviar sugestões e relatar bugs.
- O aviso de nova versão mostra as novidades no idioma da interface.
- O instalador pergunta o idioma, já sugerindo o do Windows. As atualizações feitas pelo aplicativo mantêm a escolha anterior.

### Correções
- Os itens do menu do ícone na bandeja voltaram a funcionar.
- O aplicativo espera a tela do jogo ligar antes de desligar as demais.
- A janela comum da Steam não é mais confundida com o Big Picture.
- As informações do resumo na tela inicial quebram linha em vez de ficarem cortadas.
- O atalho de um clique agora se chama "Console Mode 1 Click".

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas.
- Depois de atualizar, o aplicativo também avisa sobre as próximas betas, além da 1.5.0 final.

## [1.4.0]
### Novidades
- Interface em inglês (Estados Unidos), além do português do Brasil. O idioma é escolhido em Ajustes e muda na hora, sem reiniciar o aplicativo.

### Correções
- Os nomes das resoluções ("Não alterar", "(cache)", "(estimado)") e os botões Ativado/Desativado agora acompanham o idioma escolhido.
- Resoluções guardadas em cache não perdem mais a indicação "(cache)" quando aparecem junto das resoluções estimadas.
- O aviso de nova versão mostra o primeiro item das novidades, e não mais o título "Novidades".

### Como atualizar
- Quem usa a 1.3.0 recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. A versão instalada se atualiza sozinha e a portátil troca o próprio arquivo e reabre.
- As configurações são mantidas. O idioma continua em português até você escolher **English** em Ajustes.

## [1.3.0]
### Novidades
- Interface nativa para escolher em qual tela jogar e o que fazer com as demais.
- Organização das telas conforme a posição real delas na Área de Trabalho.
- Navegação com controle, tutorial inicial e confirmação para ajudar a evitar uma tela sem imagem.
- Ajustes para resolução, taxa de atualização, áudio, HDR, VRR e limite de quadros por segundo.
- Instalador por usuário, sem exigir permissões de administrador, além da versão portátil.
- Aviso de novas versões pelo GitHub, com atualização para as versões instaladas e portáteis.
