namespace Client.Models
{
    public class FlossModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Number { get; set; }
        public string? HexColor { get; set; }
        public FlossModel() { }
        public FlossModel(int id)
        {
            Id = id; 
        }
    }
}
