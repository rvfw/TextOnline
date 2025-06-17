using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextOnline.Models
{
    public class User
    {
        [Key]
        public int Id { get; init; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        [JsonIgnore]
        public IEnumerable<UserRoom> Rooms { get; set; }
        public User(string name, string email, string password)
        {
            Name = name;
            Email = email;
            Password = password;
        }
    }
}
