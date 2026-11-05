# POMO-325 / S4-T23 — Borrado de datos

Implementado en Santiago; pendiente de validar en Play. No se ejecutó sobre datos del usuario.

- Bienvenida exige escribir un nombre antes de habilitar Siguiente y lo guarda localmente como `pomo.profile.name`. Cada entrada y aceptación de términos limpia el campo; un nombre guardado no lo rellena ni habilita Siguiente. Vacío o solo espacios no permite avanzar.
- Inicio → Ajustes → **Borrar mis datos**: botón rojo, más pequeño y situado arriba a la izquierda. Abre las consecuencias; Continuar abre un segundo paso con la advertencia «Pomo se olvidará de ti» y un campo vacío para escribir el nombre guardado. Borrar definitivamente solo se habilita si coincide; mayúsculas y espacios exteriores no afectan. Ninguno de los dos pasos borra antes del último botón.
- Cancelar no llama al servicio de borrado: conserva archivos, preferencias y la sesión.
- Confirmar cancela avisos pendientes y visibles, descarta la sesión activa sin guardar trabajo interrumpido y elimina los cinco JSON y sus `.tmp`: tareas, bloques, progreso, inventario y recordatorios.
- Elimina las cinco preferencias propias: nombre, sonido, contadores de eliminación y aceptación de privacidad. Los permisos de Android permanecen bajo control del sistema.
- Recarga la escena para descartar repositorios, formularios y selecciones anteriores; vuelve a la bienvenida. La carga automática de datos de demostración de Ritmo queda desactivada.
- Los archivos bloqueados y los errores nativos se muestran como fallo, nunca como borrado completado. Si ya se inició el borrado, solo permite reintentarlo para no usar datos antiguos en memoria.

**Comprobado:** 142 verificaciones aisladas de permisos, fechas, nombres, controlador de bienvenida y borrado; compilación C# para Editor y Android; referencias de escena válidas. La prueba del controlador usa dobles de UI y no sustituye Play. El botón Siguiente conserva el mismo destino a través de un método que valida y guarda el nombre. Ejecutar `./Tools/TestNotificationPermissions.ps1`.

**Validar en Play:** nombre vacío/espacios → Siguiente deshabilitado; nombre válido → continuar. En Ajustes, cancelar desde ambos pasos conserva datos. En el paso final, campo vacío/nombre distinto bloquea Borrar definitivamente; nombre coincidente habilita el botón. Tras borrar, comprobar bienvenida con nombre vacío, tareas vacías, progreso sin historial/recompensas, inventario sin compras y sin favoritos propios. Los bloques predefinidos y el catálogo siguen disponibles. Reinicio corresponde a POMO-326; avisos reales requieren Android.

La APK debe incluir Santiago para probar esta interfaz. Build Settings todavía tiene habilitada Chantre 3; no se cambió la selección de compilación durante esta tarea.

Integración principal: `BorradoDatosController`, `LocalDataResetService`, `PomodoroController.DiscardSessionForDataReset` y `NotificationService.CancelAllForDataReset`.

Presentación: solo los paneles añadidos usan azul POMO `#0A3266`, botones `#4B73A9` y texto blanco; el acceso pequeño de borrado en Ajustes conserva el rojo. Corregidos cuatro textos con continuaciones YAML sobrantes. Verificación de la escena completa: `./Tools/CheckSantiagoSceneYaml.ps1`.
