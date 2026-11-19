using System.Collections.Generic;
using SmartStations.AutoFill;
using UnityEngine;

namespace SmartStations.Crafting
{
    /// <summary>
    /// Coffres utilisables pour fabriquer ou construire, et ce qu'ils contiennent, mis en cache pour l'image en cours
    /// (le menu de fabrication redemande ces chiffres à chaque image).
    /// Mêmes règles que le remplissage auto : seulement les coffres dont notre partie est propriétaire et que
    /// personne n'a ouverts, donc ce qui est compté est exactement ce qui pourra être retiré, sans copie possible.
    /// </summary>
    internal static class CraftPool
    {
        private static int _frame = -1;
        private static Vector3 _origin;
        private static List<Container> _chests = new List<Container>();
        private static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        internal static bool CraftingEnabled => Plugin.CraftFromChests.Value && OtherCraftMods.Found == null;
        internal static bool BuildingEnabled => Plugin.BuildFromChests.Value && OtherCraftMods.Found == null;

        /// <summary>Centre de recherche : la station utilisée si le menu de fabrication est ouvert, sinon le joueur.</summary>
        internal static Vector3 OriginFor(Player player)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            if (station != null && InventoryGui.IsVisible()) return station.transform.position;
            return player.transform.position;
        }

        internal static List<Container> Chests(Player player, Vector3 origin)
        {
            if (_frame != Time.frameCount || (origin - _origin).sqrMagnitude > 0.01f)
            {
                _frame = Time.frameCount;
                _origin = origin;
                _counts.Clear();
                _chests = ContainerRegistry.Near(origin, Plugin.CraftRadius.Value, player);
            }
            return _chests;
        }

        /// <summary>Nombre d'objets (nom partagé, ex. « $item_wood ») dans les coffres autour de origin.</summary>
        internal static int Count(Player player, Vector3 origin, string sharedName)
        {
            List<Container> chests = Chests(player, origin);
            int n;
            if (_counts.TryGetValue(sharedName, out n)) return n;
            n = 0;
            foreach (Container c in chests) n += c.GetInventory().CountItems(sharedName);
            _counts[sharedName] = n;
            return n;
        }

        /// <summary>Retire jusqu'à amount objets des coffres, les plus proches d'abord. Renvoie le nombre retiré.</summary>
        internal static int Remove(Player player, Vector3 origin, string sharedName, int amount, int itemQuality)
        {
            int removed = 0;
            foreach (Container c in Chests(player, origin))
            {
                if (removed >= amount) break;
                Inventory inv = c.GetInventory();
                int have = inv.CountItems(sharedName, itemQuality);
                if (have <= 0) continue;
                int take = Mathf.Min(have, amount - removed);
                inv.RemoveItem(sharedName, take, itemQuality);
                removed += take;
            }
            Invalidate();
            return removed;
        }

        internal static void Invalidate()
        {
            _frame = -1;
            _counts.Clear();
        }
    }
}
