namespace Client.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; } //this is not hashed and will only be used for logging in. Will not store Password typically
        public string? ConfirmPassword { get; set; }
        public string? Role { get; set; }
        public decimal WasteFactor { get; set; }
    }
}
