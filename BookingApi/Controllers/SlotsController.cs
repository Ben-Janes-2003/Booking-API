using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Enums;
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

        [HttpPost]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> CreateTimeSlot(CreateTimeSlotDto request)
        {
            try
            {
                if (request.StartTime < DateTime.UtcNow)
                {
                    return BadRequest("Start time cannot be in the past.");
                }
                if (request.DurationMinutes <= 0)
                {
                    return BadRequest("Duration must be a positive number.");
                }
                TimeSlot timeSlot = new()
                {
                    StartTime = request.StartTime,
                    DurationMinutes = request.DurationMinutes,
                    IsBooked = false
                };

                _context.TimeSlots.Add(timeSlot);
                await _context.SaveChangesAsync();

                return Ok(timeSlot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a time slot.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}