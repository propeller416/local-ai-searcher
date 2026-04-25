using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Application.Interfaces;
using System.Threading.Tasks;
using System;

namespace DesktopApp.ViewModels;

public partial class DocumentsViewModel : ViewModelBase
{
    private readonly IDocumentRepository _repository;
    private readonly IFilePickerService _filePickerService;
    private readonly IDocumentProcessingQueue _processingQueue;

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public DocumentsViewModel(
        IDocumentRepository repository,
        IFilePickerService filePickerService,
        IDocumentProcessingQueue processingQueue)
    {
        _repository = repository;
        _filePickerService = filePickerService;
        _processingQueue = processingQueue;
        LoadDocumentsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        var docs = await _repository.GetAllAsync();
        Documents.Clear();
        foreach (var doc in docs)
        {
            Documents.Add(doc);
        }
    }

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        var files = await _filePickerService.OpenFilePickerAsync("Выберите документы", true);
        if (files == null || files.Count == 0)
            return;

        IsUploading = true;
        ErrorMessage = string.Empty;

        try
        {
            var appDataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "documents");
            if (!System.IO.Directory.Exists(appDataPath))
            {
                System.IO.Directory.CreateDirectory(appDataPath);
            }

            foreach (var filePath in files)
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                var newExtension = fileInfo.Extension.ToLowerInvariant();
                
                if (newExtension != ".pdf" && newExtension != ".docx" && newExtension != ".txt")
                {
                    ErrorMessage = $"Программа не умеет работать с форматом {newExtension}. Поддерживаются только: .pdf, .docx, .txt";
                    continue;
                }

                var newId = Guid.NewGuid();
                var destFileName = $"{newId}{newExtension}";
                var destFilePath = System.IO.Path.Combine(appDataPath, destFileName);

                // Копируем файл
                System.IO.File.Copy(filePath, destFilePath, true);

                var newDoc = new Document
                {
                    Id = newId,
                    Filename = fileInfo.Name,
                    FilePath = destFilePath,
                    ContentType = newExtension,
                    Status = Domain.Enums.DocumentStatus.Pending,
                    UploadedAt = DateTime.Now
                };

                await _repository.AddAsync(newDoc);
                Documents.Insert(0, newDoc);

                // Отправляем в очередь обработки
                await _processingQueue.QueueDocumentAsync(newDoc.Id);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка при загрузке: {ex.Message}";
            Console.WriteLine($"Upload failed: {ex}");
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(Document document)
    {
        if (document == null) return;
        await _repository.DeleteAsync(document.Id);
        Documents.Remove(document);
    }
}