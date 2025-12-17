using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using Server.Models;

namespace Server.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(25)]
        public string Username { get; set; } = "";

        [Required]
        public string? HashPassword { get; set; }

        [Required]
        public string? Role { get; set; }

        [Required]
        [Range(0,1)]
        public decimal WasteFactor { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        public DateTime LastModified { get; set; }

        public ICollection<UserFloss> UserFloss { get; set; } = [];

        //Floss and amount
        [NotMapped]
        public Dictionary<Floss, int> Floss =>
            UserFloss?.ToDictionary(uf => uf.Floss, uf => uf.Amount) ?? new Dictionary<Floss, int>();

        [NotMapped]
        public List<Project> Projects =>
            Projects.Where(p => p.UserId == Id).ToList() ?? new List<Project>();
    }
}
