using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;
using AnnoStudio.EditorCanvas.Serialization;
using AnnoStudio.EditorCanvas.Tools;
using AnnoStudio.EditorCanvas.Transforms;
using SkiaSharp;
using CanvasControl = AnnoStudio.EditorCanvas.Controls.EditorCanvas;

namespace AnnoStudio.EditorCanvas.ViewModels;

/// <summary>
/// ViewModel for the EditorCanvas control.
/// </summary>
public partial class EditorCanvasViewModel : ObservableObject
{
    private readonly CanvasControl _canvas;
    private readonly JsonCanvasSerializer _serializer;
    private readonly IToolRegistry _toolRegistry;
    private readonly ITransformRegistry _transformRegistry;

    [ObservableProperty]
    private IEditorTool? _selectedTool;

    [ObservableProperty]
    private bool _gridVisible = true;

    [ObservableProperty]
    private float _gridSize = 16f;

    [ObservableProperty]
    private float _zoomLevel = 1.0f;

    [ObservableProperty]
    private string _documentName = "Untitled";

    [ObservableProperty]
    private bool _isDirty;

    public ObservableCollection<IEditorTool> Tools { get; } = new();
    public ObservableCollection<ICanvasObject> SelectedObjects { get; } = new();

    public EditorCanvasViewModel(CanvasControl canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _serializer = new JsonCanvasSerializer();
        _toolRegistry = new Core.Services.ToolRegistry();
        _transformRegistry = new Core.Services.TransformRegistry();

        InitializeTools();
        InitializeTransforms();
        SetupEventHandlers();
        RegisterToolShortcuts();
    }

    private void InitializeTools()
    {
        // Register and add tools
        var stampTool = new StampTool();
        var selectTool = new SelectTool();
        var drawTool = new DrawTool();
        var lineTool = new LineTool();
        var rectTool = new RectTool();

        _toolRegistry.RegisterTool(stampTool);
        _toolRegistry.RegisterTool(selectTool);
        _toolRegistry.RegisterTool(drawTool);
        _toolRegistry.RegisterTool(lineTool);
        _toolRegistry.RegisterTool(rectTool);

        Tools.Add(selectTool);
        Tools.Add(stampTool);
        Tools.Add(drawTool);
        Tools.Add(lineTool);
        Tools.Add(rectTool);

        // Set default tool to Select and activate it immediately
        SelectedTool = selectTool;
        _canvas.ActiveTool = selectTool;
    }

    private void InitializeTransforms()
    {
        _transformRegistry.RegisterTransform(new MoveTransform());
        _transformRegistry.RegisterTransform(new RotateTransform());
        _transformRegistry.RegisterTransform(new DuplicateTransform());
        _transformRegistry.RegisterTransform(new ResizeTransform());
    }

    private void SetupEventHandlers()
    {
        _canvas.Selection.SelectionChanged += OnSelectionChanged;
        _canvas.Objects.CollectionChanged += OnObjectsChanged;
        _canvas.Viewport.Changed += OnViewportChanged;
    }

    private void OnSelectionChanged(object? sender, Core.Interfaces.SelectionChangedEventArgs e)
    {
        SelectedObjects.Clear();
        foreach (var obj in e.Selection)
        {
            SelectedObjects.Add(obj);
        }
    }

    private void OnObjectsChanged(object? sender, EventArgs e)
    {
        IsDirty = true;
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        ZoomLevel = _canvas.Viewport.Zoom;
    }

    partial void OnSelectedToolChanged(IEditorTool? value)
    {
        _canvas.ActiveTool = value;
    }

    partial void OnGridVisibleChanged(bool value)
    {
        _canvas.Grid.IsEnabled = value;
        _canvas.Invalidate();
    }

    partial void OnGridSizeChanged(float value)
    {
        _canvas.Grid.Settings.GridSize = value;
        _canvas.Invalidate();
    }

    partial void OnZoomLevelChanged(float value)
    {
        if (Math.Abs(_canvas.Viewport.Zoom - value) > 0.001f)
        {
            _canvas.Viewport.Zoom = value;
        }
    }

    [RelayCommand]
    private void NewDocument()
    {
        if (IsDirty)
        {
            // TODO: Prompt to save
        }

        _canvas.Clear();
        DocumentName = "Untitled";
        IsDirty = false;
    }

    [RelayCommand]
    private async Task SaveDocument()
    {
        // TODO: Show save dialog
        var document = _canvas.GetDocument();
        document.Metadata.Title = DocumentName;
        document.Metadata.Modified = DateTime.UtcNow;

        // For now, just mark as not dirty
        IsDirty = false;
    }

    [RelayCommand]
    private async Task LoadDocument()
    {
        // TODO: Show open dialog and load document
    }

    [RelayCommand]
    private void Undo()
    {
        _canvas.History.Undo();
    }

    [RelayCommand]
    private void Redo()
    {
        _canvas.History.Redo();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        var selected = _canvas.Selection.SelectedObjects.ToList();
        foreach (var obj in selected)
        {
            _canvas.Objects.Remove(obj);
        }
        _canvas.Selection.Clear();
    }

    [RelayCommand]
    private void SelectAll()
    {
        _canvas.Selection.SelectAll(_canvas.Objects);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel *= 1.2f;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel /= 1.2f;
    }

    [RelayCommand]
    private void ZoomReset()
    {
        ZoomLevel = 1.0f;
    }

    [RelayCommand]
    private void ZoomToFit()
    {
        if (!_canvas.Objects.Any())
            return;

        var bounds = _canvas.Objects.First().Bounds;
        foreach (var obj in _canvas.Objects.Skip(1))
        {
            bounds = SKRect.Union(bounds, obj.Bounds);
        }

        _canvas.Viewport.ZoomToFit(bounds, new SkiaSharp.SKSize((float)_canvas.Bounds.Width, (float)_canvas.Bounds.Height));
    }

    private void RegisterToolShortcuts()
    {
        // Register keyboard shortcuts for tool switching
        foreach (var tool in Tools)
        {
            if (tool.Shortcut != null)
            {
                var toolToActivate = tool; // Capture the tool in closure
                _canvas.Shortcuts.RegisterShortcut(
                    tool.Shortcut,
                    () => SelectedTool = toolToActivate,
                    $"Tool_{tool.Name}",
                    $"Activate {tool.Name} tool"
                );
            }
        }
    }

    /// <summary>
    /// Activates a tool by name.
    /// </summary>
    public void ActivateTool(string toolName)
    {
        var tool = Tools.FirstOrDefault(t => t.Name == toolName);
        if (tool != null)
        {
            SelectedTool = tool;
        }
    }

    /// <summary>
    /// Gets the active canvas control.
    /// </summary>
    public CanvasControl Canvas => _canvas;
}
