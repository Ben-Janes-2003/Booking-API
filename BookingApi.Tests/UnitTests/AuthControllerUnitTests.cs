using BookingApi.Controllers;
using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Enums;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookingApi.Tests.UnitTests
{
    public class AuthControllerUnitTests
    {
        private readonly BookingDbContext _context;
        private readonly AuthController _controller;

        public AuthControllerUnitTests()
        {
            DbContextOptions<BookingDbContext> options = new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new BookingDbContext(options);

            ILogger<AuthController> logger = new Mock<ILogger<AuthController>>().Object;
            Mock<IConfiguration> config = new();
            config.Setup(c => c.GetSection("Jwt:Key").Value).Returns("ThisIsMyTestSuperSecretKeyForMySuperSecureBookingApiApplication12345!");
            config.Setup(c => c.GetSection("Jwt:Issuer").Value).Returns("https://test-issuer.com");
            config.Setup(c => c["AdminSetupKey"]).Returns("ThisIsAFakeKeyForTestingOnly");

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

        [Fact]
        public async Task SetupAdmin_WithCorrectKeyAndNoExistingAdmin_CreatesAdmin()
        {
            AdminSetupDto setupDto = new()
            {
                Name = "Admin",
                Email = "admin@example.com",
                Password = "AdminPassword123!",
                SetupKey = "ThisIsAFakeKeyForTestingOnly"
            };

            IActionResult result = await _controller.SetupAdmin(setupDto);

            Assert.IsType<OkObjectResult>(result);
            Assert.True(await _context.Users.AnyAsync(u => u.Role == Role.Admin));
        }

        [Fact]
        public async Task SetupAdmin_WithIncorrectKey_ReturnsUnauthorized()
        {
            AdminSetupDto setupDto = new()
            {
                Name = "Admin",
                Email = "admin@example.com",
                Password = "AdminPassword123!",
                SetupKey = "WRONG-KEY"
            };

            IActionResult result = await _controller.SetupAdmin(setupDto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task SetupAdmin_WhenAdminAlreadyExists_ReturnsBadRequest()
        {
            User existingAdmin = new()
            {
                Name = "Existing Admin",
                Email = "oldadmin@example.com",
                PasswordHash = "hash",
                Role = Role.Admin
            };
            _context.Users.Add(existingAdmin);
            await _context.SaveChangesAsync();

            AdminSetupDto setupDto = new()
            {
                Name = "New Admin",
                Email = "newadmin@example.com",
                Password = "AdminPassword123!",
                SetupKey = "ThisIsAFakeKeyForTestingOnly"
            };

            IActionResult result = await _controller.SetupAdmin(setupDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}