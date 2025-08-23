namespace BookingApi.Data.Models;

public class TimeSlot
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsBooked { get; set; }
}