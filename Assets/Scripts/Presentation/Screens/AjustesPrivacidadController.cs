
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.ServicesAdapters.Vibration;
using System.Collections;
using System.Collections.Generic;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class AjustesPrivacidadController : MonoBehaviour
    {
        // =====================================================
        // PANELES
        // =====================================================

        [Header("Paneles")]
        [SerializeField] private GameObject panelInicio;
        [SerializeField] private GameObject panelAjustes;

        // =====================================================
        // PRIVACIDAD
        // =====================================================

        [Header("Privacidad")]
        [SerializeField] private Button botonAbrirAjustes;
        [SerializeField] private Button botonLeerAviso;
        [SerializeField] private Button botonVolver;
        [SerializeField] private PrivacyConsentController avisoPrivacidad;

        // =====================================================
        // NOTIFICACIONES
        // =====================================================

        [Header("Notificaciones")]
        [SerializeField] private NotificationPermissionController permisosNotificaciones;
        [SerializeField] private Button botonNotificaciones;
        [SerializeField] private TMP_Text estadoNotificaciones;

        // =====================================================
        // VIBRACIÓN
        // =====================================================

        [Header("Vibración")]
        [SerializeField] private Button botonVibracion;
        [SerializeField] private TMP_Text estadoVibracion;

        // =====================================================
        // SONIDOS
        // =====================================================

        [Header("Sonidos")]
        [SerializeField] private Button botonSonido;
        [SerializeField] private TMP_Text estadoSonido;

        // =====================================================
        // CLAVES DE PREFERENCIAS
        // =====================================================

        private const string KEY_VIBRATION = "POMO_VIBRATION_ENABLED";
        private const string KEY_SOUND = "POMO_SOUND_ENABLED";

        private bool sonidosActivados = true;

        // =====================================================
        // INICIALIZACIÓN
        // =====================================================

        private void Awake()
        {
            // Cargar preferencias guardadas.
            CargarPreferencias();

            // Botones de navegación.
            if (botonAbrirAjustes != null)
                botonAbrirAjustes.onClick.AddListener(AbrirAjustes);

            if (botonLeerAviso != null)
                botonLeerAviso.onClick.AddListener(ConsultarAviso);

            if (botonVolver != null)
                botonVolver.onClick.AddListener(VolverAInicio);

            // Botón de notificaciones.
            if (botonNotificaciones != null)
                botonNotificaciones.onClick.AddListener(ConsultarPermisos);

            // Botón de vibración.
            if (botonVibracion != null)
                botonVibracion.onClick.AddListener(CambiarVibracion);

            // Botón de sonidos.
            if (botonSonido != null)
                botonSonido.onClick.AddListener(CambiarSonido);

            // Actualizar textos.
            MostrarEstadoVibracion();
            MostrarEstadoSonido();
        }

        // =====================================================
        // CARGAR PREFERENCIAS
        // =====================================================

        private void CargarPreferencias()
        {
            // ---------------------------------------------
            // VIBRACIÓN
            // ---------------------------------------------

            if (PlayerPrefs.HasKey(KEY_VIBRATION))
            {
                VibrationService.Enabled =
                    PlayerPrefs.GetInt(KEY_VIBRATION) == 1;
            }
            else
            {
                // Guardar el valor inicial del servicio.
                GuardarVibracion();
            }

            // ---------------------------------------------
            // SONIDOS
            // ---------------------------------------------

            if (PlayerPrefs.HasKey(KEY_SOUND))
            {
                sonidosActivados =
                    PlayerPrefs.GetInt(KEY_SOUND) == 1;
            }
            else
            {
                sonidosActivados = true;
                GuardarSonido();
            }

            AplicarPreferenciaSonido();
        }

        // =====================================================
        // VIBRACIÓN
        // =====================================================

        private void CambiarVibracion()
        {
            VibrationService.Enabled =
                !VibrationService.Enabled;

            GuardarVibracion();

            MostrarEstadoVibracion();
        }

        private void GuardarVibracion()
        {
            PlayerPrefs.SetInt(
                KEY_VIBRATION,
                VibrationService.Enabled ? 1 : 0
            );

            PlayerPrefs.Save();
        }

        private void MostrarEstadoVibracion()
        {
            if (estadoVibracion != null)
            {
                estadoVibracion.text =
                    VibrationService.Enabled
                    ? "Vibración activada"
                    : "Vibración desactivada";
            }
        }

        // =====================================================
        // SONIDOS
        // =====================================================

        private void CambiarSonido()
        {
            sonidosActivados = !sonidosActivados;

            GuardarSonido();

            AplicarPreferenciaSonido();

            MostrarEstadoSonido();
        }

        private void GuardarSonido()
        {
            PlayerPrefs.SetInt(
                KEY_SOUND,
                sonidosActivados ? 1 : 0
            );

            PlayerPrefs.Save();
        }

        private void AplicarPreferenciaSonido()
        {
            // Control global de todos los sonidos de Unity.
            AudioListener.volume = sonidosActivados ? 1f : 0f;
        }

        private void MostrarEstadoSonido()
        {
            if (estadoSonido != null)
            {
                estadoSonido.text =
                    sonidosActivados
                    ? "Sonidos activados"
                    : "Sonidos desactivados";
            }
        }

        // =====================================================
        // NAVEGACIÓN
        // =====================================================

        public void AbrirAjustes()
        {
            if (panelAjustes != null)
                panelAjustes.SetActive(true);

            if (panelInicio != null)
                panelInicio.SetActive(false);
        }

        public void ConsultarAviso()
        {
            if (avisoPrivacidad == null)
                return;

            avisoPrivacidad.OpenTermsForReading(
                () =>
                {
                    if (panelAjustes != null)
                        panelAjustes.SetActive(true);
                }
            );

            if (avisoPrivacidad.IsBlockingWelcome &&
                panelAjustes != null)
            {
                panelAjustes.SetActive(false);
            }
        }

        public void VolverAInicio()
        {
            if (panelInicio != null)
                panelInicio.SetActive(true);

            if (panelAjustes != null)
                panelAjustes.SetActive(false);
        }

        // =====================================================
        // NOTIFICACIONES
        // =====================================================

        public void ConsultarPermisos()
        {
            if (permisosNotificaciones != null)
            {
                StartCoroutine(HabilitarNotificaciones());
            }
        }

        private IEnumerator HabilitarNotificaciones()
        {
            if (botonNotificaciones != null)
                botonNotificaciones.interactable = false;

            bool permitido = false;

            yield return permisosNotificaciones.SolicitarNotificaciones(
                "POMO usa notificaciones para avisarte del vencimiento de tareas y del inicio o fin de bloques. Si no las permites, tareas, calendario, progreso y las demás funciones seguirán disponibles.",
                value => permitido = value,
                true
            );

            if (permitido)
            {
                var service = new TaskService();

                foreach (TaskModel task in
                    new List<TaskModel>(service.GetStoredTasks()))
                {
                    if (task != null &&
                        task.reminderNotificationId <= 0)
                    {
                        service.TryScheduleReminderForTask(task.id);
                    }
                }
            }

            if (estadoNotificaciones != null)
            {
                estadoNotificaciones.text =
                    permisosNotificaciones.MensajeResultado;
            }

            if (botonNotificaciones != null)
                botonNotificaciones.interactable = true;
        }

        // =====================================================
        // CICLO DE VIDA
        // =====================================================

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PlayerPrefs.Save();
            }
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (botonAbrirAjustes != null)
                botonAbrirAjustes.onClick.RemoveListener(AbrirAjustes);

            if (botonLeerAviso != null)
                botonLeerAviso.onClick.RemoveListener(ConsultarAviso);

            if (botonVolver != null)
                botonVolver.onClick.RemoveListener(VolverAInicio);

            if (botonNotificaciones != null)
                botonNotificaciones.onClick.RemoveListener(ConsultarPermisos);

            if (botonVibracion != null)
                botonVibracion.onClick.RemoveListener(CambiarVibracion);

            if (botonSonido != null)
                botonSonido.onClick.RemoveListener(CambiarSonido);
        }
    }
}
