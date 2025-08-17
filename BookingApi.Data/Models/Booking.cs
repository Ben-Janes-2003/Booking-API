namespace BookingApi.Data.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TimeSlotId { get; set; }

        public User User { get; set; } = null!;
        public TimeSlot TimeSlot { get; set; } = null!;
    }
}