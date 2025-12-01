namespace Client.Models
{
    public class Admin_UserModel
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastModified { get; set; }
    }
}
