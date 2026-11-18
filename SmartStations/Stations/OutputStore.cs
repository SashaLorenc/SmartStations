using System.Collections.Generic;
using System.Text;

namespace SmartStations.Stations
{
    /// <summary>
    /// Produits finis gardés dans la station (case Produits), stockés dans le ZDO de la station
    /// sous la forme « CopperBar:12;TinBar:5 ». Seul le propriétaire du ZDO écrit dedans.
    /// </summary>
    public static class OutputStore
    {
        public const string ZdoKey = "SmartStations_Out";

        public static List<ItemStack> Read(ZDO zdo)
        {
            var list = new List<ItemStack>();
            if (zdo == null) return list;
            string raw = zdo.GetString(ZdoKey, "");
            if (string.IsNullOrEmpty(raw)) return list;

            foreach (string part in raw.Split(';'))
            {
                int sep = part.LastIndexOf(':');
                if (sep <= 0) continue;
                int count;
                if (!int.TryParse(part.Substring(sep + 1), out count) || count <= 0) continue;
                list.Add(new ItemStack(part.Substring(0, sep), count));
            }
            return list;
        }

        public static int Total(ZDO zdo)
        {
            int total = 0;
            foreach (ItemStack s in Read(zdo)) total += s.Count;
            return total;
        }

        public static void Add(ZDO zdo, string prefab, int count)
        {
            if (zdo == null || string.IsNullOrEmpty(prefab) || count <= 0) return;
            List<ItemStack> list = Read(zdo);
            ItemStack found = null;
            foreach (ItemStack s in list) if (s.Prefab == prefab) { found = s; break; }
            if (found != null) found.Count += count;
            else list.Add(new ItemStack(prefab, count));
            Write(zdo, list);
        }

        /// <summary>Retire jusqu'à count objets de ce type. Renvoie le nombre réellement retiré.</summary>
        public static int Remove(ZDO zdo, string prefab, int count)
        {
            if (zdo == null || count <= 0) return 0;
            List<ItemStack> list = Read(zdo);
            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Prefab != prefab) continue;
                int take = System.Math.Min(count - removed, list[i].Count);
                list[i].Count -= take;
                removed += take;
                if (list[i].Count <= 0) list.RemoveAt(i);
                if (removed >= count) break;
            }
            if (removed > 0) Write(zdo, list);
            return removed;
        }

        public static void Clear(ZDO zdo)
        {
            if (zdo != null) zdo.Set(ZdoKey, "");
        }

        private static void Write(ZDO zdo, List<ItemStack> list)
        {
            var sb = new StringBuilder();
            foreach (ItemStack s in list)
            {
                if (s.Count <= 0) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(s.Prefab).Append(':').Append(s.Count);
            }
            zdo.Set(ZdoKey, sb.ToString());
        }
    }
}
