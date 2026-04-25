using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.Services;

public class DocumentProcessingBackgroundService : BackgroundService
{
    private readonly IDocumentProcessingQueue _queue;
    private readonly IDocumentRepository _repository;
    private readonly DocumentProcessorService _processorService;
#pragma warning disable CS0618
    private readonly Lazy<ITextEmbeddingGenerationService> _embeddingService;
#pragma warning restore CS0618
    private readonly ILogger<DocumentProcessingBackgroundService> _logger;

#pragma warning disable CS0618
    public DocumentProcessingBackgroundService(
        IDocumentProcessingQueue queue,
        IDocumentRepository repository,
        DocumentProcessorService processorService,
        Lazy<ITextEmbeddingGenerationService> embeddingService,
        ILogger<DocumentProcessingBackgroundService> logger)
#pragma warning restore CS0618
    {
        _queue = queue;
        _repository = repository;
        _processorService = processorService;
        _embeddingService = embeddingService;
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

                    try
                    {
                        var text = await _processorService.ExtractTextAsync(doc.FilePath, doc.ContentType);
                        var textChunks = _processorService.ChunkText(text);

                        if (textChunks.Any())
                        {
                            var embeddings = await _embeddingService.Value.GenerateEmbeddingsAsync(textChunks, cancellationToken: stoppingToken);
                            
                            var documentChunks = new List<DocumentChunk>();
                            var embeddingsList = new List<float[]>();

                            for (int i = 0; i < textChunks.Count; i++)
                            {
                                documentChunks.Add(new DocumentChunk
                                {
                                    Id = Guid.NewGuid(),
                                    DocumentId = doc.Id,
                                    Text = textChunks[i],
                                    ChunkIndex = i
                                });
                                embeddingsList.Add(embeddings[i].ToArray());
                            }

                            await _repository.SaveChunksAsync(doc.Id, documentChunks, embeddingsList);
                        }

                        // Mark as completed
                        doc.Status = DocumentStatus.Completed;
                        await _repository.UpdateAsync(doc);
                        _logger.LogInformation("Document {DocumentId} processing completed.", documentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
                        doc.Status = DocumentStatus.Failed;
                        await _repository.UpdateAsync(doc);
                    }
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
