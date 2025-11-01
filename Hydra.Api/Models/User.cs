namespace Hydra.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // Hashed password
    public UserRole Role { get; set; }  // Customer, Admin, etc.
}