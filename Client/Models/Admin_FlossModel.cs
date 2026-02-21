namespace Client.Models
{
    public class Admin_FlossModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Number { get; set; }
        public string? HexColor { get; set; }
        public int Amount { get; set; }
        public int? Strands { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastModified { get; set; }
    }
}
