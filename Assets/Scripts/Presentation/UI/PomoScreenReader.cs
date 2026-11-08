using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed class PomoScreenReader : MonoBehaviour
    {
        [Serializable]
        private struct ControlLabel
        {
            public Selectable control;
            public string label;
        }

        [SerializeField] private ControlLabel[] labels = Array.Empty<ControlLabel>();
        private readonly Dictionary<Selectable, string> explicitLabels = new Dictionary<Selectable, string>();
        private readonly List<Selectable> visible = new List<Selectable>();
        private readonly List<Selectable> previous = new List<Selectable>();
        private readonly List<Selectable> candidates = new List<Selectable>();
        private readonly List<AccessibilityNode> nodes = new List<AccessibilityNode>();
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private readonly Vector3[] corners = new Vector3[4];
        private static readonly Regex RichText = new Regex("<[^>]+>");
        private AccessibilityHierarchy hierarchy;
        private PointerEventData pointer;
        private Canvas canvas;
        private float nextRefresh;
        private bool supported;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            foreach (ControlLabel entry in labels)
                if (entry.control != null) explicitLabels[entry.control] = entry.label;
        }

        private void OnEnable()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            try
            {
                AssistiveSupport.screenReaderStatusChanged += OnScreenReaderChanged;
                supported = true;
                OnScreenReaderChanged(AssistiveSupport.isScreenReaderEnabled);
            }
            catch (PlatformNotSupportedException)
            {
                AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderChanged;
                supported = false;
            }
#endif
        }

        private void OnDisable()
        {
            if (!supported) return;
            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderChanged;
            OnScreenReaderChanged(false);
            supported = false;
        }

        private void OnScreenReaderChanged(bool enabled)
        {
            if (enabled)
            {
                hierarchy = new AccessibilityHierarchy();
                nextRefresh = 0f;
            }
            else
            {
                if (hierarchy != null && AssistiveSupport.activeHierarchy == hierarchy)
                    AssistiveSupport.activeHierarchy = null;
                hierarchy = null;
                previous.Clear();
                nodes.Clear();
            }
        }

        private void LateUpdate()
        {
            if (hierarchy == null || Time.unscaledTime < nextRefresh || EventSystem.current == null) return;
            nextRefresh = Time.unscaledTime + .2f;
            if (pointer == null) pointer = new PointerEventData(EventSystem.current);
            RefreshNodes();
        }

        private void RefreshNodes()
        {
            candidates.Clear();
            GetComponentsInChildren(false, candidates);
            visible.Clear();
            foreach (Selectable control in candidates)
            {
                if ((control is Button || control is Toggle || control is TMP_InputField || control is InputField) &&
                    CanUse(control) && IsExposed(control))
                    visible.Add(control);
            }

            bool rebuild = previous.Count != visible.Count;
            for (int i = 0; !rebuild && i < visible.Count; i++)
                rebuild = previous[i] != visible[i];

            if (rebuild)
            {
                hierarchy.Clear();
                nodes.Clear();
                previous.Clear();
                foreach (Selectable control in visible)
                {
                    var node = hierarchy.AddNode();
                    node.role = control is Toggle ? AccessibilityRole.Toggle :
                        control is Button ? AccessibilityRole.Button : AccessibilityRole.TextField;
                    node.frameGetter = () => control != null ? FrameFor(control) : Rect.zero;
                    node.invoked += () => Activate(control);
                    ScrollRect scroll = control.GetComponentInParent<ScrollRect>();
                    if (scroll != null) node.scrolled += direction => ScrollPage(scroll, direction);
                    nodes.Add(node);
                    previous.Add(control);
                }
            }

            for (int i = 0; i < visible.Count; i++)
            {
                var control = visible[i];
                var node = nodes[i];
                string label = LabelFor(control);
                if (node.label != label) node.label = label;
                string value = control is Toggle toggle ? (toggle.isOn ? "Marcado" : "Sin marcar") :
                    control is TMP_InputField tmp ? tmp.text :
                    control is InputField input ? input.text : string.Empty;
                if (node.value != value) node.value = value;
            }

            if (AssistiveSupport.activeHierarchy != hierarchy)
                AssistiveSupport.activeHierarchy = hierarchy;
            else if (rebuild)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(null);
        }

        public static void Announce(string message)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (AssistiveSupport.activeHierarchy != null)
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);
#endif
        }

        private static bool CanUse(Selectable control) =>
            control != null && control.isActiveAndEnabled && control.IsInteractable();

        private bool IsExposed(Selectable control)
        {
            if (EventSystem.current == null || pointer == null) return false;
            Rect frame = ScreenRect((RectTransform)control.transform);
            if (frame.width <= 0 || frame.height <= 0) return false;
            Vector2 point = frame.center;
            if (point.x < 0 || point.x >= Screen.width || point.y < 0 || point.y >= Screen.height) return false;
            pointer.position = point;
            hits.Clear();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(control.transform);
        }

        private Rect FrameFor(Selectable control)
        {
            Rect frame = ScreenRect((RectTransform)control.transform);
            frame.y = Screen.height - frame.yMax;
            return frame;
        }

        private Rect ScreenRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private string LabelFor(Selectable control)
        {
            if (explicitLabels.TryGetValue(control, out string label) && !string.IsNullOrWhiteSpace(label))
                return label;

            if (control is TMP_InputField tmp)
                return PlaceholderLabel(tmp.placeholder, control.name);
            if (control is InputField input)
                return PlaceholderLabel(input.placeholder, control.name);

            var modern = control.GetComponentInChildren<TMP_Text>();
            if (modern != null && !string.IsNullOrWhiteSpace(modern.text))
                return RichText.Replace(modern.text, string.Empty).Trim();
            var legacy = control.GetComponentInChildren<Text>();
            if (legacy != null && !string.IsNullOrWhiteSpace(legacy.text))
                return RichText.Replace(legacy.text, string.Empty).Trim();

            return control.name.Replace('_', ' ').Trim();
        }

        private static string PlaceholderLabel(Graphic placeholder, string fallback)
        {
            string label = placeholder is TMP_Text tmp ? tmp.text : placeholder is Text text ? text.text : null;
            return string.IsNullOrWhiteSpace(label) ? fallback : RichText.Replace(label, string.Empty).Trim();
        }

        private bool Activate(Selectable control)
        {
            if (!CanUse(control) || !IsExposed(control)) return false;
            nextRefresh = 0f;
            if (control is Button button) button.onClick.Invoke();
            else if (control is Toggle toggle) toggle.isOn = !toggle.isOn;
            else if (control is TMP_InputField tmp) { tmp.Select(); tmp.ActivateInputField(); }
            else if (control is InputField input) { input.Select(); input.ActivateInputField(); }
            return true;
        }

        private bool ScrollPage(ScrollRect scroll, AccessibilityScrollDirection direction)
        {
            if (!scroll.isActiveAndEnabled || scroll.content == null) return false;
            Rect viewport = (scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform).rect;
            Rect content = scroll.content.rect;
            bool forward = direction == AccessibilityScrollDirection.Forward;
            bool backward = direction == AccessibilityScrollDirection.Backward;
            float before;
            float after;
            if (scroll.vertical && content.height > viewport.height &&
                (forward || backward || direction == AccessibilityScrollDirection.Down || direction == AccessibilityScrollDirection.Up))
            {
                before = scroll.verticalNormalizedPosition;
                float delta = viewport.height * .85f / (content.height - viewport.height);
                after = Mathf.Clamp01(before + (backward || direction == AccessibilityScrollDirection.Up ? delta : -delta));
                scroll.verticalNormalizedPosition = after;
            }
            else if (scroll.horizontal && content.width > viewport.width &&
                (forward || backward || direction == AccessibilityScrollDirection.Left || direction == AccessibilityScrollDirection.Right))
            {
                before = scroll.horizontalNormalizedPosition;
                float delta = viewport.width * .85f / (content.width - viewport.width);
                after = Mathf.Clamp01(before + (backward || direction == AccessibilityScrollDirection.Left ? -delta : delta));
                scroll.horizontalNormalizedPosition = after;
            }
            else return false;
            if (Mathf.Approximately(before, after)) return false;
            nextRefresh = 0f;
            return true;
        }
    }
}
