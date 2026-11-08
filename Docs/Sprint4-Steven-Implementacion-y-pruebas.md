# Sprint 4 de Steven en POMO

La versión de entrega se carga desde `Assets/Scenes/Santiago.unity`. Esta escena reúne la bienvenida, los cambios de privacidad y permisos y los módulos integrados. Se seleccionó como única escena activa en la configuración de compilación. La base revisada era el commit `57ab183` de `main`.

| Jira | Qué existía | Qué queda preparado o comprobado |
| --- | --- | --- |
| POMO-309 | El módulo de accesibilidad venía instalado. Los controles son de uGUI y TextMesh Pro. | Se revisó el recorrido de controles y se seleccionó la API Assistive Support de Unity. TalkBack requiere Android 8.0 o posterior para esta API. |
| POMO-310 | Había etiquetas visuales y botones con ícono. | La escena entrega a la tecnología de asistencia los controles visibles e interactivos. Los controles de ícono de Memoria, Bloques, Calendario y Tienda tienen nombres explícitos. Los demás toman el texto del control o la pista del campo. Los botones conservan sus acciones y las listas permiten desplazamiento. |
| POMO-311 | Había estados visuales en Memoria, mensajes del temporizador y sonido. | Se añadió vibración de fin de foco, ronda completa y fin de partida. Ajustes guarda la preferencia. Las señales visuales funcionan con sonido apagado y se anuncian si el lector está activo. |
| POMO-312 | La entrega al dispositivo estaba pendiente. | El recorrido de Android está al final de este documento. Falta la comprobación en dispositivo con Jean. |
| POMO-327 | El inventario de datos locales ya estaba descrito por Santiago. | Se verificaron los cinco repositorios y se completó el inventario con los respaldos y las preferencias de audio y vibración. |
| POMO-328 | El guardado usaba un temporal. Los errores de carga iban a consola. | Ahora se conserva la versión anterior, se recupera cuando corresponde y se muestran avisos diferentes de fallo y de recuperación. Una escritura fallida corta el flujo antes de confirmar éxito. |
| POMO-329 | Existía documentación de archivos y de borrado. | Se ejecutaron escenarios de recuperación y fallo de escritura con archivos de prueba. El borrado de datos incluye los respaldos. |
| POMO-340 | Existía un perfil Android que usaba la lista global de escenas. | Se generó y verificó la APK de Santiago con Unity 6000.3.11f1. |
| POMO-343 | Había código reutilizable de celebraciones, comparación y temporizador, sin todas sus referencias conectadas. | Se conectaron las vistas y el motor del temporizador, se usaron períodos reales en Progreso y se verificaron las conexiones de la escena. Queda la regresión visual y de audio en el dispositivo. |

## Integración de la escena

- El motor y el audio de fin de foco viven en el Canvas activo. Volver de la pantalla de Bloques conserva el tiempo de la sesión. Si el descanso termina mientras el usuario está en otro módulo, se conserva el resultado sin abrir la pantalla de finalización encima de ese módulo.
- Memoria usa 300 segundos y el temporizador de trabajo respeta la duración del bloque elegido. Los clics del tablero y sus controles respetan la preferencia de sonido.
- El fin de foco guarda el bloque y las 20 copas en una misma escritura. La clave de sesión protege contra un premio repetido. La tarjeta de Pomo muestra el logro una vez. Si Memoria está abierta, se usa el aviso inmediato del bloque y no se repite la tarjeta al salir.
- Durante el uso de la app, el aviso nativo de descanso va a un canal de historial silencioso. La señal audible o háptica proviene del evento del temporizador y respeta sus preferencias. Android permite al usuario cambiar los ajustes de cada canal.
- Progreso semanal compara la semana actual con la anterior usando el historial. Se muestra el estado neutral si falta actividad suficiente. La fuente de hitos de rachas y el equipamiento global de Tienda siguen siendo alcances separados.
- TalkBack usa la jerarquía de Unity. El orden real de lectura, la activación de controles y el teclado nativo requieren la prueba física. [Documentación de accesibilidad de Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/mobile-accessibility.html)

## Datos locales y recuperación

| Archivo dentro de `Application.persistentDataPath` | Información |
| --- | --- |
| `tasks.json` | Títulos, asignaturas, descripciones, fechas, estados e IDs de avisos de tareas. |
| `work-blocks.json` | Bloques propios, selección y favoritos. |
| `progress.json` | Sesiones, intervalos, progreso, copas y trofeos. |
| `store-inventory.json` | IDs de artículos comprados. |
| `rhythm-reminders.json` | Recordatorios de Ritmo, fechas, estado e ID de aviso. |

El nombre y la aceptación del aviso de privacidad siguen en PlayerPrefs, junto con las preferencias de sonido y vibración y los contadores locales de avisos. El guardado usa un archivo temporal junto al principal. Al sustituir un archivo válido, la versión previa queda en `.bak`. Al recuperar, el aviso indica que pueden faltar cambios recientes. Si ambas copias están dañadas, se conservan para evitar sobrescribir la información. Los respaldos son por archivo y pueden corresponder a instantes distintos.

## Pruebas

Se ejecutaron `Tools/TestNotificationPermissions.ps1`, `Tools/CheckSantiagoSceneYaml.ps1` y `node Tools/CheckSantiagoInteraction.cjs`. Pasaron 180 comprobaciones aisladas, incluidas escritura bloqueada, recuperación, preservación de copias dañadas, borrado de respaldo y premio único. La escena tiene 5.239 bloques válidos, 215 llamadas persistentes verificadas y cero referencias locales sin resolver. El código C# se comprobó con las referencias del Editor y los símbolos de Android.

Unity 6000.3.11f1 también cargó Santiago en una copia aislada. El verificador confirmó los componentes de la escena, el motor y audio activos, los controles accesibles, el ajuste de vibración y la recuperación de un archivo dañado con JsonUtility real. El registro es `Logs/Sprint4/steven-unity-validation.log`.

Para completar el recorrido en el celular:

1. Abrir `Santiago`, entrar a Bloques y comenzar una sesión. Pausar, reanudar, volver al menú y abrir el temporizador. Verificar que conserva la misma sesión.
2. Desactivar sonido, dejar vibración activa y completar un bloque. Comprobar el aviso, una vibración y 20 copas una sola vez. Repetir con vibración desactivada. Revisar que entrar otra vez no repita la recompensa.
3. Activar TalkBack en Android. Navegar por Inicio, Bloques, Tienda y Memoria. Confirmar que lee la acción del botón, enfoca los controles visibles y activa cada acción una sola vez. Probar un campo de texto y una lista con desplazamiento.
4. En Memoria, confirmar que el reloj comienza en 05:00 y que el silencio abarca cartas y botones. Comprobar aviso de ronda y fin de partida. Si termina un bloque durante Memoria, verificar que su aviso no reaparece al salir.
5. Con copas de prueba, comprar un artículo, volver a la tienda e intentar comprarlo de nuevo. Confirmar un solo cargo y que inventario y saldo se conservan tras reabrir. En Progreso y Ritmo, confirmar que la sesión aparece una vez.
6. Para revisar recuperación manual, usar una instalación o un directorio de pruebas. Guardar dos versiones, dañar el archivo principal y reabrir. Comprobar que aparece la versión anterior y el aviso de recuperación. Usar un respaldo inválido en un segundo escenario y verificar que la app conserva ambas copias y avisa que no pudo recuperarlas.

Para preparar la APK, abrir el perfil Android en Unity 6000.3.11f1, comprobar que hereda la lista con Santiago habilitada y seleccionar Build con App Bundle desactivado. Jean puede instalarla y registrar versión de Android, TalkBack, vibración, permisos y resultado de cada recorrido.

En este equipo se instaló Android Build Support para Unity 6000.3.11f1 y se reutilizaron las carpetas SDK, NDK r27c y OpenJDK 17 de Unity 6000.3.7f1. Si esa instalación todavía es la fuente de herramientas Android, se pueden seleccionar sus rutas en las preferencias de External Tools del Editor.

La APK generada quedó en `Builds/POMO-Sprint4-Santiago.apk`. Es ARM64, tiene firma de prueba válida y declara vibración y notificaciones. Unity ejecutó el verificador de la escena y de recuperación antes de empaquetar. Registro de compilación: `Logs/Sprint4/steven-android-build.log`.

SHA-256 `02DD4851FDA04F9094F847A11849F972ED72333827A11382F0B413ABEC036407`.

