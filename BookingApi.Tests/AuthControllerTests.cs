using BookingApi.Controllers;
using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Configuration;

namespace BookingApi.Tests
{
    public class AuthControllerTests
    {
        private readonly BookingDbContext _context;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            var options = new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new BookingDbContext(options);

            ILogger<AuthController> logger = new Mock<ILogger<AuthController>>().Object;
            Mock<IConfiguration> config = new();
            config.Setup(c => c.GetSection("Jwt:Key").Value).Returns("ThisIsMyTestSuperSecretKeyForMySuperSecureBookingApiApplication12345!");
            config.Setup(c => c.GetSection("Jwt:Issuer").Value).Returns("https://test-issuer.com");

            _controller = new AuthController(_context, logger, config.Object);
        }

        [Fact]
        public async Task Register_WithNewEmail_ReturnsOk()
        {
            UserRegistrationDto newUser = new()
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123"
            };

            IActionResult result = await _controller.Register(newUser);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            _context.Users.Add(new User { Name = "Existing User", Email = "existing@example.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            UserRegistrationDto newUser = new()
            {
                Name = "Another User",
                Email = "existing@example.com",
                Password = "password123"
            };

            IActionResult result = await _controller.Register(newUser);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk()
        {
            string password = "password123";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _context.Users.Add(new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = passwordHash
            });
            await _context.SaveChangesAsync();

            UserLoginDto loginRequest = new()
            {
                Email = "test@example.com",
                Password = password
            };

            IActionResult result = await _controller.Login(loginRequest);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            string password = "password123";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _context.Users.Add(new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = passwordHash
            });
            await _context.SaveChangesAsync();

            UserLoginDto loginRequest = new()
            {
                Email = "test@example.com",
                Password = "wrong-password"
            };

            IActionResult result = await _controller.Login(loginRequest);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}