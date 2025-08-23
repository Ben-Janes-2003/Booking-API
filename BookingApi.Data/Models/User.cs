using BookingApi.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Data.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Role Role { get; set; } = Role.User;
}