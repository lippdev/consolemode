# Dependências de terceiros

O **Console Mode** usa as ferramentas abaixo. Algumas são baixadas automaticamente no build; outras devem ser instaladas pelo usuário.

## NirSoft (até a 1.5)

Até a versão 1.5, o Console Mode embutia o [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html) e o [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html), da NirSoft, para controlar monitores e áudio. A partir da 1.6 isso é feito com as APIs do próprio Windows e nenhuma ferramenta da NirSoft é distribuída (issue #91).

## Limite de FPS (opcional)

| Ferramenta | Site | Uso no projeto |
|------------|------|----------------|
| [RivaTuner Statistics Server (RTSS)](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) | guru3D | **Instalado pelo usuário** (via MSI Afterburner). Aplica o limite de FPS global |
| [rtss-cli](https://github.com/xanderfrangos/rtss-cli) | GitHub (MIT) | CLI embarcada no build (`build/Get-RtssCli.ps1`) para controlar o RTSS |

O Console Mode **não** redistribui o RTSS — apenas o `rtss-cli.exe`.

## Build

| Ferramenta | Site | Uso no projeto |
|------------|------|----------------|
| [PS2EXE](https://github.com/MScholtes/PS2EXE) | GitHub | Compilar `ConsoleMode.ps1` em `dist/ConsoleMode.exe` |

O arquivo `build/ps2exe.ps1` é baixado automaticamente pelo script de build quando necessário.
