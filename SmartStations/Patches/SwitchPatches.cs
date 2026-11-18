using HarmonyLib;
using SmartStations.Stations;
using SmartStations.UI;

namespace SmartStations.Patches
{
    /// <summary>
    /// Stations dont on se sert par des « interrupteurs » (Switch) : fours (ajouter minerai, combustible, vider)
    /// et stations de cuisson à interrupteurs (four à pain).
    /// </summary>
    internal static class SwitchPatches
    {
        private static StationView StationOf(Switch sw)
        {
            Smelter smelter = sw.GetComponentInParent<Smelter>();
            if (smelter != null && (sw == smelter.m_addOreSwitch || sw == smelter.m_addWoodSwitch || sw == smelter.m_emptyOreSwitch))
            {
                return StationRegistry.Get(smelter);
            }
            CookingStation cooking = sw.GetComponentInParent<CookingStation>();
            if (cooking != null && (sw == cooking.m_addFoodSwitch || sw == cooking.m_addFuelSwitch))
            {
                return StationRegistry.Get(cooking);
            }
            return null;
        }

        /// <summary>E sur une station : ouvre la fenêtre. Maj + E garde l'action d'origine du jeu.</summary>
        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        private static class SwitchInteract
        {
            private static bool Prefix(Switch __instance, Humanoid character, bool hold, bool alt, ref bool __result)
            {
                if (hold || alt || character == null || character != Player.m_localPlayer) return true;
                StationView view = StationOf(__instance);
                if (!StationRegistry.IsEnabled(view)) return true;

                StationPanel.Open(view);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.GetHoverText))]
        private static class SwitchHover
        {
            private static void Postfix(Switch __instance, ref string __result)
            {
                if (!StationRegistry.IsEnabled(StationOf(__instance))) return;
                __result = HoverText.Rewrite(__result);
            }
        }
    }
}
