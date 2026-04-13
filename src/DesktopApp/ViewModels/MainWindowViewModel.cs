using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace DesktopApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private NavigationItem _selectedItem = null!;

    public ObservableCollection<NavigationItem> MenuItems { get; } = new();

    [RelayCommand]
    private void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    public MainWindowViewModel(
        ChatViewModel chatViewModel, 
        DocumentsViewModel documentsViewModel,
        HistoryViewModel historyViewModel,
        SettingsViewModel settingsViewModel)
    {
        MenuItems.Add(new NavigationItem 
        { 
            Title = "Чат", 
            Icon = MaterialIconKind.Chat, 
            ViewModel = chatViewModel 
        });
        
        MenuItems.Add(new NavigationItem 
        { 
            Title = "Документы", 
            Icon = MaterialIconKind.FileDocument, 
            ViewModel = documentsViewModel 
        });
        
        MenuItems.Add(new NavigationItem 
        { 
            Title = "История", 
            Icon = MaterialIconKind.History, 
            ViewModel = historyViewModel 
        });
        
        MenuItems.Add(new NavigationItem 
        { 
            Title = "Настройки", 
            Icon = MaterialIconKind.Settings, 
            ViewModel = settingsViewModel 
        });

        SelectedItem = MenuItems[0];
    }
}
