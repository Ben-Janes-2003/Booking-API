using BookingApi.Controllers;
using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace BookingApi.Tests.UnitTests
{
    public class SlotsControllerUnitTests
    {
        private readonly BookingDbContext _context;
        private readonly SlotsController _controller;

        public SlotsControllerUnitTests()
        {
            DbContextOptions<BookingDbContext> options = new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new BookingDbContext(options);

            ILogger<SlotsController> logger = new Mock<ILogger<SlotsController>>().Object;

            _controller = new SlotsController(_context, logger);
        }

        [Fact]
        public async Task GetAvailableSlots_WhenCalled_ReturnsOnlyAvailableSlots()
        {
            _context.TimeSlots.AddRange(
                new TimeSlot { Id = 1, IsBooked = false, DurationMinutes = 60, StartTime = DateTime.UtcNow },
                new TimeSlot { Id = 2, IsBooked = true, DurationMinutes = 60, StartTime = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            ActionResult<IEnumerable<TimeSlotDto>> result = await _controller.GetAvailableSlots();

            List<TimeSlotDto> slots = Assert.IsAssignableFrom<List<TimeSlotDto>>(result.Value);
            Assert.Single(slots);
        }

        [Fact]
        public async Task CreateTimeSlot_WhenCalledByAdmin_ReturnsOk()
        {
            ClaimsPrincipal adminUser = new(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, "-1"),
                new(ClaimTypes.Role, "Admin")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = adminUser }
            };

            CreateTimeSlotDto newSlotDto = new()
            {
                StartTime = DateTime.UtcNow.AddDays(10),
                DurationMinutes = 60
            };

            IActionResult result = await _controller.CreateTimeSlot(newSlotDto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, _context.TimeSlots.Count());
        }
    }
}