# Fase 0 — teste rápido do GPUI no Windows (2026-11-16)

App descartável em `spikes/gpui-spike` (e `spikes/sdl-probe` para controles). Rodado no PC do dono,
com uma tela só ativa (`DISPLAY5`, 2560×1600 a 175 %), sem controle físico: os controles foram
virtuais, criados pelo driver ViGEm já instalado (`padsim`).

## Resultado

| O que | Resultado | Como foi visto |
|---|---|---|
| GPUI compila e abre janela no Windows | ✅ | build do Zed fixado no commit `917559d` |
| Tela cheia na tela escolhida, escala 175 % | ✅ | viewport 1462×914 lógicos, escala 1.75 |
| Tela cheia cobrindo a barra de tarefas | ✅ depois do truque de foco | sem foco a barra fica por cima |
| Pegar o foco vindo de segundo plano | ✅ com `AttachThreadInput` (o mesmo da 1.7, `NativeWindows.cs`) | `SetForegroundWindow` sozinho devolve `false` |
| Arte da Steam de fundo + troca com fade | ✅ | captura após → → → mostra a arte do jogo em foco |
| Ícone oficial lido do `.exe` (Steam, Playnite) | ✅ 256 px | `PrivateExtractIconsW` → BGRA → `RenderImage` |
| Logo do app Xbox lido do pacote | ✅ 256 px | hoje via PowerShell (330 ms); no app real, API de pacotes |
| Controle Xbox (XInput) | ✅ | gilrs com backend `xinput` |
| Controle Xbox pelo backend padrão do gilrs (WGI) | ❌ nenhum botão chega | só "conectado" |
| Controle PlayStation pelo gilrs | ❌ | XInput não enxerga PS |
| **SDL3** (estático, compilado do código) | ✅ Xbox 360 **e** PS4, com o tipo certo, **sem janela, em segundo plano** | `sdl-probe` |
| Menu da sessão (janela `PopUp`) por cima de um jogo em tela cheia sem borda | ✅ abre em cima, pega o foco, controle navega | "jogo falso" em tela cheia + menu, → → A |
| Som de UI (wav da 1.7) | ✅ saída abre em 30 ms, som enfileirado | não ouvido (dono remoto) |
| Release: tamanho | 17,8 MB (sem SDL3) | |
| Release: até a primeira tela | 0,6 s (0,33 s são o logo Xbox via PowerShell) | |
| Release: memória / CPU parado | 90 MB de working set com 8 artes grandes / 2,6 % de um núcleo | |

## Decisões

- **GPUI pelo git do Zed, fixado num commit** (`917559da35dd4d626205da705b071c800c9c2553`). O `gpui` do
  crates.io (0.2.2) é de outubro de 2025 e a API mudou: hoje a app sobe por `gpui_platform::application()`.
  Atualizar o commit é uma tarefa explícita, nunca automática.
- **Controles: SDL3**, ligado estaticamente (feature `build-from-source-static`; o CMake vem do
  Visual Studio 2022). Lê Xbox e PlayStation, informa o tipo (para os ícones de botão certos) e
  funciona com outro app em foco, que é o caso do menu da sessão. O gilrs fica de fora.
- **Foco**: o mesmo truque da 1.7 (`AttachThreadInput` + `SetForegroundWindow`), via `HasWindowHandle`
  da janela do GPUI.
- **Ícones oficiais** lidos em tempo de execução dos apps instalados, nunca empacotados.
- **Som**: rodio 0.23 (`DeviceSinkBuilder::open_default_sink`, mixer compartilhado).

## Ficou para depois (e entra no TESTING da 2.0)

- Menu por cima de um **jogo de verdade** (sem borda) e do **Steam Big Picture**: a Steam não estava
  aberta e abrir faria login/atualização no PC do dono. Tela cheia **exclusiva** (DirectX antigo) não
  deixa nenhuma janela por cima, igual na 1.7.
- Controles **físicos**: DualSense por cabo e Bluetooth, Xbox sem fio.
- **Ouvir** os sons; latência do som ao mover o foco.
- TV 4K/HDR e escala da interface: o layout foi feito em 1920 de largura e cortou em 1462 lógicos;
  a 2.0 precisa da "escala do console" (proporcional à tela) desde o design system.
- CPU parado em 2,6 %: investigar (thread de controle a cada 16 ms, quadros inativos do GPUI).
- Janela `PopUp` transparente com cantos arredondados mostra uma borda quadrada leve.
- Uma linha verde de 1 px no topo apareceu numa das capturas em tela cheia e sumiu nas seguintes.
- Linux (KDE Wayland): `WindowKind::LayerShell` existe no GPUI para o menu; testar quando houver máquina.
