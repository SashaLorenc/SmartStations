using System.Collections.Generic;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>
    /// Toutes les stations du jeu basées sur Smelter : four à charbon, fonderie, haut fourneau,
    /// raffinerie d'eitr, moulin à vent, rouet... (et celles ajoutées par d'autres mods sur le même modèle).
    /// État du jeu dans le ZDO : file « item0..itemN » + « queued », combustible « fuel ».
    /// </summary>
    public sealed class SmelterStation : StationView
    {
        private readonly Smelter _smelter;
        private List<string> _inputPrefabs;

        public SmelterStation(Smelter smelter)
        {
            _smelter = smelter;
        }

        public Smelter Smelter => _smelter;
        public override MonoBehaviour Behaviour => _smelter;
        public override ZNetView View => _smelter != null ? _smelter.m_nview : null;
        public override string Name => Localization.instance.Localize(_smelter.m_name);

        public override bool HasFuel => _smelter.m_maxFuel > 0 && _smelter.m_fuelItem != null;
        public override int InputCapacity => _smelter.m_maxOre;
        public override int FuelCapacity => _smelter.m_maxFuel;
        public override string FuelPrefab => HasFuel ? _smelter.m_fuelItem.gameObject.name : "";

        public override List<string> InputPrefabs
        {
            get
            {
                if (_inputPrefabs == null)
                {
                    _inputPrefabs = new List<string>();
                    foreach (Smelter.ItemConversion c in _smelter.m_conversion)
                    {
                        if (c != null && c.m_from != null) _inputPrefabs.Add(c.m_from.gameObject.name);
                    }
                }
                return _inputPrefabs;
            }
        }

        public override List<ItemStack> GetInputs()
        {
            var list = new List<ItemStack>();
            ZDO zdo = Zdo;
            int queued = _smelter.GetQueueSize();
            for (int i = 0; i < queued; i++) Grouped(list, zdo.GetString("item" + i, ""), 1);
            return list;
        }

        public override int GetInputCount() => _smelter.GetQueueSize();

        public override ItemStack GetFuel()
        {
            if (!HasFuel) return null;
            return new ItemStack(FuelPrefab, Mathf.CeilToInt(_smelter.GetFuel()));
        }

        public override List<ItemStack> GetOutputs()
        {
            List<ItemStack> list = OutputStore.Read(Zdo);
            // Produits que le jeu accumule lui-même avant de les faire tomber (stations « m_spawnStack »)
            int pending = Zdo.GetInt(ZDOVars.s_spawnAmount, 0);
            if (pending > 0)
            {
                Smelter.ItemConversion conv = _smelter.GetItemConversion(Zdo.GetString(ZDOVars.s_spawnOre, ""));
                if (conv != null && conv.m_to != null) Grouped(list, conv.m_to.gameObject.name, pending);
            }
            return list;
        }

        public override string GetStatus()
        {
            if (_smelter.m_requiresRoof && !_smelter.m_haveRoof) return Localize("$piece_smelter_reqroof");
            if (_smelter.m_blockedSmoke) return Localize("$smartstations_status_smoke");

            int queued = _smelter.GetQueueSize();
            if (queued == 0) return Localize("$smartstations_status_idle");
            if (HasFuel && _smelter.GetFuel() <= 0f) return Localize("$smartstations_status_nofuel");

            float power = _smelter.m_windmill != null ? _smelter.m_windmill.GetPowerOutput() : 1f;
            if (power <= 0.01f) return Localize("$smartstations_status_nowind");

            float sec = Mathf.Max(0.1f, _smelter.m_secPerProduct);
            float baked = _smelter.GetBakeTimer();
            int percent = Mathf.Clamp(Mathf.FloorToInt(baked / sec * 100f), 0, 99);
            float remaining = (queued * sec - baked) / power;
            return Localize("$smartstations_status_working") + " " + percent + " % · " + TimeText.Format(remaining);
        }

        // ---------- Joueur ----------

        public override int AddInput(Player player, ItemDrop.ItemData item, int amount)
        {
            if (!IsValid || !AcceptsInput(item)) return 0;
            int queued = EffectiveInputCount(_smelter.GetQueueSize());
            int n = Mathf.Min(Mathf.Min(amount, item.m_stack), _smelter.m_maxOre - queued);
            if (n <= 0)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                return 0;
            }

            string prefab = ItemUtil.PrefabName(item);
            string name = item.m_shared.m_name;
            Sprite icon = item.GetIcon();
            bool cheated = item.m_cheated;
            if (!player.GetInventory().RemoveItem(item, n)) return 0;
            for (int i = 0; i < n; i++) View.InvokeRPC("RPC_AddOre", prefab, cheated);
            RememberInput(queued + n);
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, n, icon);
            return n;
        }

        public override int AddFuel(Player player, ItemDrop.ItemData item, int amount)
        {
            if (!IsValid || !AcceptsFuel(item)) return 0;
            int fuel = EffectiveFuel(Mathf.CeilToInt(_smelter.GetFuel()));
            int n = Mathf.Min(Mathf.Min(amount, item.m_stack), _smelter.m_maxFuel - fuel);
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

        // ---------- Propriétaire ----------

        public override ItemStack TakeOwner(SlotKind slot, string prefab, int count)
        {
            if (count <= 0) return null;
            switch (slot)
            {
                case SlotKind.Input: return TakeQueuedOwner(prefab, count);
                case SlotKind.Fuel: return TakeFuelOwner(count);
                default: return TakeOutputOwner(prefab, count);
            }
        }

        /// <summary>Retire des minerais de la file, en partant de la fin (celui en cours de cuisson part en dernier).</summary>
        private ItemStack TakeQueuedOwner(string prefab, int count)
        {
            ZDO zdo = Zdo;
            int queued = _smelter.GetQueueSize();
            if (queued == 0) return null;
            string first = zdo.GetString("item0", "");

            var items = new List<string>(queued);
            for (int i = 0; i < queued; i++) items.Add(zdo.GetString("item" + i, ""));

            int removed = 0;
            for (int i = items.Count - 1; i >= 0 && removed < count; i--)
            {
                if (items[i] != prefab) continue;
                items.RemoveAt(i);
                removed++;
            }
            if (removed == 0) return null;

            for (int i = 0; i < items.Count; i++) zdo.Set("item" + i, items[i]);
            zdo.Set(ZDOVars.s_queued, items.Count);
            // Le minerai en cours de cuisson a été retiré : on repart de zéro pour le suivant
            if (items.Count == 0 || items[0] != first) _smelter.SetBakeTimer(0f);
            return new ItemStack(prefab, removed);
        }

        private ItemStack TakeFuelOwner(int count)
        {
            if (!HasFuel) return null;
            float fuel = _smelter.GetFuel();
            int n = Mathf.Min(count, Mathf.FloorToInt(fuel));
            if (n <= 0) return null;
            _smelter.SetFuel(fuel - n);
            return new ItemStack(FuelPrefab, n);
        }

        private ItemStack TakeOutputOwner(string prefab, int count)
        {
            if (!Plugin.KeepProducts.Value)
            {
                // Produits non gardés : on vide ce que le jeu accumule, ça tombe par terre comme d'habitude
                _smelter.SpawnProcessed();
                return null;
            }
            // Range d'abord ce que le jeu accumule (passe par notre patch de Spawn), puis on prend
            _smelter.SpawnProcessed();
            int removed = OutputStore.Remove(Zdo, prefab, count);
            return removed > 0 ? new ItemStack(prefab, removed) : null;
        }

        public override int FreeInputOwner() => _smelter.m_maxOre - _smelter.GetQueueSize();

        public override int FreeFuelOwner()
        {
            if (!HasFuel) return 0;
            return Mathf.Max(0, Mathf.FloorToInt(_smelter.m_maxFuel - _smelter.GetFuel()));
        }

        public override void InsertInputOwner(string prefab, int count, bool cheated)
        {
            if (count <= 0 || !_smelter.IsItemAllowed(prefab)) return;
            for (int i = 0; i < count; i++) _smelter.QueueOre(prefab, cheated);
            _smelter.m_oreAddedEffects.Create(_smelter.transform.position, _smelter.transform.rotation);
        }

        public override void InsertFuelOwner(int count)
        {
            if (count <= 0 || !HasFuel) return;
            _smelter.SetFuel(_smelter.GetFuel() + count);
            _smelter.m_fuelAddedEffects.Create(_smelter.transform.position, _smelter.transform.rotation, _smelter.transform);
        }

        private static string Localize(string s) => Localization.instance.Localize(s);
    }
}
