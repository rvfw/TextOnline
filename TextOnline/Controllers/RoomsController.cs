using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using TextOnline.Models;

namespace TextOnline.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : Controller
    {
        private readonly TextOnlineDbContext _context;
        public RoomsController(TextOnlineDbContext context)
        {
            _context = context;
        }
        [HttpGet("Get")]
        public IActionResult GetRooms([FromQuery] int? id)
        {
            if (id == null)
                return Ok(_context.Rooms);
            if (_context.Users.FirstOrDefault(x => x.Id == id) == null)
                return BadRequest();
            return Ok(_context.Rooms.Where(x => x.CreatorId == id));
        }
        [HttpGet("AllUsers")]
        public IActionResult GetUsers()
        {
            return Ok(_context.Users);
        }
        [HttpGet("Users")]
        public IActionResult GetUsersInRoom([FromQuery] int? id)
        {
            if(_context.Rooms.FirstOrDefault(x => x.Id == id) == null) return BadRequest();
            var users=_context.UserRooms.Where(x => x.RoomId == id).Select(x=>x.UserId);
            return Ok(_context.Users.Where(x=>users.Contains(x.Id)));
        }
        [HttpGet("History")]
        public IActionResult GetRoomHistory([FromQuery] int id)
        {
            return Ok(_context.SavedTexts.Where(x=>x.RoomId==id));
        }
        [HttpPost("Create")]
        public IActionResult CreateRoom([FromBody]RoomRequest request)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.Token);
            var userId = int.Parse(token.Claims.First(x => x.Type == "Id").Value);
            Room newRoom;
            if(request.Password==null) newRoom= new Room(request.Name, null, userId);
            else newRoom=new Room(request.Name,AuthController.GetHash( request.Password), userId);
            _context.Rooms.Add(newRoom);
            _context.SaveChanges();
            _context.UserRooms.Add(new UserRoom(userId, newRoom.Id));
            _context.SaveChanges();
            return CreatedAtAction(null, new {newRoom.Id}, newRoom);
        }
        [HttpPut("Enter")]
        public IActionResult EnterRoom([FromBody] RoomRequest request)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.Token);
            var userId = int.Parse(token.Claims.First(x => x.Type == "Id").Value);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == request.Id);
            if (room==null) return BadRequest();
            if(request.Password!=null && room.Password!=AuthController.GetHash(request.Password)) return Forbid();
            if(_context.UserRooms.FirstOrDefault(x=>x.UserId==userId && x.RoomId==room.Id) != null) return BadRequest();
            _context.UserRooms.Add(new UserRoom(userId, request.Id));
            _context.SaveChanges();
            return NoContent();
        }
        [HttpPut("Exit")]
        public IActionResult ExitRoom([FromBody] RoomRequest request)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.Token);
            var userId = int.Parse(token.Claims.First(x => x.Type == "Id").Value);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == request.Id);
            if (room == null) return BadRequest();
            var user = _context.UserRooms.FirstOrDefault(x => x.UserId == userId && x.RoomId == room.Id);
            if (user == null) return BadRequest();
            if (room.CreatorId == user.UserId)
                _context.Rooms.Remove(room);
            _context.UserRooms.Remove(user);
            _context.SaveChanges();
            return NoContent();
        }
        [HttpDelete("Delete")]
        public IActionResult DeleteRoom([FromBody] RoomRequest request)
        {
            var room=_context.Rooms.FirstOrDefault(x=>x.Id==request.Id);
            if (room==null)
                return BadRequest();
            if (!AuthController.CheckToken(request.Token,room.CreatorId))
                return Forbid();
            _context.Rooms.Remove(room);
            _context.SaveChanges();
            return Ok(room);
        }
    }
    public class RoomRequest
    {
        public string? Name { get; set; }
        public int Id { get; set; }
        public string? Password { get; set; }
        public string Token { get; set; }
    }
}
