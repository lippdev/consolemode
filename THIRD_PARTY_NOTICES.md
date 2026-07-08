# Dependências de terceiros

O **Console Mode** usa as ferramentas abaixo. Elas **não** fazem parte do código-fonte deste repositório e devem ser obtidas separadamente (o script `build/Get-NirSoftTools.ps1` automatiza o download na hora do build).

## NirSoft

| Ferramenta | Site | Uso no projeto |
|------------|------|----------------|
| [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html) | nirsoft.net | Monitores: ativar/desativar, primário, layout, mover janelas |
| [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html) | nirsoft.net | Listar e trocar saída de áudio padrão |

Consulte os termos de uso e a licença de cada ferramenta no site do autor (Nir Sofer / NirSoft).

## Build

| Ferramenta | Site | Uso no projeto |
|------------|------|----------------|
| [PS2EXE](https://github.com/MScholtes/PS2EXE) | GitHub | Compilar `ConsoleMode.ps1` em `dist/ConsoleMode.exe` |

O arquivo `build/ps2exe.ps1` é baixado automaticamente pelo script de build quando necessário.
