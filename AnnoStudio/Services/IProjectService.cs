using System.Threading.Tasks;
using AnnoStudio.ViewModels;

namespace AnnoStudio.Services;

/// <summary>
/// Service for managing project operations (new, open, save, close)
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new layout document
    /// </summary>
    Task<LayoutDocument> CreateNewDocumentAsync();

    /// <summary>
    /// Opens a layout document from file
    /// </summary>
    Task<LayoutDocument?> OpenDocumentAsync(string? filePath = null);

    /// <summary>
    /// Saves the specified document
    /// </summary>
    Task<bool> SaveDocumentAsync(LayoutDocument document);

    /// <summary>
    /// Saves the specified document with a new file path
    /// </summary>
    Task<bool> SaveDocumentAsAsync(LayoutDocument document, string? filePath = null);

    /// <summary>
    /// Closes the specified document
    /// </summary>
    Task<bool> CloseDocumentAsync(LayoutDocument document);

    /// <summary>
    /// Exports the document to an image format
    /// </summary>
    Task<bool> ExportDocumentAsync(LayoutDocument document, string? filePath = null);
}
