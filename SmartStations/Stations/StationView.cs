using System.Collections.Generic;
using UnityEngine;

namespace SmartStations.Stations
{
    public enum SlotKind
    {
        Input = 0,
        Fuel = 1,
        Output = 2
    }

    /// <summary>
    /// Vue commune d'une station de production (four, fermentateur...) pour la fenêtre et le remplissage auto.
    ///
    /// Règle réseau : seul le propriétaire du ZDO de la station modifie son contenu.
    /// - Ajouter : le joueur retire l'objet de son inventaire, puis envoie le RPC du jeu au propriétaire.
    /// - Prendre : le joueur demande au propriétaire (RPC SmartStations_Take), qui retire d'abord puis
    ///   renvoie les objets au demandeur (RPC SmartStations_Give). Un objet n'existe donc jamais en double.
    /// </summary>
    public abstract class StationView
    {
        public const string AutoFillKey = "SmartStations_AutoFill";

        public abstract MonoBehaviour Behaviour { get; }
        public abstract ZNetView View { get; }

        public bool IsValid => Behaviour != null && View != null && View.IsValid();
        public Vector3 Position => Behaviour.transform.position;
        public ZDO Zdo => View.GetZDO();
        public bool IsOwner => View.IsOwner();

        /// <summary>Nom affiché, déjà traduit.</summary>
        public abstract string Name { get; }

        public abstract bool HasFuel { get; }
        public abstract int InputCapacity { get; }
        public abstract int FuelCapacity { get; }

        /// <summary>Objets en attente de transformation, regroupés par type.</summary>
        public abstract List<ItemStack> GetInputs();
        public abstract int GetInputCount();

        /// <summary>Combustible présent (unités entières), ou null si la station n'en utilise pas.</summary>
        public abstract ItemStack GetFuel();

        /// <summary>Produits finis prêts à être pris.</summary>
        public abstract List<ItemStack> GetOutputs();

        /// <summary>Ligne d'état sous les cases (progression, toit manquant...), déjà traduite.</summary>
        public abstract string GetStatus();

        /// <summary>Noms des prefabs acceptés en entrée, dans l'ordre du jeu.</summary>
        public abstract List<string> InputPrefabs { get; }
        public abstract string FuelPrefab { get; }

        public bool AcceptsInput(ItemDrop.ItemData item) => InputPrefabs.Contains(ItemUtil.PrefabName(item));
        public bool AcceptsFuel(ItemDrop.ItemData item) => HasFuel && ItemUtil.PrefabName(item) == FuelPrefab;

        // ---------- Côté joueur (n'importe quel client) ----------

        /// <summary>Ajoute jusqu'à amount objets depuis l'inventaire du joueur. Renvoie le nombre ajouté.</summary>
        public abstract int AddInput(Player player, ItemDrop.ItemData item, int amount);
        public abstract int AddFuel(Player player, ItemDrop.ItemData item, int amount);

        public void RequestTake(SlotKind slot, string prefab, int count)
        {
            if (IsValid) View.InvokeRPC(StationRpc.Take, (int)slot, prefab ?? "", count);
        }

        public bool AutoFill => IsValid && Zdo.GetBool(AutoFillKey, Plugin.AutoFillDefault.Value);

        public void RequestAutoFill(bool on)
        {
            if (IsValid) View.InvokeRPC(StationRpc.SetAutoFill, on);
        }

        // ---------- Côté propriétaire du ZDO uniquement ----------

        /// <summary>Retire des objets de la station et renvoie ce qui a été retiré (null si rien).</summary>
        public abstract ItemStack TakeOwner(SlotKind slot, string prefab, int count);

        public abstract int FreeInputOwner();
        public abstract int FreeFuelOwner();
        public abstract void InsertInputOwner(string prefab, int count, bool cheated);
        public abstract void InsertFuelOwner(int count);

        // ---------- Ajouts en cours (le ZDO met un instant à revenir du propriétaire) ----------

        private int _expectedInput = -1;
        private int _expectedFuel = -1;
        private float _expectedTime;

        protected int EffectiveInputCount(int fromZdo)
        {
            return Time.time - _expectedTime < 2f ? Mathf.Max(fromZdo, _expectedInput) : fromZdo;
        }

        protected int EffectiveFuel(int fromZdo)
        {
            return Time.time - _expectedTime < 2f ? Mathf.Max(fromZdo, _expectedFuel) : fromZdo;
        }

        protected void RememberInput(int expected)
        {
            _expectedInput = expected;
            _expectedTime = Time.time;
        }

        protected void RememberFuel(int expected)
        {
            _expectedFuel = expected;
            _expectedTime = Time.time;
        }

        protected static void Grouped(List<ItemStack> list, string prefab, int count)
        {
            if (string.IsNullOrEmpty(prefab) || count <= 0) return;
            foreach (ItemStack s in list)
            {
                if (s.Prefab == prefab) { s.Count += count; return; }
            }
            list.Add(new ItemStack(prefab, count));
        }
    }
}
