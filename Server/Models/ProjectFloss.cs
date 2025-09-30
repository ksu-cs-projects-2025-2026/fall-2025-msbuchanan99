using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Server.Models
{
    public class ProjectFloss
    {
        [ForeignKey("Project")]
        public int ProjectId { get; set; }
        [ForeignKey("Floss")]
        public int FlossId { get; set; }

        [Required]
        public int Amount { get; set; }

        //Navigation Properties
        public Project Project { get; set; }
        public Floss Floss { get; set; }
    }
}
