using BookingApi.Controllers;
using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace BookingApi.Tests
{
    public class BookingsControllerTests
    {
        private readonly BookingDbContext _context;
        private readonly BookingsController _controller;

        public BookingsControllerTests()
        {
            DbContextOptions<BookingDbContext> options = new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new BookingDbContext(options);

            ILogger<BookingsController> logger = new Mock<ILogger<BookingsController>>().Object;

            _controller = new BookingsController(_context, logger);
        }

        [Fact]
        public async Task CreateBooking_WithAvailableSlot_ReturnsCreated()
        {
            User user = new() { Id = 1, Name = "Test User", Email = "test@test.com", PasswordHash = "hash" };
            TimeSlot timeSlot = new() { Id = 1, StartTime = DateTime.UtcNow, DurationMinutes = 60, IsBooked = false };
            _context.Users.Add(user);
            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            CreateBookingDto bookingDto = new() { TimeSlotId = timeSlot.Id };

            IActionResult result = await _controller.CreateBooking(bookingDto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateBooking_WithAlreadyBookedSlot_ReturnsBadRequest()
        {
            User user = new() { Id = 1, Name = "Test User", Email = "test@test.com", PasswordHash = "hash" };
            TimeSlot timeSlot = new() { Id = 1, StartTime = DateTime.UtcNow, DurationMinutes = 60, IsBooked = true };
            _context.Users.Add(user);
            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            CreateBookingDto bookingDto = new() { TimeSlotId = timeSlot.Id };

            IActionResult result = await _controller.CreateBooking(bookingDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateBooking_WithNonExistentSlot_ReturnsNotFound()
        {
            User user = new() { Id = 1, Name = "Test User", Email = "test@test.com", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            CreateBookingDto bookingDto = new() { TimeSlotId = 999 };

            IActionResult result = await _controller.CreateBooking(bookingDto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyBookings_WhenCalled_ReturnsOnlyBookingsForCurrentUser()
        {
            User user1 = new() { Id = 1, Name = "User One", Email = "user1@test.com", PasswordHash = "hash" };
            User user2 = new() { Id = 2, Name = "User Two", Email = "user2@test.com", PasswordHash = "hash" };

            TimeSlot slot1 = new() { Id = 1, IsBooked = true, DurationMinutes = 60, StartTime = DateTime.UtcNow };
            TimeSlot slot2 = new() { Id = 2, IsBooked = true, DurationMinutes = 60, StartTime = DateTime.UtcNow };

            _context.Users.AddRange(user1, user2);
            _context.TimeSlots.AddRange(slot1, slot2);
            _context.Bookings.AddRange(
                new Booking { Id = 1, UserId = user1.Id, TimeSlotId = slot1.Id },
                new Booking { Id = 2, UserId = user2.Id, TimeSlotId = slot2.Id }
            );
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user1.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            ActionResult<IEnumerable<BookingDetailsDto>> result = await _controller.GetMyBookings();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
            List<BookingDetailsDto> bookings = Assert.IsAssignableFrom<List<BookingDetailsDto>>(okResult.Value);
            Assert.Single(bookings);
        }

        [Fact]
        public async Task GetBooking_WithValidIdForCurrentUser_ReturnsOk()
        {
            User user = new() { Id = 1, Name = "Test User", Email = "test@test.com", PasswordHash = "hash" };
            TimeSlot timeSlot = new() { Id = 1, IsBooked = true, StartTime = DateTime.UtcNow, DurationMinutes = 60 };
            Booking booking = new() { Id = 1, UserId = user.Id, TimeSlotId = timeSlot.Id };

            _context.Users.Add(user);
            _context.TimeSlots.Add(timeSlot);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            ActionResult<BookingDetailsDto> result = await _controller.GetBooking(booking.Id);

            BookingDetailsDto resultDto = Assert.IsType<BookingDetailsDto>(result.Value);
            Assert.Equal(booking.Id, resultDto.Id);
        }

        [Fact]
        public async Task GetBooking_WithValidIdForDifferentUser_ReturnsNotFound()
        {
            User user1 = new() { Id = 1, Name = "User One", Email = "user1@test.com", PasswordHash = "hash" };
            User user2 = new() { Id = 2, Name = "User Two", Email = "user2@test.com", PasswordHash = "hash" };
            TimeSlot timeSlot = new() { Id = 1, IsBooked = true, StartTime = DateTime.UtcNow, DurationMinutes = 60 };
            Booking booking = new() { Id = 1, UserId = user2.Id, TimeSlotId = timeSlot.Id };

            _context.Users.AddRange(user1, user2);
            _context.TimeSlots.Add(timeSlot);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user1.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            ActionResult<BookingDetailsDto> result = await _controller.GetBooking(booking.Id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetBooking_WithNonExistentId_ReturnsNotFound()
        {
            User user = new() { Id = 1, Name = "Test User", Email = "test@test.com", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            }, "mock"));

            _controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            ActionResult<BookingDetailsDto> result = await _controller.GetBooking(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}