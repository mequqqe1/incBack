// Contracts/DocumentDtos.cs
namespace SharpAuthDemo.Contracts;

public record UploadChildDocumentRequest(
    string FileName,
    string ContentBase64,
    long? SizeBytes
);

public record ChildDocumentMeta(
    Guid Id, string FileName, string ContentType, long SizeBytes, DateTime CreatedAtUtc, string UploadedByUserId
);