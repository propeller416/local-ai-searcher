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

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private bool _isUploading;

    public DocumentsViewModel(IDocumentRepository repository)
    {
        _repository = repository;
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
        IsUploading = true;

        // Stub upload process
        await Task.Delay(1500); 

        var newDoc = new Document
        {
            Id = Guid.NewGuid(),
            Filename = $"Новый_документ_{DateTime.Now.Ticks}.txt",
            ContentType = "text/plain",
            Status = Domain.Enums.DocumentStatus.Pending,
            UploadedAt = DateTime.Now
        };

        await _repository.AddAsync(newDoc);
        Documents.Add(newDoc);

        IsUploading = false;
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(Document document)
    {
        if (document == null) return;
        await _repository.DeleteAsync(document.Id);
        Documents.Remove(document);
    }
}