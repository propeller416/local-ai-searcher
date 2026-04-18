namespace Application.Interfaces;

public interface IFilePickerService
{
    Task<IReadOnlyList<string>> OpenFilePickerAsync(string title, bool allowMultiple);
}
