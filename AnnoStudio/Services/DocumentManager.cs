using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Services;

/// <summary>
/// Implementation of document manager for multi-document interface
/// </summary>
public class DocumentManager : IDocumentManager
{
    private LayoutDocument? _activeDocument;
    private readonly IProjectService _projectService;

    public ObservableCollection<LayoutDocument> Documents { get; }

    public LayoutDocument? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                var previous = _activeDocument;
                _activeDocument = value;
                ActiveDocumentChanged?.Invoke(this, new DocumentChangedEventArgs(value, previous));
            }
        }
    }

    public event EventHandler<DocumentChangedEventArgs>? ActiveDocumentChanged;
    public event EventHandler<DocumentChangedEventArgs>? DocumentAdded;
    public event EventHandler<DocumentChangedEventArgs>? DocumentRemoved;

    public DocumentManager(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        Documents = new ObservableCollection<LayoutDocument>();
        
        // Subscribe to collection changes to manage active document
        Documents.CollectionChanged += Documents_CollectionChanged;
    }

    public void AddDocument(LayoutDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (!Documents.Contains(document))
        {
            Documents.Add(document);
            ActiveDocument = document;
            DocumentAdded?.Invoke(this, new DocumentChangedEventArgs(document));
        }
    }

    public async Task<bool> RemoveDocumentAsync(LayoutDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (!Documents.Contains(document))
            return true;

        // Ask to save if dirty
        var canClose = await _projectService.CloseDocumentAsync(document);
        if (!canClose)
            return false;

        var index = Documents.IndexOf(document);
        Documents.Remove(document);

        // Set new active document
        if (ActiveDocument == document)
        {
            if (Documents.Count > 0)
            {
                // Try to activate the document at the same index, or the last one
                var newIndex = Math.Min(index, Documents.Count - 1);
                ActiveDocument = Documents[newIndex];
            }
            else
            {
                ActiveDocument = null;
            }
        }

        DocumentRemoved?.Invoke(this, new DocumentChangedEventArgs(document));
        return true;
    }

    public async Task<bool> CloseAllDocumentsAsync()
    {
        // Make a copy of the documents list to avoid modification during iteration
        var documentsToClose = Documents.ToList();

        foreach (var document in documentsToClose)
        {
            var closed = await RemoveDocumentAsync(document);
            if (!closed)
                return false; // User cancelled
        }

        return true;
    }

    public LayoutDocument? GetDocumentByPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        return Documents.FirstOrDefault(d => 
            !string.IsNullOrEmpty(d.FilePath) && 
            string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasUnsavedChanges()
    {
        return Documents.Any(d => d.IsDirty);
    }

    private void Documents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // If active document was removed and collection is now empty, clear active document
        if (Documents.Count == 0)
        {
            ActiveDocument = null;
        }
        // If a document was added and there's no active document, make it active
        else if (ActiveDocument == null && Documents.Count > 0)
        {
            ActiveDocument = Documents[0];
        }
    }
}
