using System.Collections;
using UnityEngine;

public class PomoNotifications : MonoBehaviour
{
    [Header("Panel de notificación")]
    [SerializeField] private RectTransform panelNotificacion;

    [Header("Animación")]
    [SerializeField] private float duracionAnimacion = 2f;
    [SerializeField] private float tiempoVisible = 5f;
    [SerializeField] private float distanciaExtra = 30f;

    private Vector2 posicionVisible;
    private Vector2 posicionOculta;
    private Coroutine rutinaActual;

    private void Awake()
    {
        if (panelNotificacion == null)
        {
            Debug.LogError("POMO: No asignaste el panel de notificación.");
            enabled = false;
            return;
        }

        posicionVisible = panelNotificacion.anchoredPosition;

        // Ocultar el panel completamente por encima de la pantalla.
        posicionOculta = posicionVisible +
            Vector2.up * (panelNotificacion.rect.height + distanciaExtra);

        panelNotificacion.anchoredPosition = posicionOculta;
    }

    public void MostrarRecordatorio()
    {
        if (panelNotificacion == null)
            return;

        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(AnimacionNotificacion());
    }

    private IEnumerator AnimacionNotificacion()
    {
        panelNotificacion.anchoredPosition = posicionOculta;

        // Bajar el panel
        yield return Animar(posicionOculta, posicionVisible);

        // Mantenerlo visible
        yield return new WaitForSecondsRealtime(tiempoVisible);

        // Subir el panel
        yield return Animar(posicionVisible, posicionOculta);

        rutinaActual = null;
    }

    private IEnumerator Animar(Vector2 inicio, Vector2 fin)
    {
        float tiempo = 0f;

        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / Mathf.Max(duracionAnimacion, 0.01f)
            );

            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            panelNotificacion.anchoredPosition =
                Vector2.Lerp(inicio, fin, suavizado);

            yield return null;
        }

        panelNotificacion.anchoredPosition = fin;
    }
}