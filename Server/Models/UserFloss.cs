using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class UserFloss
    {
        [Key]
        public int UserId { get; set; }

        [Key]
        public int FlossId { get; set; }

        [Required]
        public int Amount { get; set; }

        //Navigation properties
        public User User { get; set; }
        public Floss Floss { get; set; }
    }
}
