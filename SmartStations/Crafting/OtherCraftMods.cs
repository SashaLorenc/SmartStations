using BepInEx.Bootstrap;

namespace SmartStations.Crafting
{
    /// <summary>
    /// Autres mods qui font déjà « fabriquer depuis les coffres ». S'ils sont là, on se retire :
    /// deux mods qui retirent les mêmes matériaux des coffres feraient payer deux fois.
    /// </summary>
    internal static class OtherCraftMods
    {
        private static readonly string[] Markers =
            { "craftfromcontainers", "craftfromchests", "craftfromstorage", "craftybox", "craftfromnearby", "nearbychests" };

        private static bool _checked;
        private static string _found;

        /// <summary>Nom du mod concurrent détecté, ou null.</summary>
        internal static string Found
        {
            get
            {
                if (_checked) return _found;
                _checked = true;
                foreach (var kv in Chainloader.PluginInfos)
                {
                    if (kv.Value == null || kv.Value.Metadata == null) continue;
                    if (kv.Key == Plugin.ModGuid) continue;
                    string text = (kv.Key + " " + kv.Value.Metadata.Name).ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
                    foreach (string marker in Markers)
                    {
                        if (!text.Contains(marker)) continue;
                        _found = kv.Value.Metadata.Name;
                        Plugin.Log.LogWarning($"« {_found} » est installé : fabrication depuis les coffres de Smart Stations désactivée pour éviter de payer deux fois.");
                        return _found;
                    }
                }
                return null;
            }
        }
    }
}
