namespace Client.Models
{
    public class FlossInProjectModel
    {
        public int ProjectId { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Number { get; set; }
        public string? HexColor { get; set; }
        public int? Amount { get; set; }
        public int? Strands { get; set; }
    }
}
