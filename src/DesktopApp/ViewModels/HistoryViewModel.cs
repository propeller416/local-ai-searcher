using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DesktopApp.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private ObservableCollection<ChatSessionInfo> _sessions = new();

    [ObservableProperty]
    private ChatSessionInfo? _selectedSession;

    [ObservableProperty]
    private string _selectedSessionContent = string.Empty;

    public HistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task LoadSessionsAsync()
    {
        var sessions = await _historyService.GetSessionsAsync();
        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }
    }

    partial void OnSelectedSessionChanged(ChatSessionInfo? value)
    {
        if (value != null)
        {
            LoadSessionContentAsync(value.Id).ConfigureAwait(false);
        }
        else
        {
            SelectedSessionContent = string.Empty;
        }
    }

    private async Task LoadSessionContentAsync(string sessionId)
    {
        SelectedSessionContent = await _historyService.GetSessionContentAsync(sessionId);
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(ChatSessionInfo? session)
    {
        if (session == null) return;

        await _historyService.DeleteSessionAsync(session.Id);
        Sessions.Remove(session);
        
        if (SelectedSession == session)
        {
            SelectedSession = null;
        }
    }
}
