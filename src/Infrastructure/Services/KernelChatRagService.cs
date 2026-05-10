using Application.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.Services;

/// <summary>RAG сервис: векторный поиск и генерация ответа через Semantic Kernel.</summary>
public sealed class KernelChatRagService(
    Lazy<IChatCompletionService> chatCompletion,
#pragma warning disable CS0618
    Lazy<ITextEmbeddingGenerationService> textEmbedding,
#pragma warning restore CS0618
    IDocumentRepository documentRepository,
    ISettingsService settingsService) : IRagService
{
    public async IAsyncEnumerable<RagResponseChunk> AskStreamAsync(string question)
    {
        var settings = settingsService.LoadSettings();
        
        ReadOnlyMemory<float> queryEmbedding = default;
        string? errorMessage = null;
        try
        {
#pragma warning disable CS0618
            var embeddings = await textEmbedding.Value.GenerateEmbeddingsAsync([question]).ConfigureAwait(false);
#pragma warning restore CS0618
            queryEmbedding = embeddings[0];
        }
        catch (Exception ex)
        {
            errorMessage = $"Не удалось сгенерировать эмбеддинг для вопроса. Проверьте модели. Подробности: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return new RagResponseChunk { Text = errorMessage };
            yield break;
        }

        List<string> similarChunks = new();
        try
        {
            similarChunks = await documentRepository.SearchSimilarChunksAsync(queryEmbedding.ToArray(), 5).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            errorMessage = $"Ошибка векторного поиска. Подробности: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return new RagResponseChunk { Text = errorMessage };
            yield break;
        }

        var contextText = string.Join("\n\n", similarChunks);
        var systemPrompt = $@"{settings.SystemPrompt}

Контекст:
{contextText}";

        if (!settings.EnableLlm)
        {
            // Если LLM отключена, просто возвращаем источники без текстового сообщения
        }
        else
        {
            var history = new ChatHistory(systemPrompt);
            history.AddUserMessage(question);

            var executionSettings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    { "temperature", settings.Temperature },
                    { "max_tokens", settings.MaxTokens }
                }
            };

            IAsyncEnumerable<StreamingChatMessageContent>? streamingResult = null;
            try
            {
                streamingResult = chatCompletion.Value.GetStreamingChatMessageContentsAsync(history, executionSettings);
            }
            catch (Exception ex)
            {
                errorMessage = $"Не удалось запустить генерацию ответа. Подробности: {ex.Message}";
            }

            if (errorMessage != null || streamingResult == null)
            {
                yield return new RagResponseChunk { Text = errorMessage ?? "Неизвестная ошибка при запуске генерации." };
            }
            else
            {
                await foreach (var chunk in streamingResult.ConfigureAwait(false))
                {
                    if (!string.IsNullOrEmpty(chunk.Content))
                    {
                        yield return new RagResponseChunk { Text = chunk.Content };
                    }
                }
            }
        }

        if (similarChunks.Count > 0)
        {
            var sources = new List<RagSource>();
            foreach (var chunk in similarChunks)
            {
                var docName = "Неизвестно";
                var text = chunk;

                if (chunk.StartsWith("[Документ: "))
                {
                    var endIndex = chunk.IndexOf("]\n");
                    if (endIndex != -1)
                    {
                        docName = chunk.Substring(11, endIndex - 11);
                        text = chunk.Substring(endIndex + 2);
                    }
                }
                
                sources.Add(new RagSource
                {
                    DocumentName = docName,
                    Text = text
                });
            }

            yield return new RagResponseChunk
            {
                Text = string.Empty,
                Sources = sources
            };
        }
    }
}
