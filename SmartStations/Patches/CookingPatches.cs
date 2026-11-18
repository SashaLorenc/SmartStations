using HarmonyLib;
using SmartStations.Stations;
using SmartStations.UI;

namespace SmartStations.Patches
{
    /// <summary>Grill, broche et stations de cuisson sans interrupteur : on interagit avec la station elle-même.</summary>
    internal static class CookingPatches
    {
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Interact))]
        private static class CookingInteract
        {
            private static bool Prefix(CookingStation __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
            {
                // Arguments par position (user, hold, alt) : résiste à un renommage dans une future version du jeu
                if (__1 || __2 || __0 == null || __0 != Player.m_localPlayer) return true;
                if (__instance.m_addFoodSwitch != null) return true; // passe par ses interrupteurs (SwitchPatches)
                StationView view = StationRegistry.Get(__instance);
                if (!StationRegistry.IsEnabled(view)) return true;

                StationPanel.Open(view);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
        private static class CookingHover
        {
            private static void Postfix(CookingStation __instance, ref string __result)
            {
                if (string.IsNullOrEmpty(__result) || __instance.m_addFoodSwitch != null) return;
                if (!StationRegistry.IsEnabled(StationRegistry.Get(__instance))) return;
                __result = HoverText.Rewrite(__result);
            }
        }
    }
}
