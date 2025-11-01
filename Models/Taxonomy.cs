using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

// специализации (логопед, дефектолог и т.д.)
public class Specialization
{
    public int Id { get; set; }
    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;
    [MaxLength(400)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public List<SpecialistSpecialization> SpecialistSpecializations { get; set; } = new();
}

// навыки/умения (ABA, PECS и т.д.)
public class Skill
{
    public int Id { get; set; }
    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;
    [MaxLength(400)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public List<SpecialistSkill> SpecialistSkills { get; set; } = new();
}

public class SpecialistSpecialization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpecialistProfileId { get; set; }
    public SpecialistProfile SpecialistProfile { get; set; } = null!;
    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;
}

public class SpecialistSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpecialistProfileId { get; set; }
    public SpecialistProfile SpecialistProfile { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}