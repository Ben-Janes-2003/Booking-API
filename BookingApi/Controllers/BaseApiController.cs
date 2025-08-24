using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingApi.Controllers;

/// <summary>
/// Serves as a base class for all API controllers in this project.
/// Provides shared functionality, accessing the current user's ID.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the ID of the currently authenticated user from their JWT token claims.
    /// </summary>
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
