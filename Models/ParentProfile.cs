// Models/ParentProfile.cs
using System.ComponentModel.DataAnnotations;

namespace INCBack.Models;

public class ParentProfile
{
    public Guid Id { get; set; }

    [Required] public string UserId { get; set; } = default!; // владелец
    public ApplicationUser? User { get; set; }

    [MaxLength(100)] public string FirstName { get; set; } = "";
    [MaxLength(100)] public string LastName { get; set; } = "";
    [MaxLength(2)]   public string CountryCode { get; set; } = "";
    [MaxLength(200)] public string City { get; set; } = "";
    [MaxLength(300)] public string AddressLine1 { get; set; } = "";
    [MaxLength(300)] public string? AddressLine2 { get; set; }
    [MaxLength(40)]  public string? Phone { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<CaregiverMember> Members { get; set; } = new List<CaregiverMember>();
}