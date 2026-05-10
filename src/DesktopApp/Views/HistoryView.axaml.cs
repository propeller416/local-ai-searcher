using Avalonia.Controls;
using DesktopApp.ViewModels;

namespace DesktopApp.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is HistoryViewModel vm)
        {
            _ = vm.LoadSessionsAsync();
        }
    }
}