namespace Application.Interfaces;

public class RagSource
{
    public string DocumentName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class RagResponseChunk
{
    public string Text { get; set; } = string.Empty;
    public List<RagSource>? Sources { get; set; }
}

public interface IRagService
{
    IAsyncEnumerable<RagResponseChunk> AskStreamAsync(string question);
}
