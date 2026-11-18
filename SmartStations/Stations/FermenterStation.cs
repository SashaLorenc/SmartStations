using System.Collections.Generic;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>
    /// Fermentateur : un seul objet en entrée, pas de combustible ; une fois prêt, la sortie contient
    /// les produits (ex. 6 hydromels). État du jeu dans le ZDO : « Content » et « StartTime ».
    /// </summary>
    public sealed class FermenterStation : StationView
    {
        private readonly Fermenter _fermenter;
        private List<string> _inputPrefabs;

        public FermenterStation(Fermenter fermenter)
        {
            _fermenter = fermenter;
        }

        public Fermenter Fermenter => _fermenter;
        public override MonoBehaviour Behaviour => _fermenter;
        public override ZNetView View => _fermenter != null ? _fermenter.m_nview : null;
        public override string Name => Localization.instance.Localize(_fermenter.m_name);

        public override bool HasFuel => false;
        public override int InputCapacity => 1;
        public override int FuelCapacity => 0;
        public override string FuelPrefab => "";
        public override ItemStack GetFuel() => null;

        public override List<string> InputPrefabs
        {
            get
            {
                if (_inputPrefabs == null)
                {
                    _inputPrefabs = new List<string>();
                    foreach (Fermenter.ItemConversion c in _fermenter.m_conversion)
                    {
                        if (c != null && c.m_from != null) _inputPrefabs.Add(c.m_from.gameObject.name);
                    }
                }
                return _inputPrefabs;
            }
        }

        private bool IsReady => _fermenter.GetStatus() == Fermenter.Status.Ready;
        private bool IsFermenting => _fermenter.GetStatus() == Fermenter.Status.Fermenting;

        public override List<ItemStack> GetInputs()
        {
            var list = new List<ItemStack>();
            if (!IsFermenting) return list;
            Fermenter.ItemConversion conv = _fermenter.GetItemConversion(_fermenter.GetContent());
            if (conv != null && conv.m_from != null) list.Add(new ItemStack(conv.m_from.gameObject.name, 1));
            return list;
        }

        public override int GetInputCount() => IsFermenting ? 1 : 0;

        public override List<ItemStack> GetOutputs()
        {
            var list = new List<ItemStack>();
            if (!IsReady) return list;
            Fermenter.ItemConversion conv = _fermenter.GetItemConversion(_fermenter.GetContent());
            if (conv != null && conv.m_to != null) list.Add(new ItemStack(conv.m_to.gameObject.name, conv.m_producedItems));
            return list;
        }

        public override string GetStatus()
        {
            if (!_fermenter.m_hasRoof) return Localize("$piece_fermenter_needroof");
            if (_fermenter.m_exposed) return Localize("$piece_fermenter_exposed");
            if (IsReady) return Localize("$piece_fermenter_ready");
            if (!IsFermenting) return Localize("$smartstations_status_idle");

            double elapsed = _fermenter.GetFermentationTime();
            float total = Mathf.Max(1f, _fermenter.m_fermentationDuration);
            int percent = Mathf.Clamp(Mathf.FloorToInt((float)(elapsed / total * 100.0)), 0, 99);
            return Localize("$piece_fermenter_fermenting") + " " + percent + " % · " + TimeText.Format(total - (float)elapsed);
        }

        // ---------- Joueur ----------

        public override int AddInput(Player player, ItemDrop.ItemData item, int amount)
        {
            if (!IsValid || !AcceptsInput(item) || amount <= 0) return 0;
            if (!PrivateArea.CheckAccess(_fermenter.transform.position)) return 0;

            // Même vérifications que le jeu
            _fermenter.UpdateCover(0f, true);
            if (!_fermenter.m_hasRoof)
            {
                player.Message(MessageHud.MessageType.Center, "$piece_fermenter_needroof");
                return 0;
            }
            if (_fermenter.m_exposed)
            {
                player.Message(MessageHud.MessageType.Center, "$piece_fermenter_exposed");
                return 0;
            }
            if (_fermenter.GetStatus() != Fermenter.Status.Empty || EffectiveInputCount(0) > 0)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                return 0;
            }

            string prefab = ItemUtil.PrefabName(item);
            string name = item.m_shared.m_name;
            Sprite icon = item.GetIcon();
            if (!player.GetInventory().RemoveOneItem(item)) return 0;
            View.InvokeRPC("RPC_AddItem", prefab.GetStableHashCode(), item.m_cheated);
            RememberInput(1);
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, 1, icon);
            return 1;
        }

        public override int AddFuel(Player player, ItemDrop.ItemData item, int amount) => 0;

        // ---------- Propriétaire ----------

        public override ItemStack TakeOwner(SlotKind slot, string prefab, int count)
        {
            if (count <= 0) return null;
            // Depuis Valheim 1.0 le contenu est le hash du nom du prefab
            Fermenter.ItemConversion conv = _fermenter.GetItemConversion(_fermenter.GetContent());
            if (conv == null) return null;

            if (slot == SlotKind.Input)
            {
                // On retire l'objet en cours de fermentation : la fermentation est annulée
                if (!IsFermenting || conv.m_from == null || conv.m_from.gameObject.name != prefab) return null;
                ClearOwner();
                return new ItemStack(prefab, 1);
            }

            if (slot == SlotKind.Output)
            {
                if (!IsReady) return null;
                if (conv.m_to == null) return null;
                ClearOwner();
                _fermenter.m_tapEffects.Create(_fermenter.transform.position, _fermenter.transform.rotation);
                return new ItemStack(conv.m_to.gameObject.name, conv.m_producedItems);
            }
            return null;
        }

        private void ClearOwner()
        {
            Zdo.Set(ZDOVars.s_content, 0);
            Zdo.Set(ZDOVars.s_startTime, 0L);
            Zdo.Set(ZDOVars.s_cheatedQueued, false);
        }

        public override int FreeInputOwner()
        {
            if (_fermenter.GetStatus() != Fermenter.Status.Empty) return 0;
            _fermenter.UpdateCover(0f, true);
            return _fermenter.m_hasRoof && !_fermenter.m_exposed ? 1 : 0;
        }

        public override int FreeFuelOwner() => 0;

        public override void InsertInputOwner(string prefab, int count, bool cheated)
        {
            if (count <= 0) return;
            // Méthode du jeu : vérifie que le fermentateur est vide et que l'objet est accepté
            _fermenter.RPC_AddItem(0L, prefab.GetStableHashCode(), cheated);
        }

        public override void InsertFuelOwner(int count) { }

        private static string Localize(string s) => Localization.instance.Localize(s);
    }
}
