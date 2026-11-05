using System;
using System.Collections;
using Pomo.ServicesAdapters.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class NotificationPermissionController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup interfazPrincipal;
        [SerializeField] private GameObject panelPermisos;
        [SerializeField] private TMP_Text titulo;
        [SerializeField] private TMP_Text explicacion;
        [SerializeField] private TMP_Text textoPermitir;
        [SerializeField] private Button botonPermitir;
        [SerializeField] private Button botonAhoraNo;

        private readonly NotificationService adapter = new NotificationService();
        private bool enCurso;
        private bool pospuestoEstaSesion;
        private bool? decision;
        private bool interfazBloqueada;
        private bool interaccionAnterior;
        private bool raycastsAnteriores;
        public string MensajeResultado { get; private set; }

        private void Awake()
        {
            botonPermitir.onClick.AddListener(Permitir);
            botonAhoraNo.onClick.AddListener(Posponer);
        }

        public IEnumerator SolicitarNotificaciones(string motivo, Action<bool> resultado, bool desdeAjustes = false)
        {
            while (enCurso) yield return null;
            enCurso = true;
            try
            {
                NotificationAccessState state = adapter.PermissionState;
                if (state == NotificationAccessState.Unsupported)
                {
                    MensajeResultado = "Los permisos de notificaciones se comprueban en un dispositivo Android.";
                    resultado(false);
                    yield break;
                }
                if (state == NotificationAccessState.Denied)
                {
                    MensajeResultado = "Las notificaciones están desactivadas en Android. Puedes habilitarlas en sus ajustes; las demás funciones de POMO siguen disponibles.";
                    resultado(false);
                    yield break;
                }
                if (state == NotificationAccessState.NotRequested)
                {
                    if (pospuestoEstaSesion && !desdeAjustes)
                    {
                        MensajeResultado = "El dato se guardó sin notificación. Puedes activar los avisos desde Ajustes.";
                        resultado(false);
                        yield break;
                    }
                    yield return MostrarExplicacion("¿Permitir notificaciones?", motivo, "Permitir avisos");
                    if (decision != true)
                    {
                        pospuestoEstaSesion = true;
                        MensajeResultado = "Puedes seguir usando POMO sin notificaciones. Actívalas cuando quieras desde Ajustes.";
                        resultado(false);
                        yield break;
                    }
                    adapter.RequestNotificationPermissionFromUser();
                }
                float limite = Time.realtimeSinceStartup + 60f;
                while (adapter.PermissionState == NotificationAccessState.Pending && Time.realtimeSinceStartup < limite)
                    yield return new WaitForSecondsRealtime(.25f);
                bool permitido = adapter.PermissionState == NotificationAccessState.Allowed;
                MensajeResultado = permitido ? "Notificaciones habilitadas." :
                    "No se habilitaron las notificaciones. Tus tareas y progreso se conservan; puedes continuar sin avisos.";
                resultado(permitido);
            }
            finally { OcultarPanel(); enCurso = false; }
        }

        public IEnumerator SolicitarAlarmasExactas(Action<bool> resultado)
        {
            while (enCurso) yield return null;
            enCurso = true;
            try
            {
                if (adapter.ExactSchedulingAllowed) { resultado(true); yield break; }
                yield return MostrarExplicacion("Recordatorios a la hora elegida",
                    "Para avisarte a la hora exacta de tu recordatorio, Android necesita habilitar alarmas y recordatorios para POMO. Si no lo permites, puedes seguir usando tareas, calendario y progreso.",
                    "Abrir ajustes de Android");
                if (decision != true)
                {
                    MensajeResultado = "El aviso no se programó. Las demás funciones siguen disponibles.";
                    resultado(false);
                    yield break;
                }
                adapter.RequestExactSchedulingFromUser();
                bool salioDeLaApp = false;
                float limite = Time.realtimeSinceStartup + 60f;
                do
                {
                    yield return new WaitForSecondsRealtime(.25f);
                    if (!Application.isFocused) salioDeLaApp = true;
                    if (adapter.ExactSchedulingAllowed || (salioDeLaApp && Application.isFocused)) break;
                } while (Time.realtimeSinceStartup < limite);
                bool permitido = adapter.ExactSchedulingAllowed;
                MensajeResultado = permitido ? "Alarmas exactas habilitadas." :
                    "No se habilitaron las alarmas exactas. Puedes volver a confirmar el recordatorio después de activarlas en Android.";
                resultado(permitido);
            }
            finally { OcultarPanel(); enCurso = false; }
        }

        private IEnumerator MostrarExplicacion(string encabezado, string motivo, string accion)
        {
            decision = null;
            titulo.text = encabezado;
            explicacion.text = motivo;
            textoPermitir.text = accion;
            interaccionAnterior = interfazPrincipal.interactable;
            raycastsAnteriores = interfazPrincipal.blocksRaycasts;
            interfazPrincipal.interactable = false;
            interfazPrincipal.blocksRaycasts = false;
            interfazBloqueada = true;
            panelPermisos.transform.SetAsLastSibling();
            panelPermisos.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonAhoraNo.gameObject);
            while (!decision.HasValue) yield return null;
            OcultarPanel();
        }

        private void Permitir() => decision = true;
        private void Posponer() => decision = false;
        private void OcultarPanel()
        {
            if (panelPermisos != null) panelPermisos.SetActive(false);
            if (interfazBloqueada && interfazPrincipal != null)
            {
                interfazPrincipal.interactable = interaccionAnterior;
                interfazPrincipal.blocksRaycasts = raycastsAnteriores;
            }
            interfazBloqueada = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnDestroy()
        {
            OcultarPanel();
            if (botonPermitir != null) botonPermitir.onClick.RemoveListener(Permitir);
            if (botonAhoraNo != null) botonAhoraNo.onClick.RemoveListener(Posponer);
        }
    }
}
