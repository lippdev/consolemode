# Console Mode 2 Alpha: teste em casa

Roteiro do primeiro teste ao vivo, do mais seguro para o mais arriscado. Anote ✅/❌ e o que viu.
Com problema, dá pra mandar o print e o fim de `%LOCALAPPDATA%\ConsoleMode2\consolemode2.log`.

## Rodada local de 23/11/2026

Windows x64, commit `6f16845`, executável debug. As caixas do roteiro abaixo continuam disponíveis para o teste completo com TV, jogo e controle físico.

- [x] Suíte Rust — 309 passaram e um utilitário de tradução ficou ignorado.
- [x] Controle virtual SDL3 — quatro execuções pelo leitor de entrada do app. Conexão e remoção, direcional, A/B/X/Y, LB/RB, Menu/View, analógico nas quatro direções, limiar, repetição e parada ao centralizar. Select + Y abriu o evento de menu uma vez e suprimiu Y. A gravação reconheceu A + B ao soltar. O teste observou os eventos entregues pelo leitor.
- [x] Interface — abertura, tour, abas Sessão e Sistema e opções de controles. A tela de jogo selecionada persistiu após reiniciar com o mesmo perfil de teste.
- [x] Sessão com telas e áudio simulados — deixar a confirmação expirar em 15 s retornou ao início. Em outra tentativa, foi possível confirmar, abrir o menu e encerrar. Nos dois casos a restauração terminou e o diário foi removido.
- [x] Playnite Fullscreen instalado — abriu pelo cartão, o app reconheceu o processo e, ao fechar a janela do Playnite, restaurou a mesa simulada e removeu o diário.
- [x] Bandeja — com a opção habilitada, a alpha iniciou oculta. Uma segunda execução mostrou a janela original. Fechar ocultou a janela e uma nova execução mostrou novamente a mesma janela no mesmo processo.
- [x] Layout real do Windows — o backup e a reaplicação do mesmo layout preservaram a tela principal, posição, resolução e frequência. Só a tela macbook estava ativa, em 2560 × 1600 a 60 Hz. O HyperX era a única saída de áudio ativa.
- [ ] Repetir na TV com troca real de telas e áudio, HDR/VRR, jogo em execução e recuperação após queda. Verificar também controle físico por USB/Bluetooth, bateria, botão central e interação com o jogo em foco.
- [ ] Validar o instalador de release. Nesta rodada, `build\Generate-Notices2.ps1 -Check` falhou porque o documento de avisos de terceiros está desatualizado.

## Validação da Home do modo Xbox em 23/11/2026

Windows 11 26H2, build 26300.9818, com Modo de Desenvolvedor já habilitado. A página **Jogos → Modo Xbox** ofereceu a opção de habilitar o modo, mas omitiu o seletor de Home neste desktop.

- [x] Registrar e remover a identidade da alpha por usuário. A ativação pelo Windows trouxe a janela da bandeja para tela cheia no processo existente.
- [x] Com o registro presente e a escolha original de Home preservada, abrir a alpha com sua preferência de entrada ligada e confirmar o diálogo do Windows. A API confirmou FSE ativa e o Console Mode recebeu o foco em 2560 × 1600.
- [x] Abrir o Playnite Fullscreen real, encerrá-lo e voltar à Home da alpha em primeiro plano, ainda na FSE. O diário foi removido. O perfil de teste simulava as alterações de telas e áudio.
- [x] Sair pelo menu do Console Mode. A API confirmou o retorno ao desktop, a janela voltou à geometria anterior e ficou oculta. Ao fim, a seleção original estava restaurada e o registro temporário removido.
- [x] Validar o SHA-256 do instalador gerado e a igualdade entre o executável de release preparado para distribuição e o usado no ciclo acima.
- [ ] Em um Windows que ofereça o seletor de Home, escolher a alpha e validar entrada pelo próprio Windows, inclusive ao iniciar o PC. Repetir a sessão com TV, saída HDMI e jogo real.

## Antes de começar o teste com TV
- Feche a 1.7 (bandeja → Sair). As duas não devem rodar sessão ao mesmo tempo.
- Abra: `C:\Users\LIPP\orca\consolemode\target\debug\ConsoleMode2.exe`
- A config da 2.0 já veio da 1.7: TV LG como tela de jogo, apagando as outras 2 telas, Playnite como padrão.
- **Se algo der errado nas telas:** espere os 15 s da confirmação (volta sozinho). Se o app travar, abra de novo: ele desfaz a sessão anterior ao iniciar. Último recurso: Win+P ou Configurações → Tela.

## 1. Abertura e interface (sem sessão)
- [ ] Animação de abertura no estilo da 1.7 (Xbox 360) com som
- [ ] Tour de primeira abertura aparece; A avança, B pula; não volta na próxima abertura
- [ ] Controle físico (Xbox e/ou DualSense): navega, LB/RB trocam aba, A abre, B volta
- [ ] Bateria do controle sem fio aparece na barra de cima, perto do relógio
  - [ ] Controle Bluetooth de outra marca (Luna, 8BitDo…): a bateria é a mesma de Configurações → Bluetooth (o log diz "bateria pelo Bluetooth do Windows")
  - [ ] DualSense por Bluetooth: continua a bateria do próprio controle
- [ ] Controle de outra marca: o botão central abre o Console Mode (atalho do botão Xbox / PS) e grava como esse botão em "Gravar combinação…"
  - [ ] O log diz "lendo o botão central pelo HID" e Relatar um problema lista as coleções HID do controle
  - [ ] Teclado com tecla Home (AC Home) não abre nada
- [ ] Mouse: clique foca, roda rola, clique direito abre opções, min/max/fechar funcionam
- [ ] Sessão: lista suas telas reais (TV, monitores), resoluções, áudio
- [ ] Sistema → Idioma: trocar para English e voltar para Português, na hora
- [ ] Sistema → Som: estilo e volume mudam de verdade
- [ ] Sistema → Sobre: "Abrir a pasta de registros" e "Licenças" abrem; "Relatar um problema" abre a issue (não precisa enviar)

## 2. Primeira sessão: Steam Big Picture
- [ ] Jogar na Steam Big Picture: a TV liga/vira principal, as outras 2 telas apagam
- [ ] Aparece a confirmação em tela cheia na TV; A confirma
- [ ] Resolução/frequência e HDR conforme o configurado em Sessão
  - [ ] Com @60 escolhido e a TV ficando em @59: a TV não pisca de novo e o log não diz "modo não aplicado"
  - [ ] Com @59 escolhido e a TV em @60: o modo é aplicado (a TV vai para @59)
- [ ] O som sai pela TV (se configurado em Sessão → Áudio)
- [ ] Mouse estacionado fora do caminho; ícones da área de trabalho não bagunçam
- [ ] Com Sessão → Ao encerrar → "Esconder os ícones da área de trabalho na sessão" ligado: os ícones somem durante a sessão e voltam ao PC
  - [ ] Com os ícones já escondidos antes (Exibir → Mostrar ícones desligado): continuam escondidos depois
  - [ ] Fechando o app à força no meio da sessão: na próxima abertura a recuperação mostra os ícones de novo
- [ ] Segurar ☰ (~1 s) abre o menu da sessão por cima do Big Picture
  - [ ] Na primeira vez, oferece o tour do menu
  - [ ] Janelas: lista e troca de janela
  - [ ] Volume: muda o volume real
  - [ ] Tela e som: troca a saída de áudio; HDR liga/desliga (X)
  - [ ] FPS: limite no RTSS (se tiver RTSS)
  - [ ] Visão de tarefas: abre e o controle navega nela
  - [ ] Bateria do controle aparece no menu
- [ ] Sair do Big Picture: telas, áudio, HDR e ícones voltam como estavam

## 3. Rollback (sem responder)
- [ ] Iniciar uma sessão e NÃO apertar nada na confirmação: em 15 s tudo volta sozinho

## 4. Playnite e Xbox
- [ ] Playnite em tela cheia abre na TV; jogar um jogo pelo Playnite não encerra a sessão
- [ ] Fechar o Playnite encerra e restaura
- [ ] Modo Xbox abre e encerra direito

## 5. Durante o jogo
- [ ] Abrir um jogo de verdade, segurar ☰: o menu aparece por cima do jogo e volta ao jogo com B
- [ ] Atalho "Voltar ao PC" (Sistema → Atalhos) encerra a sessão pelo controle
- [ ] VRR (se configurado): ativo no jogo e volta ao normal ao sair

## 6. Controle e bandeja
- [ ] Fechar a janela: o app fica na bandeja; ícone e menu da bandeja certos
- [ ] Atalho "Abrir o Console Mode" (Sistema → Atalhos) abre o app pelo controle
- [ ] "Entrar ao conectar o controle": desligar e ligar o controle abre o app
- [ ] "Usar o controle como mouse": funciona com o app fora de foco; A/B clicam
- [ ] "Controle acordar o PC" (Sistema → Controles): pede administrador e ativa (opcional)

## 7. Apps próprios
- [ ] Adicionar app (ex.: RetroArch ou outro emulador), jogar por ele e fechar: sessão encerra e restaura

## 8. Inicialização
- [ ] Iniciar com o Windows (Sistema → Inicialização): sobe na bandeja ao reiniciar o PC
