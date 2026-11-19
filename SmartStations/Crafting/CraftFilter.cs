using System.Collections.Generic;
using HarmonyLib;
using SmartStations.UI;

namespace SmartStations.Crafting
{
    /// <summary>Filtre du menu de fabrication : une catégorie (ou toutes) et un texte recherché dans le nom.</summary>
    internal static class CraftFilter
    {
        internal const string AllId = "";

        internal static string Selected = AllId;
        internal static string Search = "";

        internal static bool Matches(Recipe recipe)
        {
            if (recipe == null || recipe.m_item == null) return true;
            if (Selected != AllId && !RecipeCategories.Of(recipe).Contains(Selected)) return false;
            if (string.IsNullOrEmpty(Search)) return true;
            string name = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
            return name.ToLowerInvariant().Contains(Search.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Juste avant que le jeu remplisse la liste des recettes : on met à jour les boutons
        /// (seulement les catégories présentes à cette station) puis on retire ce qui ne correspond pas.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
        private static class UpdateRecipeListPatch
        {
            private static void Prefix(InventoryGui __instance, List<Recipe> __0)
            {
                if (!Plugin.CraftCategories.Value || __0 == null) return;

                var present = new HashSet<string>();
                foreach (Recipe r in __0)
                {
                    foreach (string id in RecipeCategories.Of(r)) present.Add(id);
                }
                if (Selected != AllId && !present.Contains(Selected)) Selected = AllId;
                CraftFilterBar.Show(__instance, present);

                if (Selected == AllId && string.IsNullOrEmpty(Search)) return;
                __0.RemoveAll(r => !Matches(r));
            }
        }

        /// <summary>Pendant la saisie de la recherche, l'inventaire ne réagit pas aux touches (E, Tab...).</summary>
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        private static class InventoryUpdatePatch
        {
            private static bool Prefix() => !UiKit.IsTyping;
        }

        /// <summary>Inventaire fermé : la recherche est effacée (la catégorie reste choisie).</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class HidePatch
        {
            private static void Postfix() => CraftFilterBar.ClearSearch();
        }
    }
}
