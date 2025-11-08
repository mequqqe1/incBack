// Models/SpecialistReview.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCBack.Models;

public class SpecialistReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string SpecialistUserId { get; set; } = null!;   
    [Required] public string ParentUserId     { get; set; } = null!;  

    public Guid? BookingId { get; set; }                              
    public Booking? Booking { get; set; }

    [Range(1,5)]
    public int Rating { get; set; }                                     

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; } = false;                     
    public bool IsVisible   { get; set; } = true;                       

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}