using Avalonia.Input;
using DesktopApp.ViewModels;

namespace DesktopApp.Views;

public partial class ChatView : Avalonia.Controls.UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                // Позволяем TextBox обработать Shift+Enter (перенос строки)
                return;
            }
            
            // Предотвращаем стандартное поведение (перенос строки)
            e.Handled = true;
            
            // Выполняем команду отправки сообщения
            if (DataContext is ChatViewModel vm && vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
            }
        }
    }
}