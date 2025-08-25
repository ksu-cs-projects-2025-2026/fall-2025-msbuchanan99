using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class Material
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        public string Name { get; set; } = "";

        public int Amount { get; set; } = 0;

        public string UnitOne { get; set; } = "";

        public string UnitMany { get; set; } = "";

        [Column(TypeName = "nvarchar(24)")]
        public MaterialType Type { get; set; }

        public User? Owner { get; set; }

        public Floss? Floss { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModifiedOn { get; set; }

        public Material() { }
    }
}
