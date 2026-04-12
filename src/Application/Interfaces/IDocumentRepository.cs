using Domain.Entities;

namespace Application.Interfaces;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync();
    Task DeleteAsync(Guid id);
    Task AddAsync(Document doc);
}