using System.Collections.Generic;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>
    /// Stations de cuisson (CookingStation) : grill, broche, four à pain et celles des mods sur le même modèle.
    /// Chaque emplacement du jeu (« slot0..N » dans le ZDO) contient un objet cru (Entrée), cuit ou brûlé (Produits).
    /// </summary>
    public sealed class CookingStationView : StationView
    {
        private readonly CookingStation _cs;
        private List<string> _inputPrefabs;

        public CookingStationView(CookingStation cs)
        {
            _cs = cs;
        }

        public override MonoBehaviour Behaviour => _cs;
        public override ZNetView View => _cs != null ? _cs.m_nview : null;
        public override string Name => Localization.instance.Localize(_cs.m_name);

        public override bool HasFuel => _cs.m_useFuel && _cs.m_fuelItem != null;
        public override int FuelCapacity => _cs.m_maxFuel;
        public override string FuelPrefab => HasFuel ? _cs.m_fuelItem.gameObject.name : "";
        public override int InputCapacity => _cs.m_slots != null ? _cs.m_slots.Length : 0;

        /// <summary>
        /// Remplissage auto du combustible seulement : remplir les emplacements tout seul ferait brûler
        /// la nourriture quand personne ne la récupère.
        /// </summary>
        public override bool SupportsAutoFill => HasFuel;

        public override List<string> InputPrefabs
        {
            get
            {
                if (_inputPrefabs == null)
                {
                    _inputPrefabs = new List<string>();
                    foreach (CookingStation.ItemConversion c in _cs.m_conversion)
                    {
                        if (c != null && c.m_from != null) _inputPrefabs.Add(c.m_from.gameObject.name);
                    }
                }
                return _inputPrefabs;
            }
        }

        private sealed class SlotState
        {
            public int Index;
            public string Item;
            public float CookedTime;
            public CookingStation.Status Status;
        }

        private List<SlotState> ReadSlots()
        {
            var list = new List<SlotState>();
            for (int i = 0; i < InputCapacity; i++)
            {
                string item;
                float cooked;
                CookingStation.Status status;
                bool cheated;
                _cs.GetSlot(i, out item, out cooked, out status, out cheated);
                list.Add(new SlotState { Index = i, Item = item ?? "", CookedTime = cooked, Status = status });
            }
            return list;
        }

        private static bool IsRaw(SlotState s) => s.Item != "" && s.Status == CookingStation.Status.NotDone;
        private static bool IsCooked(SlotState s) => s.Item != "" && s.Status != CookingStation.Status.NotDone;

        public override List<ItemStack> GetInputs()
        {
            var list = new List<ItemStack>();
            foreach (SlotState s in ReadSlots()) if (IsRaw(s)) Grouped(list, s.Item, 1);
            return list;
        }

        public override int GetInputCount()
        {
            int n = 0;
            foreach (SlotState s in ReadSlots()) if (s.Item != "") n++;
            return n;
        }

        public override ItemStack GetFuel()
        {
            if (!HasFuel) return null;
            return new ItemStack(FuelPrefab, Mathf.CeilToInt(_cs.GetFuel()));
        }

        public override List<ItemStack> GetOutputs()
        {
            var list = new List<ItemStack>();
            foreach (SlotState s in ReadSlots()) if (IsCooked(s)) Grouped(list, s.Item, 1);
            return list;
        }

        private bool IsHeating => (_cs.m_requireFire && _cs.IsFireLit()) || (HasFuel && _cs.GetFuel() > 0f);

        public override string GetStatus()
        {
            if (!IsHeating)
            {
                return Localize(_cs.m_requireFire ? "$msg_needfire" : "$smartstations_status_nofuel");
            }

            List<SlotState> slots = ReadSlots();
            bool anyDone = false;
            float bestProgress = -1f;
            float remaining = 0f;
            foreach (SlotState s in slots)
            {
                if (IsCooked(s) && s.Status == CookingStation.Status.Done) anyDone = true;
                if (!IsRaw(s)) continue;
                CookingStation.ItemConversion conv = _cs.GetItemConversion(s.Item);
                if (conv == null || conv.m_cookTime <= 0f) continue;
                float progress = s.CookedTime / conv.m_cookTime;
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                    remaining = conv.m_cookTime - s.CookedTime;
                }
            }

            string text;
            if (bestProgress >= 0f)
            {
                int percent = Mathf.Clamp(Mathf.FloorToInt(bestProgress * 100f), 0, 99);
                text = Localize("$smartstations_status_working") + " " + percent + " % · " + TimeText.Format(remaining);
            }
            else
            {
                text = Localize("$smartstations_status_idle");
            }
            if (anyDone) text += " · <color=orange>" + Localize("$smartstations_status_burning") + "</color>";
            return text;
        }

        private Skills.SkillType Skill => _cs.m_skill != Skills.SkillType.None ? _cs.m_skill : Skills.SkillType.Cooking;

        // ---------- Joueur ----------

        public override int AddInput(Player player, ItemDrop.ItemData item, int amount)
        {
            if (!IsValid || !AcceptsInput(item)) return 0;
            if (_cs.m_requireFire && !_cs.IsFireLit())
            {
                player.Message(MessageHud.MessageType.Center, "$msg_needfire");
                return 0;
            }

            int used = EffectiveInputCount(GetInputCount());
            int n = Mathf.Min(Mathf.Min(amount, item.m_stack), InputCapacity - used);
            if (n <= 0)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_nocookroom");
                return 0;
            }

            string prefab = ItemUtil.PrefabName(item);
            string name = item.m_shared.m_name;
            Sprite icon = item.GetIcon();
            bool cheated = item.m_cheated;
            if (!player.GetInventory().RemoveItem(item, n)) return 0;
            for (int i = 0; i < n; i++) View.InvokeRPC("RPC_AddItem", prefab, cheated);
            RememberInput(used + n);
            player.RaiseSkill(Skill, 0.4f * n);
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, n, icon);
            return n;
        }

        public override int AddFuel(Player player, ItemDrop.ItemData item, int amount)
        {
            if (!IsValid || !AcceptsFuel(item)) return 0;
            int fuel = EffectiveFuel(Mathf.CeilToInt(_cs.GetFuel()));
            int n = Mathf.Min(Mathf.Min(amount, item.m_stack), _cs.m_maxFuel - fuel);
            if (n <= 0)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                return 0;
            }

            string name = item.m_shared.m_name;
            Sprite icon = item.GetIcon();
            if (!player.GetInventory().RemoveItem(item, n)) return 0;
            for (int i = 0; i < n; i++) View.InvokeRPC("RPC_AddFuel");
            RememberFuel(fuel + n);
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, n, icon);
            return n;
        }

        protected override void OnRequestTake(Player player, SlotKind slot, string prefab, int count)
        {
            // Comme en reprenant un plat à la main : un peu de compétence Cuisine (pas pour un plat brûlé)
            if (player == null || slot != SlotKind.Output) return;
            if (_cs.m_overCookedItem != null && prefab == _cs.m_overCookedItem.gameObject.name) return;
            player.RaiseSkill(Skill, 0.6f * count);
        }

        // ---------- Propriétaire ----------

        public override ItemStack TakeOwner(SlotKind slot, string prefab, int count)
        {
            if (count <= 0) return null;
            if (slot == SlotKind.Fuel)
            {
                if (!HasFuel) return null;
                float fuel = _cs.GetFuel();
                int n = Mathf.Min(count, Mathf.FloorToInt(fuel));
                if (n <= 0) return null;
                _cs.SetFuel(fuel - n);
                return new ItemStack(FuelPrefab, n);
            }

            int removed = 0;
            foreach (SlotState s in ReadSlots())
            {
                if (removed >= count) break;
                if (s.Item != prefab) continue;
                if (slot == SlotKind.Input ? !IsRaw(s) : !IsCooked(s)) continue;
                _cs.SetSlot(s.Index, "", 0f, CookingStation.Status.NotDone, false, 0L);
                View.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", s.Index, "");
                removed++;
            }
            if (removed == 0) return null;
            if (slot == SlotKind.Output && _cs.m_slots.Length > 0)
            {
                _cs.m_pickEffector.Create(_cs.transform.position, Quaternion.identity);
            }
            return new ItemStack(prefab, removed);
        }

        public override int FreeInputOwner() => 0;

        public override int FreeFuelOwner()
        {
            if (!HasFuel) return 0;
            return Mathf.Max(0, Mathf.FloorToInt(_cs.m_maxFuel - _cs.GetFuel()));
        }

        public override void InsertInputOwner(string prefab, int count, bool cheated) { }

        public override void InsertFuelOwner(int count)
        {
            if (count <= 0 || !HasFuel) return;
            _cs.SetFuel(_cs.GetFuel() + count);
            _cs.m_fuelAddedEffects.Create(_cs.transform.position, _cs.transform.rotation, _cs.transform);
        }

        private static string Localize(string s) => Localization.instance.Localize(s);
    }
}
