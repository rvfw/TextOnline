using Microsoft.AspNetCore.Mvc;
using TextOnline.Models;

namespace TextOnline.Controllers
{
    [ApiController]
    public class TextController : Controller
    {
        public static TextOnlineDbContext context;
        public TextController(TextOnlineDbContext context)
        {
            TextController.context = context;
        }
        [HttpGet("/api/ws")]
        public async Task Connect()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Connection connection = new(webSocket);
                await connection.GetRequests();
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
