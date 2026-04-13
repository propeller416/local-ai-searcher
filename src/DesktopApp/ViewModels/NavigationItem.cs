using Material.Icons;

namespace DesktopApp.ViewModels;

public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public MaterialIconKind Icon { get; set; }
    public ViewModelBase ViewModel { get; set; } = null!;
}
