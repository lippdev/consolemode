# Guia do Console Mode

Detalhes que não cabem no [README](../README.pt-BR.md).

[English](GUIDE.md) · Português (Brasil)

## Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |
| **Aplicativo personalizado** | Restauração automática quando o app (ou o processo escolhido para acompanhar) fecha |

Se o app fechar ou o PC desligar no meio de uma sessão, na próxima vez que o Console Mode abrir (também ao entrar no Windows, se ele inicia com o Windows) ele restaura tudo o que a sessão mudou: telas, áudio, ícones da área de trabalho e o ponteiro, e avisa.

**Aplicativo personalizado** abre qualquer `.exe` que você escolher (RetroArch, outro launcher…) a partir da própria pasta, com argumentos opcionais, e move a janela dele para a tela de jogo. A sessão termina alguns segundos depois que esse processo fecha. Dá para adicionar quantos quiser: cada um vira um bloco no Início com o ícone do programa (**+ Adicionar app** no fim da fileira), e em Sistema dá para trocar ou remover o selecionado. Se o exe só abre outro programa e fecha (um launcher intermediário), o Console Mode passa a acompanhar esse programa sozinho; **Processo a acompanhar** só é preciso quando ele errar (digite ali o nome do processo do programa de verdade). Se o processo acompanhado nunca aparecer, a sessão continua até você encerrá-la (segurar Start + Select, o menu da sessão ou a bandeja).

**Navegador (JoyChromium)** fica fixo no menu da sessão, logo abaixo do explorador de arquivos ControlFS: o [JoyChromium](https://github.com/lippdev/joychromium), o navegador para a TV que se usa com o controle, abre em tela cheia por cima do jogo, na tela da sessão. Se ele não estiver instalado, o primeiro clique baixa a versão mais recente (conferida pelo SHA-256 do GitHub) e instala, e o próximo abre. Voltar ao PC com ele ainda aberto o tira da tela cheia e o deixa como janela, com as abas.

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

No menu da sessão há duas saídas. **Voltar ao PC** encerra a sessão e devolve telas, resolução, HDR e áudio ao que eram; o app continua na bandeja. **Sair do Console Mode** faz o mesmo e depois fecha o app, então os atalhos do controle param até você abri-lo de novo. Na primeira vez que o menu abre, ele oferece um tour que explica cada opção; para vê-lo depois, aperte **Y** (△ no PlayStation) ou **F1** no menu, ou use **Sistema → App → Tour do menu da sessão**.

O menu também funciona **fora do modo console**, para usar o PC do sofá sem iniciar nada: com o Console Mode aberto (mesmo só na bandeja), o atalho do menu ou o ícone da bandeja o abrem na tela de jogo, se ela estiver ligada, ou na principal. Ali dá para trocar de janela, mexer no volume, na resolução, na saída de áudio e no HDR, abrir o ControlFS e o JoyChromium, esconder o cursor e controlar o mouse com o controle. O que você muda fica assim até ser trocado de novo, já que não há sessão para desfazer; o limite e o contador de FPS só aparecem numa sessão. Se o atalho atrapalhar algum jogo, desligue **Sistema → Controle → Menu da sessão fora do modo console** (a bandeja continua abrindo o menu).

O **atalho da visão de tarefas** faz pelo controle o que o Win + Tab faz: abre a visão de tarefas do Windows com todas as janelas abertas, numa sessão ou fora dela. Com ela aberta, o direcional ou o analógico esquerdo escolhe a janela (segurar repete), **A** abre e **B** fecha; o atalho de novo também fecha. Ele já vem ligado com **Select + X**; troque ou desligue em **Sistema → Controle → Atalho do controle para a visão de tarefas** (o botão Xbox sozinho também serve, segurado, se não estiver no atalho de abrir o Console Mode). Nas três primeiras vezes, um aviso mostra os botões antes de a visão abrir, porque nada aparece por cima dela. Com controle Xbox, quem move a seleção é o próprio Windows; com controle PlayStation, que o Windows não entende ali, é o Console Mode (desligue em **Navegar na visão de tarefas com controle PlayStation** se usar DS4Windows ou o Steam Input e os comandos se repetirem).

## A interface

O Console Mode tem uma interface só, feita como a de um console: ocupa a tela toda e funciona com controle Xbox ou PlayStation, mouse ou teclado (clique numa aba ou use Page Up / Page Down; Enter e Esc fazem o papel de A e B). Na primeira vez que você abre, um tour guiado passa pelas abas, pelos iniciadores, pelo botão de jogar, pelas telas, pelos ajustes rápidos e pelos atalhos do controle; cada parada avança sozinha depois do tempo de ler (a barra embaixo do cartão mostra quanto falta), **A** avança na hora e **B** pula. Para ver de novo: **Sistema → App → Tutorial**. Enquanto carrega, o app mostra uma abertura com o logo e um som de entrada; qualquer botão pula, de vez em quando, de surpresa, vem uma inspirada no Xbox 360 ou no PS4, com o som do console; **Sistema → App** escolhe um estilo (o que acaba com as surpresas), mostra a abertura na hora e desliga a abertura ou só o som.

O **Início** mostra os seus iniciadores numa fileira no topo (Steam Big Picture, Playnite, modo Xbox): **A** num deles escolhe o que abre, e **Jogar agora** logo abaixo começa. O bloco **+** no fim adiciona um aplicativo seu; num deles, **☰** (OPTIONS no PlayStation) ou o botão direito do mouse abre o menu dele, com **Remover da Início**. A linha embaixo do nome diz onde você vai jogar e o que muda (áudio, resolução, FPS, HDR, VRR); o botão redondo ao lado de Jogar agora abre a **Sessão**, com as telas e os ajustes rápidos.

A aba **Sistema** lista as categorias à esquerda (Tela de jogo, Ao entrar no modo console, Controle, TV, Extras opcionais, App, Atalho, ajuda e dados, Outros apps do desenvolvedor). Passar por uma categoria mostra as opções dela à direita; **A** ou a direita entra nelas, e **B** ou a esquerda volta para as categorias.

Em **Sistema → Controle**, escolha o layout dos botões: **Automático**, **Xbox** ou **PlayStation**. A prévia e os nomes dos atalhos acompanham a escolha, que fica salva ao fechar o app. Isso muda os símbolos exibidos; as entradas do controle mantêm o mapeamento existente.

A aba **Sistema** reúne todos os ajustes, incluindo os atalhos do controle, Playnite, os argumentos do aplicativo personalizado, o relatório de diagnóstico e a pasta de dados. Os valores permanecem salvos ao reabrir o app.

Abra a ação de atalho que deseja configurar, escolha **Definir** ou **Alterar**, segure a combinação desejada em um único controle e solte os botões. **Remover** desativa aquele atalho. Durante a captura, os botões não navegam pela interface; use **Cancelar** ou **Esc** para interrompê-la. Fora da captura, **B** volta do editor. Campos de endereço e FPS personalizado aceitam digitação por teclado.

Ao voltar do Big Picture, **Mandar a Steam para a bandeja ao voltar ao PC** fecha a janela do cliente e mantém a Steam em execução. Com a opção desligada, o cliente desktop fica aberto. As janelas da Steam são preservadas quando há jogo rodando ou não é possível verificar esse estado.

Trocar a tela principal pode fazer o Windows embaralhar os ícones da área de trabalho. **Devolver os ícones da área de trabalho ao lugar** (ligado por padrão) salva a posição deles antes da sessão e os coloca de volta depois que a mesa é restaurada. Não faz nada quando **Organizar ícones automaticamente** está ligado na área de trabalho, porque aí o próprio Windows os arruma. **Esconder os ícones da área de trabalho na sessão** (desligado por padrão) some com eles enquanto a sessão roda, para o visual limpo de console, e os mostra de novo ao voltar ao PC; ícones que já estavam escondidos continuam assim, e uma sessão interrompida os mostra de novo na próxima abertura.

**Tirar o ponteiro do mouse do caminho** (ligado por padrão): quando o iniciador aparece na TV, o ponteiro vai para o canto inferior direito da tela de jogo, e de novo alguns segundos depois, porque a Steam o recentraliza ao abrir o Big Picture. Ao voltar ao PC, ele volta para onde estava. Esconder o ponteiro de vez continua sendo uma escolha no menu da sessão.

**Acordar o PC pelo controle** deixa o adaptador sem fio Xbox, o Bluetooth ou o controle no cabo acordarem o PC da suspensão pelo botão central do controle. **Ativar** pede permissão de administrador ao Windows e marca *Permitir que este dispositivo ative o computador* nesses dispositivos. Não acorda o PC desligado ou em hibernação, e alguns PCs cortam a energia do Bluetooth ao suspender. Junto com **Entrar ao conectar um controle**, o PC já acorda no modo console.


## Extras opcionais

### HDR

Ativa HDR no monitor de foco enquanto o modo console estiver ligado. Ao sair, o estado anterior é restaurado.

### VRR

O Console Mode pode alterar a opção de VRR do Windows. Para melhor resultado, ligue também VRR / G-SYNC / FreeSync no **painel do driver da GPU** (NVIDIA ou AMD).

## Limitações

- Layouts multi-monitor variam; em alguns setups a restauração pode precisar de uma nova tentativa pela bandeja
- O Modo Xbox não detecta o fim do fullscreen — restaure manualmente
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

## Solução de problemas

### O layout da área de trabalho não foi restaurado

Abra o menu da bandeja e escolha **Restaurar setup**. Se o layout ainda estiver errado, escolha **Restaurar setup** novamente depois que o Windows terminar de aplicar a alteração do monitor. Você também pode reabrir a janela pela bandeja e restaurar manualmente.

### O áudio permaneceu na saída anterior

Confira se a saída de destino está conectada e disponível no Windows antes de iniciar o modo console. Para saídas HDMI/TV, pode ser necessário reconectar o cabo e iniciar o modo novamente.

### O controle aparece mas não faz nada (DualSense / DualShock)

Abra **Sistema → Atalho, ajuda e dados → Testar controle**: ele mostra ao vivo o que o Windows entrega de cada controle. Se a leitura fica vazia enquanto você aperta botões, quase sempre é o Steam capturando o controle (Steam aberto com suporte a PlayStation no Steam Input vira teclado/mouse no desktop). Feche o Steam, ou desligue o suporte a PlayStation no Steam Input, e teste de novo. Se continuar vazio, use **Copiar diagnóstico** e cole no formulário de feedback.

### HDR ou VRR não mudaram

Confirme se o monitor de foco é compatível com o recurso e se o HDR está ativado no Windows. Para VRR, ative também G-SYNC ou FreeSync no painel de controle da GPU quando aplicável.

## Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
# Compila os dois pacotes
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça o merge do PR de release na `main` e rode Actions → **Release** → Run workflow na `main` com a versão (como `1.4.0`, ou `1.4.0-beta.2` para pré-release) e **publish** marcado: o workflow cria a tag e a release no GitHub, gera os dois arquivos e os publica, e o app oferece a atualização. Sem "publish" ele só gera e guarda os arquivos como artefato. Fazer push de uma tag `v*` continua funcionando e faz o mesmo. Com **draft** marcado também, a release é criada como rascunho: só os mantenedores do repositório a veem, o app não oferece nada, nada vai para o winget e a tag ainda não é criada, para dar tempo de experimentar os arquivos; publicar o rascunho no GitHub faz dela uma release normal (e manda uma estável para o winget). **legacy** recebe uma branch (`release/1.7-legacy`) para gerar e anexar na mesma release como `ConsoleModeLegacy-*.exe`: as interfaces do Console Mode 1.6.2, congeladas, que a verificação de atualizações nunca oferece.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.
