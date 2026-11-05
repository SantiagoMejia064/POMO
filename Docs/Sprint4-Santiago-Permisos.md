# POMO-321 y POMO-322 — Permisos

Estado: **En curso; falta comprobación en Android (POMO-323).**

- Los avisos de tareas, bloques, objetivos y eliminaciones ya no abren permisos automáticamente.
- Al guardar una tarea futura, POMO explica el aviso y ofrece Permitir avisos / Ahora no. La tarea ya está guardada y no se pierde al rechazar.
- Ritmo explica notificaciones y alarmas exactas antes de abrir Android; si no se autorizan, conserva el formulario sin crear un recordatorio nuevo.
- Ajustes → Notificaciones permite activar avisos o explica cómo habilitarlos si Android los bloqueó.
- No se repite una solicitud pendiente ni se vuelve a pedir automáticamente una rechazada. Ahora no se respeta durante la sesión.
- Los fallos nativos de envío/cancelación no bloquean las operaciones de tareas; se registran para diagnóstico.

**Verificado:** compilación C# para Editor y rama Android; 25 comprobaciones con Android simulado y archivos aislados. Cubren persistencia, rechazo, solicitud explícita, pendientes, duplicados y fallos nativos.

Repetir: `./Tools/TestNotificationPermissions.ps1`. No abre diálogos del SO ni modifica datos del usuario.

**Con Jean:** probar primera autorización, Ahora no, rechazo de Android, reapertura, habilitación desde ajustes, alarmas exactas y recepción real. Comprobar que tareas, calendario y progreso siguen utilizables. La simulación no sustituye estas pruebas ni la inspección del manifiesto final.
