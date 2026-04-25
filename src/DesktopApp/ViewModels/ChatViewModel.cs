using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Application.Interfaces;
using System.Threading.Tasks;

namespace DesktopApp.ViewModels;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public partial class ChatViewModel : ViewModelBase
{
    private readonly IRagService _ragService;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isThinking;

    public ChatViewModel(IRagService ragService, Infrastructure.Llama.LlamaConfig config)
    {
        _ragService = ragService;
        
        if (!System.IO.File.Exists(config.ChatModelPath) || !System.IO.File.Exists(config.EmbedModelPath))
        {
            Messages.Add(new ChatMessage 
            { 
                Role = "AI", 
                Text = "⚠️ Внимание: Модели не найдены! Пожалуйста, скачайте необходимые GGUF файлы в папку models. Без них приложение не сможет генерировать ответы." 
            });
        }
        else
        {
            Messages.Add(new ChatMessage { Role = "AI", Text = "Привет! Я Local AI Searcher. Задайте мне вопрос по вашим документам." });
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var question = UserInput;
        UserInput = string.Empty;

        Messages.Add(new ChatMessage { Role = "User", Text = question });
        
        IsThinking = true;

        var response = await _ragService.AskAsync(question);

        Messages.Add(new ChatMessage { Role = "AI", Text = response.Answer });

        IsThinking = false;
    }
}