using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextOnline.Models
{
    public class SavedText
    {
        [Key]
        public int Id { get; init; }
        public int RoomId { get; set; }
        [JsonIgnore]
        public Room Room { get; set; }
        public string Text { get; set; }
        public int LastRedactorId { get; set; }
        public DateTime RedactedTime { get; set; }
        public SavedText(int roomId,string text, int lastRedactorId, DateTime redactedTime)
        {
            RoomId = roomId;
            Text = text;
            LastRedactorId = lastRedactorId;
            RedactedTime = redactedTime;
        }
    }
}
