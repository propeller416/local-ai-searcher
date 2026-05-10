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
        var modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
        if (Directory.Exists(modelsDir))
        {
            var files = Directory.GetFiles(modelsDir, "*.gguf");
            foreach (var file in files)
            {
                AvailableModels.Add(Path.GetFileName(file));
            }
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
