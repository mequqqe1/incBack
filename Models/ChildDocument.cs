// Models/ChildDocument.cs
namespace INCBack.Models;

public class ChildDocument
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public Child? Child { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public string ContentBase64 { get; set; } = "";

    public string UploadedByUserId { get; set; } = default!;
    public ApplicationUser? UploadedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}