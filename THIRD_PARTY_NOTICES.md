# Dependências de terceiros

O **Console Mode** usa as ferramentas abaixo. Algumas são baixadas automaticamente no build; outras devem ser instaladas pelo usuário.

## NirSoft

| Ferramenta | Site | Uso no projeto |
|------------|------|----------------|
| [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html) | nirsoft.net | Monitores: ativar/desativar, primário, layout, mover janelas |
| [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html) | nirsoft.net | Listar e trocar saída de áudio padrão |

O script `build/Get-NirSoftTools.ps1` automatiza o download na hora do build.

Consulte os termos de uso e a licença de cada ferramenta no site do autor (Nir Sofer / NirSoft).

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
