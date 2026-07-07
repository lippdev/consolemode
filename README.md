# Console Mode

App em PowerShell + WinForms que transforma o PC em um console de jogos. Esconde monitores nao utilizados, define o monitor de foco, troca a saida de audio e abre o **Steam Big Picture** ou o **Modo Xbox** (Win+F11). Ao fechar o modo escolhido, restaura automaticamente monitores e audio.

## Release portatil (recomendado)

1. Baixe ou gere `dist\ConsoleMode.exe` (veja [Como gerar o executavel](#como-gerar-o-executavel))
2. Copie **apenas** `ConsoleMode.exe` para qualquer pasta
3. Execute com duplo clique

Na primeira execucao, o app cria ao lado do executavel:

```
ConsoleMode.exe
ConsoleMode_Data/
  tools/MultiMonitorTool.exe
  tools/SoundVolumeView.exe
  config.json
  backup_monitores.cfg
  ...
```

Nao precisa instalar. Configuracao e backups ficam em `ConsoleMode_Data`.

> **Antivirus:** executaveis gerados com ps2exe podem gerar falso positivo. O codigo-fonte esta neste repositorio para auditoria.

## Requisitos

- Windows 10/11
- PowerShell 5.1 ou superior (apenas para desenvolvimento; o `.exe` nao exige PowerShell instalado para o usuario final)
- Steam instalado (para modo Big Picture)
- Windows 11 com Modo Xbox habilitado (para modo Xbox; atalho Win+F11)

As ferramentas NirSoft (`MultiMonitorTool`, `SoundVolumeView`) vêm **embutidas no executavel** ou devem estar na pasta do projeto em modo dev.

## Como usar

O app abre um **assistente em 4 passos**:

1. **Monitores** — escolha o foco (TV/console), quais esconder e veja o diagrama do layout
2. **Modo** — estrategia para esconder + Big Picture ou Modo Xbox
3. **Audio** — saida de audio (ou "Nao mudar")
4. **Iniciar** — revise o resumo e clique em **Iniciar modo console**

Botoes uteis: **Salvar**, **Atualizar** (recarrega monitores/audio), **Restaurar agora**.

## Restaurar o setup

O app restaura automaticamente quando detecta o fechamento do Big Picture ou do Modo Xbox. Voce tambem pode restaurar manualmente:

- Botao **Restaurar agora** na janela
- Item **Restaurar setup** no icone da bandeja (system tray)
- Tecla **ESC** em qualquer cortina preta

A restauracao usa backup em `ConsoleMode_Data` (monitores + audio).

## Desenvolvimento

### Executar sem compilar

1. Coloque `MultiMonitorTool.exe` e `SoundVolumeView.exe` na raiz do projeto
2. Execute `IniciarConsoleMode.bat` ou:

```powershell
powershell -ExecutionPolicy Bypass -File .\ConsoleMode.ps1
```

Em dev, as ferramentas sao lidas da raiz; config e backups vao para `ConsoleMode_Data\`.

### Como gerar o executavel

1. Baixe e coloque na raiz do projeto:
   - [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html)
   - [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html)
2. Execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Build-ConsoleMode.ps1
```

3. O executavel sera gerado em `dist\ConsoleMode.exe`

Testes do build:

```powershell
.\build\Build-ConsoleMode.ps1 -TestDev   # valida paths em modo dev
.\build\Build-ConsoleMode.ps1 -TestExe   # valida layout do exe
```

## Arquivos do projeto

| Arquivo | Descricao |
|---------|-----------|
| `ConsoleMode.ps1` | Ponto de entrada |
| `lib\Paths.ps1` | Paths portateis (dev vs exe) |
| `lib\Engine.ps1` | Motor: monitores, audio, cortinas, monitoramento |
| `lib\Gui.ps1` | Interface wizard WinForms |
| `build\Build-ConsoleMode.ps1` | Gera `dist\ConsoleMode.exe` via ps2exe |
| `assets/` | Identidade visual: `icon.ico`, logos PNG, etc. |
| `IniciarConsoleMode.bat` | Atalho para desenvolvimento |
| `AbrirBigPicture.bat` | Script legado (referencia) |
| `telas_pretas.ps1` | Script legado (referencia) |

## Modo Xbox

O Modo Xbox e acionado via atalho **Win+F11**. Requer Windows 11 com o recurso habilitado em Configuracoes > Jogos > Modo Xbox.

A deteccao de fechamento do Modo Xbox e best-effort. Se a restauracao automatica nao ocorrer, use **Restaurar agora**.

## Licenca das dependencias

- **MultiMonitorTool** e **SoundVolumeView** sao freeware da [NirSoft](https://www.nirsoft.net), redistribuidos embutidos no executavel portatil conforme termos do autor
- Este projeto de automacao e de uso pessoal
