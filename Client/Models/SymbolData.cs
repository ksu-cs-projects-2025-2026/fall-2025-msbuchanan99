namespace Client.Models
{
    public class SymbolData
    {
        public FlossModel? Floss { get; set; }
        public int Count { get; set; }
        public SymbolData()
        {
            Count = 0;
            Floss = null;
        }
        public SymbolData(FlossModel floss)
        {
            Count = 0;
            Floss = floss;
        }
    }
}
