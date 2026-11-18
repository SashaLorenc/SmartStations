using UnityEngine;

namespace SmartStations.Stations
{
    internal static class TimeText
    {
        /// <summary>« 45 s », « 12 min », « 1 h 05 ».</summary>
        internal static string Format(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            if (s < 60) return s + " s";
            int min = Mathf.CeilToInt(s / 60f);
            if (min < 60) return min + " min";
            return (min / 60) + " h " + (min % 60).ToString("00");
        }
    }
}
