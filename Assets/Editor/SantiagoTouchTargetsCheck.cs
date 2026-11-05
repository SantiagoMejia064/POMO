#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    public static class SantiagoTouchTargetsCheck
    {
        private static int checks;
        private static T Field<T>(object owner, string name) => (T)owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        private static string Snapshot()
        {
            using var sha = SHA256.Create();
            return string.Join("\n", Directory.GetFiles(Application.persistentDataPath, "*.json").OrderBy(p => p)
                .Select(p => Path.GetFileName(p) + ":" + Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p)))));
        }
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        private static void Hit(Button expected, float x = .5f, float y = .5f)
        {
            var rect = (RectTransform)expected.transform;
            var local = new Vector3(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, x), Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, y));
            var canvas = expected.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local))
            };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            var actual = hits.Count > 0 ? hits[0].gameObject.GetComponentInParent<Button>() : null;
            Check(actual == expected, "El toque de " + expected.name + " llega a " + (actual == null ? "ningún botón" : actual.name) +
                "; punto=" + pointer.position + "; rect=" + rect.rect + "; activo=" + expected.gameObject.activeInHierarchy +
                "; impactos=" + string.Join(",", hits.Select(h => h.gameObject.name)) +
                "; grupos=" + string.Join(",", expected.GetComponentsInParent<CanvasGroup>().Select(g => g.name + ":" + g.blocksRaycasts)));
        }

        [MenuItem("POMO/Interfaz/Comprobar zonas táctiles")]
        public static async void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Inicia Play de Santiago antes de comprobar las zonas táctiles.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "Santiago") throw new InvalidOperationException("Abre Santiago para esta comprobación.");
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            T Component<T>() where T : Component => all.Select(t => t.GetComponent<T>()).Single(c => c != null);
            var canvas = all.Select(t => t.GetComponent<Canvas>()).First(c => c != null && c.gameObject.name == "Canvas");
            var states = all.ToDictionary(t => t.gameObject, t => t.gameObject.activeSelf);
            var daily = Component<DailyProgressTextView>();
            var dateField = typeof(DailyProgressTextView).GetField("selectedDate", BindingFlags.Instance | BindingFlags.NonPublic);
            object originalDate = dateField.GetValue(daily);
            string dataBefore = Snapshot(); checks = 0;
            var report = new List<string>();
            void ShowOnly(GameObject panel)
            {
                foreach (Transform child in canvas.transform) child.gameObject.SetActive(child.gameObject == panel);
                panel.SetActive(true);
            }
            try
            {
                var consent = Component<PrivacyConsentController>();
                var toggle = Field<Toggle>(consent, "acceptanceToggle");
                Check(toggle.graphic is Image, "La marca del consentimiento debe ser un sprite, no una letra.");
                var mark = (Image)toggle.graphic;
                Check(AssetDatabase.GetAssetPath(mark.sprite).EndsWith("Mesa de trabajo 16check.png"), "Se usa el chulito existente del proyecto.");
                Check(!mark.raycastTarget, "El chulito no intercepta los toques.");
                report.Add("Marca de consentimiento: sprite Check reutilizado, sin depender de caracteres de fuente.");

                var blocks = Component<BlockSelectionController>();
                ShowOnly(Field<GameObject>(blocks, "workSection"));
                Component<PomodoroController>().gameObject.SetActive(true);
                Field<GameObject>(blocks, "personalizeBlockPanel").SetActive(true);
                Field<GameObject>(blocks, "workSelectionPanel").SetActive(false);
                Field<GameObject>(blocks, "workBlockLandingPanel").SetActive(false);
                blocks.RefreshConfigurationLists();
                await System.Threading.Tasks.Task.Delay(1500);
                Canvas.ForceUpdateCanvases();
                foreach (string name in new[] { "taskContent", "durationContent" })
                {
                    var content = Field<Transform>(blocks, name);
                    var rows = content.GetComponentsInChildren<Button>().Where(button => button.transform.parent == content).ToArray();
                    Check(rows.Length > 0, "Hay opciones en " + name);
                    for (int i = 0; i < rows.Length; i++)
                    {
                        Hit(rows[i], .4f, .25f); Hit(rows[i], .4f, .75f);
                        if (i > 0) Check(!Bounds((RectTransform)rows[i - 1].transform).Overlaps(Bounds((RectTransform)rows[i].transform)), "Las filas no se superponen.");
                    }
                    foreach (var label in content.GetComponentsInChildren<TMP_Text>())
                        if (label.GetComponent<Button>() == null) Check(!label.raycastTarget, "Las etiquetas no capturan toques: " + label.name);
                }
                report.Add("Listas de tareas y tiempos: raycasts al cuarto superior/inferior de cada fila y filas sin superposición.");

                Field<GameObject>(blocks, "personalizeBlockPanel").SetActive(false);
                Field<GameObject>(blocks, "favoriteBlocksPanel").SetActive(true);
                blocks.RefreshFavoriteBlocks();
                await System.Threading.Tasks.Task.Delay(1500); Canvas.ForceUpdateCanvases();
                var favorites = Field<Transform>(blocks, "favoriteContent");
                var favoriteRows = favorites.GetComponentsInChildren<Button>().Where(button => button.transform.parent == favorites).ToArray();
                for (int i = 0; i < favoriteRows.Length; i++)
                {
                    if (i > 0) Check(!Bounds((RectTransform)favoriteRows[i - 1].transform).Overlaps(Bounds((RectTransform)favoriteRows[i].transform)), "Los favoritos no se superponen.");
                    var viewport = favorites.GetComponentInParent<ScrollRect>().viewport;
                    if (Bounds(viewport).Contains(Bounds((RectTransform)favoriteRows[i].transform).center)) Hit(favoriteRows[i], .4f);
                }
                report.Add("Favoritos: " + favoriteRows.Length + " filas revisadas sin cambiar la selección ni las estrellas.");
                foreach (var icon in favorites.GetComponentsInChildren<Image>().Where(g => g.name == "Estrella de favorito"))
                {
                    Check(icon.sprite != null, "La estrella de favorito utiliza un sprite asignado.");
                    Hit(icon.GetComponentInParent<Button>());
                }

                var progress = Component<ProgressPanelNavigationController>();
                ShowOnly(Field<GameObject>(progress, "progressSection")); progress.OpenDailyProgress();
                await System.Threading.Tasks.Task.Delay(1500);
                Canvas.ForceUpdateCanvases();
                var days = Field<RectTransform[]>(daily, "daySelectors");
                var indicator = Field<RectTransform>(daily, "selectedDayIndicator");
                Check(!indicator.GetComponent<Graphic>().raycastTarget, "El círculo no bloquea los días.");
                Check(indicator.sizeDelta.x >= 120, "El círculo de selección es más grande.");
                for (int i = 0; i < days.Length; i++)
                {
                    var button = days[i].GetComponent<Button>(); Hit(button); Hit(button, .2f); Hit(button, .8f);
                    button.onClick.Invoke(); Canvas.ForceUpdateCanvases();
                    Check(Vector3.Distance(indicator.position, days[i].position) < .01f, "El círculo se centra en el día pulsado.");
                    if (i > 0) Check(!Bounds(days[i - 1]).Overlaps(Bounds(days[i])), "Los días no tienen áreas de toque superpuestas.");
                }
                report.Add("Siete días: centro y ambos lados de cada letra seleccionan el mismo día; círculo centrado y sin bloquear.");

                var home = Field<GameObject>(progress, "homePanel"); ShowOnly(home);
                await System.Threading.Tasks.Task.Delay(1500); Canvas.ForceUpdateCanvases();
                var homeButtons = home.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Botón ") && button.name != "Botón progreso").OrderBy(button => Bounds((RectTransform)button.transform).xMin).ToArray();
                Check(homeButtons.Length == 4, "Hay cuatro acciones principales en Inicio.");
                for (int i = 0; i < homeButtons.Length; i++)
                {
                    Hit(homeButtons[i]);
                    if (i > 0) Check(Bounds((RectTransform)homeButtons[i].transform).xMin > Bounds((RectTransform)homeButtons[i - 1].transform).xMax, "Inicio conserva espacio entre botones.");
                }
                report.Add("Inicio: cuatro botones separados, con clic individual correcto.");
            }
            finally
            {
                dateField.SetValue(daily, originalDate);
                foreach (var state in states) if (state.Key != null) state.Key.SetActive(state.Value);
                if (daily.gameObject.activeInHierarchy) daily.SelectDayOfCurrentWeek(((int)((DateTime)originalDate).DayOfWeek + 6) % 7);
                Canvas.ForceUpdateCanvases();
                Check(dataBefore == Snapshot(), "Los JSON del usuario permanecen idénticos.");
            }
            report.Add("PASS: " + checks + " comprobaciones de zonas táctiles; datos intactos.");
            Directory.CreateDirectory("Logs/Sprint4"); File.WriteAllLines("Logs/Sprint4/touch-targets-play.txt", report);
            Debug.Log(string.Join("\n", report));
        }
    }
}
#endif
