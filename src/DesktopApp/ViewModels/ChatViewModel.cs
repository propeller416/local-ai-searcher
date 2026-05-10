using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace DesktopApp.ViewModels;

public partial class SourceItem : ObservableObject
{
    [ObservableProperty]
    private string _documentName = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUser))]
    [NotifyPropertyChangedFor(nameof(IsAi))]
    private string _role = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSources))]
    private ObservableCollection<SourceItem> _sources = new();

    public bool IsUser => Role == "User";
    public bool IsAi => Role != "User";

    public bool HasSources => Sources.Any();

    public void UpdateHasSources()
    {
        OnPropertyChanged(nameof(HasSources));
    }
}

public partial class ChatViewModel : ViewModelBase
{
    private readonly IRagService _ragService;
    private readonly IHistoryService _historyService;
    private string _currentSessionId = Guid.NewGuid().ToString("N");
    private string _currentSessionTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isThinking;

    public ChatViewModel(IRagService ragService, IHistoryService historyService, Infrastructure.Llama.LlamaConfig config)
    {
        _ragService = ragService;
        _historyService = historyService;
        
        if (!System.IO.File.Exists(config.ChatModelPath) || !System.IO.File.Exists(config.EmbedModelPath))
        {
            Messages.Add(new ChatMessage 
            { 
                Role = "AI", 
                Text = "⚠️ Внимание: Модели не найдены! Пожалуйста, скачайте необходимые GGUF файлы в папку models. Без них приложение не сможет генерировать ответы." 
            });
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

        var aiMessage = new ChatMessage { Role = "AI", Text = string.Empty };
        Messages.Add(aiMessage);

        try
        {
            await foreach (var chunk in _ragService.AskStreamAsync(question))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    aiMessage.Text += chunk.Text;
                }

                if (chunk.Sources != null && chunk.Sources.Count > 0)
                {
                    foreach (var source in chunk.Sources)
                    {
                        aiMessage.Sources.Add(new SourceItem
                        {
                            DocumentName = source.DocumentName,
                            Text = source.Text,
                            IsExpanded = false
                        });
                    }
                    aiMessage.UpdateHasSources();
                }
            }
            
            if (string.IsNullOrWhiteSpace(aiMessage.Text) && !aiMessage.HasSources)
            {
                aiMessage.Text = "Пустой ответ модели.";
            }
        }
        catch (Exception ex)
        {
            aiMessage.Text = $"Произошла ошибка: {ex.Message}";
        }
        finally
        {
            IsThinking = false;
            await SaveHistoryAsync();
        }
    }

    private async Task SaveHistoryAsync()
    {
        if (Messages.Count == 0) return;

        if (string.IsNullOrEmpty(_currentSessionTitle))
        {
            var firstMessage = Messages.FirstOrDefault(m => m.IsUser)?.Text ?? "Новый чат";
            var words = firstMessage.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var titleText = string.Join(" ", words.Take(5));
            _currentSessionTitle = $"{DateTime.Now:yyyy-MM-dd HH-mm} {titleText}";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {_currentSessionTitle}");
        sb.AppendLine();

        foreach (var msg in Messages)
        {
            if (msg.IsUser)
            {
                sb.AppendLine("## Вы");
                sb.AppendLine(msg.Text);
            }
            else
            {
                sb.AppendLine("## AI");
                sb.AppendLine(msg.Text);
                
                if (msg.HasSources)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Источники:**");
                    foreach (var source in msg.Sources)
                    {
                        sb.AppendLine($"- {source.DocumentName}");
                    }
                }
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        await _historyService.SaveSessionAsync(_currentSessionId, _currentSessionTitle, sb.ToString());
    }
}
