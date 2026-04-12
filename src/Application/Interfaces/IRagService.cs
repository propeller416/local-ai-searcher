using Application.DTOs;

namespace Application.Interfaces;

public interface IRagService
{
    Task<ChatResponse> AskAsync(string question);
}