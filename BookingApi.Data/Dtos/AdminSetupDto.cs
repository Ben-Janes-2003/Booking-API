using System.ComponentModel.DataAnnotations;

namespace BookingApi.Data.Dto
{
    public class AdminSetupDto
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = null!;

        [Required]
        public string SetupKey { get; set; } = null!;
    }
}
