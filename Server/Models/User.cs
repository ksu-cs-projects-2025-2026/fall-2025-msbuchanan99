using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [MaxLength(50)]
        public string Password { get; set; } = "";

        //1 is for admin, 2 is for user
        [Required]
        public int Role { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastModified { get; set; }

        public ICollection<UserFloss> UserFloss { get; set; } = [];
        public ICollection<UserProjects> UserProjects { get; set; } = [];

        //Floss and amount
        [NotMapped]
        public Dictionary<Floss, int> Floss =>
            UserFloss?.ToDictionary(uf => uf.Floss, uf => uf.Amount) ?? new Dictionary<Floss, int>();

        [NotMapped]
        public List<Project> Projects =>
            UserProjects?.Select(up => up.Project).ToList() ?? new List<Project>();
    }
}
