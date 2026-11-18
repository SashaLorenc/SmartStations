using System.Collections.Generic;
using Jotunn.Managers;
using SmartStations.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace SmartStations.UI
{
    /// <summary>
    /// Fenêtre d'une station, affichée à la place du coffre au-dessus de l'inventaire :
    /// Entrée (minerais, bois...) | Combustible | Produits.
    ///
    /// - Glisser un objet de l'inventaire sur la fenêtre : il va dans la bonne case.
    /// - Clic droit sur un objet de l'inventaire : envoie toute la pile.
    /// - Clic sur une case : reprend tout ce type ; clic droit : en reprend un.
    /// </summary>
    internal static class StationPanel
    {
        private const float Width = 630f;
        private const float Height = 376f;
        private const float SlotSize = 64f;
        private const float SlotStep = 70f;
        private const float ColumnsTop = 80f;
        private const float ButtonsTop = 262f;

        private static readonly Color TitleColor = new Color(1f, 0.82f, 0.45f);
        private static readonly Color HintColor = new Color(0.75f, 0.75f, 0.75f);

        private static GameObject _panel;
        private static StationView _station;

        private static Text _title;
        private static Text _status;
        private static Text _inputCount;
        private static Text _fuelCount;
        private static Text _outputCount;
        private static GameObject _autoFillButton;
        private static readonly List<GameObject> _fuelWidgets = new List<GameObject>();
        private static readonly List<GameObject> _inputWidgets = new List<GameObject>();
        private static readonly List<GameObject> _outputWidgets = new List<GameObject>();
        private static readonly Dictionary<GameObject, float> _outputBaseX = new Dictionary<GameObject, float>();
        private const float OutputX = 384f;
        private static readonly List<SlotView> _inputSlots = new List<SlotView>();
        private static readonly List<SlotView> _fuelSlots = new List<SlotView>();
        private static readonly List<SlotView> _outputSlots = new List<SlotView>();

        internal static bool IsOpen => _panel != null && _panel.activeSelf && _station != null;
        internal static StationView Current => _station;

        internal static void Open(StationView station)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || station == null || !station.IsValid) return;

            if (_panel == null) Build(gui);
            _station = station;

            if (!InventoryGui.IsVisible()) gui.Show(null);
            // Sinon la même pression sur E referme l'inventaire tout de suite
            ZInput.ResetButtonStatus("Use");

            PlaceOverContainerArea(gui);
            _panel.SetActive(true);
            Refresh();
        }

        internal static void Close()
        {
            _station = null;
            if (_panel != null) _panel.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // Construction
        // ---------------------------------------------------------------------

        private static void Build(InventoryGui gui)
        {
            _inputSlots.Clear();
            _fuelSlots.Clear();
            _outputSlots.Clear();
            _fuelWidgets.Clear();
            _inputWidgets.Clear();
            _outputWidgets.Clear();
            _outputBaseX.Clear();

            Transform parent = gui.m_container != null ? gui.m_container.parent : gui.transform;
            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: parent,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                width: Width,
                height: Height,
                draggable: true);
            _panel.name = "SmartStationsPanel";
            _panel.AddComponent<StationPanelUpdater>();
            Transform root = _panel.transform;

            _title = UiKit.AddText(root, "", 20f, 14f, 360f, 34f, 24, TitleColor);
            _autoFillButton = UiKit.AddButton(root, "", Width - 250f, 14f, 230f, 34f, OnToggleAutoFill);

            // Entrée : 3 x 2 cases
            BuildColumn(SlotKind.Input, "$smartstations_input", 20f, 3, 2, _inputSlots, out _inputCount, _inputWidgets);
            _inputWidgets.Add(UiKit.AddButton(root, "$smartstations_add_all", 20f, ButtonsTop, 222f, 32f, () => AddAllFromInventory(SlotKind.Input)));

            // Combustible : 1 case
            BuildColumn(SlotKind.Fuel, "$smartstations_fuel", 272f, 1, 1, _fuelSlots, out _fuelCount, _fuelWidgets);
            _fuelWidgets.Add(UiKit.AddButton(root, "$smartstations_fill", 257f, ButtonsTop, 112f, 32f, () => AddAllFromInventory(SlotKind.Fuel)));

            // Produits : 3 x 2 cases
            BuildColumn(SlotKind.Output, "$smartstations_output", OutputX, 3, 2, _outputSlots, out _outputCount, _outputWidgets);
            _outputWidgets.Add(UiKit.AddButton(root, "$smartstations_take_all", OutputX, ButtonsTop, 222f, 32f, TakeAllOutputs));
            foreach (GameObject go in _outputWidgets) _outputBaseX[go] = ((RectTransform)go.transform).anchoredPosition.x;

            _status = UiKit.AddText(root, "", 20f, 300f, Width - 40f, 26f, 17, Color.white);
            Text hint = UiKit.AddText(root, Localization.instance.Localize("$smartstations_hint"), 20f, 326f, Width - 40f, 40f, 14, HintColor);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;

            _panel.SetActive(false);
        }

        private static void BuildColumn(SlotKind kind, string label, float x, int cols, int rows, List<SlotView> slots,
            out Text countText, List<GameObject> group)
        {
            Transform root = _panel.transform;
            float w = cols * SlotStep + 12f;
            float h = rows * SlotStep + 12f;
            float labelW = Mathf.Max(w, 140f);
            float labelX = x + (w - labelW) / 2f;

            Text header = UiKit.AddText(root, Localization.instance.Localize(label), labelX, 50f, labelW, 28f, 18, Color.white, TextAnchor.MiddleCenter);
            GameObject zone = UiKit.AddDropZone(root, x, ColumnsTop, w, h, () => OnDropClick(kind, int.MaxValue));
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    slots.Add(SlotView.Create(kind, zone.transform, 9f + c * SlotStep, 9f + r * SlotStep, SlotSize, OnSlotLeft, OnSlotRight));
                }
            }
            countText = UiKit.AddText(root, "", labelX, ColumnsTop + h + 2f, labelW, 24f, 16, Color.white, TextAnchor.MiddleCenter);

            if (group != null)
            {
                group.Add(header.gameObject);
                group.Add(zone);
                group.Add(countText.gameObject);
            }
        }

        /// <summary>Même place que la fenêtre d'un coffre (au-dessus de l'inventaire du joueur).</summary>
        private static void PlaceOverContainerArea(InventoryGui gui)
        {
            if (gui.m_container == null) return;
            var corners = new Vector3[4];
            gui.m_container.GetWorldCorners(corners); // 1 = coin haut-gauche
            var rt = (RectTransform)_panel.transform;
            rt.pivot = new Vector2(0f, 1f);
            rt.position = corners[1];
            _panel.transform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------------
        // Mise à jour
        // ---------------------------------------------------------------------

        internal static void Refresh()
        {
            if (_station == null || !_station.IsValid) return;
            StationView st = _station;

            _title.text = st.Name;

            // Sans entrée (ruche, sève) : la case Produits prend la place de gauche
            foreach (GameObject go in _inputWidgets) go.SetActive(st.HasInput);
            float shift = st.HasInput ? 0f : 20f - OutputX;
            foreach (GameObject go in _outputWidgets)
            {
                var rt = (RectTransform)go.transform;
                rt.anchoredPosition = new Vector2(_outputBaseX[go] + shift, rt.anchoredPosition.y);
            }

            Fill(_inputSlots, st.GetInputs());
            _inputCount.text = st.GetInputCount() + " / " + st.InputCapacity;

            foreach (GameObject go in _fuelWidgets) go.SetActive(st.HasFuel);
            if (st.HasFuel)
            {
                ItemStack fuel = st.GetFuel();
                if (fuel != null && fuel.Count > 0) _fuelSlots[0].Set(fuel);
                else _fuelSlots[0].SetGhost(st.FuelPrefab);
                _fuelCount.text = (fuel != null ? fuel.Count : 0) + " / " + st.FuelCapacity;
            }

            List<ItemStack> outputs = st.GetOutputs();
            Fill(_outputSlots, outputs);
            int total = 0;
            foreach (ItemStack s in outputs) total += s.Count;
            _outputCount.text = total.ToString();

            _status.text = st.GetStatus();

            _autoFillButton.SetActive(Plugin.AutoFillAllowed.Value && st.SupportsAutoFill);
            UiKit.SetButtonText(_autoFillButton, Localization.instance.Localize(
                st.AutoFill ? "$smartstations_autofill_on" : "$smartstations_autofill_off"));
        }

        private static void Fill(List<SlotView> slots, List<ItemStack> stacks)
        {
            for (int i = 0; i < slots.Count; i++) slots[i].Set(i < stacks.Count ? stacks[i] : null);
        }

        // ---------------------------------------------------------------------
        // Actions
        // ---------------------------------------------------------------------

        private static void OnToggleAutoFill()
        {
            if (_station == null) return;
            _station.RequestAutoFill(!_station.AutoFill);
            Refresh();
        }

        private static void OnSlotLeft(SlotView slot)
        {
            if (OnDropClick(slot.Kind, int.MaxValue)) return;
            if (slot.Stack != null) Take(slot.Kind, slot.Stack.Prefab, slot.Stack.Count);
        }

        private static void OnSlotRight(SlotView slot)
        {
            if (OnDropClick(slot.Kind, 1)) return;
            if (slot.Stack != null) Take(slot.Kind, slot.Stack.Prefab, 1);
        }

        private static void Take(SlotKind kind, string prefab, int count)
        {
            if (_station == null) return;
            _station.RequestTake(kind, prefab, count);
        }

        private static void TakeAllOutputs()
        {
            if (_station == null) return;
            foreach (ItemStack s in _station.GetOutputs()) _station.RequestTake(SlotKind.Output, s.Prefab, s.Count);
        }

        /// <summary>
        /// Un objet de l'inventaire est en train d'être déplacé : on le dépose dans la station.
        /// Renvoie false s'il n'y avait rien en main.
        /// </summary>
        private static bool OnDropClick(SlotKind kind, int max)
        {
            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            if (gui == null || player == null || _station == null) return false;

            ItemDrop.ItemData item = gui.m_dragItem;
            if (item == null) return false;

            Inventory inv = gui.m_dragInventory;
            if (inv != player.GetInventory() || !inv.ContainsItem(item))
            {
                gui.SetupDragItem(null, null, 1);
                return true;
            }

            int dragAmount = gui.m_dragAmount;
            int added = Send(item, Mathf.Min(dragAmount, max), kind);
            int remaining = dragAmount - added;
            if (added > 0)
            {
                // Reste en main ce qui n'a pas été déposé (clic droit = un seul)
                if (remaining > 0 && inv.ContainsItem(item)) gui.SetupDragItem(item, inv, Mathf.Min(remaining, item.m_stack));
                else gui.SetupDragItem(null, null, 1);
            }
            Refresh();
            return true;
        }

        /// <summary>Clic droit sur un objet de l'inventaire pendant que la fenêtre est ouverte.</summary>
        internal static bool TrySendFromInventory(ItemDrop.ItemData item)
        {
            if (!IsOpen || item == null) return false;
            if (!_station.AcceptsInput(item) && !_station.AcceptsFuel(item)) return false;
            Send(item, item.m_stack, SlotKind.Input);
            Refresh();
            return true;
        }

        /// <summary>Envoie l'objet dans la case qui l'accepte (preferred quand les deux l'acceptent).</summary>
        private static int Send(ItemDrop.ItemData item, int amount, SlotKind preferred)
        {
            Player player = Player.m_localPlayer;
            bool asInput = _station.AcceptsInput(item);
            bool asFuel = _station.AcceptsFuel(item);
            if (!asInput && !asFuel)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_wontwork");
                return 0;
            }
            if (asFuel && (!asInput || preferred == SlotKind.Fuel)) return _station.AddFuel(player, item, amount);
            return _station.AddInput(player, item, amount);
        }

        /// <summary>Bouton « Tout ajouter » / « Remplir » : prend dans l'inventaire du joueur.</summary>
        private static void AddAllFromInventory(SlotKind kind)
        {
            Player player = Player.m_localPlayer;
            if (_station == null || player == null) return;

            var prefabs = kind == SlotKind.Fuel ? new List<string> { _station.FuelPrefab } : _station.InputPrefabs;
            bool any = false;
            foreach (string prefab in prefabs)
            {
                var items = new List<ItemDrop.ItemData>(player.GetInventory().GetAllItems());
                foreach (ItemDrop.ItemData item in items)
                {
                    if (ItemUtil.PrefabName(item) != prefab) continue;
                    int wanted = item.m_stack;
                    int added = kind == SlotKind.Fuel ? _station.AddFuel(player, item, wanted) : _station.AddInput(player, item, wanted);
                    any |= added > 0;
                    if (added < wanted) { Refresh(); return; } // station pleine
                }
            }
            if (!any) player.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");
            Refresh();
        }
    }
}
