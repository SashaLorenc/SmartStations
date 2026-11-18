namespace SmartStations.Stations
{
    /// <summary>Un type d'objet (nom du prefab, ex. « CopperOre ») et une quantité.</summary>
    public sealed class ItemStack
    {
        public string Prefab;
        public int Count;

        public ItemStack(string prefab, int count)
        {
            Prefab = prefab;
            Count = count;
        }
    }
}
