using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public DocumentsViewModel DocumentsViewModel { get; }
    public ChatViewModel ChatViewModel { get; }

    public MainWindowViewModel(DocumentsViewModel documentsViewModel, ChatViewModel chatViewModel)
    {
        DocumentsViewModel = documentsViewModel;
        ChatViewModel = chatViewModel;
    }
}
