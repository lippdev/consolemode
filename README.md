# Console Mode

App em PowerShell + WinForms que transforma o PC em um console de jogos. Esconde monitores nao utilizados, define o monitor de foco, troca a saida de audio e abre o **Steam Big Picture** ou o **Modo Xbox** (Win+F11). Ao fechar o modo escolhido, restaura automaticamente monitores e audio.

## Requisitos

- Windows 10/11
- PowerShell 5.1 ou superior
- [MultiMonitorTool](https://www.nirsoft.net/utils/multi_monitor_tool.html) (ja incluido: `MultiMonitorTool.exe`)
- [SoundVolumeView](https://www.nirsoft.net/utils/sound_volume_view.html) (**obrigatorio para troca de audio**)
- Steam instalado (para modo Big Picture)
- Windows 11 com Modo Xbox habilitado (para modo Xbox; atalho Win+F11)

## Instalacao do SoundVolumeView

1. Acesse: https://www.nirsoft.net/utils/sound_volume_view.html
2. Baixe o pacote ZIP
3. Extraia `SoundVolumeView.exe` para a pasta do projeto:

```
C:\MultiMonitorTool\SoundVolumeView.exe
```

Sem esse arquivo, o app funciona normalmente, mas a troca de saida de audio ficara desabilitada.

## Como usar

1. Execute `IniciarConsoleMode.bat` ou rode `ConsoleMode.ps1` no PowerShell
2. Marque o **monitor de foco** (TV/console) com o radio button
3. Marque os monitores que deseja **esconder** (checkbox)
4. Escolha a estrategia:
   - **Cortinas pretas** (recomendado) - overlays pretos nos monitores
   - **Desligar via DDC/CI** - desliga fisicamente os monitores (requer suporte DDC/CI)
5. Escolha o modo de tela cheia:
   - **Steam Big Picture**
   - **Modo Xbox** (envia Win+F11)
6. Selecione a saida de audio (ou deixe "Nao mudar")
7. Clique em **Salvar configuracao** e depois **Iniciar modo console**

## Restaurar o setup

O app restaura automaticamente quando detecta o fechamento do Big Picture ou do Modo Xbox. Voce tambem pode restaurar manualmente:

- Botao **Restaurar agora** na janela
- Item **Restaurar setup** no icone da bandeja (system tray)
- Tecla **ESC** em qualquer cortina preta

A restauracao usa `backup_monitores.cfg` (salvo via MultiMonitorTool) e o audio padrao anterior.

## Arquivos do projeto

| Arquivo | Descricao |
|---------|-----------|
| `ConsoleMode.ps1` | App principal com interface grafica |
| `lib\Engine.ps1` | Motor: monitores, audio, cortinas, monitoramento |
| `config.json` | Preferencias salvas do usuario |
| `IniciarConsoleMode.bat` | Atalho para iniciar o app |
| `MultiMonitorTool.exe` | Controle de monitores (NirSoft) |
| `SoundVolumeView.exe` | Troca de audio (NirSoft - baixar separadamente) |
| `AbrirBigPicture.bat` | Script legado (referencia) |
| `telas_pretas.ps1` | Script legado (referencia) |

## Modo Xbox

O Modo Xbox (antiga "Experiencia de tela cheia") e acionado via atalho **Win+F11**. Requer Windows 11 com o recurso habilitado em Configuracoes > Jogos > Modo Xbox.

A deteccao de fechamento do Modo Xbox e best-effort (monitora processos e janelas relacionadas). Se a restauracao automatica nao ocorrer, use **Restaurar agora** ou ESC nas cortinas pretas.

## Scripts legados

Os arquivos `AbrirBigPicture.bat` e `telas_pretas.ps1` continuam na pasta como referencia da implementacao original. O novo app substitui e generaliza essa logica.

## Licenca das dependencias

- **MultiMonitorTool** e **SoundVolumeView** sao freeware da NirSoft (https://www.nirsoft.net)
- Este projeto de automacao e de uso pessoal
