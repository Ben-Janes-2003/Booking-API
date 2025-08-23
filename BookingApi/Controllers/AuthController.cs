using BookingApi.Data;
using BookingApi.Data.Dto;
using BookingApi.Data.Enums;
using BookingApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookingApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(BookingDbContext context, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto request)
    {
        try
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            string token = CreateToken(user);
            return Ok(new { Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private string CreateToken(User user)
    {
        List<Claim> claims = new()
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(
            _configuration.GetSection("Jwt:Key").Value!));

        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha512Signature);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new(claims),
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = creds,
            Issuer = _configuration.GetSection("Jwt:Issuer").Value
        };

        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    [HttpPost("setup-admin")]
    public async Task<IActionResult> SetupAdmin(AdminSetupDto request)
    {
        try
        {
            string? setupKey = _configuration["AdminSetupKey"];
            if (string.IsNullOrEmpty(setupKey) || request.SetupKey != setupKey)
            {
                return Unauthorized("Invalid setup key.");
            }

            if (await _context.Users.AnyAsync(u => u.Role == Role.Admin))
            {
                return BadRequest("An admin user already exists.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            User adminUser = new()
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = Role.Admin
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            return Ok("Admin user created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during admin setup.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}