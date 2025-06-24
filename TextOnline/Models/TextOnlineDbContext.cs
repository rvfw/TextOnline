using Microsoft.EntityFrameworkCore;

namespace TextOnline.Models;
public class TextOnlineDbContext(DbContextOptions<TextOnlineDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<UserRoom> UserRooms { get; set; }
    public DbSet<SavedText> SavedTexts { get; set; }
}