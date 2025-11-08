using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharpAuthDemo.Models;

public class SpecialistProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = null!;

    [MaxLength(2000)]
    public string? About { get; set; }

    [Required, MaxLength(2)]
    public string CountryCode { get; set; } = "KZ";

    [Required, MaxLength(200)]
    public string City { get; set; } = null!;

    [Required, MaxLength(300)]
    public string AddressLine1 { get; set; } = null!;

    [MaxLength(300)]
    public string? AddressLine2 { get; set; }

    [MaxLength(120)]
    public string? Region { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public int? ExperienceYears { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal? PricePerHour { get; set; }

    [MaxLength(200)]
    public string? Telegram { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsEmailPublic { get; set; }

    public string? AvatarBase64 { get; set; }
    [MaxLength(100)]
    public string? AvatarMimeType { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
    [MaxLength(500)]
    public string? ModerationComment { get; set; }
    public DateTime? ModeratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public double AverageRating { get; set; }      // 0..5
    public int ReviewsCount { get; set; }

    public List<SpecialistDiploma> Diplomas { get; set; } = new();
    public List<SpecialistSpecialization> SpecialistSpecializations { get; set; } = new();
    public List<SpecialistSkill> SpecialistSkills { get; set; } = new();
    public string TimeZoneId { get; set; } = "Central Asia Standard Time"; 

}