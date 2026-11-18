using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>Stations chargées autour du joueur, avec une seule vue par station.</summary>
    public static class StationRegistry
    {
        private static readonly Dictionary<MonoBehaviour, StationView> _views = new Dictionary<MonoBehaviour, StationView>();

        public static StationView Get(Smelter smelter)
        {
            if (smelter == null) return null;
            StationView view;
            if (!_views.TryGetValue(smelter, out view))
            {
                view = new SmelterStation(smelter);
                _views[smelter] = view;
            }
            return view;
        }

        public static StationView Get(Fermenter fermenter)
        {
            if (fermenter == null) return null;
            StationView view;
            if (!_views.TryGetValue(fermenter, out view))
            {
                view = new FermenterStation(fermenter);
                _views[fermenter] = view;
            }
            return view;
        }

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
            return false;
        }

        [HarmonyPatch(typeof(Smelter), "Awake")]
        private static class SmelterAwake
        {
            private static void Postfix(Smelter __instance)
            {
                if (__instance.m_nview == null || __instance.m_nview.GetZDO() == null) return;
                StationRpc.RegisterOn(Get(__instance));
            }
        }

        [HarmonyPatch(typeof(Fermenter), "Awake")]
        private static class FermenterAwake
        {
            private static void Postfix(Fermenter __instance)
            {
                if (__instance.m_nview == null || __instance.m_nview.GetZDO() == null) return;
                StationRpc.RegisterOn(Get(__instance));
            }
        }
    }
}
