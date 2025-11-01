using Microsoft.AspNetCore.Identity;

namespace INCBack.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}