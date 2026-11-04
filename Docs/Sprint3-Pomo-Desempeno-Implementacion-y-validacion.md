# Sprint 3 — Expresiones de Pomo según el desempeño

Entrega de implementación: 26 de octubre de 2026. Escena: `Assets/Scenes/Steven 1.unity`.
Responsable de este bloque: Steven. La aceptación en ejecución corresponde a Steven.
Codex no ejecutó Play Mode, compilación, pruebas automatizadas ni builds.

## 1. Alcance y relación con Jira

| Tarea de Steven | Qué se implementó | Qué necesita su cierre |
|---|---|---|
| [POMO-287 / S3-T63](https://nebuly-studios-pomo.atlassian.net/browse/POMO-287) | Adaptador de la clasificación existente a la expresión y mensaje de Pomo, con entradas públicas para datos reales. Vista asignada en Progreso semanal. | Validar la conexión y entregar la selección desde el comparador definitivo. |
| [POMO-289 / S3-T65](https://nebuly-studios-pomo.atlassian.net/browse/POMO-289) | Cuatro estados en el controlador visual común, fundido de sprites, transición de movimiento y regreso al contexto después de un gesto temporal. | Validación manual de transiciones y del arte definitivo cuando esté disponible. |
| [POMO-290 / S3-T66](https://nebuly-studios-pomo.atlassian.net/browse/POMO-290) | Herramienta manual del Inspector para los cuatro escenarios, consulta de períodos reales y recorrido de revisión de este documento. | Steven debe ejecutar los escenarios y confirmar el resultado; preparar la herramienta no equivale a haberlos probado. |

HU relacionada: [POMO-87 / HU 8.2.3](https://nebuly-studios-pomo.atlassian.net/browse/POMO-87),
«Cambio de Expresión Facial Según el Desempeño», de la épica Pomo / H8.
En la consulta de Jira de esta entrega, las tres tareas seguían «Tareas por hacer».
No se modificó su estado.

Este bloque cubre las expresiones según la comparación de progreso. La reacción
inmediata al completar una tarea y las celebraciones de logros corresponden a los
otros bloques del plan: POMO-267/269 y POMO-299/301. No se presentan como
implementados aquí. La HU completa también necesita esas integraciones y el arte compartido.

La aplicación registra ejecución de tareas, no notas académicas. Una menor
actividad no significa que el estudiante haya respondido incorrectamente una tarea.

## 2. Qué aparece y dónde

Recorrido de uso: Inicio → Progreso → Progreso de la semana.

Jerarquía guardada en la escena, disponible también fuera de Play:

```text
Canvas
└─ Sección progreso
   └─ Progreso semanal                  (se activa con la navegación existente)
      └─ Pomo desempeño
         ├─ Pomo animado                (Animator)
         │  ├─ Pose anterior            (Image, apagada al terminar el fundido)
         │  └─ Pose actual              (Image)
         ├─ Clasificación               (TextMeshPro)
         ├─ Mensaje                     (TextMeshPro)
         └─ Períodos comparados         (TextMeshPro)
```

`Pomo desempeño` contiene `PomoVisualController` y `ProgressPomoFeedbackView`.
La tarjeta nueva ocupa el espacio anterior al botón de detalles de Progreso semanal:
posición local (0, -595), tamaño 900 × 330, anclada al centro de ese panel.
Sus elementos usan Poppins y la paleta azul existente. Ninguna imagen o etiqueta
de la tarjeta captura clics. Los controles anteriores conservan su distribución.
El ajuste visual final en los dispositivos lo confirma Steven durante la revisión manual.

Se añadieron 29 bloques serializados y solo se modificaron dos bloques existentes
durante la migración: los hijos de Progreso semanal y la referencia del controlador
de navegación. No se reconstruyó la escena ni el minijuego.

### Comportamiento inicial

Abrir Progreso semanal muestra Neutral y «Sin comparación seleccionada» hasta
que un productor entregue A y B. Tener sesiones guardadas no selecciona
automáticamente cuáles comparar. No se introdujo una regla oculta de ayer contra
hoy ni de semana anterior contra actual: esa elección sigue siendo del módulo de comparación.

Para probar datos reales desde el Editor ya existe una consulta manual de dos
fechas, descrita en el apartado 7. El selector definitivo de la interfaz debe
invocar las mismas entradas públicas; no necesita cambiar el presentador.

## 3. Responsabilidad de cada archivo

Rutas relativas a la raíz del repositorio:

| Archivo | Responsabilidad y punto de modificación |
|---|---|
| `Assets/Scripts/Presentation/Screens/ProgressPomoFeedbackView.cs` — nuevo | Recibe la comparación, obtiene el feedback, muestra mensaje/clasificación y llama al controlador visual. Aquí se añaden adaptadores de nuevas fuentes de comparación. |
| `Assets/Scripts/Presentation/Screens/PomoVisualController.cs` — ampliado | Presenta sprites y movimientos. Aquí se mantiene el mapeo visual y el regreso al contexto. No decide si el estudiante mejoró ni guarda recompensas. |
| `Assets/Scripts/Presentation/Screens/ProgressPanelNavigationController.cs` — ampliado | Referencia la nueva vista, refresca al entrar a Progreso semanal y retira la selección al volver a Inicio. |
| `Assets/Scripts/Business/Progress/ProgressService.cs` — ajuste de inicialización | Difiere la creación de TaskService hasta GetTaskStatusCounts. Evita que una consulta de comparación provoque normalización y guardado de fechas antiguas de tareas. Conserva las fórmulas y las firmas públicas. |
| `Assets/Editor/ProgressPomoFeedbackViewEditor.cs` — nuevo | Botones de revisión manual y consulta de fechas. Los datos de vista previa y sus métodos están excluidos del jugador mediante `UNITY_EDITOR`. |
| `Assets/Scenes/Steven 1.unity` — configurado | Objetos, referencias, tipografía, sprites provisionales, Animator y perfiles de la tarjeta. La vista se puede acomodar desde la jerarquía. |
| `Tools/PrepareProgressPomoPresentation.cjs` — nuevo | Registro de la migración puntual de la escena. Ya fue aplicada; detecta la vista y no reconstruye una existente. No es parte del juego ni una herramienta de pruebas. |

Se añadieron los `.meta` de los dos scripts nuevos. Se reutilizan, sin cambiar
sus fórmulas ni el formato de persistencia, `ProgressService`, `ProgressComparisonModel`,
`ProgressFeedbackModel`, `PerformanceFeedbackClassification`, `ProgressRepository`
y los clips de `Assets/Resources/Pomo/MemoryMotion.controller`.

## 4. Flujo de información

```text
Selector del comparador decide A (referencia) y B (evaluado)
    ↓
ProgressPomoFeedbackView recibe fechas, sesiones, métricas o feedback
    ↓
ProgressService.CompareProgress / CompareSessions (cuando corresponda)
    ↓
ProgressService.GetPerformanceFeedback
    ↓
ProgressFeedbackModel: classification + message + comparison
    ↓
Textos de la tarjeta + PomoVisualController.ShowPerformance
    ↓
Perfil de sprite + CrossFade del Animator + fundido de imágenes
```

Las diferencias significan **B menos A**. La fórmula existente evalúa primero
ausencia de sesiones, después diferencia de tareas completadas y luego las
métricas de tiempo y cantidad de sesiones. No se implementó un cálculo alternativo.

| Clasificación | Estado visual | Título de la tarjeta | Movimiento provisional |
|---|---|---|---|
| Neutral | PerformanceNeutral | AÚN SIN COMPARACIÓN | Idle |
| Improved | PerformanceImproved | VAS MEJORANDO | Match |
| Stable | PerformanceStable | RITMO CONSTANTE | Idle |
| NeedsImprovement | PerformanceNeedsImprovement | RETOMA TU RITMO | Encourage |

El mensaje procede de `GetPerformanceFeedback`, por ejemplo:

- Sin datos: «Aún no hay suficiente actividad para comparar. Completa otro bloque y revisa tu progreso.»
- Más tareas completadas en B: «¡Vas mejorando! Completaste más tareas que antes.»
- Mismas métricas: «Mantienes un ritmo constante. ¡Sigue así!»
- Menos tareas completadas en B: «Avanzaste con menos tareas. Organiza un nuevo bloque para retomar el ritmo.»

Si `ShowFeedback` recibe el modelo ya calculado, conserva su clasificación y
mensaje. Modelos nulos, clasificaciones desconocidas o comparaciones sin sesiones
en cualquiera de los períodos se presentan como Neutral, retirando el estado anterior.

### Actualización y conservación de datos

- `CompareDays` y `CompareWeeks` recuerdan las fechas elegidas. Cada `Refresh`
  vuelve a consultar el progreso guardado; también se refrescan al recibir
  `ProgressService.StudySessionsChanged` mientras la vista está activa.
- `ShowComparison`, `ShowFeedback` y `CompareCompletedSessions` reciben una
  instantánea. El módulo productor debe reenviarla cuando cambie su selección
  o sus datos. La vista no puede deducir qué sesiones había elegido ese módulo.
- Los grupos de sesiones se copian, excluyendo entradas nulas, interrumpidas
  o sin tiempo trabajado. La fuente debe entregar sesiones reales completadas,
  sin duplicarlas ni incorporar las demostraciones del módulo de ritmo.
- Ninguna entrada de esta vista guarda progreso, cambia tareas ni entrega copas.
- `ClearComparison` retira únicamente la selección en memoria. No borra JSON.
- Salir y volver a Progreso semanal conserva la selección durante esa visita
  a Progreso. Volver a Inicio la retira. Salir de Play descarta la selección.
- La vista previa es temporal; al ocultar el panel se abandona. Al reabrir se
  aplica la selección real, si la había, o Neutral.

El servicio se crea desde `Awake` o desde una llamada explícita de la vista,
nunca desde un inicializador de campo de MonoBehaviour. Así se evita consultar
`Application.persistentDataPath` durante la construcción/serialización del componente.
Además, su dependencia de tareas ahora se carga únicamente al pedir los conteos
de tareas. Antes, construir el servicio para una comparación también podía
ejecutar la normalización de fechas y guardar tareas antiguas; la vista de
desempeño no necesita esa operación. GetTaskStatusCounts conserva esa carga y
su recarga habitual cuando otro módulo realmente lo consulta.

## 5. Cómo integrarlo desde el comparador

El compañero que gestione la selección debe añadir una referencia serializada
a `ProgressPomoFeedbackView` y asignar el componente de `Pomo desempeño`.
Al confirmar los dos grupos o períodos, invoca **una** de estas entradas.
No es necesario llamar después a otro método de animación.

```csharp
using System;
using Pomo.Data.Models;
using Pomo.Presentation.Screens;
using UnityEngine;

public class EjemploConexionComparador : MonoBehaviour
{
    [SerializeField] private ProgressPomoFeedbackView feedbackView;

    // Conectar las fechas elegidas por el usuario, en ese orden.
    public void ConfirmarDias(DateTime referencia, DateTime evaluado)
        => feedbackView.CompareDays(referencia, evaluado);

    // Si el comparador ya calculó el modelo, reutilizar ese mismo resultado.
    public void ConfirmarResultado(ProgressFeedbackModel feedback)
        => feedbackView.ShowFeedback(feedback, "Comparación seleccionada");

    public void RetirarSeleccion() => feedbackView.ClearComparison();
}
```

Otras entradas disponibles:

```csharp
feedbackView.CompareWeeks(fechaDentroDeSemanaA, fechaDentroDeSemanaB);
feedbackView.CompareCompletedSessions(sesionesCompletadasA, sesionesCompletadasB,
    "Sesiones seleccionadas");
feedbackView.ShowComparison(metricasYaCalculadas, "Comparación seleccionada");
feedbackView.Refresh();
```

`CompareWeeks` utiliza semanas de lunes a domingo. Las dos fechas no necesitan
ser lunes. Para consultar progreso diario real se usa la fecha local guardada.

No usar un saldo de copas, el puntaje de Memoria, una tarea pendiente o el número
de intentos del minijuego como sustitutos de la clasificación de progreso.
`FeedbackPresented` es una notificación de presentación que puede emitirse al
refrescar; **no es un evento de logro ni debe entregar recompensas**.

## 6. Cómo cambiar o añadir expresiones

### Entrega visual de Valentina

[POMO-288](https://nebuly-studios-pomo.atlassian.net/browse/POMO-288) sigue siendo
su tarea: preparar los cuatro assets compartidos. Aquí se usaron tres recursos
existentes para dejar la conexión operativa:

- Neutral: `Assets/Images/POMO/Pomo explicando.png`.
- Improved: `Assets/Images/POMO/pomo sonriendo.png`.
- Stable: `Assets/Images/Pomo symbol.png`.
- NeedsImprovement: `Pomo explicando.png` con movimiento Encourage.

**Neutral y NeedsImprovement comparten imagen provisional.** Se distinguen por
clasificación, texto y movimiento; no se afirma que ya existan cuatro dibujos
faciales diferentes ni que el arte definitivo de la HU esté finalizado.

Para reemplazarlos:

1. Fuera de Play, seleccionar `Pomo desempeño` en la jerarquía.
2. En `Pomo Visual Controller`, desplegar `Poses`.
3. Buscar los elementos `PerformanceNeutral`, `PerformanceImproved`,
   `PerformanceStable` y `PerformanceNeedsImprovement`.
4. Arrastrar cada nuevo Sprite al campo `Sprite` de su elemento.
5. Si también cambia el movimiento, escribir en `Animator State` el nombre
   exacto de un estado de `Base Layer` del Animator asignado a `Pomo animado`.
6. Guardar la escena. Los campos del minijuego pertenecen a otra instancia;
   no es necesario cambiar su configuración para sustituir el arte de Progreso.

`Transition Seconds` está en 0,2 segundos en esta vista. El controlador mezcla
el movimiento con `CrossFadeInFixedTime`; las imágenes `Pose anterior` y
`Pose actual` hacen el fundido del sprite. Ambas quedan sin captura de clics.
La pose anterior se desactiva al terminar el fundido o al ocultar la vista.
Si ambos estados comparten el mismo Sprite, solo cambia el movimiento y el texto.

Una llamada repetida con la misma clasificación actualiza el texto pero no
reinicia la animación ni incrementa `PresentationVersion`.
Una reacción temporal, como `ShowMatch()`, vuelve después a la clasificación
contextual. Al seleccionar otro período, la nueva expresión tiene prioridad.

Los seis valores anteriores de `PomoVisualState` conservaron su orden numérico;
los cuatro nuevos se añadieron al final. Se conservan las entradas del minijuego.
Si el Animator hijo todavía no se inicializó al activar el panel, el controlador
aplaza el movimiento hasta que esté listo. Un estado de Animator inexistente
produce un aviso con el nombre que necesita corregirse en el Inspector.

Para futuras clasificaciones, ampliar primero el enum/regla del módulo de progreso
y después el mapeo `ShowPerformance`, la etiqueta y los perfiles de esta vista.
No insertar valores entre los estados visuales existentes: están serializados en escenas.

## 7. Recorrido manual de validación de Steven

### A. Ubicación y entrada

1. Abrir `Steven 1` y esperar a que Unity termine de importar. Si pregunta por
   recargar la escena modificada en disco, revisar y aceptar la recarga para ver
   los objetos nuevos. No ejecutar otra vez herramientas de preparación.
2. Sin Play, buscar en la jerarquía `Pomo desempeño`, dentro de Progreso semanal.
   Deben existir los dos componentes y las tres referencias de texto asignadas.
   El Animator y las imágenes están debajo de `Pomo animado`.
3. Entrar a Play, pulsar **Progreso** y después **Progreso de la semana**.
   Debe verse la tarjeta nueva debajo del contenido existente y antes de
   **VER MÁS DETALLES**, sin tapar ese botón.
4. Sin una comparación entregada debe mostrar **AÚN SIN COMPARACIÓN**,
   el mensaje de actividad insuficiente y «Sin comparación seleccionada».
   No debería aparecer una mejora solo por abrir el panel.

### B. Cuatro estados sin modificar partidas ni JSON

1. Mantener Play y el panel de Progreso semanal abiertos.
2. Seleccionar `Pomo desempeño` en Hierarchy. En Inspector, encontrar
   `Progress Pomo Feedback View` y el bloque **Validación manual en Play Mode**.

   Si los botones están grises, leer el aviso amarillo del Inspector. Se habilitan
   únicamente con Play activo, la pantalla semanal abierta y el componente marcado.
   Seleccionar el objeto en Hierarchy no activa su pantalla: primero entrar desde
   Game/Simulator a **Progreso → Progreso de la semana**. El aviso identifica el
   objeto desactivado si algún padre sigue apagado. Si está desmarcado el propio
   componente, activar su casilla junto a `Progress Pomo Feedback View`.
   Para observar animaciones, Unity también debe estar sin pausa.

3. Pulsar los botones de vista previa uno a uno:

| Botón del Inspector | Métricas temporales A → B | Resultado esperado |
|---|---|---|
| Vista previa: Neutral | 0 → 2 sesiones | AÚN SIN COMPARACIÓN; mensaje de actividad insuficiente; PerformanceNeutral. |
| Vista previa: Improved | 2 → 2 sesiones, 1 → 2 tareas | VAS MEJORANDO; mensaje de más tareas; PerformanceImproved y sonrisa/pulgar provisional. |
| Vista previa: Stable | 2 → 2 sesiones, 1 → 1 tarea, 1800 → 1800 segundos | RITMO CONSTANTE; mensaje de ritmo constante; PerformanceStable y pose de saludo provisional. |
| Vista previa: NeedsImprovement | 2 → 2 sesiones, 1 → 0 tareas | RETOMA TU RITMO; mensaje de menos tareas; PerformanceNeedsImprovement y movimiento de ánimo. |

4. En las cuatro vistas debe leerse **VISTA PREVIA - datos temporales**.
   El Inspector también muestra clasificación, estado visual y métricas A/B.
5. Pasar directamente de Improved a Neutral: debe retirarse la mejora, cambiar
   el mensaje y volver a la pose de espera. No debe conservar una sonrisa anterior
   como si aún hubiera una mejora.
6. Cambiar varias veces entre estados: comprobar el fundido cuando cambia el Sprite,
   sin imágenes residuales, tarjetas duplicadas ni bloqueo de botones.

### C. Refresco y regreso al contexto

1. Elegir Stable y anotar **Cambios de presentación** en el Inspector.
2. Pulsar tres veces **Refrescar sin cambiar la selección**. El número debe
   mantenerse y el movimiento no debe empezar otra vez con cada clic.
3. Pulsar **Gesto temporal y regreso al contexto**. Debe aparecer el gesto Match
   de sonrisa/pulgar y, tras aproximadamente 1,5 segundos, volver a Stable.
   Es una vista previa del gesto, no un bloque ni una tarea completada.
4. Repetir el gesto desde NeedsImprovement: después debe restaurar el ánimo
   contextual, sin quedarse en una celebración permanente.
5. Pulsar **Salir de vista previa / restaurar selección**. Si aún no se había
   entregado una selección real, debe volver a Neutral y quitar el rótulo de vista previa.

### D. Datos reales que ya estén guardados

1. En el mismo Inspector, ir a **Consulta manual del progreso guardado**.
2. Escribir en A una fecha de referencia y en B la que se quiere evaluar,
   ambas como `yyyy-MM-dd`, por ejemplo `2026-10-25` y `2026-10-26`.
   Usar fechas en las que realmente se hayan completado bloques, si existen.
3. Dejar desmarcada **Comparar semanas** para comparar días; marcarla para
   comparar las semanas lunes-domingo que contienen esas fechas.
4. Pulsar **Consultar esos dos períodos guardados**. Debe desaparecer el rótulo
   de vista previa y aparecer A/B. Revisar las cantidades que muestra el Inspector.
   Se leen los registros reales; no se crean bloques de muestra.
5. Con al menos una sesión en cada período: más tareas en B debe dar Improved,
   menos tareas debe dar NeedsImprovement; métricas iguales deben dar Stable.
   Si las tareas son iguales pero varían tiempo/sesiones, se aplica la regla
   existente de ProgressService descrita en el apartado 4.
6. Consultar un período sin sesiones, por ejemplo una fecha en la que no se haya
   usado la aplicación. Aunque antes hubiera Improved, ahora debe mostrar Neutral.
   **No borrar archivos ni datos de la aplicación para provocar este caso.**
7. Introducir una fecha inválida: debe aparecer una explicación en Inspector y
   conservarse la última consulta válida, sin errores de ejecución.
8. Pulsar **Quitar selección (Neutral, sin borrar datos)**. Debe quedar Neutral.
   Consultar después las mismas fechas debe volver a recuperar las métricas.

Si aún no hay dos períodos reales con actividad, ese escenario queda pendiente
de disponer de esos registros. Las cuatro vistas previas permiten revisar la
presentación, pero no sustituyen la integración real del comparador.

### E. Navegación, independencia y datos

1. En **VER MÁS DETALLES**, comprobar que la tarjeta no bloqueó el clic y que se
   abre el panel anterior. La tarjeta solo pertenece a Progreso semanal.
2. Regresar usando los controles existentes. Si se vuelve al panel semanal
   durante la misma visita a Progreso, comprobar que reaplica la selección real.
3. Volver a Inicio, entrar otra vez a Progreso semanal: debe estar Neutral hasta
   que se entregue una nueva comparación.
4. Revisar el saldo de copas antes y después de usar las vistas previas y las
   consultas: debe ser el mismo si no se realizaron otras acciones premiadas.
5. Salir de Play y volver a entrar: no deben quedar datos de muestra, una selección
   anterior ni una mejora almacenada. Los bloques guardados deben seguir disponibles.
6. Como regresión manual del controlador común, entrar a Memoria: comprobar
   observación inicial, acierto, error, pausa y continuidad de rondas; al agotarse
   los cinco minutos, resumen de ocho segundos y regreso a EMPEZAR como antes.

## 8. Registro de revisión y pendientes de equipo

Revisión estática: lectura del código y de las referencias serializadas de la
escena, compatibilidad de los valores del enum y de los nombres de estados del
Animator, suscripciones/desuscripciones y ausencia de llamadas de escritura o
recompensa en el adaptador. El resultado de esa revisión no confirma ejecución.

La inspección serializada encontró cero IDs duplicados y cero referencias locales
sin resolver en los 29 bloques nuevos; los movimientos asignados (Idle, Match y
Encourage) existen en el controlador utilizado. Las referencias del adaptador,
sus tres textos y la navegación están asignadas. La revisión de los constructores
detectó y corrigió la carga innecesaria de tareas descrita en el apartado 4.

Pendientes para el cierre integrado:

1. Steven realiza los recorridos del apartado 7 y comunica cualquier error,
   indicando el paso y lo que apareció en Console/Inspector.
2. El módulo de comparación conecta su selección real a una de las entradas del
   apartado 5. La selección final de sesiones/períodos no se añadió a la interfaz
   ni se eligió automáticamente en este bloque.
3. Valentina entrega POMO-288 y se sustituyen los sprites provisionales.
4. Revisar el cierre de las tres tareas con la evidencia manual; POMO-290 no queda
   verificada por Codex. La HU compartida requiere además sus otras integraciones.

Los otros cambios que ya estaban en la rama antes de esta entrega pertenecen a
trabajos anteriores. Este registro describe únicamente el bloque de desempeño.
