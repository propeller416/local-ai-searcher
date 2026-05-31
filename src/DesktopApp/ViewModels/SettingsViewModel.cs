using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Application.Interfaces;
using Application.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DesktopApp.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _chatModelFileName = string.Empty;

    [ObservableProperty]
    private string _embedModelFileName = string.Empty;

    [ObservableProperty]
    private float _temperature;

    [ObservableProperty]
    private int _maxTokens;

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    [ObservableProperty]
    private bool _enableLlm;

    [ObservableProperty]
    private bool _showSources;

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = new();

    [ObservableProperty]
    private bool _isRestartRequired;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
        LoadAvailableModels();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.LoadSettings();
        ChatModelFileName = settings.ChatModelFileName;
        EmbedModelFileName = settings.EmbedModelFileName;
        Temperature = settings.Temperature;
        MaxTokens = settings.MaxTokens;
        SystemPrompt = settings.SystemPrompt;
        EnableLlm = settings.EnableLlm;
        ShowSources = settings.ShowSources;
        IsRestartRequired = false;
    }

    private void LoadAvailableModels()
    {
        AvailableModels.Clear();
        var models = new System.Collections.Generic.HashSet<string>();

        // 1. Проверяем встроенные модели
        var bundledDir = Application.Helpers.AppPaths.GetBundledModelsDirectory();
        if (Directory.Exists(bundledDir))
        {
            var files = Directory.GetFiles(bundledDir, "*.gguf");
            foreach (var file in files)
            {
                models.Add(Path.GetFileName(file));
            }
        }

        // 2. Проверяем пользовательские модели
        var userDir = Application.Helpers.AppPaths.GetUserModelsDirectory();
        if (Directory.Exists(userDir))
        {
            var files = Directory.GetFiles(userDir, "*.gguf");
            foreach (var file in files)
            {
                models.Add(Path.GetFileName(file));
            }
        }

        foreach (var model in models.OrderBy(m => m))
        {
            AvailableModels.Add(model);
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var currentSettings = _settingsService.LoadSettings();
        
        var newSettings = new AppSettings
        {
            ChatModelFileName = ChatModelFileName,
            EmbedModelFileName = EmbedModelFileName,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            SystemPrompt = SystemPrompt,
            EnableLlm = EnableLlm,
            ShowSources = ShowSources
        };

        _settingsService.SaveSettings(newSettings);

        if (currentSettings.ChatModelFileName != newSettings.ChatModelFileName ||
            currentSettings.EmbedModelFileName != newSettings.EmbedModelFileName)
        {
            IsRestartRequired = true;
        }
    }
}
