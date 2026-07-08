# Console Mode

Turn your Windows PC into a **game console** with one click: hide extra monitors, focus on your TV, switch audio, and launch **Steam Big Picture** or **Xbox Game Bar fullscreen** (Win+F11). When you exit, everything is restored automatically.

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- 4-step setup wizard (monitors → mode → audio → launch)
- Hide monitors via **disconnect**, **black overlays**, or **DDC/CI**
- Audio option to **use output when connected** (e.g. TV HDMI when it powers on)
- Automatic restore when Big Picture or Xbox fullscreen closes
- Portable executable (`ConsoleMode.exe`) or PowerShell dev mode
- System tray icon to restore or reopen the app

## Requirements

- Windows 10 or 11
- [Steam](https://store.steampowered.com/) (Big Picture mode)
- Xbox Game Bar on Windows 11 (optional; Win+F11 shortcut)
- PowerShell 5.1+ **for development/build only** — the `.exe` does not require PowerShell

## Quick start (executable)

1. Build or download `dist/ConsoleMode.exe` (see [Build](#build))
2. Run it — on first launch it creates `ConsoleMode_Data/` next to the executable
3. Follow the wizard and click **Start console mode**

> **Antivirus note:** executables built with [PS2EXE](https://github.com/MScholtes/PS2EXE) may trigger false positives. Source code is available here for review.

## Development

```powershell
# 1. Clone the repository
git clone https://github.com/lippdev/consolemode.git
cd consolemode

# 2. Download NirSoft tools (automated)
powershell -ExecutionPolicy Bypass -File .\build\Get-NirSoftTools.ps1

# 3. Run
.\IniciarConsoleMode.bat
# or
powershell -ExecutionPolicy Bypass -File .\ConsoleMode.ps1
```

In dev mode, config and backups are stored in `ConsoleMode_Data/`.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Build-ConsoleMode.ps1
```

Output: `dist/ConsoleMode.exe` (NirSoft tools and icon embedded).

Validate without compiling:

```powershell
.\build\Build-ConsoleMode.ps1 -TestDev
.\build\Build-ConsoleMode.ps1 -TestExe
```

## Project structure

```
consolemode/
├── ConsoleMode.ps1          # Entry point
├── IniciarConsoleMode.bat   # Dev launcher
├── assets/icon.ico          # App icon
├── lib/
│   ├── Encoding.ps1         # UTF-8
│   ├── Paths.ps1            # Portable paths (dev / exe)
│   ├── Engine.ps1           # Monitors, audio, restore
│   └── Gui.ps1              # WinForms wizard
└── build/
    ├── Build-ConsoleMode.ps1
    └── Get-NirSoftTools.ps1
```

## Restoring your setup

- **Automatic** — when Big Picture or Xbox fullscreen closes (app stays in the tray)
- **Manual** — *Restore now* button or tray menu
- **ESC** — on black overlays (overlay hide strategy)

## License

This project is licensed under the [MIT License](LICENSE).

Third-party dependencies: see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## Português (BR)

Transforme seu PC Windows em um **console de jogos** com um clique: esconda monitores extras, foque na TV, ajuste o áudio e abra o **Steam Big Picture** ou o **Modo Xbox** (Win+F11). Ao sair, tudo é restaurado automaticamente.

### Funcionalidades

- Assistente em 4 passos (monitores → modo → áudio → iniciar)
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- Áudio com opção **usar ao conectar** (HDMI da TV quando ligar)
- Restauração automática ao fechar Big Picture / Modo Xbox
- Executável portátil (`ConsoleMode.exe`) ou modo desenvolvimento via PowerShell
- Ícone na bandeja para restaurar ou reabrir o app

### Requisitos

- Windows 10 ou 11
- [Steam](https://store.steampowered.com/) (modo Big Picture)
- Modo Xbox no Windows 11 (opcional; atalho Win+F11)
- PowerShell 5.1+ **apenas para desenvolvimento/build** — o `.exe` não exige PowerShell instalado

### Uso rápido (executável)

1. Gere ou baixe `dist/ConsoleMode.exe` (veja [Build](#build))
2. Execute o arquivo — na primeira vez cria `ConsoleMode_Data/` ao lado dele
3. Siga o assistente e clique em **Iniciar modo console**

> **Antivírus:** executáveis gerados com [PS2EXE](https://github.com/MScholtes/PS2EXE) podem gerar falso positivo. O código-fonte está aqui para auditoria.

### Desenvolvimento

```powershell
git clone https://github.com/lippdev/consolemode.git
cd consolemode
powershell -ExecutionPolicy Bypass -File .\build\Get-NirSoftTools.ps1
.\IniciarConsoleMode.bat
```

Config e backups ficam em `ConsoleMode_Data/`.

### Restaurar o setup

- **Automático** — ao sair do Big Picture ou Modo Xbox (app fica na bandeja)
- **Manual** — botão *Restaurar agora* ou menu da bandeja
- **ESC** — nas cortinas pretas (estratégia cortinas)

### Licença

Projeto sob licença [MIT](LICENSE). Dependências externas: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
