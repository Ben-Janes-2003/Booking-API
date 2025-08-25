using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Enums;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Controllers;

/// <summary>
/// Manages the creation and viewing of bookable time slots.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SlotsController(BookingDbContext context, ILogger<SlotsController> logger) : ControllerBase
{
    private readonly BookingDbContext _context = context;
    private readonly ILogger<SlotsController> _logger = logger;

    /// <summary>
    /// Retrieves a list of all currently available, unbooked time slots.
    /// </summary>
    /// <remarks>
    /// This is a public endpoint and does not require authentication.
    /// </remarks>
    /// <returns>A list of available time slots.</returns>
    /// <response code="200">Returns the list of available slots.</response>
    /// <response code="500">If an unexpected server error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TimeSlotDto>>> GetAvailableSlots()
    {
        try
        {
            List<TimeSlotDto> availableSlots = await _context.TimeSlots
                .Where(slot => !slot.IsBooked)
                .Select(slot => new TimeSlotDto
                {
                    Id = slot.Id,
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

    /// <summary>
    /// Creates a new time slot. (Admin Only)
    /// </summary>
    /// <remarks>
    /// This endpoint is protected and requires the user to have an "Admin" role.
    /// </remarks>
    /// <param name="request">The details of the time slot to be created.</param>
    /// <returns>The newly created time slot.</returns>
    /// <response code="200">Returns the newly created time slot object.</response>
    /// <response code="400">If the request data is invalid (e.g., start time in the past).</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpPost]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(typeof(TimeSlot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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