# Console Mode explainer

Animação Remotion do caso típico: mesa com 2 monitores, HDMI longo até uma TV apagada; o Console Mode apaga a mesa e acende a TV.

```console
npm i
npm run studio
npm run gif
```

`npm run gif` grava `assets/console-mode.gif` na raiz do repositório.

## Novidades da 1.5 (`WhatsNew`)

Menu da sessão horizontal, aviso no início da sessão, controles de PlayStation, navegação nos Ajustes, atualização e os outros apps.

```console
npm run whats-new
```

Grava `assets/whats-new.gif` (renderiza um MP4 e converte com o ffmpeg, que precisa estar no PATH, para ficar em ~3–4 MB).

## Roadmap (`Roadmap`)

Quadro com as colunas Next, Later e Exploring. Os itens ficam em `src/Roadmap.tsx` (`COLUMNS`) e devem acompanhar a seção "Roadmap" do README.

```console
npm run roadmap
```

Grava `assets/roadmap.gif`.

## Filme de apresentação (`Keynote`)

Vídeo de ~70 s em 1920×1080, no estilo de keynote: fundo preto, tipografia grande, uma ideia por cena (um clique, a mesa volta, launchers, Home, menu Select + Y, HDR/VRR/FPS, automação, as duas interfaces, código aberto).

```console
npm run keynote
```

Grava `out/keynote.mp4`. A trilha é sintetizada por `scripts/soundtrack.py` (sem áudio de terceiros) no mesmo andamento das cenas (120 BPM: 1 tempo = 15 frames). Se mudar os tempos em `src/Keynote.tsx`, ajuste o script e regenere `public/keynote/soundtrack.mp3`:

```console
pip install numpy scipy
npm run soundtrack
```
