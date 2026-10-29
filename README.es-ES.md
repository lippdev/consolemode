# Console Mode

Convierte tu PC con Windows en una **consola** con un clic: enfoca la TV, apaga las demás pantallas, cambia el audio y abre la interfaz de tus juegos. Al salir del juego, tu escritorio vuelve solo.

[English](README.md) · [Português (Brasil)](README.pt-BR.md) · Español · [Deutsch](README.de-DE.md) · [Français](README.fr-FR.md) · [Русский](README.ru-RU.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Versión](https://img.shields.io/github/v/release/lippdev/consolemode?label=versi%C3%B3n&color=brightgreen)
![Licencia](https://img.shields.io/github/license/lippdev/consolemode)

<img src="assets/console-mode-1.6-short.gif" alt="Recorrido por Console Mode 1.6: un clic lleva el juego a la TV, el menú de la sesión sobre el juego con las ventanas abiertas, el explorador de archivos ControlFS integrado, la interfaz Consola con sus pestañas Inicio, Sesión y Sistema, y los atajos del mando que tú eliges." width="800">

## Descarga

Descarga la última versión estable desde **[la página de releases](https://github.com/lippdev/consolemode/releases/latest)**:

- **`ConsoleMode-Setup-x64.exe`** (recomendado): instalación por usuario, sin permisos de administrador, se actualiza sola.
- **`ConsoleMode-Portable-x64.exe`**: un solo exe que guarda sus datos a su lado.
- O con winget: `winget install lippdev.ConsoleMode`

¿Quieres probar lo que viene antes que nadie? Activa Ajustes → "Recibir versiones de prueba (alfa y beta)".

Windows 10 u 11, más [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) o la aplicación Xbox. [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) es opcional, para el límite de FPS y el contador de FPS.

## Cómo funciona

1. Abre la aplicación y elige la pantalla en la que juegas. Las demás se apagan.
2. Pulsa **Jugar ahora**, o usa el acceso directo de un clic del escritorio. La primera vez en una pantalla nueva, la TV pregunta "¿Ves esta pantalla?" y, si nadie responde, todo vuelve atrás solo.
3. Sal de Big Picture o de Playnite y todo vuelve a su sitio: pantallas, audio y resolución.

¿Prefieres el sofá? Con la aplicación en la bandeja, mantén pulsado tu atajo del mando y empieza desde ahí.

Los atajos del mando los eliges tú: al principio no hay ninguno activado, la aplicación te pregunta la primera vez que la abres (y puedes cambiarlos en Ajustes). Un mismo botón no puede servir para dos acciones.

| Atajo del mando | Qué hace | Sugerido |
|---|---|---|
| Mantén tu atajo para **abrir** | Entra en el modo consola desde la bandeja | **Home** (Xbox / PS) |
| Mantén tu atajo del **menú** | Menú sobre el juego: ventanas abiertas, explorador de archivos, volumen, resolución, audio, FPS, HDR, salir | **Select + Y** (Create + △) |
| Mantén tu atajo para **volver al PC** | Volver al PC | **Start + Select** (Options + Create) |

## Funciones

- Oculta las demás pantallas **desconectándolas**, con **pantallas negras** o por **DDC/CI**, todo con las propias API de Windows: no se incluye ninguna herramienta de terceros de pantalla o audio
- **Resolución y frecuencia de actualización** por pantalla, **HDR**, **VRR** y **límite de FPS** (RTSS) mientras juegas
- **Contador de FPS** sobre el juego (RTSS) en una tarjeta de tres tamaños: solo FPS, compacto (uso de CPU, GPU y RAM), detallado (relojes, temperatura y memoria de la GPU) o tu propia disposición
- El audio va a la TV (o a cualquier salida) y vuelve después
- [Enciende la TV y la cambia a la entrada del PC](docs/GUIDE.es-ES.md#control-de-la-tv) (Google TV / Android TV por la red), y puede ponerla en reposo después
- Abre **Steam Big Picture**, **Playnite a pantalla completa**, el **modo Xbox** o [JoyChromium](https://github.com/lippdev/joychromium) (la web con el mando), y envía Steam a la bandeja al volver al PC
- **Menú de la sesión** sobre el juego: cambia o cierra las ventanas abiertas, ajusta volumen, resolución, audio, FPS y HDR, oculta el puntero del ratón o muévelo con el mando, y explora tus archivos con [ControlFS](https://github.com/nextestudios/ControlFS)
- Dos interfaces: **Escritorio** (ratón) y **Consola** (pantalla completa, mandos de Xbox y PlayStation, pestañas con LB/RB, un fondo de pantalla propio o tus portadas de Steam de fondo)
- Automatización con los enlaces `consolemode://start` / `stop` / `menu` (Stream Deck, scripts) y una [API de control local](docs/GUIDE.es-ES.md#api-de-control-local)
- Inglés, portugués, español, alemán, francés y ruso

Las novedades de cada versión están en el [CHANGELOG.en-US.md](CHANGELOG.en-US.md) (en inglés).

## Más

- [Guía](docs/GUIDE.es-ES.md): modos y restauración, API de control, HDR/VRR/FPS, solución de problemas
- **Comentarios:** el botón junto a Ajustes en la aplicación, o [este formulario](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Una ⭐ ayuda a que otros jugadores de sofá encuentren el proyecto.
- Las versiones todavía no están firmadas digitalmente, así que Windows SmartScreen o un antivirus pueden avisar al descargarlas; todo el código fuente está aquí.
- ¿Quieres ayudar a traducir? Consulta [docs/TRANSLATING.md](docs/TRANSLATING.md).

[GNU Affero General Public License v3.0 only](LICENSE) · [Avisos de terceros](THIRD_PARTY_NOTICES.md) · [Política de firma de código](docs/CODE_SIGNING.md)

*Esta página es una traducción del README en inglés. Si algo no coincide, la versión en inglés es la vigente.*
