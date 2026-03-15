using System.ComponentModel.DataAnnotations;

namespace INCBack.Models;

public class ParentFavoriteSpecialist
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string ParentUserId { get; set; } = null!;
    [Required] public string SpecialistUserId { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
