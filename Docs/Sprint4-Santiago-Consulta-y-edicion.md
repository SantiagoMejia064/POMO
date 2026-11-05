# POMO-324 / S4-T22 — Consulta y edición

Revisión técnica realizada sobre `Santiago`. Santiago confirmó en Play los siete recorridos de creación, consulta, edición, descarte, datos inválidos, reinicio, progreso y sonido. Queda revisar visualmente la corrección posterior de Calendario y del modal de eliminación.

| Información | Recorrido y componentes reutilizables | Alcance actual |
|---|---|---|
| Tareas | Tareas → detalle → editar; `TasksScreenController`, `TaskService`, `TaskRepository` | Consulta y edición de título, descripción, asignatura y fecha. Guardar valida y escribe `tasks.json`; descartar limpia el formulario sin guardar. |
| Horario | Mi horario; `CalendarScreenController` | Consulta tareas y recordatorios, cambia de período y recarga datos. El resumen de tarea no edita: `SaveCalendarEditSummary` solo cierra. La edición real se hace desde Tareas. |
| Bloques | Personalizar bloque y favoritos; `BlockSelectionController`, `PomodoroService` | Selección de tarea y duración, creación de bloque y cambio de favorito. No hay edición general de un bloque ya guardado. |
| Progreso | Progreso diario, semanal y Ritmo; `DailyProgressTextView`, `ProgressService`, `WorkRhythmScreenController` | Consulta de resultados calculados a partir de tareas y sesiones. No se modifican manualmente las estadísticas. |
| Preferencias | Memoria → sonido; `MemoryGameController`, `AudioService` | Memoria guarda `Pomo_AudioEnabled`. `PomodoroController.SetSoundEnabled` solo cambia el estado de la sesión. Ajustes permite consultar privacidad y gestionar permisos. |

**Comprobado:** los nueve controladores revisados existen en Santiago y sus referencias locales apuntan a objetos existentes. Lista, detalle y campos de edición de tareas están asignados; Guardar, descartar y confirmaciones tienen callbacks conectados.

**Pendientes detectados:** no hay perfil persistente de nombre/hábitos; coordinar su integración con Jean. No hay acceso de edición a recordatorios ya creados desde Calendario. Unificar la persistencia del sonido y evitar presentar el resumen de Calendario como una edición efectiva.

## Validación en Play

1. Crear una tarea, abrir detalle, editar sus cuatro campos y guardar. Comprobar la nueva fecha en Calendario y los datos después de cerrar y abrir la aplicación.
2. Editarla otra vez y descartar: conservar los datos guardados. Probar fecha inválida y título vacío: no guardar.
3. Consultar progreso diario/semanal y Ritmo; comprobar que muestran las tareas y sesiones correspondientes.
4. Cambiar sonido desde Memoria y reiniciar: conservarlo. Comprobar por separado el control de Trabajo según el límite documentado.

No se duplicaron pantallas ni se modificó el almacenamiento del usuario. La eliminación global corresponde a POMO-325; permanencia del borrado y reinicio, a POMO-326.

## Corrección visual posterior

- Semana conserva la altura de las casillas de Mes; los títulos de tareas y recordatorios usan un tamaño legible. La cuadrícula queda excluida del estilo automático de botones.
- Resumen de tarea: título y asignatura con ancho adaptable, escala normal, tamaño automático y separador fuera del título.
- El modal compartido de eliminación siempre se coloca al frente, aunque Tareas siga activa. Al cerrar restaura jerarquía y medidas originales; no se duplican confirmaciones.
- Compilación C# para Editor y Android sin errores; callbacks de la escena conservados y sin referencias locales faltantes.
- Repetir en Play: Mes → Semana → Día → Mes; abrir una tarea de título largo; eliminar → cancelar → eliminar → confirmar. Comprobar también la eliminación desde Tareas.

## Fecha desde Calendario

Corregida la inversión de día y mes al rellenar el formulario: `05/11/2026` debe seguir siendo 5 de noviembre. Las fechas antiguas `dd/MM/yyyy` se leen antes de intentar interpretar ISO. Las fechas ya guardadas en ISO mantienen su valor; una tarea creada antes en el día equivocado debe editarse manualmente.

Prueba: seleccionar 5 de noviembre → crear tarea → comprobar `05/11/2026` en el formulario, detalle y Calendario después de reiniciar. Comprobaciones automatizadas: fechas ambiguas, día mayor que 12, año bisiesto, persistencia y formatos regionales español e inglés; archivos aislados, sin usar datos del usuario.
