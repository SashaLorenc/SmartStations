using HarmonyLib;
using SmartStations.Stations;
using SmartStations.UI;
using UnityEngine;

namespace SmartStations.Patches
{
    /// <summary>Ruche et collecteur de sève : E ouvre la fenêtre (case Produits seulement).</summary>
    internal static class CollectorPatches
    {
        private static bool TryOpen(StationView view, Humanoid character, bool repeat, bool alt, Vector3 position, ref bool result)
        {
            if (repeat || alt || character == null || character != Player.m_localPlayer) return true;
            if (!StationRegistry.IsEnabled(view)) return true;
            if (!PrivateArea.CheckAccess(position, 0f, false)) return true; // le jeu refuse et fait clignoter la balise

            StationPanel.Open(view);
            result = true;
            return false;
        }

        private static void Rewrite(StationView view, Vector3 position, ref string text)
        {
            if (!StationRegistry.IsEnabled(view)) return;
            if (!PrivateArea.CheckAccess(position, 0f, false)) return;
            text = HoverText.Rewrite(text);
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
        private static class BeehiveInteract
        {
            private static bool Prefix(Beehive __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
                => TryOpen(StationRegistry.Get(__instance), __0, __1, __2, __instance.transform.position, ref __result);
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
        private static class BeehiveHover
        {
            private static void Postfix(Beehive __instance, ref string __result)
                => Rewrite(StationRegistry.Get(__instance), __instance.transform.position, ref __result);
        }

        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Interact))]
        private static class SapInteract
        {
            private static bool Prefix(SapCollector __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
                => TryOpen(StationRegistry.Get(__instance), __0, __1, __2, __instance.transform.position, ref __result);
        }

        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.GetHoverText))]
        private static class SapHover
        {
            private static void Postfix(SapCollector __instance, ref string __result)
                => Rewrite(StationRegistry.Get(__instance), __instance.transform.position, ref __result);
        }
    }
}
