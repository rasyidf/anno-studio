using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.EditorCanvas.Core.Models;
using AnnoStudio.EditorCanvas.Serialization;
using AnnoStudio.EditorCanvas.ViewModels;
using CanvasControl = AnnoStudio.EditorCanvas.Controls.EditorCanvas;

namespace AnnoStudio.ViewModels;

/// <summary>
/// Document for Anno layout files (.layout.json).
/// </summary>
public partial class LayoutDocument : FileDocument
{
    private readonly JsonCanvasSerializer _serializer;
    private EditorCanvasViewModel? _editorViewModel;
    private CanvasControl? _canvas;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    public EditorCanvasViewModel? EditorViewModel
    {
        get => _editorViewModel;
        set => SetProperty(ref _editorViewModel, value);
    }

    public CanvasControl? Canvas
    {
        get => _canvas;
        private set => _canvas = value;
    }

    public LayoutDocument()
    {
        _serializer = new JsonCanvasSerializer();
        Title = "New Layout";
    }

    public LayoutDocument(string filePath) : this()
    {
        FilePath = filePath;
        Title = Path.GetFileNameWithoutExtension(filePath);
    }

    public void Initialize(CanvasControl canvas)
    {
        Canvas = canvas;
        EditorViewModel = new EditorCanvasViewModel(canvas);
        EditorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
    }

    private void OnEditorViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorCanvasViewModel.IsDirty))
        {
            IsDirty = EditorViewModel?.IsDirty ?? false;
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        var baseName = string.IsNullOrEmpty(FilePath) 
            ? "New Layout" 
            : Path.GetFileNameWithoutExtension(FilePath);

        Title = IsDirty ? $"{baseName}*" : baseName;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            await SaveAs();
            return;
        }

        await SaveToFile(FilePath);
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        // TODO: Show save file dialog
        // For now, use a default path
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AnnoLayouts",
            $"{Title.TrimEnd('*')}.layout.json"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
        await SaveToFile(defaultPath);
    }

    private async Task SaveToFile(string path)
    {
        if (Canvas == null)
            return;

        try
        {
            var document = Canvas.GetDocument();
            document.Metadata.Title = Title.TrimEnd('*');
            document.Metadata.Modified = DateTime.UtcNow;

            using var stream = File.Create(path);
            await _serializer.SerializeAsync(document, stream);

            FilePath = path;
            IsDirty = false;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog
            Console.WriteLine($"Error saving layout: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Load()
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath) || Canvas == null)
            return;

        try
        {
            using var stream = File.OpenRead(FilePath);
            var document = await _serializer.DeserializeAsync(stream);

            Canvas.LoadDocument(document);

            Title = Path.GetFileNameWithoutExtension(FilePath);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog
            Console.WriteLine($"Error loading layout: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        // TODO: Implement export to PNG/SVG
    }

    public override string ToString() => Title;
}
