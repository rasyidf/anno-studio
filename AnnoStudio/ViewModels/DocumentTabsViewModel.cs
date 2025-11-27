using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AnnoStudio.Services;

namespace AnnoStudio.ViewModels;

/// <summary>
/// ViewModel for the document tabs control
/// </summary>
public partial class DocumentTabsViewModel : ObservableObject
{
    private readonly IDocumentManager _documentManager;

    [ObservableProperty]
    private LayoutDocument? _selectedDocument;

    public ObservableCollection<LayoutDocument> Documents => _documentManager.Documents;

    public DocumentTabsViewModel(IDocumentManager documentManager)
    {
        _documentManager = documentManager;
        
        // Sync selected document with active document
        _documentManager.ActiveDocumentChanged += (s, e) =>
        {
            SelectedDocument = e.Document;
        };
    }

    partial void OnSelectedDocumentChanged(LayoutDocument? value)
    {
        if (value != null && _documentManager.ActiveDocument != value)
        {
            _documentManager.ActiveDocument = value;
        }
    }

    [RelayCommand]
    private async Task CloseDocumentAsync(LayoutDocument document)
    {
        await _documentManager.RemoveDocumentAsync(document);
    }
}
