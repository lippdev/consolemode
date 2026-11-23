# Console Mode 2.0 — plano de execução

Port do Console Mode para **Rust + GPUI**, multiplataforma. Primeiro alvo: **Windows** e
**Linux com KDE Plasma (Wayland)** — CachyOS e afins.

## Regras desta fase

- **A 2.0 é outro produto em alpha, separado da 1.7.** Vive só na branch `v2`; nada da
  branch vai para o `main`, nenhum release, tag, workflow ou changelog da 1.7 é tocado.
- **Sem GitHub por enquanto:** a branch fica local, nada de push, nada de workflow rodando
  no GitHub. Builds e testes acontecem no PC do dono. O workflow de alpha só existe quando
  o dono pedir.
- **Convive com a 1.7 instalada no mesmo PC sem colidir:**
  - pasta de dados própria (`%LOCALAPPDATA%\ConsoleMode2` / `~/.local/share/consolemode2`);
  - não registra `consolemode://`, não usa o pipe `ConsoleMode.Control` nem o mutex
    `Local\ConsoleMode.Instance`, não cria entrada de inicialização com o Windows;
  - **lê** a config da 1.7 só para importar na primeira execução, nunca escreve nela;
  - recusa iniciar sessão se a 1.7 estiver com sessão ativa (consulta `status` no pipe
    da 1.7), para nunca haver dois apps mexendo nas telas.
- Testes de sessão no PC do dono seguem as regras de segurança de sempre: config validada
  antes do start, tela atual como foco, nada de desligar telas sem querer.

## Decisões

| Tema | Decisão |
|---|---|
| Ordem | **UI primeiro**: validar GPUI (foco por controle, escala na TV, animações, overlay) e ir plugando serviços |
| Visual | **Aprovado (2026-11-16):** [mockups/console-mode-2.html](mockups/console-mode-2.html). Início no estilo Selene (arte em tela cheia, abas LB/RB, uma fileira: iniciadores e depois jogos recentes da Steam); Sessão e Sistema no estilo Ajustes do PS4/PS5 (categorias → dois painéis); Y no card abre um menu ao lado com ações e "Ao abrir este jogo" (HDR, resolução, FPS); menu da sessão em ícones redondos; temas de cor; ícones oficiais lidos dos apps instalados |
| Modo de UI | Só console (TV + controle); mouse e teclado também navegam |
| Widgets | GPUI puro + componentes próprios; GPUI fixado em versão/commit exato |
| Linux | KDE Plasma Wayland primeiro; GNOME/Hyprland/X11 depois, como backends |
| Dados | Mesmo formato JSON de config; os 6 `Strings.*.json` reaproveitados como estão |
| MVP | Sessão base + launchers, controles + menu de sessão, updates + sons + tour |
| Depois do MVP | RTSS, ControlFS, JoyChromium, Game Bar, mouse por controle, TV (ADB) |

## Estrutura (proposta)

```
Cargo.toml                 workspace
crates/
  cm-app/                  binário: janela GPUI, telas, navegação
  cm-ui/                   design system: tokens, componentes, foco, animações, sons
  cm-core/                 lógica pura: config, engine de sessão, localização, regras (testes)
  cm-platform/             traits: Displays, Audio, Windows, Launch, Input, Tray, Update
  cm-platform-windows/     CCD, Core Audio, Win32, XInput/WGI, HID Sony
  cm-platform-linux/       KDE output-management/kscreen, PipeWire, evdev/SDL, layer-shell
src/ConsoleMode/           C# da 1.7 fica como referência até o port acabar
```

## Fases

**0. Mockups e spike (antes de qualquer tela real)**
- Mockups HTML: Home console, menu de sessão, Settings, tour — 2–3 direções, dono escolhe.
- Spike GPUI no Windows (Linux fica pra quando houver máquina): janela fullscreen na tela certa, escala 4K/150 %,
  controle movendo foco, som de UI, janela overlay sobre jogo em tela cheia
  (Windows: topmost; KWin: layer-shell). **Maior risco técnico: o overlay no Wayland.**
- Resultado: escolha da versão do GPUI e da lib de controle (SDL3 vs gilrs).

**1. Design system e shell do app** (`cm-ui`, `cm-app`)
- Tokens, tipografia, componentes (tile, lista, toggle, picker, toast), foco e navegação por
  controle/teclado/mouse, sons, localização carregando os JSON atuais.
- Home, Settings e tour navegáveis com dados falsos.

**2. Núcleo** (`cm-core`)
- Config compatível + import da 1.7, engine de sessão (start → foco → launcher → monitorar
  → restaurar → recuperação após crash), port das regras puras e dos testes delas
  (ControllerMapping, Shortcuts, SessionMenuMath, PlayniteExitPolicy, SteamArt, etc.).

**3. Plataforma Windows** — paridade com o essencial da 1.7: telas (CCD), áudio, Steam
Big Picture, Playnite, Xbox, apps custom, controles (Xbox + PlayStation), menu de sessão.

**4. Plataforma Linux (KDE Wayland)** — telas via output-management do KWin, áudio via
PipeWire, Steam Big Picture, apps custom, controles via evdev/SDL, menu via layer-shell.

**5. Updates, polimento e alpha**
- Auto-update próprio da 2.0 (canal separado; a 1.7 nunca enxerga versões 2.x).
- Empacotamento local: Windows (portable/instalador), Linux (AppImage; AUR depois).
- Só quando o dono pedir: workflow de alpha e pre-release `v2.0.0-alpha.N`.

## Em aberto

- ~~Visual~~: aprovado, ver Decisões.
- Jogos da Steam na fileira da Início: **opcional, desligado por padrão** (Sistema › Aparência › Início, decisão do dono 2026-11-16). Abrir a sessão direto no jogo (steam://rungameid) entra depois do MVP base.
- Lib de controle e suporte a DualSense no Linux (sai do spike).
- ~~Nome~~: **Console Mode 2 Alpha** (decidido).
- Linux: fases 0–3 só no Windows; o CachyOS entra depois (sem máquina de teste por enquanto).
