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
        public byte[]? EncryptedPassword { get; set; }

        [Required]
        public UserType Role { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
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

        /// <summary>
        /// Encrypts simpleText and sets it as the password
        /// </summary>
        /// <param name="simpleText">Unencrypted text</param>
        /// <param name="key">key</param>
        /// <param name="iv">initialization vector</param>
        /// <returns></returns>
        public byte[] EncryptText (string simpleText, byte[] key, byte[] iv)
        {
            byte[] cipheredText;
            using(Aes aes = Aes.Create())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
                using MemoryStream memoryStream = new();
                using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);

                using (StreamWriter writer = new(cryptoStream))
                {
                    writer.Write(simpleText);
                }
                cipheredText = memoryStream.ToArray();

                cryptoStream.Close();
                memoryStream.Close();
            }

            return cipheredText;
        }

        public string DecryptPassword(byte[] key, byte[] iv)
        {
            string simpleText;
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                using MemoryStream memoryStream = new MemoryStream(EncryptedPassword);
                using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
                using StreamReader reader = new StreamReader(cryptoStream);

                simpleText = reader.ReadToEnd();

                reader.Close();
                cryptoStream.Close();
                memoryStream.Close();
            }
            return simpleText;
        }
    }

    public enum UserType
    {
        Admin = 1,
        User = 2,
        Anon = 3
    }
}
