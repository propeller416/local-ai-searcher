using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private readonly string _historyDirectory;

    public HistoryService()
    {
        _historyDirectory = Application.Helpers.AppPaths.GetHistoryDirectory();
    }

    public async Task SaveSessionAsync(string sessionId, string title, string content)
    {
        var safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sessionId}_{safeTitle}.md";
        var filePath = Path.Combine(_historyDirectory, fileName);

        await File.WriteAllTextAsync(filePath, content);
    }

    public Task<List<ChatSessionInfo>> GetSessionsAsync()
    {
        var sessions = new List<ChatSessionInfo>();

        if (!Directory.Exists(_historyDirectory))
            return Task.FromResult(sessions);

        var files = Directory.GetFiles(_historyDirectory, "*.md");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('_', 2);
            
            if (parts.Length == 2)
            {
                var fileInfo = new FileInfo(file);
                sessions.Add(new ChatSessionInfo
                {
                    Id = parts[0],
                    Title = parts[1].Replace("_", " "),
                    CreatedAt = fileInfo.CreationTime,
                    FilePath = file
                });
            }
        }

        return Task.FromResult(sessions.OrderByDescending(s => s.CreatedAt).ToList());
    }

    public async Task<string> GetSessionContentAsync(string sessionId)
    {
        var sessions = await GetSessionsAsync();
        var session = sessions.FirstOrDefault(s => s.Id == sessionId);

        if (session != null && File.Exists(session.FilePath))
        {
            return await File.ReadAllTextAsync(session.FilePath);
        }

        return string.Empty;
    }

    public Task DeleteSessionAsync(string sessionId)
    {
        var files = Directory.GetFiles(_historyDirectory, $"{sessionId}_*.md");
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        
        return Task.CompletedTask;
    }
}
