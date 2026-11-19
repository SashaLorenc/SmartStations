using System.Collections.Generic;
using SmartStations.Crafting;
using UnityEngine;
using UnityEngine.UI;

namespace SmartStations.UI
{
    /// <summary>
    /// Barre à côté du menu de fabrication : un champ de recherche et un bouton par catégorie présente
    /// à la station (plus « Tout »). Sa position se règle dans la config ([Fabrication] PositionCategories).
    /// </summary>
    internal static class CraftFilterBar
    {
        private const float ButtonWidth = 150f;
        private const float ButtonHeight = 28f;
        private const float Gap = 4f;

        private static GameObject _bar;
        private static InputField _search;
        private static string _shownKey;
        private static bool _suppress;
        private sealed class CategoryButton
        {
            public string Id;
            public string Label;
            public GameObject Go;
        }

        private static readonly List<CategoryButton> _buttons = new List<CategoryButton>();

        internal static void Reset()
        {
            if (_bar != null) Object.Destroy(_bar);
            _bar = null;
            _search = null;
            _shownKey = null;
            _buttons.Clear();
        }

        internal static void ClearSearch()
        {
            if (string.IsNullOrEmpty(CraftFilter.Search)) return;
            CraftFilter.Search = "";
            if (_search == null) return;
            _suppress = true;
            _search.text = "";
            _suppress = false;
        }

        /// <summary>Affiche (ou reconstruit) la barre avec les catégories présentes.</summary>
        internal static void Show(InventoryGui gui, HashSet<string> present)
        {
            if (gui == null || gui.m_crafting == null) return;

            var shown = new List<CraftCategory>();
            foreach (CraftCategory c in RecipeCategories.All()) if (present.Contains(c.Id)) shown.Add(c);

            string key = string.Join("|", shown.ConvertAll(c => c.Id).ToArray());
            if (_bar == null || key != _shownKey) Build(gui, shown, key);
            Highlight();
        }

        private static void Build(InventoryGui gui, List<CraftCategory> shown, string key)
        {
            string keepSearch = CraftFilter.Search;
            Reset();
            _shownKey = key;

            BarPlacement place = Plugin.FilterPlacement.Value;
            bool vertical = place == BarPlacement.Gauche || place == BarPlacement.Droite;
            int n = shown.Count + 2; // recherche + « Tout » + catégories

            _bar = new GameObject("SmartStationsCraftFilter", typeof(RectTransform));
            var rt = (RectTransform)_bar.transform;
            rt.SetParent(gui.m_crafting, false);
            rt.sizeDelta = vertical
                ? new Vector2(ButtonWidth, n * ButtonHeight + (n - 1) * Gap)
                : new Vector2(n * ButtonWidth + (n - 1) * Gap, ButtonHeight);
            Vector2 offset = new Vector2(Plugin.FilterOffsetX.Value, Plugin.FilterOffsetY.Value);
            switch (place)
            {
                case BarPlacement.Dessus:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(0f, Gap) + offset;
                    break;
                case BarPlacement.Dessous:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -Gap) + offset;
                    break;
                case BarPlacement.Gauche:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-Gap, 0f) + offset;
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(Gap, 0f) + offset;
                    break;
            }

            int index = 0;
            _search = UiKit.AddInput(_bar.transform, Localization.instance.Localize("$smartstations_search"),
                X(vertical, index), Y(vertical, index), ButtonWidth, ButtonHeight);
            _suppress = true;
            _search.text = keepSearch ?? "";
            _suppress = false;
            CraftFilter.Search = keepSearch ?? "";
            _search.onValueChanged.AddListener(text =>
            {
                if (_suppress) return;
                CraftFilter.Search = text ?? "";
                Refresh();
            });
            index++;

            AddCategoryButton(CraftFilter.AllId, Localization.instance.Localize("$smartstations_cat_all"), "FFFFFF", vertical, index++);
            foreach (CraftCategory c in shown) AddCategoryButton(c.Id, c.Label, c.Color, vertical, index++);
        }

        private static float X(bool vertical, int i) => vertical ? 0f : i * (ButtonWidth + Gap);
        private static float Y(bool vertical, int i) => vertical ? i * (ButtonHeight + Gap) : 0f;

        private static void AddCategoryButton(string id, string label, string color, bool vertical, int index)
        {
            GameObject button = UiKit.AddButton(_bar.transform, "", X(vertical, index), Y(vertical, index), ButtonWidth, ButtonHeight, () =>
            {
                CraftFilter.Selected = id;
                Refresh();
            });
            _buttons.Add(new CategoryButton { Id = id, Label = "<color=#" + color + ">" + label + "</color>", Go = button });
        }

        private static void Highlight()
        {
            foreach (CategoryButton b in _buttons)
            {
                if (b.Go == null) continue;
                bool on = b.Id == CraftFilter.Selected;
                UiKit.SetButtonText(b.Go, on ? "<b>» " + b.Label + " «</b>" : b.Label);
            }
        }

        /// <summary>Recalcule la liste des recettes du jeu avec le nouveau filtre.</summary>
        private static void Refresh()
        {
            InventoryGui gui = InventoryGui.instance;
            Highlight();
            if (gui == null || !InventoryGui.IsVisible() || Player.m_localPlayer == null) return;
            gui.UpdateCraftingPanel();
        }
    }
}
