using BookingApi.Controllers;
using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookingApi.Tests
{
    public class SlotsControllerTests
    {
        private readonly BookingDbContext _context;
        private readonly SlotsController _controller;

        public SlotsControllerTests()
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
    }
}