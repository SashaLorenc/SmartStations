using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;
using SmartStations.AutoFill;
using UnityEngine;

namespace SmartStations
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    // Smart Chest (optionnel) : ses catégories servent à trier le menu de fabrication
    [BepInDependency(SmartChestGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // Les RPC du mod (prendre un objet, activer le remplissage) doivent exister chez tout le monde
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "sashalorenc.SmartStations";
        public const string ModName = "Smart Stations";
        public const string ModVersion = "1.1.0";
        public const string SmartChestGuid = "sashalorenc.SmartChest";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // [Stations]
        public static ConfigEntry<bool> EnableSmelters;
        public static ConfigEntry<bool> EnableFermenters;
        public static ConfigEntry<bool> EnableCooking;
        public static ConfigEntry<bool> EnableCollectors;
        public static ConfigEntry<float> MaxDistance;

        // [Produits]
        public static ConfigEntry<bool> KeepProducts;
        public static ConfigEntry<int> ProductCapacity;

        // [RemplissageAuto]
        public static ConfigEntry<bool> AutoFillAllowed;
        public static ConfigEntry<bool> AutoFillDefault;
        public static ConfigEntry<float> AutoFillRadius;
        public static ConfigEntry<float> AutoFillInterval;

        // [Fabrication]
        public static ConfigEntry<bool> CraftFromChests;
        public static ConfigEntry<bool> BuildFromChests;
        public static ConfigEntry<float> CraftRadius;
        public static ConfigEntry<bool> CraftCategories;
        public static ConfigEntry<bool> UseSmartChestCategories;
        public static ConfigEntry<BarPlacement> FilterPlacement;
        public static ConfigEntry<float> FilterOffsetX;
        public static ConfigEntry<float> FilterOffsetY;

        private Harmony _harmony;
        private float _autoFillTimer;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            BindConfig();

            _harmony = new Harmony(ModGuid);
            PatchEach();

            Logger.LogInfo($"{ModName} {ModVersion} chargé.");
        }

        /// <summary>
        /// Comme PatchAll, mais patch par patch : si une mise à jour du jeu casse un patch, seul celui-là
        /// est perdu (avec un message dans le log) au lieu de tout le mod.
        /// </summary>
        private void PatchEach()
        {
            foreach (System.Type type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
            {
                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception e)
                {
                    Logger.LogError($"Patch {type.Name} impossible (version du jeu différente ?) : {e.GetBaseException().Message}");
                }
            }
        }

        private void Update()
        {
            if (!AutoFillAllowed.Value) return;
            _autoFillTimer += Time.deltaTime;
            if (_autoFillTimer < AutoFillInterval.Value) return;
            _autoFillTimer = 0f;
            AutoFillService.Tick();
        }

        private void BindConfig()
        {
            EnableSmelters = Config.Bind("Stations", "Fours", true,
                "Interface à emplacements pour les stations de type four : four à charbon, fonderie, haut fourneau, raffinerie d'eitr, moulin à vent, rouet...");
            EnableFermenters = Config.Bind("Stations", "Fermentateur", true,
                "Interface à emplacements pour le fermentateur.");
            EnableCooking = Config.Bind("Stations", "Cuisson", true,
                "Interface à emplacements pour le grill, la broche et le four à pain.");
            EnableCollectors = Config.Bind("Stations", "RucheEtSeve", true,
                "Fenêtre (case Produits) pour la ruche et le collecteur de sève.");
            MaxDistance = Config.Bind("Stations", "DistanceMax", 6f,
                new ConfigDescription("La fenêtre se ferme quand tu t'éloignes de la station de plus de cette distance (m).",
                    new AcceptableValueRange<float>(3f, 20f)));

            KeepProducts = Config.Bind("Produits", "GarderDansLaStation", true,
                "Les produits finis restent dans la case Produits de la station au lieu de tomber par terre. " +
                "Attention : si tu désinstalles le mod, les produits encore dans une station sont perdus, vide-les avant.");
            ProductCapacity = Config.Bind("Produits", "Capacite", 100,
                new ConfigDescription("Nombre maximum de produits gardés par station. Au-delà, ils tombent par terre comme d'habitude.",
                    new AcceptableValueRange<int>(10, 1000)));

            AutoFillAllowed = Config.Bind("RemplissageAuto", "Autorise", true,
                "Interrupteur général du remplissage automatique. Si false, le bouton n'apparaît plus et aucune station ne se remplit seule.");
            AutoFillDefault = Config.Bind("RemplissageAuto", "ActifParDefaut", false,
                "Remplissage auto des stations qui n'ont jamais été réglées. Par défaut chaque station est désactivée : " +
                "tu l'actives avec le bouton dans sa fenêtre (évite qu'un four à charbon mange tout ton bois).");
            AutoFillRadius = Config.Bind("RemplissageAuto", "Rayon", 10f,
                new ConfigDescription("Distance maximale (m) des coffres où une station va chercher minerais et combustible.",
                    new AcceptableValueRange<float>(1f, 50f)));
            AutoFillInterval = Config.Bind("RemplissageAuto", "Intervalle", 5f,
                new ConfigDescription("Délai (secondes) entre deux remplissages.", new AcceptableValueRange<float>(1f, 60f)));

            CraftFromChests = Config.Bind("Fabrication", "DepuisLesCoffres", true,
                "Les stations de fabrication (établi, forge, chaudron...) utilisent aussi les matériaux des coffres proches. " +
                "Désactivé tout seul si un autre mod du même genre est installé (Craft From Containers, AzuCraftyBoxes...).");
            BuildFromChests = Config.Bind("Fabrication", "ConstructionDepuisLesCoffres", true,
                "Le marteau (et la houe, le cultivateur) utilise aussi les matériaux des coffres proches du joueur.");
            CraftRadius = Config.Bind("Fabrication", "Rayon", 15f,
                new ConfigDescription("Distance maximale (m) des coffres utilisés, autour de la station (ou du joueur pour construire).",
                    new AcceptableValueRange<float>(1f, 50f)));
            CraftCategories = Config.Bind("Fabrication", "Categories", true,
                "Boutons de catégories et champ de recherche à côté du menu de fabrication.");
            UseSmartChestCategories = Config.Bind("Fabrication", "CategoriesSmartChest", true,
                "Si Smart Chest est installé, utilise ses catégories (et tes réglages d'objets) au lieu des catégories intégrées.");
            FilterPlacement = Config.Bind("Fabrication", "PositionCategories", BarPlacement.Droite,
                "Où placer les boutons de catégories autour du menu de fabrication : Dessus, Dessous, Gauche, Droite.");
            FilterOffsetX = Config.Bind("Fabrication", "DecalageX", 0f, "Décalage horizontal des catégories en pixels (positif = vers la droite).");
            FilterOffsetY = Config.Bind("Fabrication", "DecalageY", 0f, "Décalage vertical des catégories en pixels (positif = vers le haut).");
            FilterPlacement.SettingChanged += (_, __) => UI.CraftFilterBar.Reset();
            FilterOffsetX.SettingChanged += (_, __) => UI.CraftFilterBar.Reset();
            FilterOffsetY.SettingChanged += (_, __) => UI.CraftFilterBar.Reset();
            CraftCategories.SettingChanged += (_, __) => UI.CraftFilterBar.Reset();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
