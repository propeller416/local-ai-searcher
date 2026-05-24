using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces;

public class ChatSessionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
}

public interface IHistoryService
{
    Task SaveSessionAsync(string sessionId, string title, string content);
    Task<List<ChatSessionInfo>> GetSessionsAsync();
    Task<string> GetSessionContentAsync(string sessionId);
    Task DeleteSessionAsync(string sessionId);
}
