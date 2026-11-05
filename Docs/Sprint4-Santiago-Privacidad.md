# POMO-317 / S4-T15 — Aviso de privacidad

Texto redactado: `Assets/Resources/PrivacyNotice.txt`.
Basado en el inventario de POMO-320 y el estado actual de Santiago.

- Explica datos, finalidad, ubicación, títulos conservados en el historial y contenido visible en notificaciones.
- Usa texto para el usuario final; revisar su inventario cuando Jean integre nombre y hábitos persistentes.
- No promete un borrado global o recuperación que todavía no existen.

**POMO-317:** inventario y redacción completados para la versión actual.
**POMO-318:** completada; Santiago comprobó el acceso Inicio → Ajustes → Aviso de privacidad.
**POMO-319:** validación funcional del aviso comunicada por Santiago; no se atribuye al agente una prueba en dispositivo.

Casilla vacía, enlace al texto completo desplazable y botón Aceptar deshabilitado hasta marcarla.
El contenido de bienvenida queda bloqueado por CanvasGroup y el panel captura los clics.
Solo Aceptar registra `pomo.privacy.accepted-version`; reabrir el aviso no registra aceptación.
El consentimiento queda activado en la escena al iniciar; una aceptación anterior no lo oculta.
Desde Ajustes se consulta el mismo panel en modo lectura y Volver regresa a Ajustes, sin abrir la bienvenida.
Si cambian los términos después de publicarlos, incrementar `PrivacyConsentController.NoticeVersion`.

Comprobación disponible: `POMO > Privacidad > Comprobar consentimiento` (fuera de Play).
Usa una escena de previsualización y restaura la preferencia original. No se ha ejecutado aquí.
Compilación C# de ejecución y Editor: sin errores. Referencias y bloqueo inicial de escena: revisados.
Inicio → Ajustes → Aviso de privacidad → Volver → Inicio comprobado por Santiago.

## Ajustar la presentación

- `Cuadro de consentimiento > Explicación del consentimiento`: ajustar su RectTransform y el tamaño máximo de texto. Tiene salto de línea y ajuste automático.
- `Aviso completo de privacidad > Área de lectura`: ajustar el tamaño del área visible.
- `Zona visible del aviso > Texto completo del aviso`: ancho vinculado al área; altura automática para permitir desplazamiento. Cambiar tamaño de letra o márgenes, no la altura del ContentSizeFitter.
- `Canvas > Estilo Botones Escena`: fuente Gagalin y radio de esquinas (12 por defecto, en unidades UI que escala el Canvas). Conserva los iconos e ilustraciones.
- Las tarjetas de tareas, progreso y bloques conservan su estilo original. Sus listas están excluidas del estilo automático para evitar que cambien de ancho o desaparezca su contenido.
