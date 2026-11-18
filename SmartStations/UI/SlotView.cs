using System;
using HarmonyLib;
using SmartStations.Stations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartStations.UI
{
    /// <summary>
    /// Une case de la fenêtre, construite à partir de la case d'inventaire du jeu (même apparence).
    /// Clic gauche / droit : actions données par la fenêtre.
    /// </summary>
    internal sealed class SlotView
    {
        private static readonly string[] HiddenChildren =
            { "binding", "quality", "equiped", "queued", "noteleport", "foodicon", "selected", "durability" };

        public readonly SlotKind Kind;
        public ItemStack Stack { get; private set; }

        private readonly GameObject _go;
        private readonly Image _icon;
        private readonly TMP_Text _amount;
        private readonly UITooltip _tooltip;

        private SlotView(SlotKind kind, GameObject go)
        {
            Kind = kind;
            _go = go;
            Transform icon = go.transform.Find("icon");
            Transform amount = go.transform.Find("amount");
            _icon = icon != null ? icon.GetComponent<Image>() : null;
            _amount = amount != null ? amount.GetComponent<TMP_Text>() : null;
            _tooltip = go.GetComponent<UITooltip>();

            foreach (string child in HiddenChildren)
            {
                Transform t = go.transform.Find(child);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        internal static SlotView Create(SlotKind kind, Transform parent, float x, float y, float size,
            Action<SlotView> onLeft, Action<SlotView> onRight)
        {
            GameObject prefab = InventoryGui.instance.m_playerGrid.m_elementPrefab;
            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = "SmartStationsSlot";
            UiKit.Place(go, x, y, size, size);

            var slot = new SlotView(kind, go);
            UIInputHandler handler = go.GetComponentInChildren<UIInputHandler>();
            if (handler != null)
            {
                handler.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(handler.m_onLeftDown, new Action<UIInputHandler>(_ => onLeft(slot)));
                handler.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(handler.m_onRightDown, new Action<UIInputHandler>(_ => onRight(slot)));
            }
            slot.Set(null);
            return slot;
        }

        internal void SetActive(bool on) => _go.SetActive(on);

        /// <summary>Affiche un objet et sa quantité, ou une case vide (stack null).</summary>
        internal void Set(ItemStack stack)
        {
            Stack = stack != null && stack.Count > 0 ? stack : null;
            if (Stack == null)
            {
                ShowIcon(null, 1f);
                SetTooltip("", "");
                return;
            }
            ShowIcon(ItemUtil.GetIcon(Stack.Prefab), 1f);
            if (_amount != null) _amount.text = Stack.Count > 1 ? Stack.Count.ToString() : "";
            SetTooltip(ItemUtil.GetDisplayName(Stack.Prefab), "");
        }

        /// <summary>Case vide avec l'icône en transparence (ex. le combustible attendu).</summary>
        internal void SetGhost(string prefab)
        {
            Stack = null;
            ShowIcon(ItemUtil.GetIcon(prefab), 0.3f);
            SetTooltip(ItemUtil.GetDisplayName(prefab), "");
        }

        private void ShowIcon(Sprite sprite, float alpha)
        {
            if (_amount != null) _amount.text = "";
            if (_icon == null) return;
            _icon.enabled = sprite != null;
            _icon.sprite = sprite;
            _icon.color = new Color(1f, 1f, 1f, alpha);
        }

        // Champs de l'infobulle du jeu, par réflexion pour ne pas casser si leur nom change
        private void SetTooltip(string topic, string text)
        {
            if (_tooltip == null) return;
            Traverse t = Traverse.Create(_tooltip);
            t.Field("m_topic").SetValue(topic);
            t.Field("m_text").SetValue(text);
        }
    }
}
