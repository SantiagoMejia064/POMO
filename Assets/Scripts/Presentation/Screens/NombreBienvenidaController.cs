using System;
using Pomo.Data.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class NombreBienvenidaController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nombre;
        [SerializeField] private Button botonContinuar;
        [SerializeField] private GameObject panelBienvenida;
        [SerializeField] private GameObject panelHola;
        [SerializeField] private GameObject panelInicio;
        [SerializeField] private GameObject panelHabitos;
        [SerializeField] private GameObject panelExcelente;
        [SerializeField] private PrivacyConsentController privacidad;
        [SerializeField] private TMP_Text avisoNombre;
        [SerializeField] private TMP_Text saludoPersonalizado;
        private bool nombreEscritoEnEstaEntrada;

        private void Awake()
        {
            nombre.characterLimit = LocalUserProfile.MaximumNameLength;
            nombre.richText = false;
            if (saludoPersonalizado != null) saludoPersonalizado.richText = false;
            nombre.onValueChanged.AddListener(Actualizar);
            PrepararEntrada();
        }

        private void Start() => RestaurarEntrada();

        public void RestaurarEntrada()
        {
            if (privacidad != null && privacidad.HasAccepted && LocalUserProfile.Name.Length > 0)
            {
                if (LocalUserProfile.HasCompletedOnboarding && panelInicio != null)
                {
                    AbrirInicio();
                    return;
                }
                if (panelExcelente != null)
                {
                    AbrirSaludo();
                    return;
                }
            }
            panelBienvenida.SetActive(true);
            PrepararEntrada();
        }

        private void AbrirInicio()
        {
            panelInicio.SetActive(true);
            if (panelExcelente != null) panelExcelente.SetActive(false);
            if (panelHabitos != null) panelHabitos.SetActive(false);
            if (panelHola != null) panelHola.SetActive(false);
            panelBienvenida.SetActive(false);
        }

        public void FinalizarOnboarding()
        {
            if (panelInicio == null || panelExcelente == null || !panelExcelente.activeInHierarchy ||
                privacidad == null || !privacidad.HasAccepted || LocalUserProfile.Name.Length == 0) return;
            LocalUserProfile.CompleteOnboarding();
            AbrirInicio();
        }

        public void PrepararEntrada()
        {
            nombreEscritoEnEstaEntrada = false;
            nombre.SetTextWithoutNotify(string.Empty);
            botonContinuar.interactable = false;
            avisoNombre.text = "Escribe tu nombre para continuar.";
        }

        private void Actualizar(string value)
        {
            nombreEscritoEnEstaEntrada = LocalUserProfile.NormalizeName(value).Length > 0;
            botonContinuar.interactable = nombreEscritoEnEstaEntrada;
            avisoNombre.text = botonContinuar.interactable ? string.Empty : "Escribe tu nombre para continuar.";
        }

        public void Continuar()
        {
            if (!panelBienvenida.activeInHierarchy || privacidad.IsBlockingWelcome || !nombreEscritoEnEstaEntrada) return;
            try
            {
                bool guardado = panelExcelente != null
                    ? LocalUserProfile.TryStartOnboarding(nombre.text)
                    : LocalUserProfile.TrySaveName(nombre.text);
                if (!guardado) { Actualizar(nombre.text); return; }
            }
            catch (Exception exception)
            {
                avisoNombre.text = "No se pudo guardar tu nombre. Vuelve a intentarlo.";
                Debug.LogError("POMO: no se pudo guardar el nombre. " + exception.Message);
                return;
            }
            AbrirSaludo();
        }

        private void AbrirSaludo()
        {
            if (saludoPersonalizado != null) saludoPersonalizado.text = "Querid@ " + LocalUserProfile.Name;
            panelHola.SetActive(true);
            panelBienvenida.SetActive(false);
        }

        private void OnDestroy()
        {
            if (nombre != null) nombre.onValueChanged.RemoveListener(Actualizar);
        }
    }
}
