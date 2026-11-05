# POMO-344 — Regresión final de Santiago

## Evidencia

- **166 comprobaciones aisladas:** permisos, fechas, nombre obligatorio, borrado, edición inválida, persistencia, eliminación repetida, bloques/favoritos, historial sin duplicados, copas, inventario y recordatorios. Usan código real de servicios/repositorios y dobles de Android/UI; no sustituyen la APK.
- **Play real:** cancelaciones de Tareas, edición, tarea de Calendario, recordatorios y ambos pasos del borrado; consulta de privacidad; navegación de vuelta a Inicio; Mes → Semana → Día → Mes. El cambio temporal de edición fue descartado.
- **Datos del usuario:** los cinco JSON permanecen idénticos por SHA-256. Huellas en `Logs/Sprint4/regression-user-data-{before,after}.json`.
- **Escena:** 5.166 bloques YAML válidos; 209 llamadas persistentes comprobadas, seis controladores de confirmación con referencias asignadas y un EventSystem.
- Compilación C# para Editor y rama Android sin errores. Se corrigieron dos símbolos no incluidos en las fuentes: marca de casilla y separador de mes. Se verán al recargar; los avisos del vídeo no se modificaron.

## Último recorrido manual

1. Crear **Prueba final A** y **Prueba final B**; una desde Calendario, verificando día/mes.
2. Editar A y guardar. Comprobar el cambio en Tareas/Calendario.
3. Eliminar A desde Tareas: cancelar conserva; confirmar elimina únicamente A. Eliminar B desde Calendario: cancelar conserva; confirmar elimina únicamente B.
4. Reiniciar Play: las tareas de prueba borradas no reaparecen; las tareas propias, progreso y preferencias siguen disponibles.

Con ese recorrido correcto pueden cerrarse 339 y 344 para los flujos de Santiago; 337 ya está revisada y 338 comprobada. El borrado global 325/326 se mantiene cerrado por decisión del usuario; esta revisión no lo ejecutó sobre sus datos. La validación de permisos/notificaciones en Android sigue aparte.

## Hallazgos fuera de estos recorridos

En el ajuste extra de interfaz se conectaron los siete días de Resumen semanal al detalle diario. El botón de desarrollo `NotificationProof` conserva una llamada a `ProbarNotificacion`, que ya no existe. Ese botón no pertenece a los flujos de Tareas, Calendario, Ajustes y borrado revisados. No se declara libre de defectos todo el proyecto.

Repetir: `./Tools/TestNotificationPermissions.ps1`, `./Tools/CheckSantiagoSceneYaml.ps1` y `node Tools/CheckSantiagoInteraction.cjs`. Este último informa el enlace antiguo del botón de desarrollo además de validar las confirmaciones.
