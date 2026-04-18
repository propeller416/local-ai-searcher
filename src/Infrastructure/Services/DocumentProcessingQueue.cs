using Application.Interfaces;
using System.Threading.Channels;

namespace Infrastructure.Services;

public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<Guid> _queue;

    public DocumentProcessingQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask QueueDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(documentId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueDocumentAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
