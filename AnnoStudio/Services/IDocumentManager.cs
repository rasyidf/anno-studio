using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Services;

/// <summary>
/// Service for managing multiple documents and active document state
/// </summary>
public interface IDocumentManager
{
    /// <summary>
    /// Collection of all open documents
    /// </summary>
    ObservableCollection<LayoutDocument> Documents { get; }

    /// <summary>
    /// The currently active document
    /// </summary>
    LayoutDocument? ActiveDocument { get; set; }

    /// <summary>
    /// Event raised when the active document changes
    /// </summary>
    event EventHandler<DocumentChangedEventArgs>? ActiveDocumentChanged;

    /// <summary>
    /// Event raised when a document is added
    /// </summary>
    event EventHandler<DocumentChangedEventArgs>? DocumentAdded;

    /// <summary>
    /// Event raised when a document is removed
    /// </summary>
    event EventHandler<DocumentChangedEventArgs>? DocumentRemoved;

    /// <summary>
    /// Adds a document to the collection
    /// </summary>
    void AddDocument(LayoutDocument document);

    /// <summary>
    /// Removes a document from the collection
    /// </summary>
    Task<bool> RemoveDocumentAsync(LayoutDocument document);

    /// <summary>
    /// Closes all documents
    /// </summary>
    Task<bool> CloseAllDocumentsAsync();

    /// <summary>
    /// Gets a document by file path
    /// </summary>
    LayoutDocument? GetDocumentByPath(string filePath);

    /// <summary>
    /// Checks if any documents have unsaved changes
    /// </summary>
    bool HasUnsavedChanges();
}

/// <summary>
/// Event args for document change events
/// </summary>
public class DocumentChangedEventArgs : EventArgs
{
    public LayoutDocument? Document { get; }
    public LayoutDocument? PreviousDocument { get; }

    public DocumentChangedEventArgs(LayoutDocument? document, LayoutDocument? previousDocument = null)
    {
        Document = document;
        PreviousDocument = previousDocument;
    }
}
