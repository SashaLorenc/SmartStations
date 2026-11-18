using System.Collections.Generic;
using SmartStations.Stations;
using UnityEngine;

namespace SmartStations.AutoFill
{
    /// <summary>
    /// Remplit les stations activées avec les minerais et le combustible des coffres proches.
    /// Ne tourne que chez le propriétaire de la station ET des coffres : tout est modifié en local,
    /// donc aucun objet ne peut être copié ou perdu en multijoueur.
    /// </summary>
    public static class AutoFillService
    {
        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNet.instance == null) return;

            foreach (StationView station in StationRegistry.All())
            {
                if (!station.IsOwner || !StationRegistry.IsEnabled(station) || !station.AutoFill) continue;
                Fill(station, player);
            }
        }

        private static void Fill(StationView station, Player player)
        {
            int freeInput = station.FreeInputOwner();
            int freeFuel = station.FreeFuelOwner();
            if (freeInput <= 0 && freeFuel <= 0) return;

            List<Container> chests = ContainerRegistry.Near(station.Position, Plugin.AutoFillRadius.Value, player);
            if (chests.Count == 0) return;

            if (freeFuel > 0)
            {
                bool cheatedFuel;
                int got = TakeFromChests(chests, station.FuelPrefab, freeFuel, out cheatedFuel);
                if (got > 0) station.InsertFuelOwner(got);
            }

            if (freeInput > 0)
            {
                foreach (string prefab in station.InputPrefabs)
                {
                    bool cheated;
                    int got = TakeFromChests(chests, prefab, freeInput, out cheated);
                    if (got <= 0) continue;
                    station.InsertInputOwner(prefab, got, cheated);
                    freeInput -= got;
                    if (freeInput <= 0) break;
                }
            }
        }

        /// <summary>Retire jusqu'à count objets de ce prefab dans les coffres. Renvoie le nombre retiré.</summary>
        private static int TakeFromChests(List<Container> chests, string prefab, int count, out bool cheated)
        {
            cheated = false;
            if (string.IsNullOrEmpty(prefab) || count <= 0) return 0;
            int taken = 0;
            foreach (Container c in chests)
            {
                Inventory inv = c.GetInventory();
                // Copie : on modifie l'inventaire pendant le parcours
                var items = new List<ItemDrop.ItemData>(inv.GetAllItems());
                foreach (ItemDrop.ItemData item in items)
                {
                    if (taken >= count) return taken;
                    if (ItemUtil.PrefabName(item) != prefab) continue;
                    int n = Mathf.Min(item.m_stack, count - taken);
                    bool itemCheated = item.m_cheated;
                    if (!inv.RemoveItem(item, n)) continue;
                    taken += n;
                    cheated |= itemCheated;
                }
            }
            return taken;
        }
    }
}
