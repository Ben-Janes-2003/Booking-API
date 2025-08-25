using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingApi.Controllers;

/// <summary>
/// Manages user bookings within the system. All endpoints require authentication.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookingsController(BookingDbContext context, ILogger<BookingsController> logger) : BaseApiController
{
    private readonly BookingDbContext _context = context;
    private readonly ILogger<BookingsController> _logger = logger;

    /// <summary>
    /// Creates a new booking for the authenticated user.
    /// </summary>
    /// <remarks>
    /// The user must provide a valid JWT token. The request will fail if the requested TimeSlot is already booked or does not exist.
    /// </remarks>
    /// <param name="bookingDto">The ID of the time slot to be booked.</param>
    /// <returns>The newly created booking record.</returns>
    /// <response code="201">Returns the newly created booking.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the requested time slot does not exist.</response>
    /// <response code="409">If the time slot is no longer available.</response>
    [ProducesResponseType(typeof(Booking), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<IActionResult> CreateBooking(CreateBookingDto bookingDto)
    {
        try
        {
            TimeSlot? timeSlot = await _context.TimeSlots.FindAsync(bookingDto.TimeSlotId);
            if (timeSlot == null)
            {
                return NotFound("The requested time slot does not exist.");
            }
                
            if (timeSlot.IsBooked)
            {
                return Conflict("This time slot is no longer available.");
            }

            timeSlot.IsBooked = true;
            Booking booking = new()
            {
                UserId = CurrentUserId,
                TimeSlotId = timeSlot.Id
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a booking.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Gets a specific booking by its ID.
    /// </summary>
    /// <remarks>
    /// This will only return a booking if it belongs to the currently authenticated user.
    /// </remarks>
    /// <param name="id">The ID of the booking to retrieve.</param>
    /// <returns>The requested booking details.</returns>
    /// <response code="200">Returns the booking details.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If no booking is found with that ID for the current user.</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDetailsDto>> GetBooking(int id)
    {
        try
        {
            BookingDetailsDto? booking = await _context.Bookings
                .Where(b => b.Id == id && b.UserId == CurrentUserId)
                .Select(b => new BookingDetailsDto
                {
                    Id = b.Id,
                    TimeSlot = new TimeSlotDto
                    {
                        StartTime = b.TimeSlot.StartTime,
                        DurationMinutes = b.TimeSlot.DurationMinutes
                    }
                })
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                return NotFound();
            }

            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the booking.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Gets all bookings for the currently authenticated user.
    /// </summary>
    /// <returns>A list of the user's bookings.</returns>
    /// <response code="200">Returns the list of bookings.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [ProducesResponseType(typeof(IEnumerable<BookingDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpGet("my-bookings")]
    public async Task<ActionResult<IEnumerable<BookingDetailsDto>>> GetMyBookings()
    {
        try
        {
            List<BookingDetailsDto> bookings = await _context.Bookings
                .Where(b => b.UserId == CurrentUserId)
                .Include(b => b.TimeSlot)
                .Select(b => new BookingDetailsDto
                {
                    Id = b.Id,
                    TimeSlot = new TimeSlotDto
                    {
                        StartTime = b.TimeSlot.StartTime,
                        DurationMinutes = b.TimeSlot.DurationMinutes
                    }
                })
                .ToListAsync();

            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching user bookings.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
