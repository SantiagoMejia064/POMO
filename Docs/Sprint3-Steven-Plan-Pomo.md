# Plan de cierre de las siete tareas de Pomo de Steven — Sprint 3

Estado del plan: los tres bloques tienen una entrega de implementación. Steven
confirmó el acceso y uso de las vistas previas del bloque 2; su integración con
el comparador y el arte compartido sigue pendiente. El registro de ese bloque está en
[Sprint3-Pomo-Desempeno-Implementacion-y-validacion.md](Sprint3-Pomo-Desempeno-Implementacion-y-validacion.md).
Los bloques 1 y 3 se implementaron juntos después y Steven confirmó su recorrido
manual de tareas/bloques. Su registro está en
[Sprint3-Pomo-Logros-Implementacion-y-validacion.md](Sprint3-Pomo-Logros-Implementacion-y-validacion.md).
Este documento conserva el plan de referencia; para el estado final de archivos,
eventos, dependencias y pasos se deben consultar esos registros de entrega.

Pendiente concreto del bloque 2: conectar la selección real del comparador a
ProgressPomoFeedbackView desde la interfaz del aplicativo. En la revisión de
código, las consultas de fechas se invocan desde el Inspector de Editor; aún no
hay un productor de comparación conectado desde esa interfaz. La aceptación de
las vistas previas no cierra por sí sola esta integración.

Escena de trabajo: `Assets/Scenes/Steven 1.unity`.
Se conserva el recorrido de Memoria que Steven validó: partida continua de cinco
minutos, resumen durante ocho segundos y regreso al botón EMPEZAR.

## Viabilidad y dependencias

Las siete tareas tienen una base suficiente para empezar a desarrollar su lógica.
El cierre visual e integrado requiere tener en cuenta estas dependencias:

| Elemento | Evidencia actual | Consecuencia para el plan |
|---|---|---|
| Finalización de tareas | `TaskService.TransitionStatus` guarda el cambio y rechaza estados repetidos. No declara un evento TaskCompleted. | Añadir una señal de finalización tras guardar correctamente; cubrir lista, detalle y finalización desde actividad. |
| Recompensas de tareas | `ProgressService.TryRecordTaskTrophyAward` y `ProgressRepository` impiden premiar dos veces el mismo taskId. | Mantener la entrega existente. La reacción visual solo observa el logro. |
| Bloques completados | `PomodoroController` registra sesiones y expone WorkCompleted/FocusBlockCupsAwarded. `ProgressService.RecordFocusSession` informa si el guardado fue nuevo. | Emitir o adaptar una señal identificada con sessionId solo para una sesión completada y guardada. |
| Expresiones y movimiento | Existe `PomoVisualController` y seis clips en `Assets/Resources/Pomo`. | Reutilizar la clase y sus clips, con perfiles visuales configurables; conservar las referencias del minijuego. |
| Desempeño | Existen `CompareProgress`, `CompareSessions`, `GetPerformanceFeedback` y las cuatro clasificaciones. | Conectar la salida actual con Presentation, sin inventar otra fórmula. |
| Selección de períodos | No se encontraron consumidores de GetPerformanceFeedback en Presentation. | Preparar un punto de entrada que reciba los dos períodos seleccionados. Acordar su origen en la interfaz antes del cierre integrado. |
| Rachas | No se encontró un servicio ni un evento real de hitos de racha en los scripts revisados. | Dejar un contrato para ese evento. El caso de racha se integra cuando exista su fuente y su definición; no deducirlo del saldo de copas. |
| Arte y mensajes | Jira asigna a Valentina POMO-268, 288 y 300. | Empezar con los recursos actuales y campos de Inspector; validar el aspecto definitivo cuando entregue esas tareas. |

La HU 8.2.3 habla de tareas correctas e incorrectas. El sistema actual registra
estados de ejecución de tareas, no calificaciones académicas. Para estas tareas
de Steven se consume la clasificación de progreso existente; no se interpreta
una tarea pendiente o vencida como una respuesta académica incorrecta.

## Arquitectura propuesta

```text
Tarea guardada como completada / sesión completada guardada
    -> señal con identificador de origen
    -> coordinador de feedback de Presentation
    -> mensaje contextual + PomoVisualController

Períodos seleccionados
    -> CompareProgress / CompareSessions
    -> GetPerformanceFeedback
    -> presentación de clasificación + PomoVisualController
```

Las recompensas conservan su recorrido actual. El presentador de Pomo no llama
a AddCups ni a TryRecordTaskTrophyAward para mostrar una animación.
Cambiar de pantalla, refrescar datos o consultar un saldo no produce un logro.

Un coordinador activo en Canvas escuchará las señales y utilizará una vista de
feedback común, guardada en la jerarquía. La vista será breve, no bloqueará clics
ni cubrirá botones y reutilizará la misma clase PomoVisualController. Su ubicación
se ajustará a los espacios de las pantallas existentes sin reorganizar sus controles.

Nombres propuestos para los nuevos componentes, sujetos al ajuste mínimo que
necesite la implementación: PomoFeedbackCoordinator y ProgressPomoFeedbackView.
Los servicios que accedan a datos se inicializarán en Awake o mediante inyección,
evitando inicializar Application.persistentDataPath desde campos de MonoBehaviour.

## Bloque 1 — Reacción al completar una tarea

Tareas: [POMO-267](https://nebuly-studios-pomo.atlassian.net/browse/POMO-267)
y [POMO-269](https://nebuly-studios-pomo.atlassian.net/browse/POMO-269).
HU: [POMO-82 / HU 8.1.1](https://nebuly-studios-pomo.atlassian.net/browse/POMO-82).

### Implementación

1. Publicar la señal de tarea completada en el punto común de TaskService,
   después del guardado exitoso de la transición a Completed.
2. Incluir taskId, título y fecha de finalización. No emitirla al cargar datos,
   al iniciar una tarea, ante StatusUnchanged o cuando falle el guardado.
3. Conectar el coordinador al controlador visual. Mostrar un único reconocimiento
   con el nombre de la tarea y el gesto positivo correspondiente.
4. Identificar la reacción por el taskId y su finalización; no repetirla por
   refresco, navegación o suscripciones duplicadas.
5. Mantener el flujo de premios existente en TasksScreenController/TaskListController.
   Reconocer una tarea vencida no implica otorgarle copas si las reglas actuales no lo permiten.
6. Reservar desde este bloque un único manejo del logro de tarea para que el
   bloque 3 no añada una segunda celebración del mismo hecho.

### Archivos previstos

- Modificar `Assets/Scripts/Business/Tasks/TaskService.cs`.
- Añadir el coordinador y el modelo de señal si son necesarios.
- Ampliar `Assets/Scripts/Presentation/Screens/PomoVisualController.cs` solo para
  operaciones reutilizables y selección de perfiles.
- Configurar la vista y las referencias en Steven 1.
- Revisar las rutas existentes de TasksScreenController y TaskListController;
  evitar un disparador distinto por cada botón.

### Validación manual de Steven

- Crear una tarea de prueba, iniciarla y completarla desde la lista: una reacción.
- Repetir con otra tarea desde el detalle/flujo de actividad: una reacción.
- Refrescar, salir y volver: sin celebraciones nuevas ni cambios adicionales de copas.
- Completar dos tareas distintas seguidas: un mensaje para cada una, sin superposición.
- Revisar que una transición inválida no produzca feedback de éxito.

Cierre de POMO-267/269: señal conectada en todas las rutas reales y una reacción
por finalización, conservando los premios originales. La animación definitiva y
su verificación compartida se coordinan con POMO-268/270 de Valentina.

## Bloque 2 — Expresión según desempeño

Tareas: [POMO-287](https://nebuly-studios-pomo.atlassian.net/browse/POMO-287),
[POMO-289](https://nebuly-studios-pomo.atlassian.net/browse/POMO-289)
y [POMO-290](https://nebuly-studios-pomo.atlassian.net/browse/POMO-290).
HU: [POMO-87 / HU 8.2.3](https://nebuly-studios-pomo.atlassian.net/browse/POMO-87).

### Implementación

1. Añadir una vista que reciba ProgressFeedbackModel o una comparación real ya
   seleccionada en la interfaz. Si recibe períodos, usar los métodos existentes de ProgressService.
2. Exponer cuatro perfiles claros, configurables desde el Inspector:

| Clasificación | Presentación propuesta |
|---|---|
| Neutral | Estado de espera y mensaje de datos insuficientes. |
| Improved | Sonrisa/pulgar arriba y reconocimiento del avance. |
| Stable | Expresión tranquila y mensaje de continuidad. |
| NeedsImprovement | Expresión de atención o ánimo y recomendación para retomar el ritmo. |

3. Utilizar el message existente del modelo y el controlador visual común.
4. Dar una transición suave al cambiar de clasificación. Repetir Refresh con la
   misma clasificación no reinicia continuamente una animación.
5. Si una celebración temporal interrumpe la vista de progreso, restaurar después
   su expresión contextual, en lugar de dejar un gesto de logro permanentemente.
6. Al cambiar a períodos sin datos, sustituir el feedback anterior por Neutral.
7. Preparar una forma de vista previa exclusiva del Editor para que Steven pueda
   revisar las cuatro clasificaciones sin borrar ni modificar su JSON real.
8. Conectar el adaptador con el origen de períodos acordado por el módulo de progreso.
   No imponer silenciosamente una comparación diaria o semanal nueva.

### Archivos previstos

- Añadir `ProgressPomoFeedbackView.cs` en Presentation/Screens.
- Ampliar PomoVisualController con el mapeo configurable de desempeño.
- Conectar ProgressPanelNavigationController o la pantalla que realmente entregue
  la comparación seleccionada.
- Reutilizar ProgressService, ProgressComparisonModel y ProgressFeedbackModel.
- Asignar la vista en la sección Progreso de Steven 1.

### Validación manual de Steven

- Revisar los cuatro estados por separado en la vista previa.
- Revisar una comparación con datos reales y contrastar mensaje/clasificación.
- Refrescar repetidas veces: sin reiniciar o superponer animaciones.
- Pasar de una mejora a ausencia de datos: debe verse Neutral, sin el gesto anterior.
- Cambiar de panel y volver: debe consultar el estado correspondiente.

Cierre de POMO-287/289: comparación real conectada y transición correcta.
Cierre de POMO-290: Steven confirma los cuatro escenarios, incluida ausencia
de datos. No se considera cerrada solo por entregar instrucciones de prueba.
Las expresiones finales se coordinan con POMO-288 de Valentina.

## Bloque 3 — Celebración de logros reales

Tareas: [POMO-299](https://nebuly-studios-pomo.atlassian.net/browse/POMO-299)
y [POMO-301](https://nebuly-studios-pomo.atlassian.net/browse/POMO-301).
HU: [POMO-90 / HU 8.3.3](https://nebuly-studios-pomo.atlassian.net/browse/POMO-90).

### Implementación

1. Extender el coordinador del bloque 1 para diferenciar tarea, bloque y futuro hito de racha.
2. Reutilizar la señal de tarea completada; no añadir otro observador que muestre
   una segunda celebración de la misma tarea.
3. Para bloques, transportar el sessionId desde el registro exitoso de la sesión
   completada. Una sesión interrumpida no equivale a un bloque completado.
4. No usar CupsBalanceChanged como evento de logro: también se emite al gastar copas.
   StudySessionsChanged tampoco identifica por sí solo un bloque terminado, pues
   se utiliza para cambios de sesiones completadas e interrumpidas.
5. Identificar cada logro por tipo e ID de origen. Usar una cola breve para
   reconocer logros consecutivos, mostrando una sola reacción a la vez.
6. Mostrar mensajes según el tipo de avance y datos reales disponibles, sin
   inventar duración de rachas ni montos de recompensa.
7. Mantener el feedback del minijuego en su propia vista validada; no mostrar una
   celebración global adicional encima de su resumen de ocho segundos.
8. Exponer el punto de entrada para hitos de racha. Su conexión final depende
   de un evento de racha real, con identificador y regla definida por ese módulo.
9. Integrar animaciones y mensajes de POMO-300 cuando estén disponibles.

### Archivos previstos

- Ampliar PomoFeedbackCoordinator y el modelo de señal compartido.
- Ajustar ProgressService/PomodoroController solo para transportar el evento
  de sesión completada y su ID, manteniendo el cálculo y entrega de recompensas.
- Reutilizar PomoVisualController y la vista común en Steven 1.

### Validación manual de Steven

- Completar un bloque: una celebración y la cantidad de copas establecida actualmente.
- Completar una tarea: una celebración, sin repetición del bloque 1.
- Pausar o abandonar un bloque: sin celebración de bloque completado.
- Refrescar progreso y consultar el saldo: sin celebraciones nuevas.
- Realizar una compra: sin confundir una variación de saldo con un logro.
- Completar varios avances: mensajes ordenados y sin animaciones simultáneas.
- Reabrir paneles o volver a Play: sin duplicar suscripciones ni reproducir logros históricos.
- Cuando exista la fuente de rachas, alcanzar un hito real: una celebración con ese ID.

Cierre de POMO-299/301: eventos reales conectados, deduplicación y presentación
sin nuevas entregas de copas. Si no existe aún la fuente de rachas, documentar
ese escenario pendiente de la HU compartida, sin presentarlo como integrado.

## Coordinación con Valentina

Consultado en Jira: estas cinco tareas siguen asignadas a Valentina y por hacer.

| Tarea | Responsabilidad |
|---|---|
| [POMO-268](https://nebuly-studios-pomo.atlassian.net/browse/POMO-268) | Animación/sprite de celebración de tarea y referencias. |
| [POMO-270](https://nebuly-studios-pomo.atlassian.net/browse/POMO-270) | Comprobar la reacción desde Tareas y ausencia de repetición. |
| [POMO-288](https://nebuly-studios-pomo.atlassian.net/browse/POMO-288) | Assets para Neutral, Improved, Stable y NeedsImprovement. |
| [POMO-300](https://nebuly-studios-pomo.atlassian.net/browse/POMO-300) | Animaciones y mensajes de celebración en paneles existentes. |
| [POMO-302](https://nebuly-studios-pomo.atlassian.net/browse/POMO-302) | Comprobar una celebración por evento y neutral cuando no hay logro. |

Se prepararán campos de Inspector y puntos de entrada para integrar esas entregas.
No se reasignan ni se completan automáticamente tareas de compañeros.

## Secuencia de trabajo y aceptación

1. Implementar bloque 1 y entregar archivos, ubicación en jerarquía y pasos manuales.
2. Recibir feedback de Steven, corregir y aceptar ese recorrido.
3. Bloque 2 implementado como adaptador de la comparación recibida: conectar la
   selección real desde el comparador y sustituir el arte provisional compartido.
4. Steven valida los cuatro estados y la integración real.
5. Implementar bloque 3 con tareas/bloques y el contrato para rachas.
6. Steven valida celebración única, cola y conservación de copas.
7. Integrar arte/mensajes y fuentes de otros módulos pendientes, y revisar el cierre de cada HU.

En cada entrega se revisará únicamente lo necesario para ese bloque y su
regresión sobre lo ya validado. Codex no ejecutará Play Mode, pruebas ni builds.
Los cambios de Jira se harán únicamente cuando el usuario lo solicite; disponer
del código no sustituye su validación manual.
