using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TextOnline.Dtos;
using TextOnline.Logic;
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
        [HttpGet]
        public IActionResult GetRooms()
        {
            return Ok(_context.Rooms);
        }
        [HttpGet("{roomId}")]
        public IActionResult GetRooms(int roomId)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
                return NotFound();
            return Ok(room);
        }
        [HttpGet("{roomId}/Users")]
        public IActionResult GetUsersInRoom(int roomId)
        {
            if(_context.Rooms.FirstOrDefault(x => x.Id == roomId) == null) 
                return NotFound();
            var users=_context.UserRooms.Where(x => x.RoomId == roomId).Select(x=>x.UserId);
            return Ok(_context.Users.Where(x=>users.Contains(x.Id)));
        }
        [HttpGet("{roomId}/History")]
        public IActionResult GetRoomHistory(int roomId)
        {
            if(_context.Rooms.FirstOrDefault(x=>x.Id == roomId) == null) 
                return NotFound();
            return Ok(_context.SavedTexts.Where(x=>x.RoomId== roomId));
        }
        [HttpGet("{roomId}/Backup")]
        public IActionResult GetTextBackup(int roomId, [FromQuery] int version)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null) 
                return NotFound();
            var saves = _context.SavedTexts.Where(x => x.RoomId == room.Id).ToArray();
            if (version<0 || version>saves.Length)
                return BadRequest();
            var result= RoomService.GetTextVersion(room,saves , version);
            return Ok(result);
        }
        [Authorize, HttpPost("Create")]
        public IActionResult CreateRoom([FromBody] CreateRoomDto request)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            Room newRoom = new Room(request.Name,AuthService.GetHash(request.Password), userId);
            _context.Rooms.Add(newRoom);
            _context.SaveChanges();
            _context.UserRooms.Add(new UserRoom(userId, newRoom.Id));
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetRooms), new { newRoom.Id }, newRoom);
        }
        [Authorize,HttpPut("{roomId}/Backup")]
        public IActionResult DoTextBackup(int roomId, [FromQuery]int version)
        {
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            if(room == null) 
                return NotFound();
            var saves = _context.SavedTexts.Where(x => x.RoomId == room.Id).ToArray();
            if (version < 0 || version > saves.Length)
                return BadRequest();
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            if (room.CreatorId != userId)
                return Forbid();
            string result=RoomService.GetTextVersion(room, saves,version);
            room.Text = result;
            _context.SavedTexts.Add(new SavedText(room.Id, 0, room.Text, "delete", room.CreatorId, DateTime.UtcNow));
            _context.SavedTexts.Add(new SavedText(room.Id, 0, result, "add", room.CreatorId, DateTime.UtcNow));
            _context.SaveChanges();
            return Ok(new SavedText(room.Id, 0, result, "add", room.CreatorId, DateTime.UtcNow));
        }
        [Authorize,HttpPut("{roomId}/Enter")]
        public IActionResult EnterRoom(int roomId, [FromBody] EnterRoomDto request)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room==null) 
                return NotFound();
            if(request.Password!=null && room.Password!=AuthService.GetHash(request.Password))
                return Forbid();
            if(_context.UserRooms.FirstOrDefault(x=>x.UserId==userId && x.RoomId==room.Id) != null) 
                return BadRequest();
            _context.UserRooms.Add(new UserRoom(userId, roomId));
            _context.SaveChanges();
            return NoContent();
        }
        [Authorize,HttpPut("{roomId}/Exit")]
        public IActionResult ExitRoom(int roomId)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null) return NotFound();
            var user = _context.UserRooms.FirstOrDefault(x => x.UserId == userId && x.RoomId == room.Id);
            if (user == null) return BadRequest();
            if (room.CreatorId == user.UserId)
                _context.Rooms.Remove(room);
            _context.UserRooms.Remove(user);
            _context.SaveChanges();
            return NoContent();
        }
        [Authorize,HttpPut("{roomId}/Kick")]
        public IActionResult KickUser(int roomId,[FromQuery]int kickedUserId)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            Console.WriteLine(kickedUserId+" "+ roomId);
            var kickedUser = _context.UserRooms.FirstOrDefault(x => x.UserId == kickedUserId && x.RoomId==roomId);
            if (room == null || kickedUser==null) return NotFound();
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) return BadRequest();
            if (room.CreatorId != user.Id)
                return Forbid();
            _context.UserRooms.Remove(kickedUser);
            _context.SaveChanges();
            return NoContent();
        }
        [Authorize,HttpPut("{roomId}/Settings")]
        public IActionResult ChangeSettings(int roomId,[FromBody] CreateRoomDto request)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (room == null) return NotFound();
            if (user == null) return BadRequest();
            if (room.CreatorId != user.Id) return Forbid();
            if(request.Password != null)
                room.Password = request.Password;
            if (room.Name != null)
                room.Name = request.Name;
            _context.SaveChanges();
            return Ok(room);
        }
        [Authorize, HttpDelete("{roomId}/Delete")]
        public IActionResult DeleteRoom(int roomId)
        {
            var userId = AuthService.GetUserId(Request.Headers.Authorization!);
            var room=_context.Rooms.FirstOrDefault(x=>x.Id==roomId);
            if (room==null)
                return NotFound();
            if(room.CreatorId != userId)
                return Forbid();
            _context.Rooms.Remove(room);
            _context.SaveChanges();
            return Ok(room);
        }
    }
}
