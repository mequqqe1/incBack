// Models/ChildNote.cs
using System.ComponentModel.DataAnnotations;

namespace INCBack.Models;

public class ChildNote
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }

    [Required, MaxLength(1200)]
    public string Text { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}