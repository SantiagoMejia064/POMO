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
        [SerializeField] private GameObject panelInicio;
        [SerializeField] private GameObject panelAjustes;
        [SerializeField] private Button botonAbrirAjustes;
        [SerializeField] private Button botonLeerAviso;
        [SerializeField] private Button botonVolver;
        [SerializeField] private PrivacyConsentController avisoPrivacidad;
        [SerializeField] private NotificationPermissionController permisosNotificaciones;
        [SerializeField] private Button botonNotificaciones;
        [SerializeField] private TMP_Text estadoNotificaciones;
        [SerializeField] private Button botonVibracion;
        [SerializeField] private TMP_Text estadoVibracion;

        private void Awake()
        {
            botonAbrirAjustes.onClick.AddListener(AbrirAjustes);
            botonLeerAviso.onClick.AddListener(ConsultarAviso);
            botonVolver.onClick.AddListener(VolverAInicio);
            if (botonNotificaciones != null) botonNotificaciones.onClick.AddListener(ConsultarPermisos);
            if (botonVibracion != null) botonVibracion.onClick.AddListener(CambiarVibracion);
            MostrarEstadoVibracion();
        }

        private void CambiarVibracion()
        {
            VibrationService.Enabled = !VibrationService.Enabled;
            MostrarEstadoVibracion();
        }

        private void MostrarEstadoVibracion()
        {
            if (estadoVibracion != null)
                estadoVibracion.text = VibrationService.Enabled ? "Vibración activada" : "Vibración desactivada";
        }

        public void AbrirAjustes()
        {
            panelAjustes.SetActive(true);
            panelInicio.SetActive(false);
        }

        public void ConsultarAviso()
        {
            // Hide settings underneath the existing modal, including keyboard navigation.
            avisoPrivacidad.OpenTermsForReading(() => panelAjustes.SetActive(true));
            if (avisoPrivacidad.IsBlockingWelcome) panelAjustes.SetActive(false);
        }

        public void VolverAInicio()
        {
            panelInicio.SetActive(true);
            panelAjustes.SetActive(false);
        }

        public void ConsultarPermisos()
        {
            if (permisosNotificaciones != null) StartCoroutine(HabilitarNotificaciones());
        }

        private IEnumerator HabilitarNotificaciones()
        {
            botonNotificaciones.interactable = false;
            bool permitido = false;
            yield return permisosNotificaciones.SolicitarNotificaciones(
                "POMO usa notificaciones para avisarte del vencimiento de tareas y del inicio o fin de bloques. Si no las permites, tareas, calendario, progreso y las demás funciones seguirán disponibles.",
                value => permitido = value, true);
            if (permitido)
            {
                var service = new TaskService();
                foreach (TaskModel task in new List<TaskModel>(service.GetStoredTasks()))
                    if (task != null && task.reminderNotificationId <= 0) service.TryScheduleReminderForTask(task.id);
            }
            if (estadoNotificaciones != null) estadoNotificaciones.text = permisosNotificaciones.MensajeResultado;
            botonNotificaciones.interactable = true;
        }

        private void OnDestroy()
        {
            if (botonAbrirAjustes != null) botonAbrirAjustes.onClick.RemoveListener(AbrirAjustes);
            if (botonLeerAviso != null) botonLeerAviso.onClick.RemoveListener(ConsultarAviso);
            if (botonVolver != null) botonVolver.onClick.RemoveListener(VolverAInicio);
            if (botonNotificaciones != null) botonNotificaciones.onClick.RemoveListener(ConsultarPermisos);
            if (botonVibracion != null) botonVibracion.onClick.RemoveListener(CambiarVibracion);
        }
    }
}
