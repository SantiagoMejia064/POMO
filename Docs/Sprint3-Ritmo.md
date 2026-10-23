# H5 — Ritmo y recordatorios independientes

## Corrección funcional

Crear recordatorio permanece dentro de Sección Ritmo y no llama a TaskService ni al calendario de tareas. El flujo es:

1. Gráfica y recomendaciones existentes.
2. Nombre y selector mensual independiente: mes/año, flechas, lunes a domingo, 42 celdas con números reales, años bisiestos y selección visible.
3. Selección de hora: fecha/nombre elegidos, botones por franja y borde de selección. Se conserva el nombre al volver. Fechas y horas pasadas quedan bloqueadas.
4. Confirmación con nombre, fecha, hora y estado real de programación.
5. Aceptar cierra Ritmo y regresa al inicio de POMO.

No se muestran personajes ni mensajes de Pomo en las pantallas de recordatorio. La guía opcional de la gráfica se conserva.

Los colores de hora proceden de WorkRhythmService: azul a verde según minutos registrados; gris cuando no existen al menos dos sesiones en esa franja. Se diferencia «Sin datos» de «Pocos datos». Cualquier horario futuro es elegible: el color no bloquea ni mide energía o eficiencia.

## Tipografía e interfaz

Se compararon textos manuales de Steven 1: botones de 55–74 y títulos de 85,6. Ritmo utiliza ahora cuerpo 64, botones 56 y títulos 85; Poppins para lectura. Se acortaron los mensajes para evitar párrafos diminutos. Las etiquetas de la gráfica usan 46–50 en la configuración de 12 franjas.

Todos los objetos están serializados en Assets/Scenes/Steven 1.unity. Bajo la sección existente hay Ritmo UI y Recordatorios Ritmo UI, con Elegir fecha, Elegir hora y Confirmación recordatorio. El selector tiene 42 días y 24 botones horarios preparados (12 visibles para franjas de dos horas).

El menú POMO > Ritmo > Preparar interfaz en Steven 1 permite actualizar la interfaz. La migración de versión 4 también actualiza una escena abierta con el layout anterior al terminar Play o recompilar. Si la escena ya tenía cambios sin guardar, se preservan y se deja pendiente guardarla; otras escenas no se modifican.

## Cambios retirados de Tareas

Se restauraron exactamente las versiones anteriores de TaskService, TaskModel, TasksScreenController y CalendarScreenController. Se retiraron hasScheduledTime, la sobrecarga con hora, el callback de retorno desde Ritmo y el campo Hora desde Ritmo de la escena. Las tareas vuelven a usar solo fecha y su aviso anterior a las 09:00. No se eliminaron datos de tareas que el usuario pudiera haber creado en pruebas anteriores.

## Guardado y notificaciones

RhythmReminderRepository reutiliza JsonStorageService. El archivo independiente rhythm-reminders.json se guarda en Application.persistentDataPath, junto a progress.json. Cada registro contiene ID, nombre, fecha/hora ISO con zona, fecha de creación, ID nativo, confirmación de programación y estado pendiente/enviada/cancelada.

El guardado ocurre antes de pedir una notificación. Reintentos conservan el mismo ID; nombre/instante idénticos también reutilizan el registro. Android usa SendNotificationWithExplicitID y CheckScheduledNotificationStatus, sin enviar de nuevo si ese ID ya está programado. Reabrir el flujo solo consulta estados; no crea avisos repetidos. Si falla la persistencia final, se cancela el aviso nativo. Salir de un borrador cancela su registro pendiente cuando existe.

- Editor: permite probar el guardado y muestra **Guardado local de prueba** con notificación pendiente. No afirma que Android la haya programado.
- Android: solo muestra **Recordatorio creado correctamente** después de comprobar NotificationStatus.Scheduled. Solicita permiso para notificaciones y, cuando hace falta, alarmas exactas. Si el permiso o la programación fallan, muestra un error y permite reintentar; no muestra confirmación de éxito.
- Se configuró Mobile Notifications 2.4.3 para programación exacta con SCHEDULE_EXACT_ALARM y recuperación tras reiniciar el dispositivo. Es necesario reconstruir la aplicación para incluir estos ajustes y conceder los permisos en el dispositivo.
- El estado enviada se persiste cuando Android informa Delivered. No se deduce por haber pasado la hora ni por un mensaje en Unity.

La integración compila para las ramas de Editor y Android. No se ha verificado aquí una entrega real en dispositivo; la puntualidad, permisos y recepción con la aplicación cerrada requieren esa comprobación.

## Archivos de esta corrección

Nuevos (más sus .meta):

- Assets/Scripts/Data/Models/RhythmReminderModel.cs
- Assets/Scripts/Data/Repositories/RhythmReminderRepository.cs
- Assets/Scripts/Business/Progress/RhythmReminderService.cs
- Assets/Scripts/Presentation/Screens/RhythmReminderFlowController.cs
- Assets/Editor/RhythmReminderSceneBuilder.cs

Modificados: WorkRhythmScreenController.cs, NotificationService.cs, WorkRhythmSceneSetupUtility.cs, Steven 1.unity y ProjectSettings/NotificationsSettings.asset. Los cuatro archivos de Tareas/Calendario mencionados arriba quedaron restaurados, sin diferencias respecto a su versión previa.

La gráfica, el análisis, el registro de sesiones Pomodoro, Progreso y las recompensas se conservan. Siguen disponibles las 35 sesiones de prueba separadas en progress.json; se eliminan mediante POMO > Ritmo > Datos de prueba > Eliminar.

## Comprobación manual y Jira

Salir de Play, recargar Steven 1 y esperar a que compile. Si la escena tenía cambios propios sin guardar, guardar después de la actualización visual.

1. Revisar tipografía y Crear recordatorio desde la gráfica, con y sin sesiones.
2. Escribir nombre, cambiar de mes (incluido febrero bisiesto), elegir un día y continuar.
3. Verificar números reales, días pasados deshabilitados, colores y borde de hora seleccionada; elegir también una hora gris futura.
4. Volver a fecha: conservar nombre y selección. Confirmar que no se abrió Tareas ni se creó una tarea/bloque/recompensa.
5. Crear en Editor: ver confirmación local pendiente. Reiniciar y comprobar el registro en rhythm-reminders.json.
6. En Android reconstruido, conceder permisos y programar para un horario futuro. Exigir confirmación nativa y verificar recepción a la hora elegida, también con la app cerrada.
7. Reintentar y reabrir: comprobar un registro y un ID por recordatorio, sin notificaciones duplicadas.
8. Aceptar confirmación: regresar al inicio. Comprobar también Omitir y reentrada para POMO-244.

POMO-234/POMO-235 quedan corregidas en código e interfaz al flujo independiente de recordatorio, según esta nueva definición. Las demás implementaciones de H5 se conservan. POMO-244 y la aceptación visual/Android siguen pendientes de verificación interactiva; no cerrar tareas solo por la compilación. No se modificaron estados en Jira ni se crearon pruebas automatizadas nuevas.


## 23/10/2026 — Recordatorios en Mi horario y etiquetas de la gráfica

Mi horario consulta `rhythm-reminders.json` mediante RhythmReminderService al abrir el calendario y al cambiar de periodo. Los recordatorios existentes aparecen por su nombre en la fecha local programada, en las vistas mes, semana y día. Se excluyen registros cancelados, sin nombre o con fecha inválida; los recordatorios pasados se conservan visibles. No se duplican registros ni se convierten en tareas, y esta lectura no programa notificaciones.

Cuando un día tiene tareas y recordatorios, se reserva al menos una fila para mostrar un recordatorio. Los nombres largos se abrevian visualmente en la celda; al seleccionar el día se muestra el nombre completo en el detalle junto a las tareas. El indicador «+N más» cuenta ambos tipos.

La gráfica usa filas separadas para minutos, bloques y hora inicial en formato de 24 horas. Las unidades están en la leyenda; los números no permiten saltos automáticos de línea. Poppins mantiene tamaño máximo 42 con ajuste automático limitado al espacio de cada columna. Los cambios están serializados en Steven 1 y la migración 5 solo ajusta la gráfica cuando la escena ya tiene el flujo de recordatorios.

Validación técnica: compilación C# de ejecución para Editor y rama Android, compilación del ensamblado Editor, revisión de referencias de escena y espacio de etiquetas con métricas de Poppins. Sin pruebas automatizadas nuevas. La comprobación visual e interactiva en Play/dispositivo queda por realizar; compilar no confirma esa aceptación.

Comprobación manual:
1. Crear un recordatorio desde Ritmo, aceptar y abrir Trabajo > Mi horario.
2. Ir a su fecha: comprobar su nombre; seleccionarla para ver el nombre completo. Comprobar también un recordatorio creado antes de este cambio y reabrir la aplicación.
3. Verificar mes/semana/día y una fecha que también tenga tareas. Las tareas siguen abriendo su resumen habitual.
4. Abrir Ritmo con los datos de prueba: comprobar números como 255 y 300, cantidad de bloques y horas sin cortes ni superposiciones.

POMO-244 ya fue validada y cerrada por Santiago. No se modificaron estados en Jira en esta corrección.


## 23/10/2026 — Ritmo general de todo el historial

Se reemplazó la ventana de siete días por el acumulado de todas las sesiones guardadas hasta el momento actual. Las barras suman minutos efectivos de todo el uso por franja horaria; los mínimos de sesiones y días para recomendar se evalúan sobre ese historial. El selector de horas de Crear recordatorio consulta el mismo análisis acumulado.

La interfaz muestra «Tu ritmo general», sin rango de fechas ni comparación entre periodos. Se conserva el registro detallado original: no se borran sesiones ni se modifican sus fechas, pausas o tareas. Los registros antiguos sin intervalos fiables siguen sin asignarse artificialmente a una hora. Los días activos se acumulan en una lista dispersa para evitar crear filas por cada día vacío de años de historial.

Validación manual pendiente: comprobar que una sesión de hace más de siete y de noventa días contribuye a su franja, crear un bloque nuevo y verificar actualización acumulada; comprobar que las horas sugeridas en recordatorios siguen ese mismo historial y que no aparece ninguna fecha en la pantalla de Ritmo.


## 23/10/2026 — Borrar recordatorios desde Mi horario

En Mi horario se puede pulsar un recordatorio o seleccionar el día y abrir «Recordatorios». Se muestra su nombre completo y se puede recorrer todos los recordatorios de ese día con las flechas, incluidos los que no caben en la celda del mes. «Borrar recordatorio» pide confirmar con «Eliminar» o permite «Conservar». El panel y sus controles están guardados como GameObjects editables en Steven 1.

Se reutiliza RhythmReminderService.Cancel: cancela la notificación programada y la mostrada de ese ID en Android, guarda el estado cancelada en rhythm-reminders.json y excluye el recordatorio de Mi horario tras refrescar. El registro cancelado se conserva internamente; no reaparece al reiniciar. Si falla la cancelación o la persistencia, se muestra el error sin confirmar una eliminación exitosa. No se modifican las tareas.

Validación técnica: compilación C# para Editor y rama Android, referencias/jerarquía de los controles serializados y diff sin errores de formato. Queda comprobar interactivamente abrir, Conservar, Eliminar, eliminar el último del día, navegar varios recordatorios y reiniciar; en Android verificar también que el aviso eliminado no se entregue.

Consulta Jira de cierre (solo lectura): H5 tiene 20 tareas POMO-225 a POMO-244. Ya figuran finalizadas 225, 226, 227, 228, 230, 231, 238, 242, 243 y 244. Con lo implementado y la validación del flujo comunicada por Santiago, son candidatas a cerrar 229, 232, 233, 235, 237, 239 y 240. Para 234 y 236 falta confirmar si se probó la notificación real en Android; para 241, confirmar el escenario de nuevos bloques que cambian recomendaciones conservando los recordatorios existentes. No se cambiaron estados ni responsables en Jira. La eliminación nueva todavía necesita su comprobación manual.
