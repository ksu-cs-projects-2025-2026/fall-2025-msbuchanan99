using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class UserProjects
    {
        [Key]
        public int UserId { get; set; }

        [Key]
        public int ProjectId { get; set; }

        //Navigation Properties
        public User User { get; set; }
        public Project Project { get; set; }
    }
}
