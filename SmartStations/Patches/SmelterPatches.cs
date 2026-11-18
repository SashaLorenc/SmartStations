using HarmonyLib;
using SmartStations.Stations;
using SmartStations.UI;

namespace SmartStations.Patches
{
    /// <summary>Four, fonderie, raffinerie... : ouverture de la fenêtre et produits gardés dans la station.</summary>
    internal static class SmelterPatches
    {
        /// <summary>Vrai pendant que le jeu vide la station détruite : on laisse alors tout tomber par terre.</summary>
        private static bool _dropping;

        private static Smelter SmelterOf(Switch sw)
        {
            Smelter smelter = sw.GetComponentInParent<Smelter>();
            if (smelter == null) return null;
            if (sw != smelter.m_addOreSwitch && sw != smelter.m_addWoodSwitch && sw != smelter.m_emptyOreSwitch) return null;
            return smelter;
        }

        /// <summary>E sur une station : ouvre la fenêtre. Maj + E garde l'action d'origine du jeu.</summary>
        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        private static class SwitchInteract
        {
            private static bool Prefix(Switch __instance, Humanoid character, bool hold, bool alt, ref bool __result)
            {
                if (hold || alt || character == null || character != Player.m_localPlayer) return true;
                Smelter smelter = SmelterOf(__instance);
                if (smelter == null) return true;
                StationView view = StationRegistry.Get(smelter);
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
                Smelter smelter = SmelterOf(__instance);
                if (smelter == null || !StationRegistry.IsEnabled(StationRegistry.Get(smelter))) return;
                __result = HoverText.Rewrite(__result);
            }
        }

        /// <summary>Produit fini : rangé dans la case Produits au lieu d'apparaître par terre (côté propriétaire).</summary>
        [HarmonyPatch(typeof(Smelter), "Spawn")]
        private static class SmelterSpawn
        {
            private static bool Prefix(Smelter __instance, string ore, int stack)
            {
                if (_dropping || !Plugin.KeepProducts.Value || !Plugin.EnableSmelters.Value) return true;
                if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return true;

                Smelter.ItemConversion conv = __instance.GetItemConversion(ore);
                if (conv == null || conv.m_to == null) return true;

                ZDO zdo = __instance.m_nview.GetZDO();
                if (OutputStore.Total(zdo) + stack > Plugin.ProductCapacity.Value) return true; // plein : comme d'habitude

                OutputStore.Add(zdo, conv.m_to.gameObject.name, stack);
                __instance.m_produceEffects.Create(__instance.transform.position, __instance.transform.rotation);
                return false;
            }
        }

        /// <summary>Station détruite : les produits gardés tombent par terre avec le reste.</summary>
        [HarmonyPatch(typeof(Smelter), "DropAllItems")]
        private static class SmelterDropAll
        {
            private static void Prefix(Smelter __instance)
            {
                _dropping = true;
                if (__instance.m_nview == null || !__instance.m_nview.IsValid()) return;
                ZDO zdo = __instance.m_nview.GetZDO();
                foreach (ItemStack s in OutputStore.Read(zdo))
                {
                    ItemUtil.DropOnGround(s.Prefab, s.Count, __instance.transform.position + UnityEngine.Vector3.up);
                }
                OutputStore.Clear(zdo);
            }

            private static void Finalizer()
            {
                _dropping = false;
            }
        }
    }
}
