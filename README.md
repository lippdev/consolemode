# Console Mode

Transforme seu PC Windows em um **console de jogos** com um clique: esconda monitores extras, foque na TV, ajuste o áudio e abra o **Steam Big Picture** ou o **Modo Xbox** (Win+F11). Ao sair, tudo é restaurado automaticamente.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)
![License](https://img.shields.io/badge/license-MIT-green)

## Funcionalidades

- Assistente em 4 passos (monitores → modo → áudio → iniciar)
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- Áudio com opção **usar ao conectar** (HDMI da TV quando ligar)
- Restauração automática ao fechar Big Picture / Modo Xbox
- Executável portátil (`ConsoleMode.exe`) ou modo desenvolvimento via PowerShell
- Ícone na bandeja para restaurar ou reabrir o app

## Requisitos

- Windows 10 ou 11
- [Steam](https://store.steampowered.com/) (modo Big Picture)
- Modo Xbox no Windows 11 (opcional; atalho Win+F11)
- PowerShell 5.1+ **apenas para desenvolvimento/build** — o `.exe` não exige PowerShell instalado

## Uso rápido (executável)

1. Gere ou baixe `dist/ConsoleMode.exe` (veja [Build](#build))
2. Execute o arquivo — na primeira vez cria `ConsoleMode_Data/` ao lado dele
3. Siga o assistente e clique em **Iniciar modo console**

> **Antivírus:** executáveis gerados com [PS2EXE](https://github.com/MScholtes/PS2EXE) podem gerar falso positivo. O código-fonte está aqui para auditoria.

## Desenvolvimento

```powershell
# 1. Clone o repositório
git clone https://github.com/lippdev/consolemode.git
cd consolemode

# 2. Baixe as ferramentas NirSoft (automático)
powershell -ExecutionPolicy Bypass -File .\build\Get-NirSoftTools.ps1

# 3. Execute
.\IniciarConsoleMode.bat
# ou
powershell -ExecutionPolicy Bypass -File .\ConsoleMode.ps1
```

Em dev, config e backups ficam em `ConsoleMode_Data/`.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Build-ConsoleMode.ps1
```

Saída: `dist/ConsoleMode.exe` (ferramentas NirSoft e ícone embutidos).

Validação sem compilar:

```powershell
.\build\Build-ConsoleMode.ps1 -TestDev
.\build\Build-ConsoleMode.ps1 -TestExe
```

## Estrutura do projeto

```
consolemode/
├── ConsoleMode.ps1          # Entrada
├── IniciarConsoleMode.bat   # Atalho dev
├── assets/icon.ico          # Ícone do app
├── lib/
│   ├── Encoding.ps1         # UTF-8
│   ├── Paths.ps1            # Paths portáteis (dev / exe)
│   ├── Engine.ps1           # Monitores, áudio, restauração
│   └── Gui.ps1              # Wizard WinForms
└── build/
    ├── Build-ConsoleMode.ps1
    └── Get-NirSoftTools.ps1
```

## Restaurar o setup

- **Automático** — ao sair do Big Picture ou Modo Xbox (app fica na bandeja)
- **Manual** — botão *Restaurar agora* ou menu da bandeja
- **ESC** — nas cortinas pretas (estratégia cortinas)

## Licença

Este projeto está sob a licença [MIT](LICENSE).

Dependências externas: veja [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
