using UnityEngine;

namespace SmartStations.UI
{
    /// <summary>Rafraîchit la fenêtre et la ferme quand l'inventaire se ferme ou qu'on s'éloigne.</summary>
    internal sealed class StationPanelUpdater : MonoBehaviour
    {
        private float _timer;

        private void Update()
        {
            var station = StationPanel.Current;
            if (station == null) return;
            Player player = Player.m_localPlayer;
            InventoryGui gui = InventoryGui.instance;
            if (!station.IsValid || player == null || gui == null || !InventoryGui.IsVisible() || gui.IsContainerOpen()
                || Vector3.Distance(player.transform.position, station.Position) > Plugin.MaxDistance.Value)
            {
                StationPanel.Close();
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.25f;
            StationPanel.Refresh();
        }
    }
}
