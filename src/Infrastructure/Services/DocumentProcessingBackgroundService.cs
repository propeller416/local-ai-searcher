using Application.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DocumentProcessingBackgroundService : BackgroundService
{
    private readonly IDocumentProcessingQueue _queue;
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentProcessingBackgroundService> _logger;

    public DocumentProcessingBackgroundService(
        IDocumentProcessingQueue queue,
        IDocumentRepository repository,
        ILogger<DocumentProcessingBackgroundService> logger)
    {
        _queue = queue;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var documentId = await _queue.DequeueDocumentAsync(stoppingToken);

                _logger.LogInformation("Processing document with ID: {DocumentId}", documentId);

                var doc = await _repository.GetByIdAsync(documentId);
                if (doc != null)
                {
                    // Start processing
                    doc.Status = DocumentStatus.Processing;
                    await _repository.UpdateAsync(doc);

                    // TODO: Here will be text extraction, chunking, and vectorization (step 4).
                    // For now we just simulate work.
                    await Task.Delay(2000, stoppingToken);

                    // Mark as completed
                    doc.Status = DocumentStatus.Completed;
                    await _repository.UpdateAsync(doc);

                    _logger.LogInformation("Document {DocumentId} processing completed.", documentId);
                }
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing document processing.");
            }
        }
    }
}
