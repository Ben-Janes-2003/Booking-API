using System.ComponentModel.DataAnnotations;

namespace BookingApi.Data.Dto;

public class CreateTimeSlotDto
{
    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DurationMinutes must be greater than zero.")]
    public int DurationMinutes { get; set; }
}