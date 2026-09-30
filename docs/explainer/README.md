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

Grava `out/keynote.mp4`.

## Filme curto (`Launch`)

Versão de ~32 s para redes sociais, com edição própria (não é o `Keynote` acelerado): um clique apaga a mesa e liga a TV, a câmera entra na TV e o jogo toma a tela, o menu Select + Y abre sobre a corrida, as specs passam uma por batida, sair do jogo traz a mesa de volta, e depois Home, launchers e o final.

O jogo é `src/RacingGame.tsx`, uma corrida synthwave desenhada em SVG (sem imagens de jogos de terceiros). Tudo nele é função do frame, então passar o frame global do filme mantém a mesma corrida entre as cenas.

```console
npm run launch
```

Grava `out/launch.mp4` e `out/launch-x.mp4` (H.264 yuv420p + AAC, faststart), que é o arquivo para postar.

## Novidades da 1.6.0-alpha.3 (`Alpha3`)

Vídeo de ~46 s em inglês com o que entrou na alpha.3: o novo menu da sessão sobre a corrida (painel lateral, janelas abertas, ControlFS), a interface Console redesenhada (Home, Session e System), o fundo com capas da Steam, os atalhos do controle escolhidos pelo usuário e as correções. As telas seguem o XAML do app (tamanhos em DIPs num palco de 1536 × 864, ampliado 1,25×) e usam as strings de `Strings.en-US.json`. As capas são inventadas, sem arte de jogos reais.

```console
npm run alpha3
```

Grava `out/alpha3-x.mp4` (H.264 yuv420p + AAC, faststart).

As trilhas dos filmes são sintetizadas por `scripts/soundtrack.py` (sem áudio de terceiros) no mesmo andamento das cenas (120 BPM: 1 tempo = 15 frames). Os momentos de cada filme ficam em `TIMELINES` no script. Se mudar os tempos em `src/Keynote.tsx`, `src/Launch.tsx` ou `src/Alpha3.tsx`, ajuste o script e regenere os MP3 em `public/keynote/`:

```console
pip install numpy scipy
npm run soundtrack
```

## Tour dos recursos (`Features`)

GIF de ~36 s com os recursos principais da 1.5.0, uma cena por recurso, no mesmo visual do `Keynote`. Sai em inglês e em português (prop `lang`):

```console
npm run features      # assets/features.gif (README.md)
npm run features:pt   # assets/features.pt-BR.gif (README.pt-BR.md)
```

Os textos ficam em `T` em `src/Features.tsx`. Os componentes compartilhados com o `Keynote` estão em `src/kit.tsx`.
