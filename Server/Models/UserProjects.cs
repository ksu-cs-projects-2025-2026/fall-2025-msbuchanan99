using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class UserProjects
    {
        public int UserId { get; set; }


        public int ProjectId { get; set; }

        //Navigation Properties
        public User User { get; set; }
        public Project Project { get; set; }
    }
}
