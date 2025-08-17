using BookingApi.Data.Models;
using Microsoft.EntityFrameworkCore;
namespace BookingApi.Data
{
    public class BookingDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
        {
        }
    }
}
