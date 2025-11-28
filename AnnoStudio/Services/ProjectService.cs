using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AnnoStudio.ViewModels;
using AnnoStudio.EditorCanvas.Core.Models;
using AnnoStudio.EditorCanvas.Serialization;

namespace AnnoStudio.Services;

/// <summary>
/// Implementation of project service for document operations
/// </summary>
public class ProjectService : IProjectService
{
    private readonly JsonCanvasSerializer _serializer;
    private readonly Window? _mainWindow;

    public ProjectService(Window? mainWindow = null)
    {
        _serializer = new JsonCanvasSerializer();
        _mainWindow = mainWindow;
    }

    public Task<LayoutDocument> CreateNewDocumentAsync()
    {
        var document = new LayoutDocument();
        return Task.FromResult(document);
    }

    public async Task<LayoutDocument?> OpenDocumentAsync(string? filePath = null)
    {
        try
        {
            // Show open file dialog if no path provided
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = await ShowOpenFileDialogAsync();
                if (string.IsNullOrEmpty(filePath))
                    return null;
            }

            if (!File.Exists(filePath))
                return null;

            var document = new LayoutDocument(filePath);
            return document;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening document: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SaveDocumentAsync(LayoutDocument document)
    {
        if (string.IsNullOrEmpty(document.FilePath))
        {
            return await SaveDocumentAsAsync(document);
        }

        return await SaveToFileAsync(document, document.FilePath);
    }

    public async Task<bool> SaveDocumentAsAsync(LayoutDocument document, string? filePath = null)
    {
        try
        {
            // Show save file dialog if no path provided
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = await ShowSaveFileDialogAsync(document.Title);
                if (string.IsNullOrEmpty(filePath))
                    return false;
            }

            return await SaveToFileAsync(document, filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving document: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CloseDocumentAsync(LayoutDocument document)
    {
        if (document.IsDirty)
        {
            // TODO: Show save changes dialog
            var result = await ShowSaveChangesDialogAsync(document.Title);
            if (result == SaveChangesResult.Cancel)
                return false;

            if (result == SaveChangesResult.Save)
            {
                var saved = await SaveDocumentAsync(document);
                if (!saved)
                    return false;
            }
        }

        return true;
    }

    public async Task<bool> ExportDocumentAsync(LayoutDocument document, string? filePath = null)
    {
        try
        {
            // Show export file dialog if no path provided
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = await ShowExportFileDialogAsync(document.Title);
                if (string.IsNullOrEmpty(filePath))
                    return false;
            }

            // TODO: Implement actual export logic
            // This would use SkiaSharp to render the canvas to PNG/SVG
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting document: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SaveToFileAsync(LayoutDocument document, string path)
    {
        if (document.Canvas == null)
            return false;

        try
        {
            var canvasDocument = document.Canvas.GetDocument();
            canvasDocument.Metadata.Title = document.Title.TrimEnd('*');
            canvasDocument.Metadata.Modified = DateTime.UtcNow;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var stream = File.Create(path);
            await _serializer.SerializeAsync(canvasDocument, stream);

            document.FilePath = path;
            document.IsDirty = false;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to file: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> ShowOpenFileDialogAsync()
    {
        if (_mainWindow == null)
            return null;

        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Layout File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Anno Layout Files")
                {
                    Patterns = new[] { "*.layout.json" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> ShowSaveFileDialogAsync(string defaultName)
    {
        if (_mainWindow == null)
            return null;

        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Layout File",
            SuggestedFileName = $"{defaultName.TrimEnd('*')}.layout.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Anno Layout Files")
                {
                    Patterns = new[] { "*.layout.json" }
                }
            }
        });

        return file?.Path.LocalPath;
    }

    private async Task<string?> ShowExportFileDialogAsync(string defaultName)
    {
        if (_mainWindow == null)
            return null;

        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Layout",
            SuggestedFileName = $"{defaultName.TrimEnd('*')}.png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image")
                {
                    Patterns = new[] { "*.png" }
                },
                new FilePickerFileType("SVG Image")
                {
                    Patterns = new[] { "*.svg" }
                }
            }
        });

        return file?.Path.LocalPath;
    }

    private async Task<SaveChangesResult> ShowSaveChangesDialogAsync(string documentTitle)
    {
        // TODO: Show actual dialog
        // For now, default to Don't Save
        await Task.CompletedTask;
        return SaveChangesResult.DontSave;
    }
}

public enum SaveChangesResult
{
    Save,
    DontSave,
    Cancel
}
