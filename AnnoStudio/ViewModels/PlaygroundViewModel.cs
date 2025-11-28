using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Objects;
using AnnoStudio.EditorCanvas.Tools;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Services;
using AnnoStudio.EditorCanvas.Core.Models;
using CanvasControl = AnnoStudio.EditorCanvas.Controls.EditorCanvas;

namespace AnnoStudio.ViewModels;

public partial class PlaygroundViewModel : ObservableObject
{
    private readonly CanvasControl _canvas;
    private readonly StringBuilder _debugLogBuilder = new();

    [ObservableProperty]
    private IEditorTool? _selectedTool;

    [ObservableProperty]
    private bool _gridVisible = true;

    [ObservableProperty]
    private float _gridSize = 16f;
    [ObservableProperty]
    private float _gridOpacity = 0.3f;

    [ObservableProperty]
    private int _gridTypeIndex = 2; // default to crosses (Standard=0->lines,1->dots,2->crosses)

    [ObservableProperty]
    private float _minorGridThickness = 1f;

    [ObservableProperty]
    private float _majorGridThickness = 1.5f;

    [ObservableProperty]
    private int _majorGridInterval = 5;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private bool _debugOverlay = false;

    [ObservableProperty]
    private bool _showOrigin = true;

    [ObservableProperty]
    private float _zoomLevel = 1.0f;

    [ObservableProperty]
    private string _statusMessage = "Playground Ready";

    [ObservableProperty]
    private string _cursorPosition = "X: 0, Y: 0";

    [ObservableProperty]
    private string _canvasSize = "0 x 0";

    [ObservableProperty]
    private int _selectionCount;

    [ObservableProperty]
    private int _objectCount;

    [ObservableProperty]
    private string _debugLog = "";

    public ObservableCollection<IEditorTool> Tools { get; } = new();
    public ObservableCollection<ICanvasObject> CanvasObjects { get; } = new();

    public PlaygroundViewModel(CanvasControl canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));

        InitializeTools();
        SetupEventHandlers();
        InitializeSettings();
        
        LogDebug("Playground initialized");
        LogDebug($"Canvas services: Grid={_canvas.Grid != null}, Selection={_canvas.Selection != null}, Objects={_canvas.Objects != null}");
    }

    private readonly ISettingsService _settingsService = new SettingsService();

    private void InitializeSettings()
    {
        try
        {
            var settings = _settingsService.GetSettings<EditorSettings>();

            // Apply grid settings
            if (settings?.Grid != null)
            {
                _canvas.Grid.Settings = settings.Grid;
                GridSize = settings.Grid.GridSize;
                GridOpacity = settings.Grid.Opacity;
                // Translate DisplayMode to index mapping
                GridTypeIndex = settings.Grid.DisplayMode switch
                {
                    GridDisplayMode.Lines => 0,
                    GridDisplayMode.Dots => 1,
                    GridDisplayMode.Crosses => 2,
                    _ => 2
                };
                MinorGridThickness = settings.Grid.MinorGridThickness;
                MajorGridThickness = settings.Grid.MajorGridThickness;
                MajorGridInterval = settings.Grid.MajorGridInterval;
                SnapToGrid = settings.Grid.SnapEnabled;
                // visibility based on applied grid
                GridVisible = _canvas.Grid.IsEnabled;
            }

            // Apply viewport/render settings
            if (settings?.Debug != null)
            {
                DebugOverlay = settings.Debug.ShowOverlay;
            }

            if (settings?.Render != null)
            {
                ShowOrigin = settings.Render.ShowOrigin;
            }
        }
        catch
        {
            // Ignore errors loading settings; we'll use defaults
        }
    }

    private void InitializeTools()
    {
        var selectTool = new SelectTool();
        var stampTool = new StampTool();
        var rectTool = new RectTool();
        var lineTool = new LineTool();
        var drawTool = new DrawTool();

        Tools.Add(selectTool);
        Tools.Add(stampTool);
        Tools.Add(rectTool);
        Tools.Add(lineTool);
        Tools.Add(drawTool);

        // Set default tool
        SelectedTool = selectTool;
        _canvas.ActiveTool = selectTool;
        
        LogDebug($"Initialized {Tools.Count} tools, default: {SelectedTool.Name}");
    }

    private void SetupEventHandlers()
    {
        // Track cursor position
        _canvas.EventBus.CursorPositionChanged += (s, pos) =>
        {
            CursorPosition = $"X: {pos.X:F1}, Y: {pos.Y:F1}";
        };

        // Track selection changes
        _canvas.Selection.SelectionChanged += (s, e) =>
        {
            SelectionCount = e.Selection.Count();
            LogDebug($"Selection changed: {SelectionCount} object(s) selected");
        };

        // Track object changes
        _canvas.Objects.CollectionChanged += (s, e) =>
        {
            ObjectCount = _canvas.Objects.Count();
            CanvasObjects.Clear();
            foreach (var obj in _canvas.Objects)
            {
                CanvasObjects.Add(obj);
            }
            LogDebug($"Objects changed: {ObjectCount} total objects");
        };

        // Track viewport changes
        _canvas.Viewport.Changed += (s, e) =>
        {
            ZoomLevel = _canvas.Viewport.Zoom;
        };

        // Track canvas size
        var width = _canvas.Bounds.Width;
        var height = _canvas.Bounds.Height;
        CanvasSize = $"{width:F0} x {height:F0}";
    }

    partial void OnSelectedToolChanged(IEditorTool? value)
    {
        if (value != null)
        {
            _canvas.ActiveTool = value;
            StatusMessage = $"Switched to {value.Name} tool";
            LogDebug($"Tool changed: {value.Name}");
        }
    }

    partial void OnGridVisibleChanged(bool value)
    {
        _canvas.Grid.IsEnabled = value;
        _canvas.Invalidate();
        LogDebug($"Grid visible: {value}");
        SaveEditorSettings();
    }

    partial void OnGridOpacityChanged(float value)
    {
        _canvas.Grid.Settings.Opacity = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnGridTypeIndexChanged(int value)
    {
        _canvas.Grid.Settings.DisplayMode = value switch
        {
            0 => GridDisplayMode.Lines,
            1 => GridDisplayMode.Dots,
            2 => GridDisplayMode.Crosses,
            _ => GridDisplayMode.Crosses
        };
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnMinorGridThicknessChanged(float value)
    {
        _canvas.Grid.Settings.MinorGridThickness = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnMajorGridThicknessChanged(float value)
    {
        _canvas.Grid.Settings.MajorGridThickness = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnMajorGridIntervalChanged(int value)
    {
        _canvas.Grid.Settings.MajorGridInterval = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnSnapToGridChanged(bool value)
    {
        _canvas.Grid.Settings.SnapEnabled = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnGridSizeChanged(float value)
    {
        _canvas.Grid.Settings.GridSize = value;
        _canvas.Invalidate();
        LogDebug($"Grid size: {value}");
        SaveEditorSettings();
    }

    partial void OnZoomLevelChanged(float value)
    {
        if (Math.Abs(_canvas.Viewport.Zoom - value) > 0.001f)
        {
            _canvas.Viewport.Zoom = value;
            LogDebug($"Zoom: {value:P0}");
            SaveEditorSettings();
        }
    }

    partial void OnDebugOverlayChanged(bool value)
    {
        // Keep the setting in sync
        _canvas.Settings.Debug.ShowOverlay = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    partial void OnShowOriginChanged(bool value)
    {
        _canvas.Settings.Render.ShowOrigin = value;
        _canvas.Invalidate();
        SaveEditorSettings();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SaveEditorSettings();
        StatusMessage = "Settings saved";
        LogDebug("Settings saved via UI");
    }

    [RelayCommand]
    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.GetSettings<EditorSettings>();
            if (settings?.Grid != null)
            {
                _canvas.Grid.Settings = settings.Grid;
                GridSize = settings.Grid.GridSize;
                GridVisible = _canvas.Grid.IsEnabled;
            }

            if (settings?.Debug != null)
            {
                DebugOverlay = settings.Debug.ShowOverlay;
            }

            if (settings?.Render != null)
            {
                ShowOrigin = settings.Render.ShowOrigin;
            }

            StatusMessage = "Settings loaded";
            LogDebug("Settings loaded via UI");
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to load settings: {ex.Message}");
            StatusMessage = "Settings load failed";
        }
    }

    private void SaveEditorSettings()
    {
        try
        {
            var s = _settingsService.GetSettings<EditorSettings>();

            // Update grid settings in a copy or instance
            s.Grid = s.Grid ?? new GridSettings();
            s.Grid.GridSize = _canvas.Grid.Settings.GridSize;
            s.Grid.SnapEnabled = _canvas.Grid.Settings.SnapEnabled;
            s.Grid.Offset = _canvas.Grid.Settings.Offset;
            s.Grid.DisplayMode = _canvas.Grid.Settings.DisplayMode;
            s.Grid.Opacity = _canvas.Grid.Settings.Opacity;
            s.Grid.MinorGridThickness = _canvas.Grid.Settings.MinorGridThickness;
            s.Grid.MajorGridThickness = _canvas.Grid.Settings.MajorGridThickness;
            s.Grid.MajorGridInterval = _canvas.Grid.Settings.MajorGridInterval;

            s.Debug = s.Debug ?? new DebugSettings();
            s.Debug.ShowOverlay = DebugOverlay;

            s.Render = s.Render ?? new RenderSettings();
            s.Render.ShowOrigin = ShowOrigin;

            _settingsService.SaveSettings(s);
            LogDebug("Saved EditorSettings to disk");
        }
        catch (Exception ex)
        {
            LogDebug($"Failed saving settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SelectTool(IEditorTool tool)
    {
        SelectedTool = tool;
    }

    [RelayCommand]
    private void ClearCanvas()
    {
        var count = _canvas.Objects.Count();
        _canvas.Clear();
        StatusMessage = $"Canvas cleared ({count} objects removed)";
        LogDebug($"Canvas cleared: {count} objects removed");
    }

    [RelayCommand]
    private void Undo()
    {
        if (_canvas.History.CanUndo)
        {
            _canvas.History.Undo();
            StatusMessage = "Undo executed";
            LogDebug("Undo");
        }
        else
        {
            StatusMessage = "Nothing to undo";
            LogDebug("Undo: nothing to undo");
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_canvas.History.CanRedo)
        {
            _canvas.History.Redo();
            StatusMessage = "Redo executed";
            LogDebug("Redo");
        }
        else
        {
            StatusMessage = "Nothing to redo";
            LogDebug("Redo: nothing to redo");
        }
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
        var objects = _canvas.Objects.ToList();
        if (!objects.Any())
        {
            StatusMessage = "No objects to fit";
            LogDebug("ZoomToFit: No objects on canvas");
            return;
        }

        // Calculate bounding box of all objects
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var obj in objects)
        {
            var bounds = obj.Bounds;
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        var boundsRect = new SKRect(minX, minY, maxX, maxY);
        var viewportSize = new SKSize((float)_canvas.Bounds.Width, (float)_canvas.Bounds.Height);

        // Use ViewportTransform's ZoomToFit method
        _canvas.Viewport.ZoomToFit(boundsRect, viewportSize, padding: 40);
        
        ZoomLevel = _canvas.Viewport.Zoom;
        StatusMessage = $"Zoomed to fit {objects.Count} objects";
        LogDebug($"ZoomToFit: {objects.Count} objects, zoom={_canvas.Viewport.Zoom:F2}");
    }

    [RelayCommand]
    private void ZoomToSelection()
    {
        var selected = _canvas.Selection.SelectedObjects.ToList();
        if (!selected.Any())
        {
            StatusMessage = "No objects selected";
            LogDebug("ZoomToSelection: No selection");
            return;
        }

        // Calculate bounding box of selected objects
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var obj in selected)
        {
            var bounds = obj.Bounds;
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        var boundsRect = new SKRect(minX, minY, maxX, maxY);
        var viewportSize = new SKSize((float)_canvas.Bounds.Width, (float)_canvas.Bounds.Height);

        // Use ViewportTransform's ZoomToFit method with more padding for selection
        _canvas.Viewport.ZoomToFit(boundsRect, viewportSize, padding: 60);

        ZoomLevel = _canvas.Viewport.Zoom;
        StatusMessage = $"Zoomed to {selected.Count} selected objects";
        LogDebug($"ZoomToSelection: {selected.Count} objects, zoom={_canvas.Viewport.Zoom:F2}");
    }

    [RelayCommand]
    private void CenterView()
    {
        _canvas.Viewport.Pan = new SKPoint(0, 0);
        StatusMessage = "View centered";
        LogDebug("View centered to origin");
    }

    [RelayCommand]
    private void AddTestRectangle()
    {
        var random = new Random();
        var building = new BuildingObject
        {
            Name = $"Test Rectangle {_canvas.Objects.Count() + 1}",
            Width = 3,
            Height = 2,
            BuildingType = "Test",
            Color = new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)),
            Transform = new EditorCanvas.Core.Models.Transform2D
            {
                Position = new SKPoint(random.Next(100, 400), random.Next(100, 400)),
                Rotation = 0,
                Scale = new SKPoint(1, 1)
            }
        };
        
        _canvas.Objects.Add(building);
        StatusMessage = $"Added {building.Name}";
        LogDebug($"Added test rectangle: {building.Name} at ({building.Transform.Position.X:F0}, {building.Transform.Position.Y:F0})");
    }

    [RelayCommand]
    private void AddTestCircle()
    {
        StatusMessage = "Circle objects not yet implemented";
        LogDebug("AddTestCircle: not implemented");
    }

    [RelayCommand]
    private void AddTestBuilding()
    {
        var random = new Random();
        var colors = new[] 
        { 
            SKColors.Red, SKColors.Blue, SKColors.Green, 
            SKColors.Yellow, SKColors.Purple, SKColors.Orange 
        };
        
        var building = new BuildingObject
        {
            Name = $"Building {_canvas.Objects.Count() + 1}",
            Width = random.Next(2, 6),
            Height = random.Next(2, 6),
            BuildingType = "Residence",
            Color = colors[random.Next(colors.Length)],
            Transform = new EditorCanvas.Core.Models.Transform2D
            {
                Position = new SKPoint(random.Next(50, 500), random.Next(50, 500)),
                Rotation = 0,
                Scale = new SKPoint(1, 1)
            }
        };
        
        _canvas.Objects.Add(building);
        StatusMessage = $"Added {building.Name} ({building.Width}x{building.Height})";
        LogDebug($"Added building: {building.Name} size {building.Width}x{building.Height}");
    }

    [RelayCommand]
    private void TestSelection()
    {
        if (_canvas.Objects.Any())
        {
            _canvas.Selection.SelectAll(_canvas.Objects);
            StatusMessage = $"Selected all {_canvas.Objects.Count()} objects";
            LogDebug($"Test: Selected all objects ({_canvas.Objects.Count()})");
        }
        else
        {
            StatusMessage = "No objects to select";
            LogDebug("Test: No objects to select");
        }
    }

    [RelayCommand]
    private void TestKeyboard()
    {
        LogDebug("=== Keyboard Shortcuts Test ===");
        LogDebug("Registered shortcuts:");
        
        var shortcuts = _canvas.Shortcuts.GetAllShortcuts();
        foreach (var shortcut in shortcuts)
        {
            LogDebug($"  {shortcut.Gesture}: {shortcut.Name} - {shortcut.Description}");
        }
        
        LogDebug($"Total shortcuts: {shortcuts.Count}");
        StatusMessage = $"Logged {shortcuts.Count} keyboard shortcuts to console";
    }

    [RelayCommand]
    private void ClearLog()
    {
        _debugLogBuilder.Clear();
        DebugLog = "";
        StatusMessage = "Debug log cleared";
    }

    private void LogDebug(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        _debugLogBuilder.AppendLine($"[{timestamp}] {message}");
        DebugLog = _debugLogBuilder.ToString();
    }
}
