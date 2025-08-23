using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookingsController : BaseApiController
{
    private readonly BookingDbContext _context;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(BookingDbContext context, ILogger<BookingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

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
                return BadRequest("This time slot is no longer available.");
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
