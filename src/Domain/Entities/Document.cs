using Domain.Enums;

namespace Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
}