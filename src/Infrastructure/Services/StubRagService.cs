using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.Services;

public class StubRagService : IRagService
{
    public Task<ChatResponse> AskAsync(string question) =>
        Task.FromResult(new ChatResponse { Answer = $"Вы спросили: {question}" });
}