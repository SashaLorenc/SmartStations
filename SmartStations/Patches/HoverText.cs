namespace SmartStations.Patches
{
    internal static class HoverText
    {
        /// <summary>
        /// Remplace l'action du survol (« [E] Ajouter du minerai ») par « [E] Ouvrir » et déplace l'action
        /// d'origine sur Maj + E. La première ligne (nom et quantités) est gardée telle quelle.
        /// </summary>
        internal static string Rewrite(string vanilla)
        {
            if (string.IsNullOrEmpty(vanilla)) return vanilla;

            int nl = vanilla.IndexOf('\n');
            string head = nl < 0 ? vanilla : vanilla.Substring(0, nl);
            string action = null;
            if (nl >= 0)
            {
                string rest = vanilla.Substring(nl + 1);
                int end = rest.IndexOf('\n');
                if (end >= 0) rest = rest.Substring(0, end);
                int bracket = rest.IndexOf("] ");
                if (bracket >= 0) action = rest.Substring(bracket + 2);
            }

            string text = head + "\n" + Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $smartstations_open");
            if (!string.IsNullOrEmpty(action))
            {
                text += "\n" + Localization.instance.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ") + action;
            }
            return text;
        }
    }
}
