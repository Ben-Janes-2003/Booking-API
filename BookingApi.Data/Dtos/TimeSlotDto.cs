namespace BookingApi.Data.Dto;

public class TimeSlotDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
}
