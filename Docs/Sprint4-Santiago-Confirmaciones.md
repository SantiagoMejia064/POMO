# POMO-337 / 338 / 339 — Confirmación y cancelación

Revisión realizada en código, escena y Play de Santiago. No se borraron datos del usuario.

| Recorrido | Resultado observado en Play |
|---|---|
| Tareas → eliminar → cancelar | La tarea permanece en la lista. |
| Editar → cerrar → volver | Conserva el cambio temporal en el formulario. |
| Editar → cerrar → cerrar sin guardar | Descarta el cambio temporal y conserva el título original guardado. |
| Calendario → tarea → eliminar → cancelar | Confirmación al frente; vuelve al resumen con la tarea intacta. |
| Calendario → recordatorio → borrar → conservar | Mantiene el recordatorio y restablece los controles. |
| Ajustes → borrar datos → cancelar | Regresa a Ajustes sin borrar. |
| Ajustes → borrar datos → continuar → cancelar | También conserva los datos desde el segundo paso. |
| Aviso de privacidad desde Ajustes → volver | Regresa a Ajustes, sin reiniciar bienvenida. |

Los cinco JSON se compararon por SHA-256 antes y después: **idénticos**. Los avisos bloquearon los botones de fondo en los recorridos comprobados.

**337:** revisión completada. **338:** confirmaciones existentes y reutilización del modal de Tareas comprobadas; no hicieron falta modales nuevos. **339:** cancelación comprobada en Play y guardado/eliminación comprobados con servicios reales y archivos aislados. La comprobación final del clic de confirmar debe hacerse con una tarea de prueba.

ConfirmDelete limpia la acción pendiente antes de ejecutarla; cancelar la descarta. El modal compartido restaura jerarquía y medidas al cerrarse. Guardar en el resumen de Calendario solo cierra el resumen; la edición real está en Tareas.

Recorrido final y límites: Docs/Sprint4-Santiago-Regresion.md. No se modificaron estados de Jira.
