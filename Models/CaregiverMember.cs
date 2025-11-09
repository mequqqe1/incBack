// Models/CaregiverMember.cs
namespace INCBack.Models;

public enum CaregiverStatus { Pending = 0, Active = 1, Revoked = 2,Accepted = 4 }

public class CaregiverMember
{
    public Guid Id { get; set; }

    public Guid ParentProfileId { get; set; }
    public ParentProfile? ParentProfile { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Email { get; set; } = "";
    public string? Relation { get; set; }

    public bool IsAdmin { get; set; }
    public CaregiverStatus Status { get; set; } = CaregiverStatus.Pending;

    public DateTime InvitedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

