using Application.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.Services;

/// <summary>RAG сервис: векторный поиск и генерация ответа через Semantic Kernel.</summary>
public sealed class KernelChatRagService(
    Lazy<IChatCompletionService> chatCompletion,
    Lazy<ITextEmbeddingGenerationService> textEmbedding,
    IDocumentRepository documentRepository) : IRagService
{
    public async IAsyncEnumerable<string> AskStreamAsync(string question)
    {
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
            yield return errorMessage;
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
            yield return errorMessage;
            yield break;
        }

        var contextText = string.Join("\n\n", similarChunks);
        var systemPrompt = $@"Ты — ассистент для поиска по документации. Отвечай на вопросы пользователя на русском языке, используя только предоставленный контекст. Если в контексте нет ответа, скажи об этом.
Обязательно добавляй в свой ответ ссылки на документы, из которых была взята информация. Ссылка должна быть в формате: [Документ: имя_файла].

Контекст:
{contextText}";

        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(question);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                { "temperature", 0.1 },
                { "max_tokens", 1024 }
            }
        };

        IAsyncEnumerable<StreamingChatMessageContent>? streamingResult = null;
        try
        {
            streamingResult = chatCompletion.Value.GetStreamingChatMessageContentsAsync(history, settings);
        }
        catch (Exception ex)
        {
            errorMessage = $"Не удалось запустить генерацию ответа. Подробности: {ex.Message}";
        }

        if (errorMessage != null || streamingResult == null)
        {
            yield return errorMessage ?? "Неизвестная ошибка при запуске генерации.";
            yield break;
        }

        await foreach (var chunk in streamingResult.ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }

        if (similarChunks.Count > 0)
        {
            yield return "\n\n---\n**Найденные фрагменты:**\n\n";
            for (int i = 0; i < similarChunks.Count; i++)
            {
                yield return $"{i + 1}. {similarChunks[i]}\n\n";
            }
        }
    }
}
