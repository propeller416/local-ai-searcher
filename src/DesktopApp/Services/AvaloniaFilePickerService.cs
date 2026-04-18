using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;

namespace DesktopApp.Services;

public class AvaloniaFilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> OpenFilePickerAsync(string title, bool allowMultiple)
    {
        var application = Avalonia.Application.Current;
        if (application?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var storageProvider = TopLevel.GetTopLevel(mainWindow)?.StorageProvider;
                if (storageProvider != null)
                {
                    var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = title,
                        AllowMultiple = allowMultiple,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Documents")
                            {
                                Patterns = new[] { "*.txt", "*.md", "*.pdf", "*.docx" }
                            }
                        }
                    });

                    return result.Select(file => file.Path.LocalPath).ToList();
                }
            }
        }
        return Array.Empty<string>();
    }
}
