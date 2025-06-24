using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace TextOnline.Models
{
    public class SavedText
    {
        [Key]
        public int Id { get; init; }
        public int RoomId { get; set; }
        [JsonIgnore]
        public Room Room { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public string TextEvent { get; set; }
        public int UserId { get; set; }
        public DateTime RedactedTime { get; set; }
        public SavedText(int roomId, int position, string text, string textEvent, int userId, DateTime redactedTime)
        {
            RoomId = roomId;
            Position = position;
            Text = text;
            TextEvent=textEvent;
            UserId = userId;
            RedactedTime = redactedTime;
        }
    }
}
