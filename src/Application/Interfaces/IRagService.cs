namespace Application.Interfaces;

public interface IRagService
{
    IAsyncEnumerable<string> AskStreamAsync(string question);
}