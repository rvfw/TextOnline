using System.ComponentModel.DataAnnotations;

namespace TextOnline.Dtos
{
    public class CreateRoomDto
    {
        [Required]
        [StringLength(maximumLength: 32, MinimumLength = 4)]
        public string Name { get; set; }
        [StringLength(maximumLength: 32)]
        public string? Password { get; set; }
    }
    public class EnterRoomDto
    {
        [StringLength(maximumLength: 32)]
        public string? Password { get; set; }
    }
    public class RegisterDto
    {
        [Required]
        [StringLength(maximumLength: 16, MinimumLength = 4)]
        public string Username { get; set; }
        [Required]
        [StringLength(maximumLength: 16, MinimumLength = 4)]
        public string Password { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
    public class LoginDto
    {
        [Required]
        [StringLength(maximumLength: 16, MinimumLength = 4)]
        public string Password { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
