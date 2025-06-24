using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TextOnline.Logic;
using TextOnline.Models;
using TextOnline.Dtos;
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
        [HttpPost("Register")]
        public IActionResult Register([FromBody] RegisterDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);
            if (_context.Users.FirstOrDefault(x => x.Email == request.Email) != null)
                return BadRequest();
            var user = new User(request.Username, request.Email, AuthService.GetHash(request.Password));
            _context.Users.Add(user);
            _context.SaveChanges();
            return CreatedAtAction(null, new { user.Id }, user);
        }
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            var userByEmail = _context.Users.FirstOrDefault(x => x.Email == login.Email);
            if (userByEmail == null || userByEmail.Password != AuthService.GetHash(login.Password))
                return BadRequest();

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("Id",userByEmail.Id.ToString()),
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
    }
}
