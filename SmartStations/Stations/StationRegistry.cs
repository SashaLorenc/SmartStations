using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>Stations chargées autour du joueur, avec une seule vue par station.</summary>
    public static class StationRegistry
    {
        private static readonly Dictionary<MonoBehaviour, StationView> _views = new Dictionary<MonoBehaviour, StationView>();

        private static StationView GetOrCreate(MonoBehaviour key, Func<StationView> create)
        {
            if (key == null) return null;
            StationView view;
            if (!_views.TryGetValue(key, out view))
            {
                view = create();
                _views[key] = view;
            }
            return view;
        }

        public static StationView Get(Smelter s) => GetOrCreate(s, () => new SmelterStation(s));
        public static StationView Get(Fermenter f) => GetOrCreate(f, () => new FermenterStation(f));
        public static StationView Get(CookingStation c) => GetOrCreate(c, () => new CookingStationView(c));
        public static StationView Get(Beehive b) => GetOrCreate(b, () => new BeehiveStation(b));
        public static StationView Get(SapCollector s) => GetOrCreate(s, () => new SapCollectorStation(s));

        /// <summary>Stations encore valides (les détruites sont retirées au passage).</summary>
        public static List<StationView> All()
        {
            var dead = new List<MonoBehaviour>();
            var list = new List<StationView>();
            foreach (KeyValuePair<MonoBehaviour, StationView> kv in _views)
            {
                if (kv.Key == null) dead.Add(kv.Key);
                else if (kv.Value.IsValid) list.Add(kv.Value);
            }
            foreach (MonoBehaviour d in dead) _views.Remove(d);
            return list;
        }

        /// <summary>Est-ce une station gérée par le mod (et activée dans la config) ?</summary>
        public static bool IsEnabled(StationView view)
        {
            if (view == null || !view.IsValid) return false;
            if (view is SmelterStation) return Plugin.EnableSmelters.Value;
            if (view is FermenterStation) return Plugin.EnableFermenters.Value;
            if (view is CookingStationView) return Plugin.EnableCooking.Value;
            if (view is CollectorStation) return Plugin.EnableCollectors.Value;
            return false;
        }

        private static void Register(StationView view, ZNetView nview)
        {
            if (view == null || nview == null || nview.GetZDO() == null) return;
            StationRpc.RegisterOn(view);
        }

        [HarmonyPatch(typeof(Smelter), "Awake")]
        private static class SmelterAwake
        {
            private static void Postfix(Smelter __instance) => Register(Get(__instance), __instance.m_nview);
        }

        [HarmonyPatch(typeof(Fermenter), "Awake")]
        private static class FermenterAwake
        {
            private static void Postfix(Fermenter __instance) => Register(Get(__instance), __instance.m_nview);
        }

        [HarmonyPatch(typeof(CookingStation), "Awake")]
        private static class CookingAwake
        {
            private static void Postfix(CookingStation __instance) => Register(Get(__instance), __instance.m_nview);
        }

        [HarmonyPatch(typeof(Beehive), "Awake")]
        private static class BeehiveAwake
        {
            private static void Postfix(Beehive __instance) => Register(Get(__instance), __instance.m_nview);
        }

        [HarmonyPatch(typeof(SapCollector), "Awake")]
        private static class SapAwake
        {
            private static void Postfix(SapCollector __instance) => Register(Get(__instance), __instance.m_nview);
        }
    }
}
