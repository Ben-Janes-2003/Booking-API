using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlotsController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly ILogger<SlotsController> _logger;

        public SlotsController(BookingDbContext context, ILogger<SlotsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeSlotDto>>> GetAvailableSlots()
        {
            try
            {
                List<TimeSlotDto> availableSlots = await _context.TimeSlots
                    .Where(slot => !slot.IsBooked)
                    .Select(slot => new TimeSlotDto
                    {
                        StartTime = slot.StartTime,
                        DurationMinutes = slot.DurationMinutes
                    })
                    .ToListAsync();

                return availableSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching available slots.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred. Please try again later.");
            }
        }
    }
}