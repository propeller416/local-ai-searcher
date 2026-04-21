using Application.DTOs;
using Application.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.Services;

/// <summary>Пока без поиска по документам: ответ через локальную LLM (Semantic Kernel). RAG подключится позже.</summary>
public sealed class KernelChatRagService(IChatCompletionService chatCompletion) : IRagService
{
    public async Task<ChatResponse> AskAsync(string question)
    {
        try
        {
            var history = new ChatHistory("Ты — ассистент Local AI Searcher. Отвечай кратко и по делу на русском языке.");
            history.AddUserMessage(question);

            var contents = await chatCompletion.GetChatMessageContentsAsync(history).ConfigureAwait(false);
            var answer = contents is { Count: > 0 }
                ? contents[^1].Content ?? string.Empty
                : string.Empty;

            return new ChatResponse { Answer = string.IsNullOrWhiteSpace(answer) ? "Пустой ответ модели." : answer };
        }
        catch (Exception ex)
        {
            return new ChatResponse
            {
                Answer = "Не удалось получить ответ от модели. Проверьте, что в папке models/ лежат GGUF-файлы (см. docs/models-download.md). " +
                          $"Подробности: {ex.Message}",
            };
        }
    }
}
