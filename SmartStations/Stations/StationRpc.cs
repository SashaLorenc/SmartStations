using HarmonyLib;

namespace SmartStations.Stations
{
    /// <summary>
    /// RPC du mod. Prendre un objet dans une station :
    /// 1. le joueur envoie SmartStations_Take au propriétaire de la station ;
    /// 2. le propriétaire retire l'objet du ZDO (lui seul a le droit de l'écrire) ;
    /// 3. il renvoie SmartStations_Give au joueur, qui reçoit exactement ce qui a été retiré.
    /// </summary>
    public static class StationRpc
    {
        public const string Take = "SmartStations_Take";
        public const string SetAutoFill = "SmartStations_SetAutoFill";
        public const string Give = "SmartStations_Give";

        internal static void RegisterOn(StationView station)
        {
            ZNetView nview = station.View;
            nview.Register<int, string, int>(Take, (sender, slot, prefab, count) => OnTake(station, sender, slot, prefab, count));
            nview.Register<bool>(SetAutoFill, (sender, on) => OnSetAutoFill(station, on));
        }

        private static void OnTake(StationView station, long sender, int slot, string prefab, int count)
        {
            if (!station.IsValid || !station.IsOwner) return;
            if (slot < 0 || slot > (int)SlotKind.Output || count <= 0) return;

            ItemStack taken = station.TakeOwner((SlotKind)slot, prefab, count);
            if (taken == null || taken.Count <= 0) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Give, taken.Prefab, taken.Count);
        }

        private static void OnSetAutoFill(StationView station, bool on)
        {
            if (!station.IsValid || !station.IsOwner) return;
            station.Zdo.Set(StationView.AutoFillKey, on);
        }

        private static void OnGive(long sender, string prefab, int count)
        {
            if (Player.m_localPlayer == null) return;
            ItemUtil.GiveToPlayer(Player.m_localPlayer, prefab, count);
        }

        /// <summary>RPC global (pas lié à une station) : enregistré une fois par partie.</summary>
        [HarmonyPatch(typeof(Game), "Start")]
        private static class GameStart
        {
            private static void Postfix()
            {
                ZRoutedRpc.instance.Register<string, int>(Give, OnGive);
            }
        }
    }
}
