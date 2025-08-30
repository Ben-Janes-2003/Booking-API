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

/// <summary>
/// Manages user authentication, registration, and initial setup.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(BookingDbContext context, ILogger<AuthController> logger, IConfiguration configuration) : ControllerBase
{
    private readonly BookingDbContext _context = context;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Registers a new user in the system with the default "User" role.
    /// </summary>
    /// <param name="request">The user's details for registration.</param>
    /// <response code="201">User was registered successfully.</response>
    /// <response code="409">If a user with the same email already exists.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegistrationDto request)
    { 
        try
        {
            bool userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);

            if (userExists)
            {
                _logger.LogWarning("User registration failed: Email already exists.");
                return Conflict("Email already exists.");
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

            var response = new { user.Id, user.Name, user.Email };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user registration.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The user's login credentials.</param>
    /// <returns>A JWT token upon successful authentication.</returns>
    /// <response code="200">Returns the JWT token.</response>
    /// <response code="401">If the provided credentials are invalid.</response>
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

    /// <summary>
    /// Creates the initial administrative user.
    /// </summary>
    /// <remarks>
    /// This is a one-time use endpoint protected by a secret key. It should be called once during the initial setup of a new environment to create the first admin account.
    /// </remarks>
    /// <param name="request">The details for the new admin and the required setup key.</param>
    /// <response code="201">Admin user was created successfully.</response>
    /// <response code="401">If the provided setup key is invalid.</response>
    /// <response code="409">If an admin user already exists in the database.</response>
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
                return Conflict("An admin user already exists.");
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

            var response = new { adminUser.Id, adminUser.Name, adminUser.Email, adminUser.Role };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during admin setup.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}