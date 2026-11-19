using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SmartStations.Crafting
{
    /// <summary>
    /// Fabriquer (établi, forge, chaudron...) et construire avec les matériaux des coffres proches.
    /// 1. « Ai-je de quoi ? » : si l'inventaire ne suffit pas, on ajoute ce que contiennent les coffres.
    /// 2. Au moment de payer : on retire d'abord des coffres ce qui manque dans l'inventaire,
    ///    puis le jeu retire le reste de l'inventaire comme d'habitude.
    /// 3. Affichage : « +N » en vert à côté de chaque matériau présent dans les coffres.
    /// </summary>
    internal static class CraftFromChestsPatches
    {
        /// <summary>
        /// Depuis Valheim 1.0 une recette peut avoir des matériaux « d'amélioration », comptés seulement à une
        /// station d'amélioration (et ignorés partout ailleurs) : même filtre que le jeu.
        /// </summary>
        private static bool AppliesAtStation(Piece.Requirement req, CraftingStation station)
        {
            if (req == null || req.m_resItem == null) return false;
            return station != null ? station.m_upgrader == req.m_upgraderResource : !req.m_upgraderResource;
        }

        /// <summary>Centre de recherche des coffres pour l'affichage en cours (null = pas d'ajout).</summary>
        private static Vector3? _displayOrigin;

        // ---------- Fabrication : ai-je de quoi ? ----------

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems), new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
        private static class HaveRequirementItemsPatch
        {
            private static void Postfix(Player __instance, ref bool __result, Recipe piece, bool discover, int qualityLevel, int amount)
            {
                if (__result || discover || !CraftPool.CraftingEnabled) return;
                if (__instance == null || __instance != Player.m_localPlayer || piece == null) return;
                // Recette « un seul des ingrédients » : le jeu va chercher l'objet dans l'inventaire du joueur
                // et planterait s'il n'y est pas. On ne s'en mêle pas.
                if (piece.m_requireOnlyOneIngredient) return;

                CraftingStation station = __instance.GetCurrentCraftingStation();
                Vector3 origin = CraftPool.OriginFor(__instance);
                if (CraftPool.Chests(__instance, origin).Count == 0) return;

                foreach (Piece.Requirement req in piece.m_resources)
                {
                    if (!AppliesAtStation(req, station)) continue;
                    int needed = req.GetAmount(qualityLevel) * amount;
                    if (needed <= 0) continue;
                    string name = req.m_resItem.m_itemData.m_shared.m_name;

                    // Comme le jeu : la meilleure pile d'une seule qualité dans l'inventaire, plus les coffres
                    int best = 0;
                    for (int q = 1; q < req.m_resItem.m_itemData.m_shared.m_maxQuality + 1; q++)
                    {
                        int c = __instance.m_inventory.CountItems(name, q);
                        if (c > best) best = c;
                    }
                    if (best + CraftPool.Count(__instance, origin, name) < needed) return;
                }
                __result = true;
            }
        }

        // ---------- Construction : ai-je de quoi ? ----------

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
        private static class HaveRequirementsPiecePatch
        {
            private static void Postfix(Player __instance, ref bool __result, Piece piece, Player.RequirementMode mode)
            {
                if (__result || !CraftPool.BuildingEnabled || piece == null) return;
                if (mode != Player.RequirementMode.CanBuild && mode != Player.RequirementMode.CanAlmostBuild) return;
                if (__instance == null || __instance != Player.m_localPlayer) return;

                // On ne change que la raison « matériaux manquants », jamais les autres conditions du jeu
                if (piece.m_craftingStation != null
                    && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, __instance.transform.position)
                    && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)) return;
                if (piece.m_dlc != null && piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc)) return;

                Vector3 origin = __instance.transform.position;
                if (CraftPool.Chests(__instance, origin).Count == 0) return;

                foreach (Piece.Requirement req in piece.m_resources)
                {
                    if (req.m_resItem == null || req.m_amount <= 0) continue;
                    string name = req.m_resItem.m_itemData.m_shared.m_name;
                    int have = __instance.m_inventory.CountItems(name) + CraftPool.Count(__instance, origin, name);
                    if (mode == Player.RequirementMode.CanAlmostBuild ? have <= 0 : have < req.m_amount) return;
                }
                __result = true;
            }
        }

        // ---------- Payer ----------

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        private static class ConsumeResourcesPatch
        {
            private static void Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
            {
                if (__instance == null || __instance != Player.m_localPlayer || requirements == null) return;
                bool crafting = InventoryGui.IsVisible();
                if (crafting ? !CraftPool.CraftingEnabled : !CraftPool.BuildingEnabled) return;

                Vector3 origin = CraftPool.OriginFor(__instance);
                if (CraftPool.Chests(__instance, origin).Count == 0) return;
                CraftingStation station = crafting ? __instance.GetCurrentCraftingStation() : null;

                foreach (Piece.Requirement req in requirements)
                {
                    if (crafting ? !AppliesAtStation(req, station) : (req == null || req.m_resItem == null)) continue;
                    int needed = req.GetAmount(qualityLevel) * multiplier;
                    if (needed <= 0) continue;
                    string name = req.m_resItem.m_itemData.m_shared.m_name;
                    int missing = needed - __instance.m_inventory.CountItems(name, itemQuality);
                    if (missing > 0) CraftPool.Remove(__instance, origin, name, missing, itemQuality);
                }
                // Le jeu retire ensuite le reste de l'inventaire du joueur
            }
        }

        // ---------- Affichage ----------

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        private static class RequirementListPatch
        {
            private static void Prefix(Player player)
            {
                _displayOrigin = null;
                if (player == null || player != Player.m_localPlayer || !CraftPool.CraftingEnabled) return;
                _displayOrigin = CraftPool.OriginFor(player);
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => _displayOrigin = null;
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
        private static class PieceInfoPatch
        {
            private static void Prefix()
            {
                Player player = Player.m_localPlayer;
                _displayOrigin = player != null && CraftPool.BuildingEnabled ? (Vector3?)player.transform.position : null;
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => _displayOrigin = null;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        private static class RequirementPatch
        {
            [HarmonyPriority(Priority.Low)]
            private static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result)
            {
                if (!__result || !_displayOrigin.HasValue || req == null || req.m_resItem == null) return;
                if (player == null || player != Player.m_localPlayer) return;

                string name = req.m_resItem.m_itemData.m_shared.m_name;
                int stored = CraftPool.Count(player, _displayOrigin.Value, name);
                if (stored <= 0) return;

                Transform t = elementRoot.Find("res_amount");
                TMP_Text text = t != null ? t.GetComponent<TMP_Text>() : null;
                if (text == null) return;

                // Le jeu met le chiffre en rouge d'après l'inventaire seul : les coffres complètent
                int needed = req.GetAmount(quality) * craftMultiplier;
                if (player.GetInventory().CountItems(name) + stored >= needed) text.color = Color.white;
                text.text += " <color=#9BDB9B>+" + stored + "</color>";
            }
        }
    }
}
