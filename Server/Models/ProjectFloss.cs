using System.ComponentModel.DataAnnotations;
namespace Server.Models
{
    public class ProjectFloss
    {
        [Key]
        public int ProjectId { get; set; }
        [Key]
        public int FlossId { get; set; }

        [Required]
        public int Amount { get; set; }

        //Navigation Properties
        public Project Project { get; set; }
        public Floss Floss { get; set; }
    }
}
