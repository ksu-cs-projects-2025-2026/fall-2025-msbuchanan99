using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Server.Models
{
    public class Floss
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Number { get; set; } = "";

        [StringLength(6)]
        public string? HexColor { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        public DateTime LastModified { get; set; }

        public ICollection<UserFloss>? UserFloss { get; set; }
        public ICollection<UserProjects>? UserProjects { get; set; }
        public ICollection<ProjectFloss>? ProjectFloss { get; set; }
    }
}
