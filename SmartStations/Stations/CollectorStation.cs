using System.Collections.Generic;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>
    /// Stations qui produisent seules sans rien recevoir (ruche, collecteur de sève) :
    /// seule la case Produits s'affiche. Le jeu compte les unités prêtes dans le ZDO (« level »).
    /// </summary>
    public abstract class CollectorStation : StationView
    {
        private static readonly List<string> NoInput = new List<string>();

        protected abstract ItemDrop Product { get; }
        protected abstract int Level { get; }
        protected abstract int MaxLevel { get; }
        protected abstract Vector3 SpawnEffectPoint { get; }
        protected abstract EffectList SpawnEffect { get; }

        /// <summary>Après avoir vidé (propriétaire) : effets visuels du jeu.</summary>
        protected virtual void AfterExtractOwner() { }

        public override bool HasInput => false;
        public override bool HasFuel => false;
        public override bool SupportsAutoFill => false;
        public override int InputCapacity => 0;
        public override int FuelCapacity => 0;
        public override string FuelPrefab => "";
        public override List<string> InputPrefabs => NoInput;
        public override List<ItemStack> GetInputs() => new List<ItemStack>();
        public override int GetInputCount() => 0;
        public override ItemStack GetFuel() => null;

        public override List<ItemStack> GetOutputs()
        {
            var list = new List<ItemStack>();
            int level = Level;
            if (level > 0 && Product != null) list.Add(new ItemStack(Product.gameObject.name, level));
            return list;
        }

        public override int AddInput(Player player, ItemDrop.ItemData item, int amount) => 0;
        public override int AddFuel(Player player, ItemDrop.ItemData item, int amount) => 0;

        public override ItemStack TakeOwner(SlotKind slot, string prefab, int count)
        {
            if (slot != SlotKind.Output || Product == null || prefab != Product.gameObject.name) return null;
            int level = Level;
            int n = Mathf.Min(count, level);
            if (n <= 0) return null;

            Zdo.Set(ZDOVars.s_level, level - n);
            // Comme le jeu : chaque unité est multipliée par le réglage de ressources du monde
            int amount = 0;
            for (int i = 0; i < n; i++) amount += Game.instance.ScaleDrops(Product.m_itemData, 1);
            SpawnEffect.Create(SpawnEffectPoint, Quaternion.identity);
            AfterExtractOwner();
            return new ItemStack(prefab, amount);
        }

        public override int FreeInputOwner() => 0;
        public override int FreeFuelOwner() => 0;
        public override void InsertInputOwner(string prefab, int count, bool cheated) { }
        public override void InsertFuelOwner(int count) { }

        protected static string Localize(string s) => Localization.instance.Localize(s);
    }

    public sealed class BeehiveStation : CollectorStation
    {
        private readonly Beehive _hive;

        public BeehiveStation(Beehive hive)
        {
            _hive = hive;
        }

        public override MonoBehaviour Behaviour => _hive;
        public override ZNetView View => _hive != null ? _hive.m_nview : null;
        public override string Name => Localize(_hive.m_name);
        protected override ItemDrop Product => _hive.m_honeyItem;
        protected override int Level => _hive.GetHoneyLevel();
        protected override int MaxLevel => _hive.m_maxHoney;
        protected override Vector3 SpawnEffectPoint => _hive.m_spawnPoint != null ? _hive.m_spawnPoint.position : _hive.transform.position;
        protected override EffectList SpawnEffect => _hive.m_spawnEffect;

        public override string GetStatus()
        {
            string state;
            if (!_hive.CheckBiome()) state = _hive.m_areaText;
            else if (!_hive.HaveFreeSpace()) state = _hive.m_freespaceText;
            else if (_hive.m_effectOnlyInDaylight && !EnvMan.IsDaylight()) state = _hive.m_sleepText;
            else state = _hive.m_happyText;
            return Localize(state) + " · " + Level + " / " + MaxLevel;
        }
    }

    public sealed class SapCollectorStation : CollectorStation
    {
        private readonly SapCollector _sap;

        public SapCollectorStation(SapCollector sap)
        {
            _sap = sap;
        }

        public override MonoBehaviour Behaviour => _sap;
        public override ZNetView View => _sap != null ? _sap.m_nview : null;
        public override string Name => Localize(_sap.m_name);
        protected override ItemDrop Product => _sap.m_spawnItem;
        protected override int Level => _sap.GetLevel();
        protected override int MaxLevel => _sap.m_maxLevel;
        protected override Vector3 SpawnEffectPoint => _sap.m_spawnPoint != null ? _sap.m_spawnPoint.position : _sap.transform.position;
        protected override EffectList SpawnEffect => _sap.m_spawnEffect;

        protected override void AfterExtractOwner()
        {
            View.InvokeRPC(ZNetView.Everybody, "RPC_UpdateEffects");
        }

        public override string GetStatus() => Localize(_sap.GetStatusText()) + " · " + Level + " / " + MaxLevel;
    }
}
