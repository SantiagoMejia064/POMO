# Sprint 3 — Memoria: presentación y feedback de Pomo

Implementación en `Assets/Scenes/Steven 1.unity`, dentro de `Canvas/Minijuegos`.
Referencia: pantalla Memoria aportada por Steven y secciones 3, 5, 6 y 7 de
“Documentación del prototipo.md”.

No se ejecutaron pruebas, compilación, Play Mode ni builds por parte de Codex.
Los comportamientos de esta guía son resultados esperados para la validación manual.

## Qué se implementó

- Pantalla dividida en encabezado, sonido/puntuación, tablero, feedback y Pomo.
- Conservación del tablero de 12 cartas / 6 parejas, organizado en 4 columnas y 3 filas.
- Cartas crema sin signos de interrogación en el reverso, imágenes de Valentina
  en el frente y un color distinto para parejas encontradas.
- Distribución que adapta el tamaño de las cartas al espacio disponible también
  en edición, manteniendo el GridLayoutGroup editable.
- Texto de instrucciones, parejas, intentos y rondas debajo del tablero.
- Tiempo visible MM:SS y aviso de color durante los últimos 30 segundos.
- EMPEZAR antes de jugar y PAUSAR/CONTINUAR durante el juego. La X conserva el retorno a Inicio.
- Resultado encima del tablero, dejando visible a Pomo debajo. Muestra tiempo
  activo, puntuación, parejas, intentos, rondas, precisión y mensaje motivacional.
- El resultado permanece 8 segundos y vuelve automáticamente a la pantalla de
  EMPEZAR, con los contadores en cero. No hay botón dentro del resultado.
- Result Display Seconds permite ajustar esa espera desde el Inspector.
- Adaptación de MemoryGameContent al área segura del dispositivo durante Play.
- Seis estados de Pomo: Idle, Observe, Match, Mismatch, RoundComplete y Encourage.
- Pausa de la animación junto con el reloj y las cartas.
- Cambio de pose, signo de duda y movimientos mediante un solo controlador visual.
- Eventos de sonido adicionales para cada carta realmente volteada y clic de controles.
- Protección del marcador: muestra 9999+ si el total excede cuatro dígitos; el
  resultado final conserva el puntaje completo. No se reinicia al superar 9999.
- Herramienta antigua del Editor actualizada: ya no busca scoreText ni
  soundToggleLabel, y no reconstruye automáticamente el diseño editado a mano.

Se conserva la sesión de 300 segundos: cada tablero completado inicia otra
ronda sin borrar puntos, intentos ni parejas totales. La reacción de ronda
se deja terminar antes de mostrar las cartas del tablero nuevo; el tiempo sigue corriendo.
Al finalizar los 300 segundos, se conserva el resumen durante su tiempo de lectura
y después se prepara una sesión nueva en espera. Esa nueva sesión requiere EMPEZAR.

## Jerarquía principal

```text
Canvas
  Minijuegos                           [MemoryGameController + AudioSource]
    MemoryGameContent                  [ScreenSafeArea]
      Background
      Header
        Volver
        Título
      StatsBar
        Sonido del minijuego
        Puntos
        ScoreOverflow
      Tablero
        Cuadrícula de cartas           [GridLayoutGroup + MemoryCardGridLayout]
          Carta 01 ... Carta 12
      FeedbackPanel
        Instrucción
        Pares
        Intentos
        TimerBadge
          Tiempo restante
      PomoFeedbackArea                 [PomoVisualController]
        Pomo                           [Image + Animator]
        ReactionSymbol
        Empezar o reiniciar
        Pausar o continuar
      Resultado final                  [desactivado inicialmente]
        Mensaje final
```

Minijuegos queda desactivado y Panel INICIO activado al guardar la escena.
Los objetos del minijuego existen fuera de Play. Para editarlos puedes activar
temporalmente Minijuegos, desactivar Panel INICIO y seleccionar sus hijos.
Antes de probar el recorrido desde Inicio, restablece Minijuegos desactivado
y Panel INICIO activado.

## Scripts y responsabilidades

| Archivo | Responsabilidad |
|---|---|
| `Presentation/Screens/MemoryGameController.cs` | Conecta estados reales con UI, Pomo y audio; captura el resumen y lo cierra por tiempo, sin salir del minijuego. FinalResultReady se emite una vez por partida. |
| `Presentation/Screens/MemoryCardView.cs` | Reverso liso, figura revelada y color de pareja encontrada. |
| `Presentation/Screens/PomoVisualController.cs` | Cambia pose, reproduce un estado del Animator, interrumpe la reacción anterior y vuelve a Idle cuando corresponde. |
| `Presentation/UI/MemoryCardGridLayout.cs` | Calcula cartas cuadradas en 4×3 en edición y Play según el rectángulo, espaciado y padding del Grid. |
| `Presentation/UI/ScreenSafeArea.cs` | Ajusta el contenido al área segura del Canvas durante Play. |
| `ServicesAdapters/Audio/AudioService.cs` | Conserva la preferencia compartida y añade MemoryCardFlip/MemoryButtonClick. |
| `Editor/PomoMemoryMotionAssets.cs` | Prepara seis clips y un AnimatorController una sola vez durante la importación. |
| `Editor/MemoryGameSceneSetupUtility.cs` | Reconexión opcional de referencias del panel guardado; no es necesario usar menús para empezar a probar. |

Las rutas de scripts son relativas a `Assets/Scripts`, salvo las de Editor,
que están en `Assets/Editor`.

`Tools/PrepareMemoryPresentation.cjs` fue la migración aplicada a la escena.
No necesitas ejecutarla para usar ni para validar el minijuego.

## Recursos de Pomo y Animator

Al terminar la importación de scripts, Unity debe crear:

```text
Assets/Resources/Pomo/
  MemoryMotion.controller
  Idle.anim
  Observe.anim
  Match.anim
  Mismatch.anim
  RoundComplete.anim
  Encourage.anim
```

El controlador visual carga MemoryMotion en el Animator al entrar al minijuego
si no se ha asignado otro controller. Puedes asignarlo también manualmente
al campo Controller del Animator de Pomo. Los clips ya existentes no se sobrescriben.
Incluye esta carpeta y sus metadatos al hacer el commit después de la importación.

Estos movimientos son microgestos de la ilustración completa: respiración,
inclinaciones, asentimiento y pequeños saltos. No animan huesos ni reconstruyen
la boca o los brazos por separado.

Las poses actuales reutilizan Pomo symbol, Pomo explicando y pomo sonriendo.
La cara definitiva de duda/corrección todavía debe proporcionarse como recurso artístico.
No se presentan estas poses provisionales como las seis expresiones finales.

Para sustituir una pose:

1. Sal de Play.
2. Selecciona Canvas > Minijuegos > MemoryGameContent > PomoFeedbackArea.
3. En Pomo Visual Controller, despliega Poses.
4. Cada elemento tiene State, Sprite, Animator State y Duration.
5. Arrastra el PNG importado como Sprite al campo Sprite del estado correspondiente.
6. Conserva los nombres Idle, Observe, Match, Mismatch, RoundComplete y Encourage
   mientras se use el controller incluido. Puedes editar sus clips en Animation.
7. Guarda la escena.

Para ajustar los movimientos puedes modificar los .anim o asignar un AnimatorController
propio al Animator de Pomo, conservando esos seis nombres de estado en Base Layer.

| Estado | Disparador | Movimiento / pose provisional |
|---|---|---|
| Idle | Entrada o fin de reacción breve | Pomo base y respiración suave. |
| Observe | Revelación inicial de cada ronda | Pomo explicando, balanceo y puntos suspensivos. |
| Match | Pareja correcta | Sonrisa con pulgar arriba y movimiento corto. |
| Mismatch | Pareja incorrecta | Inclinación de duda y signo de interrogación. |
| RoundComplete | Sexta pareja o resultado de precisión alta | Pulgar arriba y pequeños saltos. |
| Encourage | Resultado sin parejas o precisión baja | Gesto suave de ánimo. |

## Ubicación de los sonidos

Selecciona `Canvas > Minijuegos`, no el objeto hijo Pomo.
En Memory Game Controller > Sonidos opcionales están:

- Sound Effect Audio Source: ya asignado al AudioSource de Minijuegos.
- Match Clip: pareja correcta.
- Mismatch Clip: pareja incorrecta.
- Round Completed Clip: tablero completo.
- Time Up Clip: fin de los cinco minutos.
- Card Flip Clip: cada carta aceptada.
- Button Click Clip: empezar, volver a jugar, continuar o activar sonido.

Los AudioClip son opcionales y continúan vacíos. En modo sonido ON aparece
un Debug.Log cuando falta un clip. En OFF no se pide reproducir el efecto ni
se escribe ese registro. Pomo puede seguir reaccionando visualmente con el sonido apagado.

## Paso a paso de validación manual

### 0. Cargar los cambios

1. Sal de Play, si estabas jugando.
2. Espera a que Unity termine de importar y recompilar los scripts.
3. Si ofrece recargar Steven 1 desde disco, acepta Reload para ver la nueva jerarquía.
4. Abre Assets/Scenes/Steven 1.unity.
5. Revisa si la consola muestra errores de importación o compilación. Si aparecen,
   comparte el primer error completo antes de seguir.
6. Comprueba que los seis clips y MemoryMotion.controller aparezcan en Assets/Resources/Pomo.

### 1. Pantalla y navegación

1. Inicia Play. Debe aparecer Inicio, sin el minijuego encima.
2. Pulsa MINIJUEGOS.
3. Espera ver Memoria, X de salida, sonido, puntuación 0000, tablero 4×3,
   instrucciones, tiempo 05:00 y Pomo debajo.
4. Antes de empezar, el tiempo no baja, las cartas no se pueden seleccionar y
   aparecen EMPEZAR y el mensaje de inicio. El resultado está oculto.
5. Pomo debe permanecer dentro de su zona con un movimiento suave.

### 2. Inicio y observación

1. Pulsa EMPEZAR una sola vez.
2. Las 12 figuras deben mostrarse durante 3 segundos y quedar bloqueadas.
3. El mensaje muestra el tiempo de observación restante y Pomo mira/gesticula.
4. El reloj baja desde 05:00. EMPEZAR se oculta y aparece PAUSAR.
5. Al terminar la observación, las cartas se ocultan, el mensaje permite elegir
   dos cartas y Pomo vuelve al estado base.

### 3. Error y bloqueo

1. Selecciona dos figuras diferentes.
2. Intentos aumenta exactamente en uno; puntos y parejas no aumentan.
3. Debe aparecer el mensaje de error. Pomo se inclina y muestra un signo de duda.
4. Intenta tocar una tercera carta durante la espera: no debe abrirse ni sumar intento.
5. Después de 0,9 s las dos cartas incorrectas se ocultan y puedes volver a elegir.
6. Pomo regresa al estado base al terminar la reacción breve.

### 4. Acierto y puntuación

1. Encuentra una pareja correcta.
2. Debe quedar visible y no permitir seleccionarla otra vez.
3. Intentos y parejas aumentan una vez; aparece el mensaje de acierto y pulgar arriba.
4. Sin errores desde el acierto anterior, la pareja vale 100 puntos.
5. Con un error previo, vale 90; con dos, 80; el mínimo es 10.
6. Fallar no resta puntos ya acumulados. El siguiente acierto vuelve a iniciar la regla.

### 5. Rondas continuas

1. Completa las seis parejas antes de agotar el reloj. Puedes anotar las posiciones
   durante la observación. Para facilitar esta prueba, fuera de Play puedes cambiar
   Preview Seconds de 3 a 10; restáuralo al terminar.
2. Debe aparecer Ronda 1 completa y una celebración de unos 2,2 s.
3. Se mezcla el tablero y ocurre otra observación inicial.
4. Pares de la ronda vuelve a 0/6; Total conserva 6 y Rondas muestra 1.
5. El puntaje y los intentos conservan lo acumulado. El reloj continúa desde
   el tiempo restante, no desde 05:00.

### 6. Pausa y continuación

1. Durante la partida pulsa PAUSAR.
2. Anota el tiempo y espera varios segundos.
3. El reloj, la fase de cartas y el movimiento de Pomo deben quedar detenidos.
4. Las cartas no deben admitir selecciones. El botón cambia a CONTINUAR.
5. Pulsa CONTINUAR: la sesión debe seguir desde el mismo tiempo y contadores.
6. Repite durante la observación, un error y la celebración de ronda.
7. En Android, si después se prueba en dispositivo, enviar la app al fondo debe
   pausarla; al volver se usa CONTINUAR. No se requiere un build para revisar este bloque ahora.

### 7. Sonido

1. Con SONIDO ON, voltea una carta y mira Console.
2. Sin clips debe aparecer MemoryCardFlip; al resolver, MemoryMatch o MemoryMismatch.
3. Con clips asignados desde el Inspector, debe escucharse el efecto correspondiente.
4. Desactiva sonido. El indicador debe cambiar a OFF y detener cualquier efecto activo.
5. Sigue jugando: Pomo y los contadores funcionan, sin nuevos efectos ni registros de reproducción.
6. Reactívalo: el indicador vuelve a ON y los siguientes eventos se reproducen o registran.
7. Cierra y vuelve a abrir Memoria en el mismo Play: la preferencia se conserva.

### 8. Fin por tiempo y resultado

1. Para una comprobación rápida, sal de Play y selecciona Canvas > Minijuegos.
2. En Memory Game Controller cambia Game Duration Seconds de 300 a 15.
3. Entra a Play, abre el minijuego y pulsa EMPEZAR.
4. Juega hasta que llegue a 00:00.
5. Las cartas deben bloquearse, PAUSAR desaparecer y el resultado aparecer sobre el tablero.
6. Compara puntos, parejas, intentos y rondas con lo visto durante la partida.
7. Pomo permanece visible debajo y da ánimo o celebra según la precisión.
8. La precisión es parejas totales / intentos. Cero parejas o menos del 50%:
   ánimo. Del 50% al 79%: gesto positivo. Desde el 80%: celebración.
9. Repite sin tocar cartas para revisar ausencia de datos: 0 puntos y mensaje de ánimo,
   sin divisiones por cero ni una celebración de logro.
10. El resumen debe indicar Tiempo jugado: 00:15 en esta prueba, precisión o Sin intentos,
    y una cuenta atrás que empieza en 8 s. No aparece un botón VOLVER A JUGAR.
11. Los datos se conservan durante esos 8 segundos; el tiempo de pausa y el tiempo
    de lectura del resultado no se suman al tiempo de juego activo.

### 9. Regreso automático a EMPEZAR

1. Deja el resultado visible sin tocar nada.
2. Después de 8 segundos debe desaparecer por sí solo, sin usar la X ni cambiar de sección.
3. Deben aparecer EMPEZAR y el mensaje inicial. PAUSAR queda oculto.
4. Puntos, parejas, intentos y rondas vuelven a cero; Pomo vuelve al estado base.
5. El tiempo vuelve a 00:15 si usas la configuración rápida, o a 05:00 con la normal.
6. Espera unos segundos: el reloj no debe avanzar hasta pulsar EMPEZAR.
7. Pulsa EMPEZAR. Debe iniciar la observación y luego aceptar cartas con normalidad.
8. Repite tres partidas seguidas sin salir del minijuego. El reloj debe avanzar una vez
   y los resultados no deben mezclarse entre partidas.
9. Fuera de Play, cambia Result Display Seconds a 5 o 10 para revisar otra duración.
   Al terminar, déjalo en 8 si quieres conservar la configuración inicial de este cambio.

### 10. Cerrar y volver

1. Durante una partida pulsa la X de arriba a la izquierda.
2. Debe volver a Inicio y cesar la sesión anterior.
3. Entra otra vez a MINIJUEGOS.
4. Debe estar en espera, con EMPEZAR y contadores en cero, sin resultados o gestos anteriores.
5. Inicia otra partida y comprueba que el reloj avance una vez, sin acelerarse por reaperturas.
6. Repite cerrando con la X mientras el resumen final está visible. Vuelve a entrar
   enseguida: debe mostrar EMPEZAR y no aparecer un reinicio atrasado de la partida anterior.

### 11. Adaptación visual

1. En Simulator/Game prueba una pantalla 1080×1920 y otra alta como iPhone 13 Pro.
2. Revisa que X, título, sonido y puntuación no queden bajo el notch.
3. Deben seguir visibles las 12 cartas, el mensaje, el tiempo y Pomo.
4. Ningún texto debe tapar cartas o botones; el resultado debe leerse completo y cerrarse solo.
5. Fuera de Play, activa temporalmente Minijuegos para comprobar que la misma
   jerarquía exista y que las cartas se ajusten al cambiar el rectángulo del tablero.

### 12. Restaurar la configuración

1. Sal de Play.
2. Deja Game Duration Seconds = 300, Result Display Seconds = 8,
   Preview Seconds = 3 y Mismatch Seconds = 0.9.
3. Desactiva Use Fixed Seed For Debug si lo activaste.
4. Deja Minijuegos desactivado y Panel INICIO activado.
5. Guarda Steven 1 con Ctrl+S.

## Relación con Jira y límites de este bloque

- HU 1.1 / POMO-246 y 247: presentación y revelación conservadas.
- HU 1.2 / POMO-250 y 251: comparación, colores de cartas y bloqueo conservados.
- HU 1.3 / POMO-255: conexión de acierto/error con el controlador visual implementada;
  la ilustración facial definitiva de error queda pendiente de arte y validación.
- HU 1.4 / POMO-257 y 259: temporizador, pausa, cierre y final conectados con la nueva UI.
- HU 1.5 / POMO-261 y 262: resultado y reacción final conectados.
- HU 1.6 / POMO-264: toggle conservado, más eventos de clic/carta.
- POMO-279 y 281: base reutilizable de seis estados, clips y controlador de Presentation.
  La integración en tareas, progreso y personalización debe hacerse en sus propios bloques.

No se marcan tareas completadas ni se actualiza Jira automáticamente.
FinalResultReady sigue siendo el punto de conexión para el equipo de recompensas/rachas;
este bloque no vuelve a otorgar copas ni conecta todavía tareas o progreso con Pomo.

Para reportar feedback, indica el número de recorrido, lo esperado y lo que apareció.
Si falla un recorrido, comparte el primer error de Console y la pantalla afectada.
