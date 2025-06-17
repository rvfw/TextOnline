using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TextOnline.Models;

namespace TextOnline.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TextOnlineDbContext _context;
        private readonly IConfiguration _config;
        public AuthController(TextOnlineDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (request.Password.Length < 4 || request.Username.Length < 4 || request.Email.Length < 4) 
                return BadRequest();
            if (_context.Users.FirstOrDefault(x => x.Email == request.Email) != null)
                return BadRequest();
            var user = new User(request.Username, request.Email, GetHash(request.Password));
            _context.Users.Add(user);
            _context.SaveChanges();
            return CreatedAtAction(null, new { user.Id }, user);
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            var userByEmail = _context.Users.FirstOrDefault(x => x.Email == login.Email);
            if (userByEmail == null || userByEmail.Password != GetHash(login.Password))
                return BadRequest();

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
{
                new Claim("Id",userByEmail.Id.ToString()),
                new Claim("Password",GetHash(login.Password)),
                new Claim(ClaimTypes.Role, "User")
            };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    _config.GetValue<double>("Jwt:ExpiryInMinutes")),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        public static string GetHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        public static bool CheckToken(string token, int userId)
        {
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken? jwtToken;
            try{jwtToken = handler.ReadJwtToken(token);}
            catch (Exception ex){ return false;}
            if (jwtToken.Claims.First(x => x.Type == "Id").Value != userId.ToString())
                return false;
            return true;
        }
    }
    public class RegisterRequest
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
