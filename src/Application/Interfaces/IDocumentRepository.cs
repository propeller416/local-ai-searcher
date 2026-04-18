using Domain.Entities;

namespace Application.Interfaces;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task AddAsync(Document doc);
    Task UpdateAsync(Document doc);
}