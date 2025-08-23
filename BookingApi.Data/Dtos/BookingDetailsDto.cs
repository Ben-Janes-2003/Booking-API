namespace BookingApi.Data.Dto;

public class BookingDetailsDto
{
    public int Id { get; set; }
    public TimeSlotDto TimeSlot { get; set; } = null!;
}