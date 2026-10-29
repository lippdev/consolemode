# Guía de Console Mode

Detalles que no caben en el [README](../README.es-ES.md).

[English](GUIDE.md) · [Português (Brasil)](GUIDE.pt-BR.md) · Español · [Deutsch](GUIDE.de-DE.md) · [Français](GUIDE.fr-FR.md) · [Русский](GUIDE.ru-RU.md)

## Modos y restauración

| Modo | Al salir |
|------|---------|
| **Steam Big Picture** | Restauración automática (la aplicación sigue en la bandeja) |
| **Playnite a pantalla completa** | Restauración automática (la aplicación sigue en la bandeja) |
| **Modo Xbox** | Manual: usa *Restaurar ahora*, el menú de la bandeja o vuelve a abrir la ventana |
| **Aplicación personalizada** | Restauración automática cuando se cierra la aplicación (o el proceso que elegiste vigilar) |
| **JoyChromium** | Restauración automática al cerrar JoyChromium; si vuelves al PC antes, queda abierto como ventana |

Si la aplicación se cierra o el PC se apaga en medio de una sesión, la próxima vez que Console Mode se abra (también al iniciar sesión en Windows, si arranca con Windows) restaura todo lo que la sesión cambió: pantallas, audio, RTSS, iconos del escritorio y el puntero, y lo avisa.

También puedes restaurar en cualquier momento desde la bandeja (*Restaurar pantallas* / *Mostrar ventana*). Con las pantallas negras, **ESC** quita las cortinas.

El menú de la sesión tiene dos salidas. **Volver al PC** termina la sesión y devuelve pantallas, resolución, HDR, audio y límite de FPS a como estaban; la aplicación sigue en la bandeja. **Salir de Console Mode** hace lo mismo y además cierra la aplicación, así que los atajos del mando dejan de funcionar hasta que la vuelvas a abrir. La primera vez que se abre el menú, ofrece un recorrido que explica cada opción; para verlo más tarde, pulsa **Y** (△ en PlayStation) o **F1** en el menú, o usa **Ajustes → Recorrido del menú de sesión**.

## Ajustes en la interfaz Consola

En **Sistema → Mando**, elige el diseño de botones: **Automático**, **Xbox** o **PlayStation**. La vista previa y las etiquetas de los atajos siguen esa elección, que se guarda al cerrar la aplicación. Esto solo cambia los símbolos que se muestran; las entradas del mando mantienen su asignación.

La pestaña **Sistema** ofrece los mismos ajustes que la interfaz Escritorio, incluidos los atajos del mando, Playnite, Android TV, el FPS personalizado y la carpeta de datos. Las dos interfaces comparten los valores guardados, que se mantienen al reiniciar la aplicación.

Abre la acción del atajo que quieras configurar, elige **Definir** o **Cambiar**, mantén la combinación deseada en un mando y suelta los botones. **Quitar** desactiva ese atajo. Durante la captura, los botones del mando no navegan por la interfaz; usa **Cancelar** o **Esc** para detener la captura. Fuera de la captura, **B** sale del editor. Los campos de dirección y de FPS personalizado aceptan el teclado.

Al volver de Big Picture, **Enviar Steam a la bandeja al volver al PC** cierra la ventana del cliente y deja Steam en marcha. Con la opción desactivada, el cliente de escritorio se queda abierto. Las ventanas de Steam no se tocan si hay un juego en marcha o si no se puede comprobar su estado.

Cambiar la pantalla principal puede hacer que Windows reordene los iconos del escritorio. **Devolver los iconos del escritorio a su sitio** (activado por defecto) guarda su posición antes de la sesión y los vuelve a colocar después de restaurar el escritorio. No hace nada si **Organizar iconos automáticamente** está activado en el escritorio, porque entonces Windows los coloca por sí mismo.

**Apartar el puntero del ratón** (activado por defecto): cuando el iniciador aparece en la TV, el puntero va a la esquina inferior derecha de la pantalla de juego, y otra vez unos segundos después, porque Steam lo vuelve a centrar al abrir Big Picture. Al volver al PC, vuelve a donde estaba. Ocultar el puntero del todo sigue siendo una opción del menú de la sesión.

**Despertar el PC con el mando** permite que el adaptador inalámbrico Xbox, el Bluetooth o un mando con cable despierten el PC de la suspensión con el botón central del mando. **Activar** pide permiso de administrador a Windows y marca *Permitir que este dispositivo reactive el equipo* en esos dispositivos. No despierta un PC apagado o en hibernación, y algunos PC cortan la energía del Bluetooth al suspender. Junto con **Entrar al conectar un mando**, el PC despierta directamente en modo consola.

Los **perfiles por juego** dan a cada juego de Steam su propia resolución, HDR, límite de FPS y salida de audio. Con el juego en marcha, abre el menú de la sesión y elige **Perfil del juego → Guardar los ajustes actuales para…**: se guarda lo que usa la pantalla de juego en ese momento. La próxima vez que ese juego se abra durante una sesión, Console Mode aplica el perfil y, cuando el juego se cierra, la pantalla vuelve a estar como antes. El juego se detecta a través de Steam, tanto si se abre desde Big Picture como desde Playnite o la aplicación Xbox. Los perfiles guardados aparecen en Ajustes → **Perfiles por juego**, donde puedes borrarlos (en la interfaz Consola: Sistema → Al entrar en modo consola).

## API de control local

Los enlaces `consolemode://` se lanzan sin esperar respuesta. Las herramientas que necesitan una respuesta (un agente de control remoto que funciona como servicio de Windows, un plugin de Stream Deck que muestra si el modo consola está activo) pueden usar la canalización con nombre (named pipe) `\\.\pipe\ConsoleMode.Control` mientras la aplicación está abierta: envía una línea JSON y recibe otra.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` y `show` hacen exactamente lo mismo que el enlace `consolemode://` correspondiente y responden cuando la aplicación termina (`ok:false` con un `error` si el modo consola no se inició o la restauración no terminó). `status` solo lee. Solo pueden conectarse el usuario que ha iniciado sesión y LocalSystem; nada se expone a la red.

## Control de la TV

Ajustes → **TV** puede encender la TV y cambiarla a la entrada HDMI del PC cuando empieza el modo consola, y, si quieres, volver a ponerla en reposo cuando se restaura el escritorio. Una TV que no responde nunca bloquea el modo consola: la aplicación lo anota en el registro y espera a la pantalla de juego como siempre.

Con la opción predeterminada, **No controlarla**, no se envía nada y no se añade nada al inicio ni a la restauración. Con una ruta elegida, el paso de la TV va primero cuando empieza el modo consola y dura como máximo **30 segundos** (el peor caso, cuando la TV no responde y se prueba Wake-on-LAN); el reposo opcional al restaurar dura como máximo **15 segundos**. Una TV que ya está encendida suele responder en uno o dos segundos.

La mayoría de las tarjetas gráficas de PC no pueden enviar HDMI-CEC, así que la aplicación habla con la TV por la red:

### Google TV / Android TV

TCL, Sony, Hisense, Philips y otras TV con Google TV o Android TV, mediante ADB (el protocolo de depuración de Android). No hay que instalar nada en el PC.

1. En la TV: **Ajustes → Sistema → Información**, pulsa 7 veces **Compilación del SO de Android TV** (Android TV OS build) para desbloquear las Opciones de desarrollador.
2. **Ajustes → Sistema → Opciones de desarrollador**: activa **Depuración USB** (en algunas TV, **Depuración por red** / **ADB por red**).
3. En Console Mode, elige *Google TV / Android TV (red)*, escribe la IP de la TV (Ajustes → Red en la TV; resérvala en el router) y la entrada HDMI que usa el PC.
4. Pulsa **Probar ahora**. La TV pregunta "¿Permitir depuración desde este ordenador?": marca **Permitir siempre** y pulsa **Permitir**.

La clave ADB del PC se guarda cifrada para tu usuario de Windows (DPAPI), así que la TV solo pregunta una vez. Si copias la carpeta de datos a otro usuario de Windows o a otro PC, la clave no se puede leer allí: la aplicación crea una nueva y la TV vuelve a preguntar "¿Permitir depuración?".

Para encenderla se usa la tecla de activación de Android y después la tecla **HDMI 1-4**. Si tu TV ignora esa tecla, rellena **Comando de la entrada (avanzado)** con cualquier comando de shell de Android que abra la entrada del PC. Si la TV se desconecta de la red en reposo, rellena su **Dirección MAC** para que la aplicación envíe antes Wake-on-LAN (la opción de la TV "Activar por red" / "Activar por Wi-Fi" (Wake on network / Wake on Wi-Fi) tiene que estar activada).

La "Depuración inalámbrica" con código de emparejamiento (móviles con Android 11 o posterior) es otro protocolo, envuelto en TLS, y no es compatible: usa la depuración USB / por red.

## Extras opcionales

### HDR

Activa el HDR en la pantalla de juego mientras el modo consola está activo. Se vuelve a desactivar (o a su estado anterior) al salir.

### VRR

Console Mode puede activar o desactivar el ajuste de optimización de VRR de Windows. Para mejores resultados, activa también VRR / G-SYNC / FreeSync en el **panel de control de la GPU** (NVIDIA o AMD).

### Límite de FPS (RTSS)

Limita la tasa de fotogramas global mientras el modo consola está en marcha (útil en una TV de 60 Hz). Requiere RTSS instalado y en ejecución. El límite anterior se restaura al salir.

## Limitaciones

- Las configuraciones con varios monitores varían; en algunos equipos la restauración puede necesitar un segundo intento desde la bandeja
- El modo Xbox no detecta cuándo termina la pantalla completa: restaura a mano
- El límite de FPS es global (limitación de RTSS), no por pantalla
- La versión WinUI 3 de momento requiere Windows para compilar (`net8.0-windows`)

## Solución de problemas

### La disposición del escritorio no se restauró

Abre el menú de la bandeja y elige **Restaurar pantallas**. Si la disposición sigue sin estar bien, vuelve a elegir **Restaurar pantallas** cuando Windows termine de aplicar el cambio de monitores. También puedes volver a abrir la ventana desde la bandeja y restaurar a mano.

### El audio se quedó en la salida anterior

Comprueba que la salida de destino está conectada y disponible en Windows antes de iniciar el modo consola. Con salidas HDMI o de TV, puede que haga falta volver a conectar el cable e iniciar el modo de nuevo.

### El mando aparece pero no hace nada (DualSense / DualShock)

Abre **Ajustes → Probar mando**: muestra en directo lo que Windows recibe de cada mando. Si la lectura sigue vacía mientras pulsas botones, lo más probable es que Steam esté capturando el mando (Steam abierto con la compatibilidad con PlayStation de Steam Input activada lo convierte en teclado y ratón en el escritorio). Cierra Steam, o desactiva la compatibilidad con PlayStation en Steam Input, y vuelve a probar. Si la lectura sigue sin mostrar nada, usa **Copiar diagnóstico** y pégalo en el formulario de comentarios.

### El HDR o el VRR no cambiaron

Confirma que la pantalla de juego admite la función y que el HDR está activado en Windows. Para el VRR, activa también G-SYNC o FreeSync en el panel de control de la GPU cuando corresponda.

## Compilar desde el código fuente (Windows)

Esta sección solo está en inglés: [GUIDE.md#build-from-source-windows](GUIDE.md#build-from-source-windows).

*Esta página es una traducción de la guía en inglés. Si algo no coincide, la versión en inglés es la vigente.*
