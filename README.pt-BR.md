# Console Mode

🇺🇸 [Read in English](README.md)

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, esconda monitores extras, ajuste o áudio e abra a interface de jogos em tela cheia que você preferir.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![License](https://img.shields.io/badge/licença-MIT-green)

> [!NOTE]
> **Vem mais melhoria por aí.** Estou trabalhando ativamente em novos recursos e correções. As próximas já estão na [1.5.0-beta.11](https://github.com/lippdev/consolemode/releases/tag/v1.5.0-beta.11). Sua opinião define o que vem depois: use o **botão de feedback** no aplicativo ou [envie seu feedback aqui](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Veja [Feedback](#feedback).

<img src="assets/console-mode.gif" alt="Os monitores da mesa desligam para a TV no HDMI; ao sair do Big Picture, a mesa volta sozinha." width="800">

## Funcionalidades

- **Tela inicial de 1 clique**: escolha a tela onde você joga e clique em **Jogar agora**
- **Atalho na Área de Trabalho** (`ConsoleMode.exe --start`) que entra direto no modo console, com o app aguardando na bandeja
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- **Resolução e Hz** opcionais por monitor no modo console
- Abrir **Steam Big Picture**, **Playnite em tela cheia** ou **Modo Xbox** (Win+F11)
- **HDR** opcional no monitor de foco e **VRR** (ajuste do Windows)
- **Limite de FPS** opcional via RivaTuner (RTSS)
- Roteamento de áudio, inclusive “usar ao conectar” (ex.: HDMI da TV)
- Ícone na bandeja para restaurar o layout ou reabrir o app
- **Instalador** (por usuário, sem admin) ou **executável portátil em um único `.exe`** — os dois avisam das versões novas pelo GitHub
- Interface em **português do Brasil**, **inglês** ou **espanhol**, seguindo o idioma do Windows por padrão e trocável em Ajustes sem reiniciar. Adicionar um idioma é um arquivo JSON ([#14](https://github.com/lippdev/consolemode/issues/14))
- App nativo **C# / WinUI 3** (Windows App SDK), sem MSIX e self-contained

## Por que não só…?

- **Escolher o monitor nos ajustes do Playnite / Big Picture?** Isso leva a interface para a TV, mas os monitores da mesa continuam ligados, o áudio fica nas caixas da mesa e depois você desfaz tudo na mão.
- **Usar um add-on de trocar tela?** Eles mudam o monitor principal. O Console Mode também desliga ou cobre as outras telas, manda o áudio para a TV, ajusta resolução, HDR, VRR e limite de FPS se você quiser, e devolve tudo ao sair.
- **Win+P "Somente segunda tela"?** Funciona para um setup fixo. O Console Mode lembra qual tela é a TV, abre Steam, Playnite ou Xbox e, numa tela nova, pergunta "Está vendo esta tela?" e desfaz tudo sozinho se ninguém responder.

## Requisitos

- Windows 10 ou 11
- Um dos modos que você for usar:
  - [Steam](https://store.steampowered.com/) (Big Picture — recomendado)
  - [Playnite](https://playnite.link/) (app em tela cheia)
  - Xbox / Game Bar no Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — opcional, só se quiser o limite de FPS (em geral via MSI Afterburner)

## Como usar

1. Baixe em [Releases](https://github.com/lippdev/consolemode/releases):
   - **`ConsoleMode-Setup-x64.exe`** (recomendado) — instala por usuário, sem admin, com menu Iniciar e, se quiser, atalho de 1 clique e iniciar com o Windows; dados em `%LOCALAPPDATA%\ConsoleMode`
   - **`ConsoleMode-Portable-x64.exe`** — um único exe que guarda os dados em `ConsoleMode_Data\` ao lado dele
2. Na primeira vez, um tour curto mostra o mapa das telas: escolha a tela onde você joga (as outras desligam)
3. Depois é 1 clique: **Jogar agora**, ou o atalho **Console Mode 1 Click** na Área de Trabalho (Ajustes → Criar atalho). Na primeira vez com uma tela de jogo nova, a TV pergunta "Está vendo esta tela?" e tudo volta sozinho se ninguém responder (mouse, teclado ou controle de Xbox/PlayStation)
4. Ou nem encoste no PC: com o app na bandeja (ligue **Iniciar com o Windows**), **segure o botão Xbox do controle por 1 segundo** e o modo console entra direto do sofá. **Segure Start + Select por 1 segundo** durante a sessão para voltar ao PC. **Segure Select + Y** durante a sessão para um menu sobre o jogo: volume, resolução, saída de áudio, limite de FPS, HDR, voltar ao PC ou sair (não funciona sobre jogos em tela cheia exclusiva). Só controles Xbox (XInput) para o botão Home. Em Ajustes dá para trocar por um **toque curto**: aí o app desliga o atalho do controle para a Game Bar no Windows (Win+G continua funcionando); se a Steam estiver aberta, desligue também "Botão Guide foca a Steam" na Steam
5. Automação: os links `consolemode://start`, `consolemode://stop`, `consolemode://show` e `consolemode://menu` funcionam em Stream Deck, launchers, scripts ou qualquer atalho. Ferramentas que também precisam saber o estado (um agente de controle remoto, um plugin de Stream Deck) podem usar a API de controle local abaixo
6. Duas interfaces: a **Desktop** (mouse, mapa de telas) e a **Console** (tela cheia, cartões grandes, D-pad/analógico + A/B, funciona com controles Xbox e PlayStation). Por padrão o app escolhe Console sempre que há um controle conectado; mude em Ajustes → Interface ou pelo botão de troca em qualquer uma das telas
4. Ao terminar, saia do Big Picture / Playnite (ou restaure manualmente no Modo Xbox)
5. O app avisa das versões novas pelas releases do GitHub (instalado: atualiza sozinho; portátil: troca o exe)

> **Antivírus:** alguns scanners podem sinalizar as ferramentas auxiliares. O código-fonte está neste repositório para auditoria.

## Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça push de uma tag como `v1.4.0` (ou `v1.4.0-beta.2` para pré-release): o workflow `Release` gera e publica os dois arquivos, e o app oferece a atualização.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.

## Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

## API de controle local

Os links `consolemode://` não dão resposta. Ferramentas que precisam de uma — um agente de controle remoto rodando como serviço do Windows, um plugin de Stream Deck que mostra se o modo console está ligado — podem usar o named pipe `\\.\pipe\ConsoleMode.Control` com o app aberto: envie uma linha JSON e receba outra.

```
→ {"cmd":"status"}          // ou "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` e `show` fazem o mesmo que o link `consolemode://` correspondente e respondem quando o app terminou (`ok:false` com `error` se o modo console não entrou ou a restauração não terminou). `status` só consulta. Só o usuário logado e o LocalSystem conseguem conectar; nada fica exposto na rede.

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
- Monitores e áudio dependem das ferramentas [NirSoft](https://www.nirsoft.net/) incluídas no pacote
- O limite de FPS é global (limitação do RTSS), não por tela
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

## Solução de problemas

**O controle aparece mas não faz nada (DualSense / DualShock):** abra **Ajustes → Testar controle**; ele mostra ao vivo o que o Windows entrega de cada controle. Se a leitura fica vazia enquanto você aperta botões, quase sempre é o Steam capturando o controle (Steam aberto com suporte a PlayStation no Steam Input vira teclado/mouse no desktop). Feche o Steam, ou desligue o suporte a PlayStation no Steam Input, e teste de novo. Se continuar vazio, use **Copiar diagnóstico** e cole no formulário de feedback.

## Roadmap

<img src="assets/roadmap.gif" alt="Roadmap do Console Mode: colunas Next, Later e Exploring." width="800">

São planos, não promessas: a ordem pode mudar conforme o feedback. Vote com 👍 na issue ou [sugira algo](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml).

**Próximos**
- 1.5.0 estável, fechando as betas
- Overlay de FPS, ligado pelo menu da sessão ([#66](https://github.com/lippdev/consolemode/issues/66))
- Gravar os últimos 30 segundos pelo menu da sessão
- Salvar e restaurar a posição dos ícones da área de trabalho ([#30](https://github.com/lippdev/consolemode/issues/30))

**Depois**
- Perfis por jogo: resolução, HDR e áudio para cada jogo ([#69](https://github.com/lippdev/consolemode/issues/69))
- Bateria do controle no menu da sessão ([#70](https://github.com/lippdev/consolemode/issues/70))
- Publicar no winget ([#13](https://github.com/lippdev/consolemode/issues/13))
- Mais idiomas na interface ([#14](https://github.com/lippdev/consolemode/issues/14))

**Explorando**
- Widget na Xbox Game Bar ([#11](https://github.com/lippdev/consolemode/issues/11))
- Comandos de voz e assistentes: Alexa, MCP ([#12](https://github.com/lippdev/consolemode/issues/12))
- Integração com o WakeOn: ligar o PC já no modo console ([#71](https://github.com/lippdev/consolemode/issues/71))
- Controle pelo celular usando a API de controle local ([#72](https://github.com/lippdev/consolemode/issues/72))
- HDMI-CEC: ligar a TV e trocar para a entrada do PC ([#75](https://github.com/lippdev/consolemode/issues/75))
- Suporte a Linux e macOS ([#74](https://github.com/lippdev/consolemode/issues/74))

## Feedback

Encontrou um bug ou tem uma ideia? É o feedback que define as próximas melhorias, então todo relato conta.

- **Pelo aplicativo** (1.5.0-beta.1 em diante): clique no botão de feedback, ao lado de **Ajustes** na tela inicial. Ele abre uma issue no GitHub que já traz a versão do aplicativo, se é instalado ou portátil, a versão do Windows e o idioma da interface. É só contar o que aconteceu ou o que você gostaria, anexar prints se quiser e enviar.
- **Sem o aplicativo:** [abra o formulário de feedback](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml) direto.

Para enviar, é preciso uma conta gratuita no GitHub. Nada é enviado automaticamente: nem logs, nem dados pessoais, e você vê o texto inteiro antes de enviar. Pode escrever em português ou em inglês.

Se o Console Mode te poupou da dança dos monitores, uma ⭐ no repositório ajuda outras pessoas a encontrá-lo.

## Licença

Licença [MIT](LICENSE).

Avisos de terceiros: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
