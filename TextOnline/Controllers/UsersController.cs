using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextOnline.Models;

namespace TextOnline.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly TextOnlineDbContext _context;
        public UsersController(TextOnlineDbContext context)
        {
            _context = context;
        }
        [HttpGet("{userId}/Rooms")]
        public IActionResult GetRoomsByUser(int userId)
        {
            if (_context.Users.FirstOrDefault(x => x.Id == userId) == null)
                return BadRequest();
            return Ok(_context.Rooms.Where(x => x.CreatorId == userId));
        }
        [HttpGet]
        public IActionResult GetUsers()
        {
            return Ok(_context.Users);
        }
    }
}
