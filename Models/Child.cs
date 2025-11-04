// Models/Child.cs
using System.ComponentModel.DataAnnotations;

namespace INCBack.Models;

public enum Sex { Unknown = 0, Male = 1, Female = 2 }
public enum SupportLevel { Unknown = 0, Mild = 1, Moderate = 2, High = 3 }

public class Child
{
    public Guid Id { get; set; }

    public Guid ParentProfileId { get; set; }
    public ParentProfile? ParentProfile { get; set; }

    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [MaxLength(100)] public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public Sex Sex { get; set; } = Sex.Unknown;

    public SupportLevel SupportLevel { get; set; } = SupportLevel.Unknown;
    [MaxLength(300)] public string? PrimaryDiagnosis { get; set; }
    public bool NonVerbal { get; set; }
    [MaxLength(300)] public string? CommunicationMethod { get; set; }

    [MaxLength(500)] public string? Allergies { get; set; }
    [MaxLength(500)] public string? Medications { get; set; }

    [MaxLength(800)] public string? Triggers { get; set; }
    [MaxLength(800)] public string? CalmingStrategies { get; set; }

    [MaxLength(300)] public string? SchoolOrCenter { get; set; }
    [MaxLength(800)] public string? CurrentGoals { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ChildNote> Notes { get; set; } = new List<ChildNote>();
    public ICollection<ChildDocument> Documents { get; set; } = new List<ChildDocument>();
}