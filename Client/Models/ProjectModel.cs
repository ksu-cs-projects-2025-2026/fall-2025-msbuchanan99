namespace Client.Models
{
    public class ProjectModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? FileName { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime CompletionDate { get; set; }
        public int? KeyPage { get; set; }
        public int? Aida { get; set; }
    }
}
