using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using TextOnline.Controllers;

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
                await ProcessRequest(buffer, receiveResult);
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            connectionRooms[room.Id].Remove(this);
            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
        private async Task ProcessRequest(byte[]? buffer,WebSocketReceiveResult receiveResult)
        {
            var request = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            Dictionary<string, JsonElement> dict;
            try { dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request); }
            catch { await Responce(webSocket, "Error", "Wrong JSON"); return; }
            switch (dict["event"].ToString())
            {
                case "login": await LoginRoom(dict); break; //"event":"login", "room_id":1, "token":"jwt"
                case "add_text": await AddText(dict); break; //"event":"add_text", "new_text":"abcd", "position":13
                case "delete_text": await DeleteText(dict); break; //"event":"delete_text", "length":15, "position":13
                case "move_cursor": await MoveCursor(dict); break; //"event":"move_cursor", "new_position":12
                default: await Responce(webSocket, "Error", "Bad Request"); break;
            }
        }
        private async Task LoginRoom(Dictionary<string, JsonElement> dict)
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
        private async Task AddText(Dictionary<string, JsonElement> dict)
        {
            var position = dict["position"].GetInt32();
            string newText = dict["new_text"].ToString();
            try { room.Text = room.Text.Insert(position, newText); }
            catch { await Responce(webSocket, "Error", "Wrong Add"); return; }
            CreateNewSave(position, newText,"add");
            foreach (var user in connectionRooms[room.Id])
            {
                await Responce(user.webSocket, "Text Updated", room.Text);
                if(user.cursorPos>= dict["position"].GetInt32())
                    user.cursorPos += dict["new_text"].ToString().Length;
            }
            await MoveUsersCursor();
        }
        private async Task DeleteText(Dictionary<string, JsonElement> dict)
        {
            int position = dict["position"].GetInt32();
            int length = dict["length"].GetInt32();
            string deletedText;
            try { deletedText = room.Text.Substring(position, length); }
            catch { await Responce(webSocket, "Error", "Wrong Delete"); return; }
            room.Text = room.Text.Remove(position, length);
            CreateNewSave(position, deletedText, "delete");
            foreach (var user in connectionRooms[room.Id])
            {
                await Responce(user.webSocket, "Text Updated", room.Text);
                if(user.cursorPos>= position + length)
                    user.cursorPos -= length;
                else if(user.cursorPos >= position)
                    user.cursorPos = position;
            }
            await MoveUsersCursor();
        }
        private void CreateNewSave(int position, string editedText, string textEvent)
        {
            TextController.context.SavedTexts.Add(new SavedText(room.Id, position, editedText, textEvent, userId, DateTime.UtcNow));
            TextController.context.SaveChanges();
        }
        private async Task MoveUsersCursor()
        {
            foreach (var user in connectionRooms[room.Id])
                foreach (var anotherUserCursor in connectionRooms[room.Id])
                    await Responce(user.webSocket, $"Cursor {anotherUserCursor.userId} Moved", anotherUserCursor.cursorPos + "");
        }
        private async Task MoveCursor(Dictionary<string, JsonElement> dict)
        {
            if (cursorPos < 0 || cursorPos > room.Text.Length)
                await Responce(webSocket, "Error", "Wrong Position");
            else
            {
                cursorPos = dict["new_position"].GetInt32();
                foreach (var user in connectionRooms[room.Id])
                    await Responce(user.webSocket, $"Cursor {userId} Moved", cursorPos + "");
            }
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
