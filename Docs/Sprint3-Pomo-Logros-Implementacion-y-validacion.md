# Sprint 3 — Reacciones de tareas y celebraciones de logros

Entrega: 26 de octubre de 2026. Responsable: Steven.
Escena: `Assets/Scenes/Steven 1.unity`.
Estado: implementación entregada y recorrido de tareas/bloques confirmado por
Steven tras su revisión manual. La confirmación corresponde al último bloque
entregado; las integraciones compartidas indicadas más abajo siguen pendientes.
Codex no ejecutó compilación, pruebas automatizadas, Play Mode ni builds.

## 1. Tareas de Jira cubiertas

| Tarea | Implementación de esta entrega |
|---|---|
| [POMO-267 / S3-T43](https://nebuly-studios-pomo.atlassian.net/browse/POMO-267) | Señal de tarea completada desde el punto común de Business, después del guardado exitoso. Presentation consume el hecho y conserva los premios existentes. |
| [POMO-269 / S3-T45](https://nebuly-studios-pomo.atlassian.net/browse/POMO-269) | Reacción desde PomoVisualController con identificación por tarea y protección frente a repeticiones. |
| [POMO-299 / S3-T75](https://nebuly-studios-pomo.atlassian.net/browse/POMO-299) | Eventos reales de tareas y bloques completados, deduplicación y cola única. Refrescar datos no crea logros. |
| [POMO-301 / S3-T77](https://nebuly-studios-pomo.atlassian.net/browse/POMO-301) | Conexión de celebraciones al controlador visual común. El presentador no vuelve a entregar copas. |

HUs relacionadas: [POMO-82 / HU 8.1.1](https://nebuly-studios-pomo.atlassian.net/browse/POMO-82)
y [POMO-90 / HU 8.3.3](https://nebuly-studios-pomo.atlassian.net/browse/POMO-90).
No se modificaron estados de Jira. La presencia del código no sustituye la aceptación manual.

Pendientes compartidos: arte/animaciones de POMO-268 y mensajes/animaciones de
POMO-300, asignados a Valentina, y sus comprobaciones POMO-270/302. Se usan
recursos actuales como provisionales. Para rachas se entrega un contrato, no un
sistema nuevo de cálculo o persistencia de rachas.

## 2. Separación por capas

```text
Presentation: botones de lista/detalle/actividad o controlador del temporizador
    ↓ solicita la operación
Business: TaskService / ProgressService
    ↓ solicita guardar
Data: TaskRepository / ProgressRepository → JsonStorageService
    ↑ confirma el guardado
Business: PomoReactionService publica un PomoAchievement
    ↓ contrato inmutable definido en Shared
Presentation: PomoFeedbackCoordinator → cola → PomoAchievementView
    ↓
Presentation: PomoVisualController → Image / Animator / TextMeshPro
```

- **Shared** define datos de la señal y tipos de logro. No conoce Unity, vistas,
  modelos editables de Data, repositorios ni montos de recompensa.
- **Business** confirma el hecho después de guardar y publica su instantánea.
  El servicio de señales no conoce Presentation, Animator, Canvas ni TextMeshPro.
- **Data** conserva su formato y sus repositorios. No emite animaciones ni eventos visuales.
- **Presentation** se suscribe, organiza y muestra. No marca tareas completadas,
  guarda sesiones, calcula rachas ni asigna recompensas al presentar una tarjeta.
- **ServicesAdapters** conserva sus responsabilidades de notificaciones y audio;
  no se añadió allí lógica de logros o de interfaz.
- **Editor** solo muestra diagnósticos de eventos reales durante Play.
  **Tests** no fue modificado ni ejecutado.

## 3. Archivos y responsabilidad

Rutas relativas a la raíz del repositorio:

| Archivo | Responsabilidad |
|---|---|
| `Assets/Scripts/Shared/Enums/PomoAchievementKind.cs` — nuevo | TaskCompleted, FocusBlockCompleted y StreakMilestone. |
| `Assets/Scripts/Shared/Events/PomoAchievement.cs` — nuevo | Instantánea inmutable: tipo, ID, título, fecha, segundos trabajados y días de racha cuando corresponda. Su clave combina tipo e ID. |
| `Assets/Scripts/Business/Pomo/PomoReactionService.cs` — implementado | Publicación y deduplicación de logros confirmados. Antes era un MonoBehaviour vacío, sin referencias encontradas en escenas/prefabs/assets. Ahora es un servicio estático de Business; no se añade a un GameObject. |
| `Assets/Scripts/Business/Tasks/TaskService.cs` — ampliado | Recarga el estado persistido antes de una transición y publica la finalización después del guardado exitoso. |
| `Assets/Scripts/Business/Progress/ProgressService.cs` — ampliado | Publica un bloque completado cuando el repositorio acepta por primera vez su sessionId. |
| `Assets/Scripts/Presentation/Feedback/PomoFeedbackCoordinator.cs` — nuevo | Una suscripción activa, cola FIFO, deduplicación de recepción, temporización y suspensión mientras esté abierto Memoria. |
| `Assets/Scripts/Presentation/Feedback/PomoAchievementView.cs` — nuevo | Tarjeta con título, mensaje y origen. Gestiona visibilidad y llama al controlador visual. |
| `Assets/Scripts/Presentation/Screens/PomoVisualController.cs` — ampliado | Estados AchievementTask, AchievementBlock y AchievementStreak; método ShowAchievement. Conserva los valores anteriores del enum. |
| `Assets/Editor/PomoFeedbackCoordinatorEditor.cs` — nuevo | Contadores de recepción, presentación, cola, suspensión y origen actual. No genera hechos simulados. |
| `Assets/Scenes/Steven 1.unity` — configurado | Tarjeta nativa, referencias, perfiles, mensajes, área segura y coordinador activo. |
| `Tools/PreparePomoAchievementPresentation.cjs` — nuevo | Registro de migración puntual de la escena. Ya aplicado; no reconstruye una vista que ya existe. No es parte del juego ni un ejecutor de pruebas. |

Se añadieron los `.meta` de los archivos y carpetas nuevos de Assets.
La clase de PomoVisualController se reutiliza en Memoria, desempeño y logros;
cada vista tiene su propia instancia y sus propios sprites/estado contextual.

## 4. Orden de las operaciones

### Tarea completada

1. La UI solicita `TaskService.TransitionStatus(taskId, Completed)`, directamente
   o a través de `CompleteTaskFromActivity`.
2. El servicio recarga el repositorio. Esto evita que una segunda instancia de
   TaskService opere sobre un estado anterior al que ya guardó otra pantalla.
3. Verifica existencia, cambio real y transición permitida. Un estado repetido
   devuelve StatusUnchanged; una transición inválida devuelve InvalidTransition.
4. Asigna estado y fecha, y pide `TaskRepository.Update`. Si el guardado lanza
   una excepción o no encuentra la tarea, no se publica el logro.
5. Tras ese guardado, publica TaskCompleted con el ID y título guardados.
6. Continúa la cancelación de recordatorios existente y devuelve el resultado.
7. Las rutas actuales de TasksScreenController/TaskListController conservan
   `TryAwardCompletedTask` y su protección persistida de una recompensa por tarea.
8. El coordinador solo encola durante la notificación. La tarjeta se muestra
   desde Update, después de que la operación síncrona y su flujo de UI hayan continuado.

El evento representa una tarea guardada como completada. No significa que se
haya ganado necesariamente una recompensa: por ejemplo, las reglas anteriores
pueden asignar cero copas a una tarea completada después de su fecha de entrega.
La tarjeta no anuncia cantidades de copas que todavía no haya confirmado ese flujo.

### Bloque completado

1. PomodoroController llega al final del trabajo y crea la sesión con su sessionId.
2. `ProgressService.RecordFocusSession` solicita el guardado al repositorio.
3. ProgressRepository rechaza un sessionId que ya esté registrado como completado
   o interrumpido, y guarda la nueva sesión antes de devolver éxito.
4. Solo después de ese éxito, Business publica FocusBlockCompleted si la sesión
   no está interrumpida y tiene ID y tiempo trabajado válidos.
5. Continúa el evento existente StudySessionsChanged para actualizar métricas.
6. PomodoroController conserva su entrega actual de 20 copas por bloque nuevo,
   las notificaciones y los eventos que ya utilizaba el temporizador.
7. La cola presenta el reconocimiento; ninguna de sus funciones llama a AddCups.

Una sesión interrumpida sigue registrándose para sus métricas existentes, pero
no se publica como bloque completado. La finalización de una tarea y la de un
bloque son dos hechos distintos: si ocurren ambos, corresponden dos tarjetas
ordenadas, una por cada hecho.

### Deduplicación y fallos de observadores

- **Datos:** el estado completado de una tarea y el sessionId guardado del bloque
  impiden producir una finalización nueva al recargar el historial.
- **Publicación:** PomoReactionService conserva una clave `tipo:ID` por logro
  durante la sesión de ejecución y publica cada clave una sola vez.
- **Presentación:** el coordinador admite una vez cada clave y presenta una
  tarjeta por vez; no se añadió un observador separado para el mismo logro de tarea.
- Cada observador se invoca por separado. Su excepción se informa mediante
  ObserverFailed, que el coordinador registra en Console, sin invalidar el
  guardado ya confirmado ni impedir avisar a los demás observadores.
- Abrir paneles, refrescar progreso, consultar saldo o gastar copas no llama a
  los puntos de publicación. CupsBalanceChanged y StudySessionsChanged no son
  fuentes de celebraciones.

## 5. Jerarquía y configuración visual

```text
Canvas
└─ Pomo feedback                        [ScreenSafeArea + PomoFeedbackCoordinator]
   └─ Reconocimiento de Pomo            [CanvasGroup + PomoAchievementView + PomoVisualController]
      ├─ Pomo animado                   [Animator]
      │  ├─ Pose anterior               [Image]
      │  └─ Pose actual                 [Image]
      ├─ Título del logro               [TextMeshPro]
      ├─ Mensaje                        [TextMeshPro]
      └─ Origen del logro               [TextMeshPro]
```

Todos los objetos ya están guardados en la escena. El coordinador y la tarjeta
permanecen activos en la jerarquía; su CanvasGroup comienza con Alpha = 0.
Esto permite escuchar eventos sin tener que crear interfaz en Play.

La tarjeta dura al menos tres segundos, usa Poppins y la paleta azul actual y
está situada debajo del margen superior del área segura. No incluye botones,
no captura clics ni cambia Time.timeScale. Su duración usa tiempo no escalado.
La comprobación del espacio en el dispositivo real queda para Steven.

### Modificar mensajes y arte

1. Seleccionar `Reconocimiento de Pomo` fuera de Play.
2. En PomoAchievementView, editar Task/Block/Streak Heading y Message.
3. `Minimum Display Seconds` controla la lectura. Nunca se recorta por debajo
   de la duración configurada de la reacción visual.
4. En PomoVisualController → Poses, sustituir los sprites de AchievementTask,
   AchievementBlock y AchievementStreak por los que entregue Valentina.
5. Si cambia el movimiento, asignar su nombre exacto en Animator State y el
   controller correspondiente al Animator de `Pomo animado`.
6. Para acomodar la tarjeta en edición, se puede subir temporalmente el Alpha
   del CanvasGroup a 1. Restaurarlo a 0 y guardar antes de revisar el flujo normal.

Los perfiles actuales usan `pomo sonriendo.png` para los reconocimientos y
movimientos genéricos ya disponibles: Match para tareas y RoundComplete para
bloques/rachas. Son recursos provisionales, no el arte final de POMO-268/300.
Los estados nuevos del enum visual se añadieron al final (10, 11 y 12).
No se renumeraron los anteriores ni se modificaron los clips compartidos.

### Memoria y ciclo de vida

El coordinador tiene `Minijuegos` asignado en Suppress While Panels Active.
Mientras ese panel esté activo, oculta la tarjeta y pausa su movimiento y el
tiempo de lectura. Conserva la cola. Al salir de Memoria, continúa la tarjeta
pendiente o presenta la siguiente. El resumen de ocho segundos del minijuego
mantiene su presentación propia.

Al iniciar una ejecución, se limpia el estado estático de publicación; el
coordinador conecta su suscripción y oculta la tarjeta. Hay reconexión tras la
carga para contemplar la configuración de recarga del Editor. OnDisable retira
las suscripciones y descarta la cola local. No debe desactivarse `Pomo feedback`
para navegar: la navegación afecta a los paneles, no al coordinador.

No se creó un historial persistido de celebraciones ni se releen logros viejos
para mostrarlos. Las celebraciones pendientes son transitorias y se descartan
al salir de Play o descargar la escena. El guardado de la tarea/bloque permanece.

## 6. Contrato para rachas y futuras fuentes

La fuente real de rachas debe calcular sus reglas y guardar el hito antes de llamar:

```csharp
using Pomo.Business.Pomo;

// Solo dentro del flujo que acaba de confirmar y guardar un hito nuevo.
PomoReactionService.ReportRecordedStreakMilestone(
    milestoneId: idPersistidoDelHito,
    days: diasConfirmados,
    reachedAt: fechaConfirmada);
```

El ID debe distinguir la regla, la racha concreta y el hito; no usar únicamente
`"3dias"` para todas las rachas. La fuente debe impedir reproducir hitos históricos
al arrancar y duplicarlos entre ejecuciones. El servicio de esta entrega filtra
IDs repetidos durante una ejecución; no crea un repositorio de rachas.

Actualmente ninguna pantalla llama a este contrato ni calcula rachas a partir
del saldo. Ese escenario sigue pendiente de una fuente real y no está validado.

Para nuevas fuentes, seguir estas reglas:

- Tareas y bloques deben seguir pasando por sus servicios actuales. No invocar
  una celebración adicional desde cada botón, porque duplicaría la reacción.
- Para actividad vinculada, usar `TasksScreenController.OnLinkedActivityCompleted(taskId)`
  o `TaskService.CompleteTaskFromActivity(taskId)` según la capa del consumidor.
  La tarea debe estar iniciada. Ambos recorridos llegan al punto común de transición.
- Los productores actuales y observadores trabajan en el hilo principal de Unity.
  Una futura fuente asíncrona debe volver a ese hilo antes de publicar; la cola
  de Presentation no es un mecanismo de despacho entre hilos.
- Para nuevos tipos, ampliar Shared, el productor de Business, el mensaje de la
  vista y ShowAchievement. Mantener los valores visuales ya serializados.
- `ResetForNewSession` pertenece al arranque. No llamarlo desde botones de
  navegación, Refresh o pantallas de saldo.

## 7. Recorrido manual para Steven

### Preparación y diagnóstico

1. Abrir `Steven 1` y dejar que Unity termine de importar. Si informa un cambio
   de escena en disco, revisar la recarga para ver la jerarquía nueva.
2. Fuera de Play, buscar `Canvas > Pomo feedback`. El coordinador debe tener
   Achievement View asignado y Minijuegos en su lista de supresión.
3. En `Reconocimiento de Pomo`, revisar referencias del CanvasGroup, los textos
   y el controlador visual. Su Alpha inicial debe ser 0, con Interactable y
   Blocks Raycasts desmarcados. Mantener activos ambos GameObjects.
4. Entrar a Play. No debe aparecer ninguna celebración por las tareas que ya
   estaban completadas antes de esta ejecución.
5. Seleccionar `Pomo feedback`: el Inspector muestra Logros recibidos,
   Celebraciones iniciadas, En cola, Oculto por otro panel, tipo e ID actuales.
   Se pueden anotar los valores antes/después de cada caso.
6. Opcionalmente marcar Log Presentations durante Play para ver en Console
   cada presentación real, su tipo, ID y número. No hay botones para crear logros falsos.

### A. Tarea nueva, inicio y finalización — POMO-267/269

1. Desde Tareas, crear una tarea con nombre único, por ejemplo `Feedback A`,
   y una fecha de entrega futura. Anotar el saldo de copas.
2. Crear la tarea no debe aumentar los contadores de logros ni mostrar celebración.
3. Pulsar Comenzar. Debe pasar a En progreso, sin celebración de tarea completada.
4. Pulsar Completar en esa tarea. Debe aparecer una única tarjeta con
   **¡TAREA COMPLETADA!**, el mensaje positivo, el nombre de la tarea y la pose de Pomo.
5. En el coordinador, Logros recibidos y Celebraciones iniciadas deben aumentar
   en uno. El tipo debe ser TaskCompleted y el ID debe corresponder a esa tarea.
6. Tras unos tres segundos debe ocultarse. Los controles de la aplicación deben
   seguir respondiendo mientras la tarjeta se muestra.
7. La recompensa debe ser la que ya establecía el sistema y entregarse una vez.
   Como referencia, una tarea creada hoy, con entrega mañana y completada hoy
   obtiene 300 según la regla actual. No debe recibir un segundo premio por la tarjeta.
8. Salir de Tareas y volver, consultar el detalle y refrescar la lista/progreso.
   No debe aparecer otra tarjeta ni aumentar de nuevo los contadores o el premio.
   La tarea completada debe conservar su estado y no permitir una nueva finalización.

### B. Dos tareas consecutivas — cola y ausencia de duplicados

1. Crear dos tareas nuevas e iniciar ambas.
2. Completar la primera y, mientras se ve su tarjeta, completar la segunda.
3. Debe verse una tarjeta por vez, en orden de finalización y con su nombre correcto.
4. Logros recibidos debe aumentar en dos. Al terminar ambas tarjetas,
   Celebraciones iniciadas también debe haber aumentado en dos y En cola quedar en cero.
5. Los premios deben corresponder a las dos tareas, una vez por cada una.
   No debe haber dos tarjetas simultáneas para una misma tarea.

### C. Bloque real completado — POMO-299/301

1. Entrar a Trabajo → Bloque de trabajo y elegir una tarea disponible.
2. Usar un bloque personalizado de **1 minuto de trabajo** y **0 de descanso**,
   con un nombre único. La validación existente permite esos valores.
   Usar el flujo de personalización que ya estaba disponible, sin modificar JSON.
3. Seleccionarlo, prepararlo e iniciarlo. Anotar saldo y contadores.
4. Dejar terminar el minuto. Debe aparecer **¡BLOQUE COMPLETADO!**, con el mensaje
   correspondiente y el nombre de la tarea asociada.
5. Los contadores deben aumentar una vez; el tipo debe ser FocusBlockCompleted,
   con un sessionId. La entrega de copas debe seguir siendo **20**, una sola vez.
6. Consultar Progreso o volver a la pantalla del bloque no debe producir otra
   celebración ni añadir otras 20 copas.
7. Iniciar y completar un bloque nuevo debe producir otro ID y otra celebración.

### D. Pausa e interrupción

1. Preparar otro bloque de un minuto, iniciarlo y pausarlo a los pocos segundos.
2. Pausarlo no debe publicar un bloque completado ni aumentar los contadores.
3. Reiniciar/abandonar el bloque con el control existente antes de que llegue a cero.
4. No debe aparecer ¡BLOQUE COMPLETADO! ni añadirse la recompensa del bloque.
   La sesión interrumpida puede seguir apareciendo donde ya se consultaban sus métricas.
5. Un nuevo bloque terminado normalmente debe seguir funcionando después de esta interrupción.

### E. Navegación, saldo y Memoria

1. Con la cola vacía, abrir/cerrar Progreso y Tienda y consultar el saldo.
   No deben aparecer celebraciones nuevas. Si ya hay una compra funcional,
   gastar copas tampoco debe celebrarse como un logro de estudio.
2. Para observar la suspensión con comodidad, durante Play se puede cambiar
   Minimum Display Seconds a 8 en `Reconocimiento de Pomo` (cambio temporal).
3. Completar una tarea o bloque y, mientras se ve la tarjeta, abrir Minijuegos
   usando la navegación normal. Si el flujo actual no permite ese recorrido,
   no forzar activaciones de paneles: comunicar en qué paso se bloquea.
4. En Memoria no debe verse la tarjeta global. El coordinador debe indicar
   Oculto por otro panel = Sí, conservando su evento actual o la cola.
5. Salir con la X del minijuego. Debe reanudarse la tarjeta o mostrarse la siguiente,
   sin duplicar su conteo ni entregar otro premio.
6. Una partida de Memoria conserva sus propias reacciones y su resumen de ocho
   segundos. Su puntaje no debe producir una celebración global de bloque de estudio.

### F. Nueva ejecución y rutas de otros módulos

1. Salir de Play y volver a entrar. No deben reaparecer las celebraciones de las
   tareas/bloques guardados. Los contadores del coordinador deben empezar en cero.
2. Las tareas completadas y el saldo deben seguir guardados; una tarea nueva
   completada en esta ejecución debe volver a generar su única reacción.
3. Cuando el módulo de actividad tenga un control real conectado a
   OnLinkedActivityCompleted, completar desde ese recorrido una tarea iniciada
   y comprobar una única tarjeta. No se añadió un botón nuevo de actividad en esta entrega.
4. Las rachas se validarán cuando su servicio confirme un hito real y use el
   contrato del apartado 6. No se deben simular con cambios de saldo o refrescos.

## 8. Revisión estática y condiciones de cierre

Se revisaron el orden guardado→evento, el filtrado de transiciones repetidas,
el rechazo persistido de sessionId duplicados, las suscripciones de ciclo de vida,
los límites de capas y la ausencia de operaciones de recompensa en el presentador.

La migración añadió 34 bloques serializados y solo cambió la lista de hijos del
Canvas principal. La inspección encontró cero IDs duplicados, cero referencias
locales sin resolver y propietarios correctos de los componentes nuevos. Los
movimientos asignados existen y los gráficos nuevos no capturan clics. La tarjeta
comienza oculta y tiene asignada la supresión de Minijuegos.

Los resultados de la inspección describen lectura de archivos. Posteriormente,
Steven confirmó que verificó el recorrido entregado y que funcionaba. Esa es la
evidencia de aceptación manual del bloque de tareas/bloques; Codex no ejecutó pruebas.
El código de las cuatro tareas está entregado y aceptado para tareas/bloques reales. El cierre
visual compartido depende del arte y mensajes de Valentina, y la integración de
rachas depende de su fuente real. No se declara completada toda la HU de rachas
ni se cambian estados en Jira sin esa evidencia.
