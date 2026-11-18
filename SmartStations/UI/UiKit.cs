using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace SmartStations.UI
{
    /// <summary>Petits utilitaires d'interface (Jötunn GUIManager), repris de Smart Chest.</summary>
    internal static class UiKit
    {
        /// <summary>Place un élément depuis le coin haut-gauche de son parent (x vers la droite, y vers le bas).</summary>
        internal static void Place(GameObject go, float x, float y, float width, float height)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        internal static GameObject AddButton(Transform parent, string label, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize(label),
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                width: width,
                height: height);
            Place(go, x, y, width, height);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            return go;
        }

        internal static void SetButtonText(GameObject button, string text)
        {
            if (button == null) return;
            Text t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        internal static Text AddText(Transform parent, string text, float x, float y, float width, float height, int size,
            Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject go = GUIManager.Instance.CreateText(
                text: text,
                parent: parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: Vector2.zero,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: size,
                color: color,
                outline: true,
                outlineColor: Color.black,
                width: width,
                height: height,
                addContentSizeFitter: false);
            Place(go, x, y, width, height);
            Text t = go.GetComponent<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.alignment = alignment;
            }
            return t;
        }

        /// <summary>Fond sombre cliquable (zone où l'on dépose les objets).</summary>
        internal static GameObject AddDropZone(Transform parent, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("DropZone", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place(go, x, y, width, height);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            Button b = go.GetComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            return go;
        }
    }
}
