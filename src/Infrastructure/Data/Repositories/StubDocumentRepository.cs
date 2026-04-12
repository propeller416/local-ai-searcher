using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Data.Repositories;

public class StubDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _docs = new()
    {
        new Document { Id = Guid.NewGuid(), Filename = "Руководство пользователя.pdf",   ContentType = "application/pdf",  Status = DocumentStatus.Completed, UploadedAt = DateTime.Now.AddDays(-3) },
        new Document { Id = Guid.NewGuid(), Filename = "Техническое задание.docx",        ContentType = "application/docx", Status = DocumentStatus.Completed, UploadedAt = DateTime.Now.AddDays(-1) },
        new Document { Id = Guid.NewGuid(), Filename = "Заметки по проекту.md",           ContentType = "text/markdown",    Status = DocumentStatus.Processing, UploadedAt = DateTime.Now.AddMinutes(-5) },
    };

    public Task<IEnumerable<Document>> GetAllAsync() => Task.FromResult(_docs.AsEnumerable());
    public Task DeleteAsync(Guid id) { _docs.RemoveAll(d => d.Id == id); return Task.CompletedTask; }
    public Task AddAsync(Document doc) { _docs.Add(doc); return Task.CompletedTask; }
}