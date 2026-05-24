using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using DesktopApp.ViewModels;
using DesktopApp.Views;
using DesktopApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Services;
using Domain.Entities;

namespace DesktopApp;

public partial class App : Avalonia.Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(configure => configure.AddConsole());

        // Register Database Service
        services.AddSingleton<SqliteDatabaseService>(sp => new SqliteDatabaseService("local_ai_searcher.db"));
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<SqliteDatabaseService>());

        services.AddLlamaAndSemanticKernelServices();

        // Register App Services
        services.AddSingleton<IDocumentRepository, SqliteDocumentRepository>();
        services.AddSingleton<IRagService, KernelChatRagService>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddSingleton<DocumentProcessorService>();
        services.AddSingleton<DocumentProcessingBackgroundService>();
        services.AddSingleton<IHistoryService, HistoryService>();

        // Register ViewModels
        services.AddTransient<DocumentsViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        // Initialize Database
        var dbService = Services.GetRequiredService<IDatabaseService>();
        dbService.InitializeAsync().GetAwaiter().GetResult();

        // Start Background Service
        var bgService = Services.GetRequiredService<DocumentProcessingBackgroundService>();
        _ = bgService.StartAsync(System.Threading.CancellationToken.None);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
