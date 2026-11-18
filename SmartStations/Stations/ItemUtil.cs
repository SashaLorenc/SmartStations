using UnityEngine;

namespace SmartStations.Stations
{
    /// <summary>Accès aux prefabs d'objets : icône, nom affiché, création d'objets dans l'inventaire ou par terre.</summary>
    public static class ItemUtil
    {
        public static ItemDrop GetItemDrop(string prefab)
        {
            if (string.IsNullOrEmpty(prefab) || ObjectDB.instance == null) return null;
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            return go != null ? go.GetComponent<ItemDrop>() : null;
        }

        public static Sprite GetIcon(string prefab)
        {
            ItemDrop drop = GetItemDrop(prefab);
            return drop != null ? drop.m_itemData.GetIcon() : null;
        }

        public static string GetDisplayName(string prefab)
        {
            ItemDrop drop = GetItemDrop(prefab);
            return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : prefab;
        }

        /// <summary>Nom du prefab d'un objet d'inventaire (« CopperOre »), ou "" s'il n'en a pas.</summary>
        public static string PrefabName(ItemDrop.ItemData item)
        {
            return item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
        }

        /// <summary>
        /// Ajoute count objets dans l'inventaire du joueur, pile par pile.
        /// Ce qui ne rentre pas tombe à ses pieds : rien n'est jamais perdu.
        /// </summary>
        public static void GiveToPlayer(Player player, string prefab, int count)
        {
            ItemDrop drop = GetItemDrop(prefab);
            if (player == null || drop == null || count <= 0) return;

            Inventory inv = player.GetInventory();
            int total = count;
            int maxStack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            int dropped = 0;
            while (count > 0)
            {
                int chunk = Mathf.Min(count, maxStack);
                count -= chunk;
                if (inv.CanAddItem(drop.gameObject, chunk) && inv.AddItem(drop.gameObject, chunk)) continue;
                SpawnOnGround(drop, chunk, player.transform.position + player.transform.forward + Vector3.up);
                dropped += chunk;
            }

            string name = drop.m_itemData.m_shared.m_name;
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, total, drop.m_itemData.GetIcon());
            if (dropped > 0) player.Message(MessageHud.MessageType.Center, "$inventory_full");
        }

        /// <summary>Fait apparaître des objets par terre, pile par pile.</summary>
        public static void DropOnGround(string prefab, int count, Vector3 position)
        {
            ItemDrop drop = GetItemDrop(prefab);
            if (drop == null || count <= 0) return;
            int maxStack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            while (count > 0)
            {
                int chunk = Mathf.Min(count, maxStack);
                count -= chunk;
                SpawnOnGround(drop, chunk, position + Random.insideUnitSphere * 0.3f);
            }
        }

        private static void SpawnOnGround(ItemDrop prefab, int stack, Vector3 position)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
            ItemDrop created = Object.Instantiate(prefab.gameObject, position, rotation).GetComponent<ItemDrop>();
            created.m_itemData.m_stack = stack;
            ItemDrop.OnCreateNew(created);
        }
    }
}
