# POMO-338 / S4-T36 — Confirmaciones

Estado: **En curso**. Primer recorrido corregido: borrar una tarea desde Calendario.

- El botón ahora solicita confirmación; la tarea solo se borra al aceptar.
- Cancelar mantiene la tarea y el resumen de Calendario abiertos.
- Se reutiliza el modal de Tareas. Siempre se muestra al frente sobre el Canvas, incluso si Tareas sigue activa detrás de Calendario; al cerrar restaura su posición, medidas y jerarquía.
- Se recargan las tareas antes de solicitar y ejecutar el borrado.

Archivo: `Assets/Scripts/Presentation/Screens/TasksScreenController.cs`.
Validación ejecutada: compilación C# con referencias de Unity, sin errores; advertencias existentes.
No se modificaron escenas ni el almacenamiento del usuario.

## Comprobar en Play de Santiago

1. Calendario → tarea → eliminar: debe aparecer el modal sin borrar todavía.
2. Cancelar: conservar tarea y resumen; volver a solicitar y aceptar: eliminar y refrescar Calendario.
3. Volver a solicitar y cancelar varias veces: no acumular acciones ni borrar al confirmar otro flujo.
4. Repetir eliminación desde Tareas: conservar su comportamiento anterior.

El usuario confirmó que las confirmaciones necesarias funcionan como deberían.
Validación comunicada por Santiago; no se presenta como una prueba ejecutada por el agente.
El recorrido de Calendario corregido queda validado por el usuario. Esta nota no sustituye evidencias de otros recorridos ni cambia Jira automáticamente.
