# Dependências de terceiros: Console Mode 2 Alpha

O **Console Mode 2 Alpha** (Rust + GPUI) é um produto separado da 1.7; a lista da 1.7 está em
[`THIRD_PARTY_NOTICES.md`](../../THIRD_PARTY_NOTICES.md) na raiz. O Console Mode é licenciado sob a AGPL-3.0-only (arquivo `LICENSE`; código-fonte em
<https://github.com/lippdev/consolemode>). O instalador da 2.0 grava este
arquivo em `THIRD_PARTY_NOTICES.md` dentro da pasta do app.

## Escritos à mão

| Componente | Site | Licença | Uso no projeto |
|------------|------|---------|----------------|
| **GPUI** | [zed-industries/zed](https://github.com/zed-industries/zed) (`crates/gpui`, fixado no commit `917559da`) | Apache License 2.0 | Biblioteca de interface (janelas, layout, renderização). Compilada dentro do executável. A parte do Windows (`gpui_windows`) vai como cópia modificada em `vendor/gpui_windows` (quadros sob demanda; mudanças em `CHANGES-CONSOLE-MODE.md`) |
| **SDL3** | [libsdl-org/SDL](https://github.com/libsdl-org/SDL) (compilado estático pelo crate `sdl3-src`) | zlib | Leitura dos controles (Xbox, PlayStation e outros). Compilada dentro do executável; o aviso de copyright da SDL está nos arquivos-fonte do crate |
| **Material Symbols Rounded** (Filled) | [Google Fonts / Material Symbols](https://fonts.google.com/icons) | Apache License 2.0 | Fonte de ícones da interface, embarcada em `crates/cm-ui/assets/fonts/MaterialSymbolsRounded-Filled.ttf` |
| **Source Sans 3** | [Adobe](https://github.com/adobe-fonts/source-sans) | SIL Open Font License 1.1 | Fonte de texto embarcada no executável. Licença em [SourceSans3-OFL.txt](../../crates/cm-ui/assets/fonts/SourceSans3-OFL.txt), incluída na pasta do app pelo instalador. |
| Sons `move`, `confirm`, `back` | [Interface Sounds](https://kenney.nl/assets/interface-sounds), da Kenney | CC0 1.0 (domínio público; crédito opcional) | Sons de navegação, embarcados no executável |
| Som `tab` | Gerado pelo `tools/make_sounds.py` do [Selene](https://github.com/mattxslv/Selene), de Matthew Silva (mattxslv): síntese pura, sem amostras | MIT, Copyright (c) 2026 Matthew Silva | Som de troca de aba |
| Som de abertura `boot` (Console Mode) | Gerado pelo `tools/make_sounds.py` do [Selene](https://github.com/mattxslv/Selene): síntese pura, sem amostras | MIT, Copyright (c) 2026 Matthew Silva | Abertura do Console Mode |
| Sons de abertura `bootx360`, `bootxbox` (Xbox, 2001), `bootxone` (Xbox One) | Sons de inicialização dos consoles Xbox, © Microsoft. Os da 2.0 vieram do MyInstants, cortados no tamanho da abertura. Estão aqui por escolha do mantenedor e serão removidos se a Microsoft pedir | Não é livre: © Microsoft | Aberturas "Inspirada no Xbox", "no Xbox 360" e "no Xbox One" |
| Sons de abertura `bootps1`, `bootps2`, `bootps3`, `bootps4` | Sons de inicialização dos consoles PlayStation, © Sony Interactive Entertainment. Os da 2.0 vieram do MyInstants, cortados no tamanho da abertura. Estão aqui por escolha do mantenedor e serão removidos se a Sony pedir | Não é livre: © Sony | Aberturas "Inspirada no PS1" a "no PS4" |
| Ícone do app | Logo do Console Mode (o mesmo da 1.7) | AGPL-3.0-only (licença do projeto) | `crates/cm-app/assets/ConsoleMode2.ico` |

### Licença Apache 2.0

GPUI e a fonte Material Symbols são distribuídos sob a [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
Eles são usados sem modificação. O texto completo está em <https://www.apache.org/licenses/LICENSE-2.0.txt>.

### Licença zlib (SDL3)

> This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.
>
> Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:
>
> 1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
> 2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
> 3. This notice may not be removed or altered from any source distribution.

### Dados lidos em tempo de execução (não redistribuídos)

Ícones e artes de apps e jogos (Steam, Playnite, Xbox) são lidos da instalação do próprio usuário
e nunca fazem parte do instalador.

## Dependências Rust

Todas as dependências Rust do `ConsoleMode2.exe` (grafo do `cm-app` para Windows, sem dependências
de teste; entram também as usadas só para compilar), com a licença declarada por cada uma. Cada
pacote traz o texto completo da sua licença no repositório indicado. Nenhuma é GPL/LGPL/AGPL. O
`symphonia` (MPL-2.0, decodifica os sons) é usado sem modificação; o código-fonte dele está no
repositório indicado. **Seção gerada:** não edite à mão, rode `build\Generate-Notices2.ps1`
(`-Check` só confere se está em dia).

<!-- BEGIN GENERATED (build/Generate-Notices2.ps1) -->

Resumo por licenca (378 pacotes):

- `MIT OR Apache-2.0`: 195
- `MIT`: 60
- `Apache-2.0 OR MIT`: 28
- `Unicode-3.0`: 22
- `Apache-2.0`: 14
- `MPL-2.0`: 12
- `Zlib`: 11
- `BSD-3-Clause`: 8
- `BSD-2-Clause`: 4
- `Unlicense OR MIT`: 4
- `MIT OR Apache-2.0 OR Zlib`: 4
- `Zlib OR Apache-2.0 OR MIT`: 3
- `MIT OR Zlib OR Apache-2.0`: 2
- `BSD-3-Clause OR Apache-2.0`: 2
- `BSD-2-Clause OR Apache-2.0 OR MIT`: 2
- `0BSD OR MIT OR Apache-2.0`: 1
- `(MIT OR Apache-2.0) AND Unicode-3.0`: 1
- `(Apache-2.0 OR MIT) AND BSD-3-Clause`: 1
- `Apache-2.0 OR BSL-1.0`: 1
- `CC0-1.0 OR Apache-2.0`: 1
- `CC0-1.0 OR MIT-0 OR Apache-2.0`: 1
- `Zlib OR MIT OR Apache-2.0`: 1

| Pacote | Versao | Licenca | Origem |
|--------|--------|---------|--------|
| `accesskit` | 0.24.1 | MIT OR Apache-2.0 | https://github.com/AccessKit/accesskit |
| `accesskit_consumer` | 0.38.0 | MIT OR Apache-2.0 | https://github.com/AccessKit/accesskit |
| `accesskit_windows` | 0.34.0 | MIT OR Apache-2.0 | https://github.com/AccessKit/accesskit |
| `adler2` | 2.0.1 | 0BSD OR MIT OR Apache-2.0 | https://github.com/oyvindln/adler2 |
| `aho-corasick` | 1.1.5 | Unlicense OR MIT | https://github.com/BurntSushi/aho-corasick |
| `aligned` | 0.4.3 | MIT OR Apache-2.0 | https://github.com/rust-embedded-community/aligned |
| `aligned-vec` | 0.6.4 | MIT | https://github.com/sarah-ek/aligned-vec/ |
| `allocator-api2` | 0.2.21 | MIT OR Apache-2.0 | https://github.com/zakarumych/allocator-api2 |
| `anyhow` | 1.0.104 | MIT OR Apache-2.0 | https://github.com/dtolnay/anyhow |
| `arg_enum_proc_macro` | 0.3.4 | MIT | https://github.com/lu-zero/arg_enum_proc_macro |
| `arrayref` | 0.3.9 | BSD-2-Clause | https://github.com/droundy/arrayref |
| `arrayvec` | 0.7.8 | MIT OR Apache-2.0 | https://github.com/bluss/arrayvec |
| `as-slice` | 0.2.1 | MIT OR Apache-2.0 | https://github.com/japaric/as-slice |
| `async-channel` | 2.5.0 | Apache-2.0 OR MIT | https://github.com/smol-rs/async-channel |
| `async-task` | 4.7.1 | Apache-2.0 OR MIT | https://github.com/smol-rs/async-task |
| `autocfg` | 1.5.1 | Apache-2.0 OR MIT | https://github.com/cuviper/autocfg |
| `av1-grain` | 0.2.5 | BSD-2-Clause | https://github.com/rust-av/av1-grain |
| `avif-serialize` | 0.8.9 | BSD-3-Clause | https://github.com/kornelski/avif-serialize |
| `av-scenechange` | 0.14.1 | MIT | https://github.com/rust-av/av-scenechange |
| `backtrace` | 0.3.77 | MIT OR Apache-2.0 | https://github.com/rust-lang/backtrace-rs |
| `base64` | 0.22.1 | MIT OR Apache-2.0 | https://github.com/marshallpierce/rust-base64 |
| `bit_field` | 0.10.4 | MIT OR Apache-2.0 | https://github.com/phil-opp/rust-bit-field |
| `bitflags` | 1.3.2 | MIT OR Apache-2.0 | https://github.com/bitflags/bitflags |
| `bitflags` | 2.13.2 | MIT OR Apache-2.0 | https://github.com/bitflags/bitflags |
| `bitstream-io` | 4.10.0 | MIT OR Apache-2.0 | https://github.com/tuffy/bitstream-io |
| `block-buffer` | 0.10.4 | MIT OR Apache-2.0 | https://github.com/RustCrypto/utils |
| `built` | 0.8.1 | MIT | https://github.com/lukaslueg/built |
| `bumpalo` | 3.20.4 | MIT OR Apache-2.0 | https://github.com/fitzgen/bumpalo |
| `bytemuck` | 1.25.2 | Zlib OR Apache-2.0 OR MIT | https://github.com/Lokathor/bytemuck |
| `bytemuck_derive` | 1.12.1 | Zlib OR Apache-2.0 OR MIT | https://github.com/Lokathor/bytemuck |
| `byteorder` | 1.5.0 | Unlicense OR MIT | https://github.com/BurntSushi/byteorder |
| `byteorder-lite` | 0.1.0 | Unlicense OR MIT | https://github.com/image-rs/byteorder-lite |
| `bytes` | 1.12.1 | MIT | https://github.com/tokio-rs/bytes |
| `cc` | 1.11.2 | MIT OR Apache-2.0 | https://github.com/rust-lang/cc-rs |
| `cfg-if` | 1.0.5 | MIT OR Apache-2.0 | https://github.com/rust-lang/cfg-if |
| `chacha20` | 0.10.2 | MIT OR Apache-2.0 | https://github.com/RustCrypto/stream-ciphers |
| `chrono` | 0.4.45 | MIT OR Apache-2.0 | https://github.com/chronotope/chrono |
| `cmake` | 0.1.58 | MIT OR Apache-2.0 | https://github.com/rust-lang/cmake-rs |
| `collections` | 0.1.0 | Apache-2.0 | https://crates.io/crates/collections |
| `color_quant` | 1.1.0 | MIT | https://github.com/image-rs/color_quant.git |
| `concurrent-queue` | 2.5.0 | Apache-2.0 OR MIT | https://github.com/smol-rs/concurrent-queue |
| `convert_case` | 0.10.0 | MIT | https://github.com/rutrum/convert-case |
| `core_detect` | 1.0.0 | MIT OR Apache-2.0 | https://github.com/thomcc/core_detect |
| `core_maths` | 0.1.1 | MIT | https://github.com/robertbastian/core_maths |
| `cpal` | 0.18.2 | Apache-2.0 | https://github.com/RustAudio/cpal |
| `cpufeatures` | 0.2.17 | MIT OR Apache-2.0 | https://github.com/RustCrypto/utils |
| `cpufeatures` | 0.3.1 | MIT OR Apache-2.0 | https://github.com/RustCrypto/utils |
| `crc32fast` | 1.5.5 | MIT OR Apache-2.0 | https://github.com/srijs/rust-crc32fast |
| `crossbeam-deque` | 0.8.8 | MIT OR Apache-2.0 | https://github.com/crossbeam-rs/crossbeam |
| `crossbeam-epoch` | 0.9.21 | MIT OR Apache-2.0 | https://github.com/crossbeam-rs/crossbeam |
| `crossbeam-utils` | 0.8.23 | MIT OR Apache-2.0 | https://github.com/crossbeam-rs/crossbeam |
| `crypto-common` | 0.1.7 | MIT OR Apache-2.0 | https://github.com/RustCrypto/traits |
| `dasp_sample` | 0.11.0 | MIT OR Apache-2.0 | https://github.com/rustaudio/sample.git |
| `data-url` | 0.3.2 | MIT OR Apache-2.0 | https://github.com/servo/rust-url |
| `derive_more` | 2.1.1 | MIT | https://github.com/JelteF/derive_more |
| `derive_more-impl` | 2.1.1 | MIT | https://github.com/JelteF/derive_more |
| `derive_refineable` | 0.1.0 | Apache-2.0 | https://crates.io/crates/derive_refineable |
| `digest` | 0.10.7 | MIT OR Apache-2.0 | https://github.com/RustCrypto/traits |
| `displaydoc` | 0.2.7 | MIT OR Apache-2.0 | https://github.com/yaahc/displaydoc |
| `dunce` | 1.0.5 | CC0-1.0 OR MIT-0 OR Apache-2.0 | https://gitlab.com/kornelski/dunce |
| `dyn-clone` | 1.0.20 | MIT OR Apache-2.0 | https://github.com/dtolnay/dyn-clone |
| `either` | 1.19.0 | MIT OR Apache-2.0 | https://github.com/rayon-rs/either |
| `embed-resource` | 3.0.12 | MIT | https://github.com/nabijaczleweli/rust-embed-resource |
| `encoding_rs` | 0.8.44 | (Apache-2.0 OR MIT) AND BSD-3-Clause | https://github.com/hsivonen/encoding_rs |
| `enumn` | 0.1.14 | MIT OR Apache-2.0 | https://github.com/dtolnay/enumn |
| `equator` | 0.4.2 | MIT | https://github.com/sarah-ek/equator/ |
| `equator-macro` | 0.4.2 | MIT | https://github.com/sarah-ek/equator/ |
| `equivalent` | 1.0.3 | Apache-2.0 OR MIT | https://github.com/indexmap-rs/equivalent |
| `etagere` | 0.2.15 | MIT OR Apache-2.0 | https://github.com/nical/etagere |
| `euclid` | 0.22.14 | MIT OR Apache-2.0 | https://github.com/servo/euclid |
| `event-listener` | 5.4.2 | Apache-2.0 OR MIT | https://github.com/smol-rs/event-listener |
| `event-listener-strategy` | 0.5.4 | Apache-2.0 OR MIT | https://github.com/smol-rs/event-listener-strategy |
| `exr` | 1.74.3 | BSD-3-Clause | https://github.com/johannesvollmer/exrs |
| `extended` | 0.1.0 | MIT | https://github.com/depp/extended-rs |
| `fax` | 0.2.7 | MIT | https://github.com/pdf-rs/fax |
| `fdeflate` | 0.3.7 | MIT OR Apache-2.0 | https://github.com/image-rs/fdeflate |
| `find-msvc-tools` | 0.1.15 | MIT OR Apache-2.0 | https://github.com/rust-lang/cc-rs |
| `flate2` | 1.1.11 | MIT OR Apache-2.0 | https://github.com/rust-lang/flate2-rs |
| `float-cmp` | 0.9.0 | MIT | https://github.com/mikedilger/float-cmp |
| `flume` | 0.12.0 | Apache-2.0 OR MIT | https://github.com/zesterer/flume |
| `foldhash` | 0.2.0 | Zlib | https://github.com/orlp/foldhash |
| `fontdb` | 0.23.0 | MIT | https://github.com/RazrFalcon/fontdb |
| `form_urlencoded` | 1.2.2 | MIT OR Apache-2.0 | https://github.com/servo/rust-url |
| `futures` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-channel` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-core` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-executor` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-io` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-macro` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-sink` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-task` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `futures-util` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/futures-rs |
| `generic-array` | 0.14.7 | MIT | https://github.com/fizyk20/generic-array.git |
| `getrandom` | 0.3.4 | MIT OR Apache-2.0 | https://github.com/rust-random/getrandom |
| `getrandom` | 0.4.3 | MIT OR Apache-2.0 | https://github.com/rust-random/getrandom |
| `gif` | 0.14.2 | MIT OR Apache-2.0 | https://github.com/image-rs/image-gif |
| `gpui` | 0.2.2 | Apache-2.0 | https://github.com/zed-industries/zed |
| `gpui_macros` | 0.1.0 | Apache-2.0 | https://crates.io/crates/gpui_macros |
| `gpui_platform` | 0.1.0 | Apache-2.0 | https://crates.io/crates/gpui_platform |
| `gpui_shared_string` | 0.1.0 | Apache-2.0 | https://crates.io/crates/gpui_shared_string |
| `gpui_util` | 0.1.0 | Apache-2.0 | https://crates.io/crates/gpui_util |
| `half` | 2.7.1 | MIT OR Apache-2.0 | https://github.com/VoidStarKat/half-rs |
| `hash32` | 0.3.1 | MIT OR Apache-2.0 | https://github.com/japaric/hash32 |
| `hashbrown` | 0.16.1 | MIT OR Apache-2.0 | https://github.com/rust-lang/hashbrown |
| `hashbrown` | 0.17.1 | MIT OR Apache-2.0 | https://github.com/rust-lang/hashbrown |
| `heapless` | 0.9.3 | MIT OR Apache-2.0 | https://github.com/rust-embedded/heapless |
| `heck` | 0.5.0 | MIT OR Apache-2.0 | https://github.com/withoutboats/heck |
| `http` | 1.5.0 | MIT OR Apache-2.0 | https://github.com/hyperium/http |
| `http_client` | 0.1.0 | Apache-2.0 | https://crates.io/crates/http_client |
| `http-body` | 1.1.0 | MIT | https://github.com/hyperium/http-body |
| `icu_collections` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_locale_core` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_locale_fallback` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_locale_fallback_data` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_normalizer` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_normalizer_data` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_properties` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_properties_data` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_provider` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_segmenter` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `icu_segmenter_data` | 2.4.0 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `idna` | 1.1.0 | MIT OR Apache-2.0 | https://github.com/servo/rust-url/ |
| `idna_adapter` | 1.2.2 | Apache-2.0 OR MIT | https://github.com/hsivonen/idna_adapter |
| `image` | 0.25.10 | MIT OR Apache-2.0 | https://github.com/image-rs/image |
| `imagesize` | 0.14.0 | MIT | https://github.com/Roughsketch/imagesize |
| `image-webp` | 0.2.4 | MIT OR Apache-2.0 | https://github.com/image-rs/image-webp |
| `imgref` | 1.12.3 | CC0-1.0 OR Apache-2.0 | https://github.com/kornelski/imgref |
| `indexmap` | 2.14.2 | Apache-2.0 OR MIT | https://github.com/indexmap-rs/indexmap |
| `inventory` | 0.3.25 | MIT OR Apache-2.0 | https://github.com/dtolnay/inventory |
| `itertools` | 0.14.0 | MIT OR Apache-2.0 | https://github.com/rust-itertools/itertools |
| `itoa` | 1.0.19 | MIT OR Apache-2.0 | https://github.com/dtolnay/itoa |
| `jobserver` | 0.1.35 | MIT OR Apache-2.0 | https://github.com/rust-lang/jobserver-rs |
| `kurbo` | 0.13.1 | Apache-2.0 OR MIT | https://github.com/linebender/kurbo |
| `lazy_static` | 1.5.1 | MIT OR Apache-2.0 | https://github.com/rust-lang-nursery/lazy-static.rs |
| `lebe` | 0.5.3 | BSD-3-Clause | https://github.com/johannesvollmer/lebe |
| `libc` | 0.2.191 | MIT OR Apache-2.0 | https://github.com/rust-lang/libc |
| `libm` | 0.2.16 | MIT | https://github.com/rust-lang/compiler-builtins |
| `litemap` | 0.8.3 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `lock_api` | 0.4.14 | MIT OR Apache-2.0 | https://github.com/Amanieu/parking_lot |
| `log` | 0.4.35 | MIT OR Apache-2.0 | https://github.com/rust-lang/log |
| `loop9` | 0.1.5 | MIT | https://gitlab.com/kornelski/loop9.git |
| `lyon_geom` | 1.0.20 | MIT OR Apache-2.0 | https://github.com/nical/lyon |
| `lyon_path` | 1.0.19 | MIT OR Apache-2.0 | https://github.com/nical/lyon |
| `maybe-rayon` | 0.1.1 | MIT | https://github.com/shssoichiro/maybe-rayon |
| `memchr` | 2.8.3 | Unlicense OR MIT | https://github.com/BurntSushi/memchr |
| `memmap2` | 0.9.12 | MIT OR Apache-2.0 | https://github.com/RazrFalcon/memmap2-rs |
| `miniz_oxide` | 0.8.9 | MIT OR Zlib OR Apache-2.0 | https://github.com/Frommi/miniz_oxide/tree/master/miniz_oxide |
| `miniz_oxide` | 0.9.1 | MIT OR Zlib OR Apache-2.0 | https://github.com/Frommi/miniz_oxide/tree/master/miniz_oxide |
| `moxcms` | 0.8.1 | BSD-3-Clause OR Apache-2.0 | https://github.com/awxkee/moxcms.git |
| `multiversion` | 0.8.1 | MIT OR Apache-2.0 | https://github.com/calebzulawski/multiversion |
| `multiversion_no_op` | 1.0.0 | Apache-2.0 OR MIT | https://github.com/hsivonen/multiversion_no_op |
| `multiversion-macros` | 0.8.1 | MIT OR Apache-2.0 | https://github.com/calebzulawski/multiversion |
| `new_debug_unreachable` | 1.0.6 | MIT | https://github.com/mbrubeck/rust-debug-unreachable |
| `no_std_io2` | 0.9.4 | Apache-2.0 OR MIT | https://github.com/wcampbell0x2a/no-std-io2 |
| `nom` | 8.0.0 | MIT | https://github.com/rust-bakery/nom |
| `noop_proc_macro` | 0.3.0 | MIT | https://github.com/lu-zero/noop_proc_macro |
| `ntapi` | 0.4.3 | Apache-2.0 OR MIT | https://github.com/MSxDOS/ntapi |
| `num_cpus` | 1.17.0 | MIT OR Apache-2.0 | https://github.com/seanmonstar/num_cpus |
| `num-bigint` | 0.4.8 | MIT OR Apache-2.0 | https://github.com/rust-num/num-bigint |
| `num-derive` | 0.4.2 | MIT OR Apache-2.0 | https://github.com/rust-num/num-derive |
| `num-integer` | 0.1.47 | MIT OR Apache-2.0 | https://github.com/rust-num/num-integer |
| `num-rational` | 0.4.2 | MIT OR Apache-2.0 | https://github.com/rust-num/num-rational |
| `num-traits` | 0.2.19 | MIT OR Apache-2.0 | https://github.com/rust-num/num-traits |
| `once_cell` | 1.21.4 | MIT OR Apache-2.0 | https://github.com/matklad/once_cell |
| `parking` | 2.2.1 | Apache-2.0 OR MIT | https://github.com/smol-rs/parking |
| `parking_lot` | 0.12.5 | MIT OR Apache-2.0 | https://github.com/Amanieu/parking_lot |
| `parking_lot_core` | 0.9.12 | MIT OR Apache-2.0 | https://github.com/Amanieu/parking_lot |
| `paste` | 1.0.15 | MIT OR Apache-2.0 | https://github.com/dtolnay/paste |
| `pastey` | 0.1.1 | MIT OR Apache-2.0 | https://github.com/as1100k/pastey |
| `percent-encoding` | 2.3.2 | MIT OR Apache-2.0 | https://github.com/servo/rust-url/ |
| `pico-args` | 0.5.0 | MIT | https://github.com/RazrFalcon/pico-args |
| `pin-project` | 1.1.13 | Apache-2.0 OR MIT | https://github.com/taiki-e/pin-project |
| `pin-project-internal` | 1.1.13 | Apache-2.0 OR MIT | https://github.com/taiki-e/pin-project |
| `pin-project-lite` | 0.2.17 | Apache-2.0 OR MIT | https://github.com/taiki-e/pin-project-lite |
| `pkg-config` | 0.3.34 | MIT OR Apache-2.0 | https://github.com/rust-lang/pkg-config-rs |
| `png` | 0.17.16 | MIT OR Apache-2.0 | https://github.com/image-rs/image-png |
| `png` | 0.18.1 | MIT OR Apache-2.0 | https://github.com/image-rs/image-png |
| `pollster` | 0.4.0 | Apache-2.0 OR MIT | https://github.com/zesterer/pollster |
| `polycool` | 0.4.0 | MIT OR Apache-2.0 | https://github.com/linebender/kurbo |
| `potential_utf` | 0.1.6 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `ppv-lite86` | 0.2.21 | MIT OR Apache-2.0 | https://github.com/cryptocorrosion/cryptocorrosion |
| `proc-macro2` | 1.0.107 | MIT OR Apache-2.0 | https://github.com/dtolnay/proc-macro2 |
| `profiling` | 1.0.18 | MIT OR Apache-2.0 | https://github.com/aclysma/profiling |
| `profiling-procmacros` | 1.0.18 | MIT OR Apache-2.0 | https://github.com/aclysma/profiling |
| `pxfm` | 0.1.30 | BSD-3-Clause OR Apache-2.0 | https://github.com/awxkee/pxfm |
| `qoi` | 0.4.1 | MIT OR Apache-2.0 | https://github.com/aldanor/qoi-rust |
| `quick-error` | 2.0.1 | MIT OR Apache-2.0 | http://github.com/tailhook/quick-error |
| `quote` | 1.0.47 | MIT OR Apache-2.0 | https://github.com/dtolnay/quote |
| `rand` | 0.10.4 | MIT OR Apache-2.0 | https://github.com/rust-random/rand |
| `rand` | 0.9.5 | MIT OR Apache-2.0 | https://github.com/rust-random/rand |
| `rand_chacha` | 0.9.0 | MIT OR Apache-2.0 | https://github.com/rust-random/rand |
| `rand_core` | 0.10.1 | MIT OR Apache-2.0 | https://github.com/rust-random/rand_core |
| `rand_core` | 0.9.5 | MIT OR Apache-2.0 | https://github.com/rust-random/rand |
| `rand_distr` | 0.6.0 | MIT OR Apache-2.0 | https://github.com/rust-random/rand_distr |
| `rav1e` | 0.8.1 | BSD-2-Clause | https://github.com/xiph/rav1e/ |
| `ravif` | 0.13.0 | BSD-3-Clause | https://github.com/kornelski/cavif-rs |
| `raw-window-handle` | 0.6.2 | MIT OR Apache-2.0 OR Zlib | https://github.com/rust-windowing/raw-window-handle |
| `rayon` | 1.12.0 | MIT OR Apache-2.0 | https://github.com/rayon-rs/rayon |
| `rayon-core` | 1.13.0 | MIT OR Apache-2.0 | https://github.com/rayon-rs/rayon |
| `ref-cast` | 1.0.27 | MIT OR Apache-2.0 | https://github.com/dtolnay/ref-cast |
| `ref-cast-impl` | 1.0.27 | MIT OR Apache-2.0 | https://github.com/dtolnay/ref-cast |
| `refineable` | 0.1.0 | Apache-2.0 | https://crates.io/crates/refineable |
| `regex` | 1.13.1 | MIT OR Apache-2.0 | https://github.com/rust-lang/regex |
| `regex-automata` | 0.4.18 | MIT OR Apache-2.0 | https://github.com/rust-lang/regex |
| `regex-syntax` | 0.8.11 | MIT OR Apache-2.0 | https://github.com/rust-lang/regex |
| `resvg` | 0.46.0 | Apache-2.0 OR MIT | https://github.com/linebender/resvg |
| `rgb` | 0.8.53 | MIT | https://github.com/kornelski/rust-rgb |
| `rodio` | 0.23.0 | MIT OR Apache-2.0 | https://github.com/RustAudio/rodio |
| `roxmltree` | 0.21.2 | MIT OR Apache-2.0 | https://github.com/RazrFalcon/roxmltree |
| `rpkg-config` | 0.1.2 | Zlib OR MIT OR Apache-2.0 | https://github.com/maia-s/rpkg-config-rs |
| `rtrb` | 0.3.6 | MIT OR Apache-2.0 | https://github.com/mgeier/rtrb |
| `rustc_version` | 0.4.1 | MIT OR Apache-2.0 | https://github.com/djc/rustc-version-rs |
| `rustc-demangle` | 0.1.28 | MIT OR Apache-2.0 | https://github.com/rust-lang/rustc-demangle |
| `rustc-hash` | 2.1.3 | Apache-2.0 OR MIT | https://github.com/rust-lang/rustc-hash |
| `rustversion` | 1.0.23 | MIT OR Apache-2.0 | https://github.com/dtolnay/rustversion |
| `rustybuzz` | 0.20.1 | MIT | https://github.com/harfbuzz/rustybuzz |
| `ryu` | 1.0.23 | Apache-2.0 OR BSL-1.0 | https://github.com/dtolnay/ryu |
| `scheduler` | 0.1.0 | Apache-2.0 | https://crates.io/crates/scheduler |
| `schemars` | 1.2.3 | MIT | https://github.com/GREsau/schemars |
| `schemars_derive` | 1.2.3 | MIT | https://github.com/GREsau/schemars |
| `scopeguard` | 1.2.0 | MIT OR Apache-2.0 | https://github.com/bluss/scopeguard |
| `sdl3` | 0.22.1 | MIT | https://github.com/vhspace/sdl3-rs |
| `sdl3-image-src` | 3.4.8 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-image-sys` | 0.7.1+SDL-image-3.4.8 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-mixer-src` | 3.2.4 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-mixer-sys` | 0.7.0+SDL-mixer-3.2.4 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-src` | 3.4.20 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-sys` | 0.7.3+SDL-3.4.20 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `sdl3-ttf-src` | 3.2.3 | Zlib | https://github.com/maia-s/sdl3-sys-rs |
| `sdl3-ttf-sys` | 0.7.1+SDL-ttf-3.2.2 | Zlib | https://codeberg.org/maia/sdl3-sys-rs |
| `seahash` | 4.1.0 | MIT | https://gitlab.redox-os.org/redox-os/seahash |
| `semver` | 1.0.28 | MIT OR Apache-2.0 | https://github.com/dtolnay/semver |
| `serde` | 1.0.229 | MIT OR Apache-2.0 | https://github.com/serde-rs/serde |
| `serde_core` | 1.0.229 | MIT OR Apache-2.0 | https://github.com/serde-rs/serde |
| `serde_derive` | 1.0.229 | MIT OR Apache-2.0 | https://github.com/serde-rs/serde |
| `serde_derive_internals` | 0.30.0 | MIT OR Apache-2.0 | https://github.com/serde-rs/serde |
| `serde_json` | 1.0.154 | MIT OR Apache-2.0 | https://github.com/serde-rs/json |
| `serde_spanned` | 1.1.3 | MIT OR Apache-2.0 | https://github.com/toml-rs/toml |
| `serde_urlencoded` | 0.7.1 | MIT OR Apache-2.0 | https://github.com/nox/serde_urlencoded |
| `sha1_smol` | 1.0.1 | BSD-3-Clause | https://github.com/mitsuhiko/sha1-smol |
| `sha2` | 0.10.9 | MIT OR Apache-2.0 | https://github.com/RustCrypto/hashes |
| `shlex` | 2.0.1 | MIT OR Apache-2.0 | https://github.com/comex/rust-shlex |
| `simd_helpers` | 0.1.0 | MIT | https://github.com/lu-zero/simd_helpers |
| `simd-adler32` | 0.3.10 | MIT | https://github.com/mcountryman/simd-adler32 |
| `simdutf8` | 0.1.5 | MIT OR Apache-2.0 | https://github.com/rusticstuff/simdutf8 |
| `simplecss` | 0.2.2 | Apache-2.0 OR MIT | https://github.com/linebender/simplecss |
| `siphasher` | 1.0.4 | MIT OR Apache-2.0 | https://github.com/jedisct1/rust-siphash |
| `slab` | 0.4.12 | MIT | https://github.com/tokio-rs/slab |
| `slotmap` | 1.1.1 | Zlib | https://github.com/orlp/slotmap |
| `smallvec` | 1.16.5 | MIT OR Apache-2.0 | https://github.com/servo/rust-smallvec |
| `smol_str` | 0.3.7 | MIT OR Apache-2.0 | https://github.com/rust-lang/rust-analyzer/tree/master/lib/smol_str |
| `spin` | 0.10.1 | MIT | https://github.com/mvdnes/spin-rs.git |
| `spin` | 0.9.9 | MIT | https://github.com/mvdnes/spin-rs.git |
| `stable_deref_trait` | 1.2.1 | MIT OR Apache-2.0 | https://github.com/storyyeller/stable_deref_trait |
| `static_assertions` | 1.1.0 | MIT OR Apache-2.0 | https://github.com/nvzqz/static-assertions-rs |
| `strict-num` | 0.1.1 | MIT | https://github.com/RazrFalcon/strict-num |
| `strum` | 0.28.0 | MIT | https://github.com/Peternator7/strum |
| `strum_macros` | 0.28.0 | MIT | https://github.com/Peternator7/strum |
| `subtle` | 2.6.1 | BSD-3-Clause | https://github.com/dalek-cryptography/subtle |
| `sum_tree` | 0.1.0 | Apache-2.0 | https://crates.io/crates/sum_tree |
| `svg_fmt` | 0.4.5 | MIT OR Apache-2.0 | https://github.com/nical/rust_debug |
| `svgtypes` | 0.16.2 | Apache-2.0 OR MIT | https://github.com/linebender/svgtypes |
| `symphonia` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-bundle-flac` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-bundle-mp3` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-codec-aac` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-codec-pcm` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-codec-vorbis` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-core` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-format-isomp4` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-format-ogg` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-format-riff` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-metadata` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `symphonia-utils-xiph` | 0.5.5 | MPL-2.0 | https://github.com/pdeljanov/Symphonia |
| `syn` | 2.0.119 | MIT OR Apache-2.0 | https://github.com/dtolnay/syn |
| `syn` | 3.0.10 | MIT OR Apache-2.0 | https://github.com/dtolnay/syn |
| `synstructure` | 0.14.0 | MIT | https://github.com/mystor/synstructure |
| `sysinfo` | 0.31.4 | MIT | https://github.com/GuillaumeGomez/sysinfo |
| `taffy` | 0.15.0 | MIT | https://github.com/DioxusLabs/taffy |
| `target-features` | 0.1.6 | MIT OR Apache-2.0 | https://github.com/calebzulawski/target-features |
| `thiserror` | 2.0.21 | MIT OR Apache-2.0 | https://github.com/dtolnay/thiserror |
| `thiserror-impl` | 2.0.21 | MIT OR Apache-2.0 | https://github.com/dtolnay/thiserror |
| `tiff` | 0.11.3 | MIT | https://github.com/image-rs/image-tiff |
| `tiny-skia` | 0.11.4 | BSD-3-Clause | https://github.com/RazrFalcon/tiny-skia |
| `tiny-skia-path` | 0.11.4 | BSD-3-Clause | https://github.com/RazrFalcon/tiny-skia/tree/master/path |
| `tinystr` | 0.8.5 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `tinyvec` | 1.13.3 | Zlib OR Apache-2.0 OR MIT | https://github.com/Lokathor/tinyvec |
| `toml` | 1.2.1+spec-1.1.0 | MIT OR Apache-2.0 | https://github.com/toml-rs/toml |
| `toml_datetime` | 1.1.3+spec-1.1.0 | MIT OR Apache-2.0 | https://github.com/toml-rs/toml |
| `toml_parser` | 1.1.5+spec-1.1.0 | MIT OR Apache-2.0 | https://github.com/toml-rs/toml |
| `toml_writer` | 1.2.0+spec-1.1.0 | MIT OR Apache-2.0 | https://github.com/toml-rs/toml |
| `tracing` | 0.1.44 | MIT | https://github.com/tokio-rs/tracing |
| `tracing-attributes` | 0.1.31 | MIT | https://github.com/tokio-rs/tracing |
| `tracing-core` | 0.1.36 | MIT | https://github.com/tokio-rs/tracing |
| `ttf-parser` | 0.25.1 | MIT OR Apache-2.0 | https://github.com/harfbuzz/ttf-parser |
| `typenum` | 1.20.1 | MIT OR Apache-2.0 | https://github.com/paholg/typenum |
| `unicode-bidi` | 0.3.18 | MIT OR Apache-2.0 | https://github.com/servo/unicode-bidi |
| `unicode-bidi-mirroring` | 0.4.0 | MIT OR Apache-2.0 | https://github.com/RazrFalcon/unicode-bidi-mirroring |
| `unicode-ccc` | 0.4.0 | MIT OR Apache-2.0 | https://github.com/RazrFalcon/unicode-ccc |
| `unicode-ident` | 1.0.27 | (MIT OR Apache-2.0) AND Unicode-3.0 | https://github.com/dtolnay/unicode-ident |
| `unicode-properties` | 0.1.3 | MIT OR Apache-2.0 | https://github.com/unicode-rs/unicode-properties |
| `unicode-script` | 0.5.9 | MIT OR Apache-2.0 | https://github.com/unicode-rs/unicode-script |
| `unicode-segmentation` | 1.13.3 | MIT OR Apache-2.0 | https://github.com/unicode-rs/unicode-segmentation |
| `unicode-vo` | 0.1.0 | MIT OR Apache-2.0 | https://github.com/RazrFalcon/unicode-vo |
| `url` | 2.5.8 | MIT OR Apache-2.0 | https://github.com/servo/rust-url |
| `usvg` | 0.46.0 | Apache-2.0 OR MIT | https://github.com/linebender/resvg |
| `utf_types` | 1.0.0 | Apache-2.0 OR MIT | https://github.com/hsivonen/utf_types |
| `utf8_iter` | 1.1.0 | Apache-2.0 OR MIT | https://github.com/hsivonen/utf8_iter |
| `uuid` | 1.29.2 | Apache-2.0 OR MIT | https://github.com/uuid-rs/uuid |
| `v_frame` | 0.3.9 | BSD-2-Clause | https://github.com/rust-av/v_frame |
| `vcpkg` | 0.2.15 | MIT OR Apache-2.0 | https://github.com/mcgoo/vcpkg-rs |
| `version_check` | 0.9.5 | MIT OR Apache-2.0 | https://github.com/SergioBenitez/version_check |
| `vswhom` | 0.1.0 | MIT | https://github.com/nabijaczleweli/vswhom.rs |
| `vswhom-sys` | 0.1.3 | MIT | https://github.com/nabijaczleweli/vswhom-sys.rs |
| `waker-fn` | 1.2.0 | Apache-2.0 OR MIT | https://github.com/smol-rs/waker-fn |
| `wasm-bindgen` | 0.2.130 | MIT OR Apache-2.0 | https://github.com/wasm-bindgen/wasm-bindgen |
| `wasm-bindgen-macro` | 0.2.130 | MIT OR Apache-2.0 | https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/macro |
| `wasm-bindgen-macro-support` | 0.2.130 | MIT OR Apache-2.0 | https://github.com/wasm-bindgen/wasm-bindgen/tree/main/crates/macro-support |
| `wasm-bindgen-shared` | 0.2.130 | MIT OR Apache-2.0 | https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/shared |
| `web-time` | 1.1.0 | MIT OR Apache-2.0 | https://github.com/daxpedda/web-time |
| `weezl` | 0.1.12 | MIT OR Apache-2.0 | https://github.com/image-rs/weezl |
| `which` | 8.0.6 | MIT | https://github.com/harryfei/which-rs.git |
| `winapi` | 0.3.9 | MIT OR Apache-2.0 | https://github.com/retep998/winapi-rs |
| `windows` | 0.57.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows` | 0.61.3 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows` | 0.62.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows_x86_64_msvc` | 0.52.6 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-capture` | 1.5.0 | MIT | https://github.com/NiiightmareXD/windows-capture |
| `windows-collections` | 0.2.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-collections` | 0.3.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-core` | 0.57.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-core` | 0.61.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-core` | 0.62.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-future` | 0.2.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-future` | 0.3.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-implement` | 0.57.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-implement` | 0.60.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-interface` | 0.57.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-interface` | 0.59.3 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-link` | 0.1.3 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-link` | 0.2.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-numerics` | 0.2.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-numerics` | 0.3.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-registry` | 0.6.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-result` | 0.1.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-result` | 0.3.4 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-result` | 0.4.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-strings` | 0.4.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-strings` | 0.5.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-sys` | 0.59.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-sys` | 0.61.2 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-targets` | 0.52.6 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-threading` | 0.1.0 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `windows-threading` | 0.2.1 | MIT OR Apache-2.0 | https://github.com/microsoft/windows-rs |
| `winnow` | 1.0.5 | MIT | https://github.com/winnow-rs/winnow |
| `winreg` | 0.55.0 | MIT | https://github.com/gentoo90/winreg-rs |
| `winreg` | 0.56.0 | MIT | https://github.com/gentoo90/winreg-rs |
| `winresource` | 0.1.34 | MIT | https://github.com/BenjaminRi/winresource |
| `writeable` | 0.6.4 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `xmlwriter` | 0.1.0 | MIT | https://github.com/RazrFalcon/xmlwriter |
| `y4m` | 0.8.0 | MIT | https://github.com/image-rs/y4m.git |
| `yoke` | 0.8.4 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `yoke-derive` | 0.8.4 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zed-scap` | 0.0.8-zed | MIT | https://github.com/helmerapp/scap |
| `zerocopy` | 0.8.63 | BSD-2-Clause OR Apache-2.0 OR MIT | https://github.com/google/zerocopy |
| `zerocopy-derive` | 0.8.63 | BSD-2-Clause OR Apache-2.0 OR MIT | https://github.com/google/zerocopy |
| `zerofrom` | 0.1.8 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zerofrom-derive` | 0.1.8 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zerotrie` | 0.2.5 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zerovec` | 0.11.9 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zerovec-derive` | 0.11.7 | Unicode-3.0 | https://github.com/unicode-org/icu4x |
| `zlib-rs` | 0.6.8 | Zlib | https://github.com/trifectatechfoundation/zlib-rs |
| `zmij` | 1.0.23 | MIT | https://github.com/dtolnay/zmij |
| `ztracing` | 0.1.0 | Apache-2.0 | https://crates.io/crates/ztracing |
| `ztracing_macro` | 0.1.0 | Apache-2.0 | https://crates.io/crates/ztracing_macro |
| `zune-core` | 0.5.3 | MIT OR Apache-2.0 OR Zlib | https://github.com/etemesi254/zune-image |
| `zune-inflate` | 0.2.54 | MIT OR Apache-2.0 OR Zlib | https://github.com/etemesi254/zune-image/tree/main/zune-inflate |
| `zune-jpeg` | 0.5.15 | MIT OR Apache-2.0 OR Zlib | https://github.com/etemesi254/zune-image/tree/dev/crates/zune-jpeg |

<!-- END GENERATED -->
