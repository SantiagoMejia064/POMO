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
        [SerializeField] private PrivacyConsentController privacidad;
        [SerializeField] private TMP_Text avisoNombre;
        private bool nombreEscritoEnEstaEntrada;

        private void Awake()
        {
            nombre.characterLimit = LocalUserProfile.MaximumNameLength;
            nombre.richText = false;
            nombre.onValueChanged.AddListener(Actualizar);
            PrepararEntrada();
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
                if (!LocalUserProfile.TrySaveName(nombre.text)) { Actualizar(nombre.text); return; }
            }
            catch (Exception exception)
            {
                avisoNombre.text = "No se pudo guardar tu nombre. Vuelve a intentarlo.";
                Debug.LogError("POMO: no se pudo guardar el nombre. " + exception.Message);
                return;
            }
            panelHola.SetActive(true);
            panelBienvenida.SetActive(false);
        }

        private void OnDestroy()
        {
            if (nombre != null) nombre.onValueChanged.RemoveListener(Actualizar);
        }
    }
}
