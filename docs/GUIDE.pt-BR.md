# Guia do Console Mode

Detalhes que não cabem no [README](../README.pt-BR.md).

[English](GUIDE.md) · Português (Brasil) · [Español](GUIDE.es-ES.md) · [Deutsch](GUIDE.de-DE.md) · [Français](GUIDE.fr-FR.md) · [Русский](GUIDE.ru-RU.md)

## Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |
| **Aplicativo personalizado** | Restauração automática quando o app (ou o processo escolhido para acompanhar) fecha |
| **JoyChromium** | Restauração automática quando você fecha o JoyChromium; voltando ao PC antes, ele fica aberto como janela |

**Aplicativo personalizado** abre qualquer `.exe` que você escolher (RetroArch, outro launcher…) a partir da própria pasta, com argumentos opcionais, e move a janela dele para a tela de jogo. A sessão termina alguns segundos depois que esse processo fecha. Se o exe só abre outro programa e fecha (um launcher intermediário), digite o nome do processo do programa de verdade em **Processo a acompanhar**. Se o processo acompanhado nunca aparecer, a sessão continua até você encerrá-la (segurar Start + Select, o menu da sessão ou a bandeja).

**JoyChromium** aparece como iniciador quando o [JoyChromium](https://github.com/lippdev/joychromium), o navegador para a TV que se usa com o controle, está instalado (encontrado na pasta de instalação do usuário ou pela entrada do desinstalador). Ele abre em tela cheia na tela de jogo com `--tv`, sem mudar a preferência salva dele, e a sessão termina alguns segundos depois que você o fecha (Start → *Fechar o JoyChromium*). Voltar ao PC com ele ainda aberto o tira da tela cheia e o deixa como janela, com as abas.

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

No menu da sessão há duas saídas. **Voltar ao PC** encerra a sessão e devolve telas, resolução, HDR, áudio e limite de FPS ao que eram; o app continua na bandeja. **Sair do Console Mode** faz o mesmo e depois fecha o app, então os atalhos do controle param até você abri-lo de novo. Na primeira vez que o menu abre, ele oferece um tour que explica cada opção; para vê-lo depois, aperte **Y** (△ no PlayStation) ou **F1** no menu, ou use **Ajustes → Tour do menu da sessão**.

## Configurações no modo Console

O **Início** mostra os seus iniciadores numa fileira no topo (Steam Big Picture, Playnite, modo Xbox): **A** num deles escolhe o que abre, e **Jogar agora** logo abaixo começa. A linha embaixo do nome diz onde você vai jogar e o que muda (áudio, resolução, FPS, HDR, VRR); o botão redondo ao lado de Jogar agora abre a **Sessão**, com as telas e os ajustes rápidos.

A aba **Sistema** lista as categorias à esquerda (Tela de jogo, Ao entrar no modo console, Controle, TV, Extras opcionais, App, Atalho, ajuda e dados, Outros apps do desenvolvedor). Passar por uma categoria mostra as opções dela à direita; **A** ou a direita entra nelas, e **B** ou a esquerda volta para as categorias.

Em **Sistema → Controle**, escolha o layout dos botões: **Automático**, **Xbox** ou **PlayStation**. A prévia e os nomes dos atalhos acompanham a escolha, que fica salva ao fechar o app. Isso muda os símbolos exibidos; as entradas do controle mantêm o mapeamento existente.

A aba **Sistema** oferece os mesmos ajustes do modo PC, incluindo os atalhos do controle, Playnite, Android TV, FPS personalizado e pasta de dados. Os valores são compartilhados entre as duas interfaces e permanecem salvos ao reabrir o app.

Abra a ação de atalho que deseja configurar, escolha **Definir** ou **Alterar**, segure a combinação desejada em um único controle e solte os botões. **Remover** desativa aquele atalho. Durante a captura, os botões não navegam pela interface; use **Cancelar** ou **Esc** para interrompê-la. Fora da captura, **B** volta do editor. Campos de endereço e FPS personalizado aceitam digitação por teclado.

Ao voltar do Big Picture, **Mandar a Steam para a bandeja ao voltar ao PC** fecha a janela do cliente e mantém a Steam em execução. Com a opção desligada, o cliente desktop fica aberto. As janelas da Steam são preservadas quando há jogo rodando ou não é possível verificar esse estado.

Trocar a tela principal pode fazer o Windows embaralhar os ícones da área de trabalho. **Devolver os ícones da área de trabalho ao lugar** (ligado por padrão) salva a posição deles antes da sessão e os coloca de volta depois que a mesa é restaurada. Não faz nada quando **Organizar ícones automaticamente** está ligado na área de trabalho, porque aí o próprio Windows os arruma.

**Acordar o PC pelo controle** deixa o adaptador sem fio Xbox, o Bluetooth ou o controle no cabo acordarem o PC da suspensão pelo botão central do controle. **Ativar** pede permissão de administrador ao Windows e marca *Permitir que este dispositivo ative o computador* nesses dispositivos. Não acorda o PC desligado ou em hibernação, e alguns PCs cortam a energia do Bluetooth ao suspender. Junto com **Entrar ao conectar um controle**, o PC já acorda no modo console.

**Perfis por jogo** dão a cada jogo da Steam sua própria resolução, HDR, limite de FPS e saída de áudio. Com o jogo rodando, abra o menu da sessão e escolha **Perfil do jogo → Salvar os ajustes atuais para…**: o que a tela de jogo usa naquele momento fica salvo. Da próxima vez que esse jogo abrir durante uma sessão, o Console Mode aplica o perfil e, quando o jogo fecha, a tela volta a ser como era. O jogo é detectado pela Steam, seja ele aberto pelo Big Picture, pelo Playnite ou pelo app Xbox. Os perfis salvos ficam em Ajustes → **Perfis por jogo**, onde dá para apagá-los (na interface Console: Sistema → Ao entrar no modo console).

## API de controle local

Os links `consolemode://` não dão resposta. Ferramentas que precisam de uma — um agente de controle remoto rodando como serviço do Windows, um plugin de Stream Deck que mostra se o modo console está ligado — podem usar o named pipe `\\.\pipe\ConsoleMode.Control` com o app aberto: envie uma linha JSON e receba outra.

```
→ {"cmd":"status"}          // ou "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` e `show` fazem o mesmo que o link `consolemode://` correspondente e respondem quando o app terminou (`ok:false` com `error` se o modo console não entrou ou a restauração não terminou). `status` só consulta. Só o usuário logado e o LocalSystem conseguem conectar; nada fica exposto na rede.

## Controle da TV

Ajustes → **TV** pode ligar a TV e trocar para a entrada HDMI do PC quando o modo console começa e, se você quiser, colocá-la em espera depois que a mesa volta. Uma TV que não responde nunca trava o modo console: o app registra no log e espera a tela de jogo como sempre.

Com o padrão, **Não controlar**, nada é enviado e nada é acrescentado ao iniciar nem ao restaurar. Com um caminho escolhido, a etapa da TV roda primeiro quando o modo console começa e tem limite de **30 segundos** (o pior caso, quando a TV não responde e o Wake-on-LAN é tentado); a espera opcional ao restaurar tem limite de **15 segundos**. Uma TV que já está ligada costuma responder em um ou dois segundos.

A maioria das placas de vídeo de PC não envia HDMI-CEC, então o app fala com a TV pela rede:

### Google TV / Android TV

TVs TCL, Sony, Hisense, Philips e outras com Google TV ou Android TV, via ADB (o protocolo de depuração do Android). Nada para instalar no PC.

1. Na TV: **Configurações → Sistema → Sobre**, aperte **Build do Android TV OS** 7 vezes para liberar as Opções do desenvolvedor.
2. **Configurações → Sistema → Opções do desenvolvedor**: ative **Depuração USB** (em algumas TVs, **Depuração pela rede** / **ADB pela rede**).
3. No Console Mode, escolha *Google TV / Android TV*, informe o IP da TV (Configurações → Rede na TV; reserve esse IP no roteador) e a entrada HDMI do PC.
4. Aperte **Testar agora**. A TV pergunta "Permitir depuração deste computador?": marque **Sempre permitir** e aperte **Permitir**.

A chave ADB deste PC fica guardada criptografada para o seu usuário do Windows (DPAPI), então a TV só pergunta uma vez. Se a pasta de dados for copiada para outro usuário do Windows ou outro PC, a chave não pode ser lida lá: o app cria outra e a TV pergunta "Permitir depuração?" de novo.

Para acordar, o app usa a tecla de despertar do Android e depois a tecla **HDMI 1-4**. Se a sua TV ignorar essa tecla, preencha **Comando da entrada** com qualquer comando de shell do Android que abra a entrada do PC. Se a TV sai da rede em espera, informe o **endereço MAC** para o app mandar Wake-on-LAN antes (a opção "Ligar pela rede" / "Wake on Wi-Fi" da TV precisa estar ativa).

A "Depuração sem fio" com código de pareamento (Android 11+ em celulares) é outro protocolo, com TLS, e não é suportada: use a depuração USB / pela rede.

## Extras opcionais

### HDR

Ativa HDR no monitor de foco enquanto o modo console estiver ligado. Ao sair, o estado anterior é restaurado.

### VRR

O Console Mode pode alterar a opção de VRR do Windows. Para melhor resultado, ligue também VRR / G-SYNC / FreeSync no **painel do driver da GPU** (NVIDIA ou AMD).

### Limite de FPS (RTSS)

Limita a taxa de quadros global durante o modo console (útil em TV 60 Hz). Exige RTSS instalado e em execução. O limite anterior volta ao sair.

## Limitações

- Layouts multi-monitor variam; em alguns setups a restauração pode precisar de uma nova tentativa pela bandeja
- O Modo Xbox não detecta o fim do fullscreen — restaure manualmente
- O limite de FPS é global (limitação do RTSS), não por tela
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

## Solução de problemas

### O layout da área de trabalho não foi restaurado

Abra o menu da bandeja e escolha **Restaurar setup**. Se o layout ainda estiver errado, escolha **Restaurar setup** novamente depois que o Windows terminar de aplicar a alteração do monitor. Você também pode reabrir a janela pela bandeja e restaurar manualmente.

### O áudio permaneceu na saída anterior

Confira se a saída de destino está conectada e disponível no Windows antes de iniciar o modo console. Para saídas HDMI/TV, pode ser necessário reconectar o cabo e iniciar o modo novamente.

### O controle aparece mas não faz nada (DualSense / DualShock)

Abra **Ajustes → Testar controle**: ele mostra ao vivo o que o Windows entrega de cada controle. Se a leitura fica vazia enquanto você aperta botões, quase sempre é o Steam capturando o controle (Steam aberto com suporte a PlayStation no Steam Input vira teclado/mouse no desktop). Feche o Steam, ou desligue o suporte a PlayStation no Steam Input, e teste de novo. Se continuar vazio, use **Copiar diagnóstico** e cole no formulário de feedback.

### HDR ou VRR não mudaram

Confirme se o monitor de foco é compatível com o recurso e se o HDR está ativado no Windows. Para VRR, ative também G-SYNC ou FreeSync no painel de controle da GPU quando aplicável.

## Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
# Baixa o rtss-cli e compila os dois pacotes
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça o merge do PR de release na `main` e rode Actions → **Release** → Run workflow na `main` com a versão (como `1.4.0`, ou `1.4.0-beta.2` para pré-release) e **publish** marcado: o workflow cria a tag e a release no GitHub, gera os dois arquivos e os publica, e o app oferece a atualização. Sem "publish" ele só gera e guarda os arquivos como artefato. Fazer push de uma tag `v*` continua funcionando e faz o mesmo.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.
