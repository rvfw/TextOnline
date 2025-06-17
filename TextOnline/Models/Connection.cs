using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using TextOnline.Controllers;
using Microsoft.AspNetCore.SignalR;

namespace TextOnline.Models
{
    public class Connection
    {
        readonly WebSocket webSocket;
        int userId;
        private Room room;
        public int cursorPos;
        static Dictionary<int, List<Connection>> connectionRooms = new();
        public Connection(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }
        public async Task GetRequests()
        {
            var buffer = new byte[1024*4];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!receiveResult.CloseStatus.HasValue)
            {
                var request = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var dict=JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request);
                switch (dict["event"].ToString())
                {
                    case "login": await LoginRoom(dict); break; //"event":"login", "room_id":1, "token":"jwt"
                    case "add_text": await AddText(dict); break; //"event":"add_text", "new_text":"abcd", "position":13
                    case "delete_text": await DeleteText(dict); break; //"event":"delete_text", "length":15, "position":13
                    case "move_cursor": await MoveCursor(dict); break; //"event":"move_cursor", "new_position":12
                    default: await Responce(webSocket, "Error", "Bad Request"); break;
                }
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            connectionRooms[room.Id].Remove(this);
            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
        async Task LoginRoom(Dictionary<string, JsonElement> dict)
        {
            int roomId = dict["room_id"].GetInt32();
            room=TextController.context.Rooms.FirstOrDefault(x => x.Id == roomId);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(dict["token"].ToString());
            userId = int.Parse(token.Claims.First(x => x.Type == "Id").Value);
            var user = TextController.context.UserRooms.FirstOrDefault(x => x.UserId == userId && x.RoomId == roomId);
            if (user == null)
                await Responce(webSocket, "Error", "Forbidden");
            else
            {
                if (!connectionRooms.ContainsKey(roomId))
                    connectionRooms.Add(roomId, new List<Connection>());
                connectionRooms[roomId].Add(this);
                await Responce(webSocket, "Success", TextController.context.Rooms.FirstOrDefault(x => x.Id == roomId).Text);
            }
        }
        async Task AddText(Dictionary<string, JsonElement> dict)
        {
            room.Text=room.Text.Insert(dict["position"].GetInt32(), dict["new_text"].ToString());
            TextController.context.SavedTexts.Add(new SavedText(room.Id,room.Text, userId, DateTime.UtcNow));
            TextController.context.SaveChanges();
            foreach (var user in connectionRooms[room.Id])
            {
                await Responce(user.webSocket, "Text Updated", room.Text);
                if(user.cursorPos>= dict["position"].GetInt32())
                    user.cursorPos += dict["new_text"].ToString().Length;
            }
            foreach (var user in connectionRooms[room.Id])
                foreach (var anotherUserCursor in connectionRooms[room.Id])
                    await Responce(user.webSocket, $"Cursor {anotherUserCursor.userId} Moved", anotherUserCursor.cursorPos + "");
        }
        async Task DeleteText(Dictionary<string, JsonElement> dict)
        {
            int pos = dict["position"].GetInt32();
            int len = dict["length"].GetInt32();
            room.Text = room.Text.Remove(pos, len);
            TextController.context.SavedTexts.Add(new SavedText(room.Id, room.Text, userId, DateTime.UtcNow));
            TextController.context.SaveChanges();
            foreach (var user in connectionRooms[room.Id])
            {
                await Responce(user.webSocket, "Text Updated", room.Text);
                if(user.cursorPos>= pos+len)
                    user.cursorPos -= len;
                else if(user.cursorPos >= pos)
                    user.cursorPos = pos;
            }
            foreach (var user in connectionRooms[room.Id])
                foreach (var anotherUserCursor in connectionRooms[room.Id])
                    await Responce(user.webSocket, $"Cursor {anotherUserCursor.userId} Moved", anotherUserCursor.cursorPos + "");
        }
        async Task MoveCursor(Dictionary<string, JsonElement> dict)
        {
            cursorPos=dict["new_position"].GetInt32();
            if (cursorPos < 0 || cursorPos > room.Text.Length)
                await Responce(webSocket, "Error", "Wrong Position");
            else
                foreach (var user in connectionRooms[room.Id])
                    await Responce(user.webSocket, $"Cursor {userId} Moved", cursorPos + "");
        }
        private async Task Responce(WebSocket ws, string type, string text)
        {
            var message = Encoding.UTF8.GetBytes($"{{\"event\": \"{type}\", \"message\": \"{text}\"}}");
            await ws.SendAsync(new ArraySegment<byte>(message, 0, message.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}
