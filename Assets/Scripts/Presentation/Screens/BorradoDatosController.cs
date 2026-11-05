using System;
using Pomo.Business.Pomo;
using Pomo.Data.Persistence;
using Pomo.ServicesAdapters.Audio;
using Pomo.ServicesAdapters.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class BorradoDatosController : MonoBehaviour
    {
        [SerializeField] private Button botonBorrar;
        [SerializeField] private GameObject panelConfirmacion;
        [SerializeField] private Button botonConfirmar;
        [SerializeField] private Button botonCancelar;
        [SerializeField] private TMP_Text mensaje;
        [SerializeField] private CanvasGroup interfazPrincipal;
        [SerializeField] private GameObject primerPaso;
        [SerializeField] private GameObject segundoPaso;
        [SerializeField] private TMP_InputField nombreConfirmacion;
        [SerializeField] private TMP_Text mensajeFinal;
        [SerializeField] private Button botonEliminarDefinitivo;
        [SerializeField] private Button botonCancelarFinal;
        private string explicacion;
        private string advertenciaFinal;
        private string nombreEsperado;
        private bool pasoFinal;
        private bool abierto, procesando, borradoIniciado;
        private bool interaccionAnterior, raycastsAnteriores;

        private void Awake()
        {
            explicacion = mensaje.text;
            advertenciaFinal = mensajeFinal.text;
            botonBorrar.onClick.AddListener(Abrir);
            botonConfirmar.onClick.AddListener(SolicitarConfirmacionFinal);
            botonCancelar.onClick.AddListener(Cancelar);
            botonEliminarDefinitivo.onClick.AddListener(Confirmar);
            botonCancelarFinal.onClick.AddListener(Cancelar);
            nombreConfirmacion.onValueChanged.AddListener(ActualizarNombre);
            botonEliminarDefinitivo.interactable = false;
        }

        public void Abrir()
        {
            if (abierto || !interfazPrincipal.interactable) return;
            abierto = true;
            mensaje.text = explicacion;
            primerPaso.SetActive(true);
            segundoPaso.SetActive(false);
            botonConfirmar.interactable = LocalUserProfile.Name.Length > 0;
            if (!botonConfirmar.interactable) mensaje.text = "Primero completa tu nombre en la bienvenida. Después podrás confirmar el borrado con ese nombre.";
            interaccionAnterior = interfazPrincipal.interactable;
            raycastsAnteriores = interfazPrincipal.blocksRaycasts;
            interfazPrincipal.interactable = false;
            interfazPrincipal.blocksRaycasts = false;
            panelConfirmacion.transform.SetAsLastSibling();
            panelConfirmacion.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(botonCancelar.gameObject);
        }

        public void Cancelar()
        {
            if (!abierto || procesando || borradoIniciado) return;
            abierto = false;
            pasoFinal = false;
            nombreEsperado = null;
            nombreConfirmacion.SetTextWithoutNotify(string.Empty);
            botonEliminarDefinitivo.interactable = false;
            panelConfirmacion.SetActive(false);
            interfazPrincipal.interactable = interaccionAnterior;
            interfazPrincipal.blocksRaycasts = raycastsAnteriores;
            EventSystem.current?.SetSelectedGameObject(botonBorrar.gameObject);
        }

        public void Confirmar()
        {
            if (!abierto || !pasoFinal || procesando || !panelConfirmacion.activeInHierarchy ||
                !LocalUserProfile.MatchesName(nombreConfirmacion.text, nombreEsperado)) return;
            procesando = true;
            botonEliminarDefinitivo.interactable = false;
            nombreConfirmacion.readOnly = true;
            try
            {
                string scenePath = gameObject.scene.path;
#if !UNITY_EDITOR
                if (!Application.CanStreamedLevelBeLoaded(scenePath))
                    throw new InvalidOperationException("La escena actual debe incluirse en la compilación antes de borrar datos.");
#endif
                var reset = new LocalDataResetService(Application.persistentDataPath,
                    new NotificationService().CancelAllForDataReset, LimpiarPreferencias);
                reset.Erase(DetenerSesion);
#if UNITY_EDITOR
                // Santiago can be tested in Play even before being added to Build Settings.
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
#else
                SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
#endif
            }
            catch (Exception exception)
            {
                Debug.LogError("POMO: no se completó el borrado local. " + exception.Message);
                mensajeFinal.text = borradoIniciado
                    ? "No se completó el borrado. Pulsa Borrar mis datos para reintentarlo. No continúes usando tus datos hasta completar el proceso."
                    : "No se pudo iniciar el borrado. Tus datos se conservan. Puedes cancelar o volver a intentarlo.";
                // Once deletion starts, do not resume services with stale in-memory data.
                botonCancelar.interactable = !borradoIniciado;
                botonCancelarFinal.interactable = !borradoIniciado;
                nombreConfirmacion.readOnly = borradoIniciado;
                procesando = false;
                ActualizarNombre(nombreConfirmacion.text);
            }
        }

        public void SolicitarConfirmacionFinal()
        {
            if (!abierto || procesando || pasoFinal || !panelConfirmacion.activeInHierarchy) return;
            nombreEsperado = LocalUserProfile.Name;
            if (nombreEsperado.Length == 0) return;
            pasoFinal = true;
            nombreConfirmacion.SetTextWithoutNotify(string.Empty);
            nombreConfirmacion.readOnly = false;
            mensajeFinal.text = advertenciaFinal;
            botonCancelarFinal.interactable = true;
            botonEliminarDefinitivo.interactable = false;
            segundoPaso.SetActive(true);
            primerPaso.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(nombreConfirmacion.gameObject);
            nombreConfirmacion.ActivateInputField();
        }

        private void ActualizarNombre(string value)
        {
            botonEliminarDefinitivo.interactable = abierto && pasoFinal && !procesando &&
                LocalUserProfile.MatchesName(value, nombreEsperado);
        }

        private static void LimpiarPreferencias()
        {
            foreach (string key in LocalDataResetService.PreferenceKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            new AudioService().SetEnabled(true);
            PomoReactionService.ResetForNewSession();
        }

        private void DetenerSesion()
        {
            borradoIniciado = true;
            botonCancelar.interactable = false;
            var roots = gameObject.scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                // Stop delayed callbacks and saves before destroying the old scene.
                foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (script != null && script != this && script.GetType().Assembly == GetType().Assembly)
                    {
                        script.StopAllCoroutines();
                        script.enabled = false;
                    }
                foreach (PomodoroController pomo in root.GetComponentsInChildren<PomodoroController>(true))
                    pomo.DiscardSessionForDataReset();
                foreach (AudioSource audio in root.GetComponentsInChildren<AudioSource>(true)) audio.Stop();
            }
        }

        private void OnDestroy()
        {
            if (botonBorrar != null) botonBorrar.onClick.RemoveListener(Abrir);
            if (botonConfirmar != null) botonConfirmar.onClick.RemoveListener(SolicitarConfirmacionFinal);
            if (botonCancelar != null) botonCancelar.onClick.RemoveListener(Cancelar);
            if (botonEliminarDefinitivo != null) botonEliminarDefinitivo.onClick.RemoveListener(Confirmar);
            if (botonCancelarFinal != null) botonCancelarFinal.onClick.RemoveListener(Cancelar);
            if (nombreConfirmacion != null) nombreConfirmacion.onValueChanged.RemoveListener(ActualizarNombre);
        }
    }
}
