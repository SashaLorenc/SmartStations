using HarmonyLib;
using SmartStations.Stations;
using SmartStations.UI;

namespace SmartStations.Patches
{
    internal static class FermenterPatches
    {
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Interact))]
        private static class FermenterInteract
        {
            private static bool Prefix(Fermenter __instance, Humanoid user, bool hold, bool alt, ref bool __result)
            {
                if (hold || alt || user == null || user != Player.m_localPlayer) return true;
                StationView view = StationRegistry.Get(__instance);
                if (!StationRegistry.IsEnabled(view)) return true;
                // Protégé par une balise d'un autre joueur : le jeu refuse et fait clignoter la balise
                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, false)) return true;

                StationPanel.Open(view);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
        private static class FermenterHover
        {
            private static void Postfix(Fermenter __instance, ref string __result)
            {
                if (!StationRegistry.IsEnabled(StationRegistry.Get(__instance))) return;
                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, false)) return;
                __result = HoverText.Rewrite(__result);
            }
        }
    }
}
