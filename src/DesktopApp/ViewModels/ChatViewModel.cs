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
        }
    }
}
