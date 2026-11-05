#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Pomo.Presentation.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    public static class PrivacyConsentCheck
    {
        [MenuItem("POMO/Privacidad/Comprobar consentimiento")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Ejecuta esta comprobación fuera de Play.");

            const string key = PrivacyConsentController.AcceptedVersionKey;
            bool existed = PlayerPrefs.HasKey(key);
            int previous = PlayerPrefs.GetInt(key, 0);
            var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Santiago.unity");
            try
            {
                var controller = preview.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<PrivacyConsentController>(true)).Single();
                var fields = new SerializedObject(controller);
                T Field<T>(string name) where T : UnityEngine.Object => (T)fields.FindProperty(name).objectReferenceValue;
                var content = Field<CanvasGroup>("welcomeContent");
                var panel = Field<GameObject>("consentPanel");
                var notice = Field<GameObject>("noticePanel");
                var toggle = Field<Toggle>("acceptanceToggle");
                var accept = Field<Button>("acceptButton");
                var terms = Field<Button>("termsButton");
                var close = Field<Button>("closeNoticeButton");
                var enable = typeof(PrivacyConsentController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);

                PlayerPrefs.DeleteKey(key);
                toggle.SetIsOnWithoutNotify(true);
                enable.Invoke(controller, null);
                Check(!toggle.isOn && !accept.interactable, "La casilla empieza vacía y Aceptar está deshabilitado.");
                Check(panel.activeSelf && !content.interactable && !content.blocksRaycasts, "La bienvenida queda bloqueada.");
                controller.Accept();
                Check(!controller.HasAccepted && panel.activeSelf, "Una llamada directa no omite la casilla.");
                terms.onClick.Invoke();
                Check(notice.activeSelf && !content.interactable, "El enlace muestra el texto completo sin desbloquear.");
                close.onClick.Invoke();
                Check(!notice.activeSelf && !controller.HasAccepted, "Volver no registra aceptación.");
                toggle.isOn = true;
                Check(accept.interactable, "Marcar habilita Aceptar.");
                toggle.isOn = false;
                Check(!accept.interactable, "Desmarcar deshabilita Aceptar otra vez.");
                toggle.isOn = true;
                controller.OpenTerms();
                controller.Accept();
                Check(!controller.HasAccepted, "No se acepta desde detrás del texto completo.");
                controller.CloseTerms();
                accept.onClick.Invoke();
                Check(controller.HasAccepted && !panel.activeSelf && content.interactable && content.blocksRaycasts,
                    "Aceptar guarda la versión y desbloquea la bienvenida.");
                enable.Invoke(controller, null);
                Check(!panel.activeSelf && content.interactable, "La aceptación se reconoce al reabrir.");
                panel.SetActive(true); // Simulate the initial active state saved in Santiago.
                enable.Invoke(controller, null);
                Check(controller.HasAccepted && panel.activeSelf && !content.interactable && !accept.interactable,
                    "Una aceptación anterior no oculta el panel activo de la escena.");
                terms.onClick.Invoke();
                close.onClick.Invoke();
                Check(panel.activeSelf && !content.interactable, "Leer y volver no omite la aceptación de esta apertura.");
                PlayerPrefs.SetInt(key, PrivacyConsentController.NoticeVersion - 1);
                enable.Invoke(controller, null);
                Check(panel.activeSelf && !accept.interactable, "Una versión anterior requiere aceptar el aviso actual.");
                toggle.isOn = true;
                accept.onClick.Invoke();
                int acceptedVersion = PlayerPrefs.GetInt(key);
                Transform originalParent = panel.transform.parent;
                controller.gameObject.SetActive(false); // Welcome has finished.
                var settings = preview.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<AjustesPrivacidadController>(true)).Single();
                var settingsFields = new SerializedObject(settings);
                var settingsPanel = (GameObject)settingsFields.FindProperty("panelAjustes").objectReferenceValue;
                var home = (GameObject)settingsFields.FindProperty("panelInicio").objectReferenceValue;
                settings.AbrirAjustes();
                settings.ConsultarAviso();
                Check(panel.activeInHierarchy && notice.activeSelf && !settingsPanel.activeSelf && !controller.gameObject.activeSelf,
                    "Ajustes consulta el aviso existente sin reabrir la bienvenida.");
                close.onClick.Invoke();
                Check(settingsPanel.activeSelf && panel.transform.parent == originalParent && !panel.activeSelf && !controller.gameObject.activeSelf,
                    "Volver regresa a Ajustes y restaura la ubicación del aviso.");
                Check(PlayerPrefs.GetInt(key) == acceptedVersion, "Consultar no vuelve a registrar consentimiento.");
                settings.VolverAInicio();
                Check(home.activeSelf && !settingsPanel.activeSelf, "Ajustes permite regresar a Inicio.");
                Debug.Log("POMO privacidad: comprobaciones de consentimiento correctas. Preferencia original restaurada al terminar.");
            }
            finally
            {
                try { EditorSceneManager.ClosePreviewScene(preview); }
                finally
                {
                    if (existed) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                    PlayerPrefs.Save();
                }
            }
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("POMO privacidad: " + message);
        }
    }
}
#endif
