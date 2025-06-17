using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextOnline.Models
{
    public class Room
    {
        [Key]
        public int Id { get; init; }
        public string Name { get; set; }
        public string Text { get; set; } = "";
        public string? Password { get; set; }
        public int CreatorId { get; set; }
        [JsonIgnore]
        public User Creator { get; set; }
        [JsonIgnore]
        public IEnumerable<UserRoom> Users { get; set; }
        [JsonIgnore]
        public IEnumerable<SavedText> Saves { get; set; }
        public Room(string name, string? password, int creatorId)
        {
            Name = name;
            Password = password;
            CreatorId = creatorId;
        }
    }
    public sealed class RoomTypeConfiguraion : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder
               .HasOne(x => x.Creator)
               .WithMany()
               .HasForeignKey(x => x.CreatorId);
            builder.HasMany(x => x.Saves)
                .WithOne(x => x.Room)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
