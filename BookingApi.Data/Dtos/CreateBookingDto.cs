using System.ComponentModel.DataAnnotations;

namespace BookingApi.Data.Dto;

public class CreateBookingDto
{
    [Required]
    public int TimeSlotId { get; set; }
}