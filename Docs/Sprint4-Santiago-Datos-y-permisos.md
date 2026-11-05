# Sprint 4 — POMO-320 / S4-T18

Escena: `Santiago`. Estado: **En curso**; revisión estática hecha, manifiesto de APK pendiente.

## Datos guardados

Todo se guarda localmente en `Application.persistentDataPath`, mediante `JsonStorageService`.

| Archivo | Datos y finalidad | Responsable |
|---|---|---|
| `tasks.json` | Título, descripción, asignatura, fechas, estado e ID de aviso; organizar tareas | `TaskService` / `TaskRepository` |
| `work-blocks.json` | Duraciones, nombres, favoritos, selección y tarea asociada | `PomodoroService` / `WorkBlockRepository` |
| `progress.json` | Sesiones, intervalos, títulos de tareas, trofeos, copas y datos de demostración de Ritmo | `ProgressService` / `ProgressRepository` |
| `rhythm-reminders.json` | Nombre, fecha/hora, estado e ID de aviso | `RhythmReminderService` / `RhythmReminderRepository` |
| `store-inventory.json` | Artículos adquiridos | `StoreInventoryService` / `StoreInventoryRepository` |

**PlayerPrefs:** `Pomo_AudioEnabled`, `pomo.deleted-tasks.date`, `pomo.deleted-tasks.count`
y `pomo.privacy.accepted-version` (versión del aviso aceptado).
El paquete de notificaciones también mantiene `com.unity.androidnotifications.PostNotificationsPermission`;
esa clave no concede ni revoca permisos de Android.

La bienvenida integrada tiene nombre y hábitos en pantalla, pero todavía no los persiste.
Coordinar con Jean el nombre y el indicador de primera entrada. Guardar hábitos solo si una función real los usa.

## Permisos esperados

| Permiso | Uso | Comprobación |
|---|---|---|
| `POST_NOTIFICATIONS` | Avisos de tareas, bloques y Ritmo | El paquete lo declara; falta comprobar APK y rechazo |
| `SCHEDULE_EXACT_ALARM` | Puntualidad de recordatorios de Ritmo | Configurado; revisar si la exactitud es indispensable |
| `RECEIVE_BOOT_COMPLETED` | Recuperar avisos al reiniciar dispositivo | Activado en configuración; comprobar en Android |

No se encontraron solicitudes propias de contactos, ubicación, cámara o micrófono.
Internet y almacenamiento externo forzados están desactivados. Esto no sustituye inspeccionar el manifiesto final.

## Pendientes que afectan otras tareas

- **POMO-321/322:** explicar permisos antes de pedirlos, manejar rechazo y evitar solicitudes repetidas.
- **POMO-325:** cubrir los cinco JSON, sus `.tmp`, preferencias propias y avisos Android; limpiar también memoria y sesiones activas.
- Borrar una tarea no elimina sus títulos históricos de `progress.json`.
- Cancelar un recordatorio lo conserva en JSON como `cancelada`.
- Evitar que datos de demostración o sesiones anteriores reaparezcan después del borrado.
- Coordinar preferencias con Valentina y errores de almacenamiento con Steven.

**Fuentes:** `Assets/Scripts/Data/{Models,DTOs,Repositories,Persistence}`, `NotificationService`,
`MemoryGameController`, `AudioService`, `ProjectSettings/NotificationsSettings.asset` y paquete Mobile Notifications 2.4.3.

**Para finalizar:** contrastar el manifiesto de una APK de Santiago. Las pruebas físicas de autorización/rechazo corresponden a POMO-323 con Jean.
