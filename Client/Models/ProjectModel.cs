using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class ProjectModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Name { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }

        [Range(0, int.MaxValue)]
        public int? KeyPage { get; set; }
        
        [Range(7,20)]
        public int? Aida { get; set; }
    }
}
