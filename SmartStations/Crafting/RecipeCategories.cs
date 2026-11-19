using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SmartStations.Crafting
{
    internal sealed class CraftCategory
    {
        public string Id;
        public string Label;   // déjà traduit
        public string Color;   // « RRGGBB »

        public CraftCategory(string id, string label, string color)
        {
            Id = id;
            Label = label;
            Color = color;
        }
    }

    /// <summary>
    /// Catégories des recettes pour filtrer le menu de fabrication : celles de Smart Chest s'il est installé
    /// (mêmes catégories et mêmes réglages d'objets que pour les coffres), sinon des catégories intégrées
    /// d'après le type d'objet du jeu.
    /// </summary>
    internal static class RecipeCategories
    {
        private sealed class Builtin
        {
            public string Id;
            public string Color;
            public string[] Types;
        }

        private static readonly Builtin[] Builtins =
        {
            new Builtin { Id = "weapons", Color = "E74C3C", Types = new[] { "OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft", "Bow", "Attach_Atgeir" } },
            new Builtin { Id = "armor", Color = "3498DB", Types = new[] { "Helmet", "Chest", "Legs", "Shoulder", "Hands", "Shield", "Utility", "Trinket" } },
            new Builtin { Id = "tools", Color = "B5B5B5", Types = new[] { "Tool", "Torch" } },
            new Builtin { Id = "ammo", Color = "E67E22", Types = new[] { "Ammo", "AmmoNonEquipable" } },
            new Builtin { Id = "food", Color = "2ECC71", Types = new[] { "Consumable" } },
            new Builtin { Id = "other", Color = "A0703C", Types = new string[0] },
        };

        internal static bool UsingSmartChest => Plugin.UseSmartChestCategories.Value && SmartChestBridge.Available;

        /// <summary>Toutes les catégories possibles, dans l'ordre d'affichage.</summary>
        internal static List<CraftCategory> All()
        {
            if (UsingSmartChest)
            {
                List<CraftCategory> fromSmartChest = SmartChestBridge.Categories();
                if (fromSmartChest != null) return fromSmartChest;
            }
            var list = new List<CraftCategory>();
            foreach (Builtin b in Builtins)
            {
                list.Add(new CraftCategory(b.Id, Localization.instance.Localize("$smartstations_cat_" + b.Id), b.Color));
            }
            return list;
        }

        /// <summary>Identifiants des catégories de l'objet fabriqué par cette recette.</summary>
        internal static List<string> Of(Recipe recipe)
        {
            var ids = new List<string>();
            if (recipe == null || recipe.m_item == null) return ids;

            if (UsingSmartChest)
            {
                List<string> fromSmartChest = SmartChestBridge.ItemCategories(DataOf(recipe));
                if (fromSmartChest != null) return fromSmartChest;
            }

            string type = recipe.m_item.m_itemData.m_shared.m_itemType.ToString();
            foreach (Builtin b in Builtins)
            {
                if (Array.IndexOf(b.Types, type) >= 0) { ids.Add(b.Id); return ids; }
            }
            ids.Add("other");
            return ids;
        }

        // Smart Chest reconnaît un objet par son prefab : on lui donne une copie qui le connaît
        private static readonly Dictionary<Recipe, ItemDrop.ItemData> _data = new Dictionary<Recipe, ItemDrop.ItemData>();

        private static ItemDrop.ItemData DataOf(Recipe recipe)
        {
            ItemDrop.ItemData data;
            if (_data.TryGetValue(recipe, out data) && data != null) return data;
            data = recipe.m_item.m_itemData.Clone();
            data.m_dropPrefab = recipe.m_item.gameObject;
            _data[recipe] = data;
            return data;
        }
    }

    /// <summary>Accès aux catégories de Smart Chest par réflexion (aucune dépendance obligatoire).</summary>
    internal static class SmartChestBridge
    {
        private static bool _init;
        private static MethodInfo _visible;
        private static MethodInfo _itemCategories;

        internal static bool Available
        {
            get
            {
                Init();
                return _visible != null && _itemCategories != null;
            }
        }

        private static void Init()
        {
            if (_init) return;
            _init = true;
            if (!Chainloader.PluginInfos.ContainsKey(Plugin.SmartChestGuid)) return;
            Type manager = AccessTools.TypeByName("SmartChest.Catalog.CatalogManager");
            if (manager == null) return;
            _visible = AccessTools.Method(manager, "VisibleCategories");
            _itemCategories = AccessTools.Method(manager, "GetItemCategories", new[] { typeof(ItemDrop.ItemData) });
            if (Available) Plugin.Log.LogInfo("Menu de fabrication : catégories de Smart Chest utilisées.");
        }

        internal static List<CraftCategory> Categories()
        {
            try
            {
                var list = new List<CraftCategory>();
                var raw = _visible.Invoke(null, null) as IEnumerable;
                if (raw == null) return null;
                foreach (object c in raw)
                {
                    Traverse t = Traverse.Create(c);
                    string id = t.Field("Id").GetValue<string>();
                    string label = t.Property("DisplayName").GetValue<string>();
                    string color = t.Field("Color").GetValue<string>();
                    if (string.IsNullOrEmpty(id)) continue;
                    list.Add(new CraftCategory(id, label ?? id, string.IsNullOrEmpty(color) ? "FFFFFF" : color));
                }
                return list;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Catégories de Smart Chest illisibles : " + e.Message);
                _visible = null;
                return null;
            }
        }

        internal static List<string> ItemCategories(ItemDrop.ItemData item)
        {
            try
            {
                return _itemCategories.Invoke(null, new object[] { item }) as List<string>;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Catégories de Smart Chest illisibles : " + e.Message);
                _itemCategories = null;
                return null;
            }
        }
    }
}
