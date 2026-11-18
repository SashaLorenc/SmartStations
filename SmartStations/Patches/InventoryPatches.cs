using HarmonyLib;
using SmartStations.UI;

namespace SmartStations.Patches
{
    /// <summary>Clic droit sur un objet de l'inventaire : l'envoie dans la station ouverte.</summary>
    [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
    internal static class InventoryRightClick
    {
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (!StationPanel.IsOpen || grid != __instance.m_playerGrid) return true;
            return !StationPanel.TrySendFromInventory(item);
        }
    }
}
