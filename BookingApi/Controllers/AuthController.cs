using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly ILogger<AuthController> _logger;
        
        public AuthController(BookingDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegistrationDto request)
        {
            try
            {
                bool userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);

                if (userExists)
                {
                    _logger.LogWarning("User registration failed: Email already exists.");
                    return BadRequest("Email already exists.");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                User user = new()
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = passwordHash
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok("User registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
