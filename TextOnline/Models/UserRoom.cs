using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TextOnline.Models
{
    public class UserRoom
    {
        [Key]
        public int Id { get; init; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public UserRoom(int userId, int roomId)
        {
            UserId = userId;
            RoomId = roomId;
        }
    }
    public sealed class UserRoomTypeConfiguraion : IEntityTypeConfiguration<UserRoom>
    {
        public void Configure(EntityTypeBuilder<UserRoom> builder)
        {
            builder
               .HasOne(x => x.User)
               .WithMany(x => x.Rooms)
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(x => x.Room)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
