using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string? FileName { get; set; }

        [Required]
        public bool IsCompleted { get; set; }

        public DateTime? CompletionDate { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastModified { get; set; }

        public ICollection<ProjectFloss> ProjectFloss { get; set; } = [];

        [NotMapped]
        public Dictionary<Floss, int> Floss =>
            ProjectFloss?.ToDictionary(pf => pf.Floss, pf => pf.Amount) ?? new Dictionary<Floss, int>();
    }
}
