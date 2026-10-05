#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace WhiteBoxKit
{
    /// Small procedural-UI helpers shared by GameplayBuilder and SceneFlowBuilder. Everything
    /// here is built at editor time so the resulting Canvas is a normal, saved part of the
    /// scene — nothing is created at runtime.
    public static class UIBuilder
    {
        public static Canvas CreateScreenCanvas(string name)
        {
            EnsureEventSystem();

            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            // The project's Active Input Handling is "Input System Package (New)" only, so the
            // legacy StandaloneInputModule (which reads the old Input Manager) would not work.
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform AddRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform FullRect(Transform parent, string name)
        {
            RectTransform rt = AddRect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image AddPanel(Transform parent, string name, Color color)
        {
            RectTransform rt = FullRect(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI AddText(Transform parent, string name, string text, float fontSize,
            TextAlignmentOptions align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            RectTransform rt = AddRect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        public static (GameObject go, Button button, TextMeshProUGUI label) AddButton(Transform parent, string name,
            string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
            Color bg, Color fg, float fontSize = 24f)
        {
            RectTransform rt = AddRect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            Button button = rt.gameObject.AddComponent<Button>();

            TextMeshProUGUI label = AddText(rt, "Label", text, fontSize, TextAlignmentOptions.Center, fg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return (rt.gameObject, button, label);
        }

        public static void BindClick(Button button, Object target, string methodName)
        {
            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, methodName);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        }
    }
}
#endif
