# Refactorización visual de POMO

## Alcance y referencias

Se trabajó sobre `Assets/Scenes/Santiago.unity`, la escena habilitada para compilación en Unity 6000.3.11f1. Inicio, Trabajo, Ritmo, Recordatorios, Mi horario y Tareas son paneles de esa escena. Las escenas de ensayo reutilizan parte de sus controladores. La referencia visual fue la página 2 del tablero oficial, donde están los recorridos de Trabajo, Calendario, Ritmo y Recordatorios, junto con las especificaciones del encargo. El adjunto recibido contenía el texto del encargo. Las seis imágenes mencionadas por separado no estaban disponibles. [Prototipo SMMV](https://canva.link/9wtzw0gbvkfy45p)

La escena usa uGUI, TextMeshPro, un Canvas principal con escala por ancho de referencia y grupos de paneles con referencias serializadas. Inicio conserva el dormitorio con Pomo, el trofeo, el contador y los accesos existentes. Se reutilizaron el dormitorio desenfocado, el sprite de Pomo explicando, las fuentes Gagalin y Poppins y el sprite de nueve segmentos `BotonRedondeado12`. Los prefabs de tareas, bloques, confirmaciones y progreso siguen suministrando sus propias vistas. La refactorización usa las referencias existentes en vez de regenerar la escena con las utilidades antiguas de Steven 1.

## Comparación por pantalla

| Pantalla | Resultado | Diferencias con el tablero |
| --- | --- | --- |
| Inicio | Dormitorio azul, Pomo centrado, contador arriba y cuatro accesos alineados sobre Progreso. La pulsación llena el botón de izquierda a derecha en 0,26 s antes de ejecutar su acción original. | Se conservó el acceso existente a Ajustes. El trofeo muestra el saldo registrado. |
| Trabajo | Fondo azul del dormitorio y tres accesos anchos redondeados. | Se conservaron los accesos a bloques, horario y ritmo. El menú no incorpora una segunda gráfica. |
| Ritmo | Barras redondeadas con alturas reales, curva coral que une los mismos valores, marcas de sol y luna, panel blanco y Pomo al lado de las acciones. | El eje usa las franjas que calcula el servicio. Se conservan la guía y el selector de horas de Recordatorio. El gráfico heredado carece de selección por pulsación de barras. |
| Recordatorios | Mes con flechas y cuadrícula, día seleccionado, horas redondeadas con estados azul, turquesa y verde, selección con contorno dorado y tarjeta blanca con Pomo al confirmar. | La confirmación conserva el nombre, la fecha y el estado real de la notificación. La recompensa ilustrada en Canva no tiene una acción equivalente en el flujo. |
| Calendario | Encabezado azul oscuro, columnas por día, cuadrícula desplazable en Semana y Día, bloques redondeados, selección de fecha y botón coral. La vista Mes conserva su cuadrícula. | La semana mantiene los siete días de la implementación. La franja de tareas sin hora se separa de la cuadrícula horaria. Los avisos y los vencimientos antiguos con hora muestran el minuto guardado. |
| Alta y edición de color | Fondo azul oscuro, panel interior azul, separadores, seis muestras redondas y una acción verde para guardar. El resumen existente muestra también la descripción. | El alta conserva asignatura y fecha, exigidas por la tarea. El resumen de Calendario permite editar el color. La edición de los demás campos sigue en el formulario de Tareas. |

## Paleta

Los valores son aproximaciones visuales del tablero consultado.

| Uso | Hex |
| --- | --- |
| Encabezados y azul oscuro de eventos | `#0C2F5B` |
| Azul medio | `#4A70A2` |
| Panel interior | `#608FC6` |
| Azul claro | `#A8C4E4` |
| Texto oscuro | `#15212F` |
| Turquesa | `#00919F` |
| Verde | `#00C165` |
| Rojo oscuro | `#86140D` |
| Dorado | `#FFBD49` |
| Coral | `#E84438` |
| Violeta | `#D89EDE` |

## Datos y comportamiento

- Ritmo conserva `WorkRhythmService`, sus umbrales, el origen de sesiones y la recomendación. La curva interpola solo al dibujar y pasa por el valor de cada barra.
- El flujo de Recordatorios sigue usando `RhythmReminderService`, `RhythmReminderRepository`, `rhythm-reminders.json` y el adaptador de notificaciones existente. Mi horario usa la misma lectura de recordatorios. El flujo existente permite crearlos, consultarlos y cancelarlos. La edición de avisos independientes y la creación de eventos con hora de inicio y fin son diferencias con el prototipo.
- Las seis opciones de color se guardan en `TaskModel.calendarColor` con el resto de la tarea en `tasks.json`. El valor cero corresponde al azul oscuro de los registros previos. El alta guarda el color en la misma operación que los campos existentes. Cambiar solo el color relee la tarea y conserva los demás campos y el identificador de su notificación. Cancelar descarta el borrador.
- Cuando varios registros tienen la misma hora visible, la semana los apila en su columna y la vista diaria aprovecha el ancho para separarlos. Si coinciden también en el minuto, el aviso usa dorado y una tarea con color propio recibe un borde dorado. Las agrupaciones son de presentación y no intervienen en el guardado.
- Se ajustaron anclajes y tamaños de Inicio, Trabajo, Ritmo, Recordatorios, Calendario y los formularios de Tareas. El rectángulo útil se adapta a la zona segura de la pantalla. Los bloques de calendario y las etiquetas estrechas admiten truncamiento.

## Archivos

| Tipo | Archivos |
| --- | --- |
| Escena | `Assets/Scenes/Santiago.unity` |
| Controladores | `Assets/Scripts/Presentation/Screens/HomeScreenController.cs`, `Assets/Scripts/Presentation/Screens/WorkRhythmScreenController.cs`, `Assets/Scripts/Presentation/Screens/RhythmReminderFlowController.cs`, `Assets/Scripts/Presentation/Screens/CalendarScreenController.cs`, `Assets/Scripts/Presentation/Screens/TasksScreenController.cs` |
| Interfaz existente | `Assets/Scripts/Presentation/UI/ButtonFeedback.cs`, `Assets/Scripts/Presentation/UI/EstiloBotonesEscena.cs`, `Assets/Scripts/Presentation/UI/ScreenSafeArea.cs` |
| Interfaz nueva | `Assets/Scripts/Presentation/UI/PomoVisualStyle.cs`, `Assets/Scripts/Presentation/UI/RhythmPlot.cs`, `Assets/Scripts/Presentation/UI/CalendarEventPalette.cs` y sus tres archivos `.meta` |
| Datos y servicio | `Assets/Scripts/Data/Models/TaskModel.cs`, `Assets/Scripts/Business/Tasks/TaskService.cs` |
| Informe | `Docs/POMO-Refactor-visual-Canva.md` |

No hubo cambios en archivos de prefabs, sprites, fuentes o configuración como parte de la entrega. El repositorio ya tenía modificaciones locales en cinco fuentes de TextMeshPro. Los cambios de la refactorización quedan en el árbol de trabajo, sin commits ni push.

## Verificación

- La escena conserva los 5.242 bloques serializados anteriores. Tras la integración de hábitos contiene 5.321 bloques válidos, 266 botones y 214 acciones serializadas con destinos resueltos. La finalización del onboarding usa una acción que guarda el estado y abre Inicio en lugar de las dos activaciones directas de paneles.
- La validación estructural se puede repetir con `node Tools/CheckSantiagoInteraction.cjs` y `./Tools/CheckSantiagoSceneYaml.ps1`.
- Compiló en Unity 6000.3.11f1. También se compiló el código C# con las referencias y los símbolos de Android mediante Roslyn.
- Pasaron 51 comprobaciones en una copia aislada de Unity, incluida la lectura en un segundo proceso. Se verificaron los cinco destinos de Inicio, el bloqueo durante el relleno, el acceso a Bloques, el ciclo de pausa y descanso del temporizador, el registro de sesiones y sus proporciones en Ritmo, el flujo de Recordatorio, las tres vistas del calendario, las coincidencias, las operaciones de Tareas y la persistencia y cancelación del color. La apertura comprobó además que la escena no tiene componentes con scripts faltantes. [Resultados de las pruebas](../Logs/VisualEvidence/checks.txt)
- Se renderizaron nueve pantallas en Unity a 450 × 950 y se revisaron Inicio, Ritmo, Calendario semanal y alta de tarea también a 360 × 640. La comparación permitió recuperar el trofeo y la curva, mejorar el contraste y hacer legibles las horas coincidentes en el ancho compacto. [Capturas de Unity](../Logs/VisualEvidence/Revision-visual.png)
- La consola registró una excepción en la indexación de Unity Search al arrancar la copia de pruebas. El registro de la ejecución final no contiene excepciones de los controladores de POMO. [Registro de consola de la prueba](../Logs/VisualEvidence/errors.txt)

Queda pendiente la prueba en un dispositivo Android de la zona segura con recorte, teclado, permisos, alarmas y entrega real de notificaciones. La confirmación de las capturas corresponde al guardado local en el Editor.

## Correcciones posteriores e integración de Chantrix

### Ritmo y legibilidad

La guía, la tendencia y el mensaje están dentro de la tarjeta blanca. El paso 3 amplía esa tarjeta y sustituye temporalmente los rótulos de estadísticas repetidos. Al salir de la guía se recupera la presentación normal. El mensaje de ausencia de registros dispone de una zona propia en la parte superior del gráfico.

En Calendario se ampliaron la columna y la tipografía de las horas, los números de fecha, los textos de los eventos y el acceso a Recordatorios. Las tarjetas mantienen la apertura de sus detalles para consultar títulos largos.

### Pantalla completa

El fondo se dibuja hasta los límites del Canvas mediante `FullBleedBackground`. Los controles y el texto siguen usando el rectángulo de zona segura. El mismo tratamiento se aplica al dormitorio, Ritmo, Calendario, los formularios y las etapas de Recordatorio. Se comprobaron los límites del fondo en Unity con recortes superiores e inferiores simulados.

### Diferencias funcionales de Chantrix

La comparación de scripts serializados, eventos, activación y raíces de escena identificó estas conexiones.

- Se añadió el componente existente `HealthyHabitsFeedbackController` a Hábitos, con el mismo destino de retroalimentación de Chantrix. Las trece casillas conservan las claves `pomo.healthy-habits.v2.*`. Los títulos se toman de la fila de cada casilla y los hábitos pendientes se pueden desplazar en Excelente.
- Se conectaron `panelInicio` y `saludoPersonalizado` en el controlador de bienvenida de Santiago.
- `TaskReminderController` ya estaba serializado con los mismos ajustes de Chantrix, pero faltaba su entrada en `SceneRoots`. Se añadió esa raíz. El aviso, inicialmente inactivo en ambas escenas, ahora se activa al mostrarlo y se oculta al terminar la animación.
- Los parámetros de notificaciones, los textos de logros y las acciones compartidas de navegación ya coincidían. Las diferencias de posiciones, escalas, colores y materiales de Chantrix se excluyeron de la integración.

La escena fuente `Assets/Scenes/Chantrix.unity` conserva el hash Git `9815c3f9af9aba7e99327c66c7e918ff10fbe0b4`.

### Arranque y compatibilidad

El nuevo marcador `pomo.profile.onboarding-completed` distingue guardar el nombre de finalizar la bienvenida. Una entrada interrumpida recupera el saludo y las casillas guardadas. La acción final guarda la finalización y abre Inicio. Al arrancar con el recorrido completado y consentimiento vigente se activa Inicio, incluso si la bienvenida estaba apagada. Esta restauración se ejecuta al arrancar o al aceptar el aviso y deja la navegación posterior bajo sus controladores habituales.

Para perfiles anteriores que carecen del marcador se infiere que el onboarding terminó cuando existe un nombre válido y consentimiento vigente. Las aceptaciones 2 y 4 corresponden al mismo aviso de este repositorio, cuya versión se cambió durante pruebas. Se reconocen ambas. Las demás versiones mantienen la comprobación de consentimiento.

El borrado local incluye ahora el marcador de onboarding y las trece preferencias de hábitos.

### Archivos adicionales afectados

- `Assets/Scripts/Business/Pomo/PomoNotifications.cs`
- `Assets/Scripts/Data/Persistence/LocalUserProfile.cs`
- `Assets/Scripts/Data/Persistence/LocalDataResetService.cs`
- `Assets/Scripts/Presentation/Screens/NombreBienvenidaController.cs`
- `Assets/Scripts/Presentation/Screens/PrivacyConsentController.cs`
- `Assets/Scripts/Presentation/Screens/HealthyHabitsFeedbackController.cs`
- `Assets/Scripts/Presentation/UI/FullBleedBackground.cs` y su archivo `.meta`
- `Tools/Tests/WelcomeNameChecks.cs`

También se ajustaron Santiago, los controladores visuales de Ritmo, Calendario y Recordatorio y el estilo compartido de la entrega inicial.

### Pruebas posteriores

La suite `Tools/TestNotificationPermissions.ps1` pasó 199 comprobaciones, incluidas las nuevas transiciones de onboarding. Se compilaron las variantes C# de Editor y Android. Las pruebas de Play se ejecutaron con un nombre de producto y archivos de usuario aislados.

Pasaron otras 37 comprobaciones en Play. Cubren alta e interrupción del onboarding, hábitos guardados, saludo, perfiles antiguos, consentimiento, navegación, límites de la tarjeta de Ritmo, legibilidad de horas y cobertura del fondo. Una tarea de prueba que vencía ese día activó el aviso periódico, que volvió a ocultarse al terminar su animación. Un segundo proceso de Unity confirmó Inicio activo y hábitos conservados. [Pruebas de Play](../Logs/FollowupEvidence/checks.txt), [verificación tras reinicio](../Logs/FollowupEvidence/restart.txt)

Se revisaron renders de 360 × 640 y 393 × 852, con recortes de zona segura simulados. Hay capturas de Ritmo sin registros, Ritmo con una sesión registrada, Calendario e Inicio. [Revisión de las correcciones](../Logs/FollowupEvidence/Revision-correcciones.png)

La consola de la copia aislada volvió a registrar el error de indexación de Unity Search y un aviso de carga de textura. Las comprobaciones de POMO finalizaron correctamente. La validación en Android físico sigue pendiente.
