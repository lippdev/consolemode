# Dependências de terceiros

O **Console Mode** usa as ferramentas abaixo. Algumas são baixadas automaticamente no build; outras devem ser instaladas pelo usuário.

## NirSoft (até a 1.5)

Até a versão 1.5, o Console Mode embutia o [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html) e o [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html), da NirSoft, para controlar monitores e áudio. A partir da 1.6 isso é feito com as APIs do próprio Windows e nenhuma ferramenta da NirSoft é distribuída (issue #91).

## Fonte

| Fonte | Site | Uso no projeto |
|-------|------|----------------|
| [Inter](https://rsms.me/inter/) 4.1 | GitHub (SIL Open Font License 1.1) | Embarcada (`src/ConsoleMode/Assets/Fonts/InterVariable.ttf`) como fonte da interface Console. A licença vai junto, em `Inter-OFL.txt` |

## Sons da interface

Embarcados no executável (`src/ConsoleMode/Assets/Sounds`), convertidos para WAV mono de 44,1 kHz com o volume igualado.

| Som | Origem | Licença |
|-----|--------|---------|
| `move.wav` (`glass_002`), `confirm.wav` (`confirmation_001`), `back.wav` (`minimize_008`) | [Interface Sounds](https://kenney.nl/assets/interface-sounds), da Kenney | CC0 1.0 (domínio público; crédito opcional) |
| `tab.wav` (`cursor_c`) | Gerado pelo [`tools/make_sounds.py`](https://github.com/mattxslv/Selene/blob/main/tools/make_sounds.py) do [Selene](https://github.com/mattxslv/Selene), de Matthew Silva (mattxslv): síntese pura, sem amostras | MIT, Copyright (c) 2026 Matthew Silva |

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
