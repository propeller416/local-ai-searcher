using System.Threading.Channels;

namespace Application.Interfaces;

public interface IDocumentProcessingQueue
{
    ValueTask QueueDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueDocumentAsync(CancellationToken cancellationToken = default);
}
