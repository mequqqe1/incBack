using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public class SpecialistDiploma
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = null!; // владелец

    public Guid? SpecialistProfileId { get; set; }
    public SpecialistProfile? SpecialistProfile { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(260)]
    public string? FileName { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public string Base64Data { get; set; } = null!;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}