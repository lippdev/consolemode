# Console Mode 2 Alpha — checklist da migração

Alpha interna para Windows x64 na branch `v2-config` do GitHub. Atualizado em 26/11/2026.

## Resultado dos testes

- O teste local confirmou o registro da Home e o aviso quando o Windows repõe o Xbox como Home. Falta comprovar a abertura direta de um launcher em um Windows que mantenha a Home da alpha. A FSE é experimental e opcional.
- O roteiro [Teste em casa](TESTE-EM-CASA.md) registra o que foi observado com Steam, Playnite, controle virtual e mesa simulada. Os testes físicos abaixo continuam pendentes.

## Feito

### Base
- [x] Plano, decisões e spike GPUI (GPUI fixado, SDL3 para controles, rodio para som)
- [x] Mockups e redesign aprovados (Início, Sessão, Sistema, opções do card, menu da sessão)
- [x] Workspace Rust: cm-core, cm-platform, cm-ui, cm-app
- [x] Isolado da 1.7: pasta de dados, mutex, pipe, protocolo `consolemode2alpha://` e inicialização próprios
- [x] Config no mesmo formato JSON, com import somente leitura da 1.7
- [x] Mesmo ícone da 1.7 (decisão do dono, 2026-11-17)

### Interface
- [x] Janela normal (min/max/fechar), tela cheia opcional, tudo clicável com mouse
- [x] Navegação por controle, teclado e mouse; clique direito abre menus
- [x] Transições suaves; cards com visual próprio da Steam, Playnite e Xbox (ícones oficiais)
- [x] Jogos da Steam na Início (opcional, desligado por padrão)
- [x] Telas de Sessão e Sistema completas, ajustes por jogo dentro das opções do card
- [x] Tour de primeira abertura e "Ver o tour de novo"
- [x] Sons de navegação (estilo Console/Suave, volume) e som de abertura
- [x] Temas Lua, Âmbar e Jade
- [x] Interface em 6 idiomas (pt-BR, en, es, de, fr, ru), troca na hora em Sistema → Idioma; fr/ru/de pedem revisão de nativo
- [x] Sistema → Sobre: relatar problema, pasta de registros, licenças; log na pasta de dados

### Sessão
- [x] Engine com journal e recuperação após travamento
- [x] Telas reais: foco na TV, apagar telas, resolução, HDR, áudio (CCD + Core Audio)
- [x] Tela de confirmação com volta automática em 15 s
- [x] Steam Big Picture, Playnite, modo Xbox e apps próprios (vários, com monitoramento de processo)
- [x] Regras de saída da 1.7 (Playnite, Big Picture, apps próprios)
- [x] Extras: limite e contador de FPS (RTSS), mouse estacionado, ícones da área de trabalho, Steam na bandeja, perfis por jogo

### Menu da sessão e controles
- [x] Segurar ☰ abre o menu por cima do jogo
- [x] Janelas, Tela e som, volume real, FPS, Visão de tarefas, encerrar sessão
- [x] Atalhos globais pelo controle (abrir app, menu, voltar ao PC, Visão de tarefas)
- [x] Visão de tarefas guiada pelo controle (PlayStation)
- [x] Controle como mouse
- [x] Abrir o app ao conectar o controle

### App e entrega
- [x] Bandeja, instância única, inicialização com o Windows, pipe de controle
- [x] Atualizações próprias da 2.0 (canal separado) e instalador local
- [x] Avisos de terceiros e guia de release

## Recursos portados da 1.7
- [x] Bateria do controle na interface (barra de cima, menu da sessão e aviso de bateria fraca; lida pelo SDL3)
- [x] Controle acordar o PC (ControllerWake): leitura testada ao vivo; ativar (UAC) igual à 1.7, ainda sem teste ao vivo
- [x] Tour do menu da sessão (SessionMenuTour; chave `sessionMenuTourDone` da 1.7)
- [x] Animação de abertura (BootIntro): os três estilos da 1.7 e mais PS1, PS2, PS3, Xbox e Xbox One (som de abertura do Console Mode em todos os estilos), `introAnimation`/`introSound`/`introStyle` da 1.7, escolha e prévia em Sistema → Aparência
- [x] VRR / recursos de vídeo (VideoFeaturesService): VRR na sessão com backup no diário e restauração (também na recuperação), HDR pelo menu da sessão
- [x] Notas da versão ao atualizar (ReleaseNotes): cartão uma vez, com o primeiro item das notas do release ou só a versão e o link
- [x] Relatório de diagnóstico/feedback dentro do app: reúne versão, Windows, telas, áudio, controles, programas e o fim do log (sem dados privados), copia e salva, e abre a issue; nada é enviado sozinho
- [x] ControlFS e JoyChromium: botão "Apps" no menu da sessão (abre por cima do jogo na tela de jogo; instala sob pedido com SHA-256 verificado; JoyChromium volta a janela ao encerrar); sem teste ao vivo
- [x] Tela de confirmação em tela cheia na tela do jogo (por cima de tudo, no retângulo da tela de jogo; depois o app sai da frente como antes)
- [x] Item "Visão de tarefas" do menu da sessão guiado pelo controle (mesmo driver do atalho global)
- [x] Captura de atalho personalizado: "Gravar combinação…" em Sistema → Atalhos (regras e formato da 1.7); sem teste ao vivo com controle
- [x] Controle de TV (Google TV / Android TV por ADB na rede, como na 1.7.1/1.8): ligar e trocar a entrada ao iniciar, Wake-on-LAN, comando e espera ao voltar (no diário, também na recuperação), Testar a TV com pareamento; chave em `adbkey` com DPAPI. Testado contra uma TV falsa local; falta o teste ao vivo

## Testes ao vivo pendentes (no seu PC)
- [ ] TV ligada, apagar telas, trocar modo, HDR, trocar áudio
- [ ] Controle de TV com uma Google TV / Android TV de verdade (docs/TESTING.md)
- [ ] Big Picture e Playnite de verdade
- [ ] Controles físicos (Xbox e DualSense), atalhos, controle como mouse
- [ ] Ícone da bandeja, opções que mexem no registro, seletor de arquivo
- [ ] Checklists da 2.0 em `docs/TESTING.md`

## Depois
- [ ] Linux (KDE Plasma Wayland / CachyOS)
- [x] Build e testes de Windows na branch dedicada da alpha. Distribuição interna por rascunho.
- [ ] Validar o canal de atualização antes de uma pré-release pública.
