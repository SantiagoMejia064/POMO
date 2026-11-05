# Integración en Santiago

## Resultado

Escena destino: `Assets/Scenes/Santiago.unity`, conservando su `.meta` y GUID.
Base: versión guardada actual de `Steven 1`; aportes: versión guardada actual
de `Chantre 3`, incluidos sus cambios locales anteriores a esta integración.
Las escenas fuente no fueron modificadas.

Se conservaron los 4.398 objetos y componentes serializados de Steven y se
añadieron los 492 exclusivos de Chantre: total 4.890. No se duplicaron el Canvas
principal, EventSystem ni los controladores compartidos.

- Bienvenid@ → Hola → Habitos → Excelente → Panel INICIO.
- Se conservaron los 13 toggles y el Scroll View de hábitos.
- Se añadieron también el VideoPlayer, su presentación en Inicio, el panel
  Finalizar de bloques y los elementos adicionales de Progreso de Chantre.
- Se conservaron los objetos exclusivos de Steven, sus scripts, eventos,
  tarjetas y distribución de los módulos existentes.
- Las listas de hijos se unieron, sin sustituir las listas completas.
- Se conectaron las dos referencias de finalización que Steven tenía vacías
  a los objetos correspondientes que ya existían en Chantre.
- Se ocultaron inicialmente Inicio, Trabajo, Progreso, Minijuegos y la
  notificación para que la bienvenida no se superponga con otros módulos.
  Se conservan los accesos originales para abrir esos módulos desde Inicio.

No se modificó código de ejecución ni se implementaron funcionalidades nuevas
de privacidad, persistencia del nombre, permisos o preferencias. La entrada del
nombre y los textos de bienvenida se copiaron con la lógica que efectivamente
tenían en Chantre: los botones originales cambian de panel; no se encontró allí
un controlador que persistiera el nombre o personalizara los saludos.

## Comprobaciones ejecutadas

- Cero identificadores locales duplicados o referencias locales sin resolver.
- Relaciones padre/hijo de los Transform comprobadas en ambas direcciones.
- Todos los identificadores de las dos fuentes presentes en Santiago.
- Los 492 objetos adicionales son equivalentes al contenido de Chantre.
- Los GUID de recursos externos de la escena resuelven a recursos existentes.
- Simulación de las cuatro transiciones originales de bienvenida: correcta.
- Hashes SHA-256 de ambas escenas fuente sin cambios después de integrar.
- El verificador auxiliar de Editor compila con Roslyn y las referencias de
  Unity instaladas; salida local: `Logs/SceneIntegration/IntegrationCheck.dll`.

Para repetir la comprobación estructural sin escribir:

```powershell
node Tools/IntegrateSantiagoScene.cjs --check
```

`--write` reconstruye Santiago desde las fuentes; no usarlo después de comenzar
a editar la escena integrada, porque reemplazaría esas ediciones posteriores.

El respaldo de Santiago anterior y el informe de unión están en
`Logs/SceneIntegration/` (carpeta ignorada por Git).

## Validación pendiente en Unity

La prueba en Play no se ejecutó. Los intentos de iniciar una segunda instancia
de Unity terminaron antes de cargar el proyecto; ya había una sesión del
proyecto abierta. El acceso para controlar esa ventana no se completó.
No se cerró esa sesión ni se descartaron sus cambios pendientes.

Si Unity conserva una versión antigua de Santiago en memoria, no guardarla
sobre la escena integrada. Si contiene cambios propios sin guardar, conservar
primero una copia con otro nombre y después abrir Santiago desde disco.

Se dejó una comprobación explícita en el menú:
`POMO > Integración > Verificar escena Santiago`.
Requiere no tener cambios sin guardar. Recorre la bienvenida, alterna una
casilla de hábitos y abre los módulos existentes; no crea tareas, no compra
artículos ni inicia temporizadores. Comprueba que los JSON existentes no
cambien y escribe resultados en `Logs/SceneIntegration/unity-check.txt`.
No guarda la escena durante la prueba.

La comprobación automática no reemplaza revisar aspecto visual, pulsaciones,
audio, temporizadores y notificaciones Android. Los defectos heredados de los
módulos no se corrigieron como parte de esta unión.

La lista de compilación sigue apuntando a Chantre 3. No se cambió porque esta
entrega integra la escena Santiago; para una APK de la integración habrá que
seleccionar Santiago en el perfil de compilación.
