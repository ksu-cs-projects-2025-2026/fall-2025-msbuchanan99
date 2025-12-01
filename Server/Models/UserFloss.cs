using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class UserFloss
    {
        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Project")]
        public int FlossId { get; set; }

        [Required]
        public int Amount { get; set; }

        public UserFloss() { }
        public UserFloss(int userId, int flossId, int amount)
        {
            UserId = userId;
            FlossId = flossId;
            Amount = amount;
        }


        //Navigation properties
        public User? User { get; set; }
        public Floss? Floss { get; set; }
    }
}
