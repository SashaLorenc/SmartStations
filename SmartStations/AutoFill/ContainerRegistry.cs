using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmartStations.AutoFill
{
    /// <summary>Coffres chargés, et règles pour savoir si on peut y puiser sans risque.</summary>
    public static class ContainerRegistry
    {
        private static readonly HashSet<Container> _all = new HashSet<Container>();

        [HarmonyPatch(typeof(Container), "Awake")]
        private static class AwakePatch
        {
            private static void Postfix(Container __instance)
            {
                if (__instance != null) _all.Add(__instance);
            }
        }

        /// <summary>Coffres utilisables à moins de radius mètres de position, les plus proches d'abord.</summary>
        public static List<Container> Near(Vector3 position, float radius, Player player)
        {
            _all.RemoveWhere(c => c == null);
            var list = new List<Container>();
            float r2 = radius * radius;
            foreach (Container c in _all)
            {
                if ((c.transform.position - position).sqrMagnitude > r2) continue;
                if (!CanTakeFrom(c, player)) continue;
                list.Add(c);
            }
            list.Sort((a, b) => (a.transform.position - position).sqrMagnitude.CompareTo((b.transform.position - position).sqrMagnitude));
            return list;
        }

        /// <summary>
        /// Seulement un vrai coffre posé par un joueur, dont on est propriétaire (aucune copie possible
        /// en multijoueur), que personne n'a ouvert, et auquel le joueur a accès (coffre privé, balise).
        /// </summary>
        private static bool CanTakeFrom(Container c, Player player)
        {
            if (c.m_nview == null || !c.m_nview.IsValid() || !c.m_nview.IsOwner()) return false;
            if (c.GetInventory() == null) return false;
            if (c.IsInUse() || (c.m_wagon != null && c.m_wagon.InUse())) return false;
            if (c.GetComponent<TombStone>() != null || c.GetComponentInParent<Player>() != null) return false;
            if (c.GetComponentInParent<Ship>() != null) return false;

            Piece piece = c.m_piece != null ? c.m_piece : c.GetComponentInParent<Piece>();
            if (piece == null || !piece.IsPlacedByPlayer()) return false;

            if (!c.CheckAccess(player.GetPlayerID())) return false;
            if (c.m_checkGuardStone && !PrivateArea.CheckAccess(c.transform.position, 0f, false, false)) return false;
            return true;
        }
    }
}
