using System.ComponentModel.DataAnnotations;

namespace BookingApi.Data.Dto
{
    public class CreateTimeSlotDto
    {
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public int DurationMinutes { get; set; }
    }
}
