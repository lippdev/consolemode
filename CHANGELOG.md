# Histórico de alterações

Notas em português do Brasil; a versão em inglês (Estados Unidos) fica em `CHANGELOG.en-US.md`. Antes de publicar uma versão, adicione uma seção `## [VERSÃO]` com o changelog daquela versão **nos dois arquivos**. O workflow publica as duas seções correspondentes à tag na mesma release e falha se faltar alguma.

## [Unreleased]
### Novidades
- Segurar o botão Xbox do controle por 1 segundo, com o app na bandeja, entra no modo console sem tocar no PC. Só controles Xbox (XInput); pode ser desligado em Ajustes.
- Links `consolemode://start`, `consolemode://stop` e `consolemode://show` para automação (Stream Deck, launchers, scripts).

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
- Esta é uma versão de testes. Quem usa a 1.4.0 não recebe o aviso dela: para testar, baixe o instalador ou a versão portátil abaixo. Depois disso, o aplicativo avisa sobre as próximas betas e sobre a 1.5.0 final.

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
