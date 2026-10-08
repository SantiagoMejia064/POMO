using UnityEngine;
using TMPro;

public class Cronometro : MonoBehaviour
{
    [Header("Textos del cronómetro")]
    public TMP_Text textoHoras;
    public TMP_Text textoMinutos;
    public TMP_Text textoSegundos;
    public GameObject feedbackPausa;

    [Header("Panel de advertencia")]
    public GameObject panelAdvertencia;

    private float tiempoTranscurrido = 0f;
    private bool cronometroActivo = false;
    private bool cronometroIniciado = false;


    void Start()
    {
        ActualizarCronometro();

        // Ocultar el panel al iniciar
        panelAdvertencia.SetActive(false);
    }

    void Update()
    {
        if (cronometroActivo)
        {
            tiempoTranscurrido += Time.deltaTime;
            ActualizarCronometro();
        }
    }

    // BOTÓN INICIAR
    public void IniciarCronometro()
    {
        tiempoTranscurrido = 0f;

        cronometroActivo = true;
        cronometroIniciado = true;

        panelAdvertencia.SetActive(false);

        ActualizarCronometro();
    }

    // BOTÓN PAUSAR
    public void PausarCronometro()
    {
        if (!cronometroIniciado)
        {
            MostrarAdvertencia();
            return;
        }

        cronometroActivo = false;
        feedbackPausa.SetActive(true);
    }

    // BOTÓN REANUDAR
    public void ReanudarCronometro()
    {
        if (!cronometroIniciado)
        {
            MostrarAdvertencia();
            return;
        }

        if (!cronometroActivo)
        {
            cronometroActivo = true;
        }
    }

    // MOSTRAR PANEL DE ADVERTENCIA
    public void MostrarAdvertencia()
    {
        panelAdvertencia.SetActive(true);
    }

    // CERRAR PANEL DE ADVERTENCIA
    public void CerrarAdvertencia()
    {
        panelAdvertencia.SetActive(false);
    }

    private void ActualizarCronometro()
    {
        int tiempoTotal = Mathf.FloorToInt(tiempoTranscurrido);

        int horas = tiempoTotal / 3600;
        int minutos = (tiempoTotal % 3600) / 60;
        int segundos = tiempoTotal % 60;

        textoHoras.text = horas.ToString("00");
        textoMinutos.text = minutos.ToString("00");
        textoSegundos.text = segundos.ToString("00");
    }
}