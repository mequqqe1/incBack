using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public record RegisterRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(6)] string Password,
    [param: Required] string Role,
    string? FullName
);

public record LoginRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required] string Password
);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserId,
    string Email,
    string? FullName,
    string[] Roles
);