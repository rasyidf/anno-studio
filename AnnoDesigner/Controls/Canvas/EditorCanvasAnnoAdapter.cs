using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.CustomEventArgs;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services.Undo;

namespace AnnoDesigner.Controls.Canvas;

/// <summary>
/// Shim UserControl that wraps EditorCanvas + AnnoEditorAdapter to satisfy IAnnoCanvas.
/// ponytail: MVP adapter — unimplemented members throw or no-op.
/// Ceiling: full parity once Phase 4 stabilizes.
/// </summary>
public sealed class EditorCanvasAnnoAdapter : UserControl, IAnnoCanvas
{
    private readonly EditorCanvas.EditorCanvas _editorCanvas;
    private readonly AnnoEditorAdapter _adapter;
    private readonly IUndoManager _undoManager;
    private readonly BuildingPresets _buildingPresets;
    private readonly Dictionary<string, IconImage> _icons;

    private QuadTree<LayoutObject> _placedObjects;
    private HashSet<LayoutObject> _selectedObjects;
    private string _loadedFile;
    private int _gridSize = 30; // ponytail: 30px per grid cell for clear visibility. Old canvas uses 20.
    private readonly List<LayoutObject> _currentObjects = new();

    #region IAnnoCanvas Events

    public event EventHandler<EventArgs> ColorsInLayoutUpdated;
    public event EventHandler<UpdateStatisticsEventArgs> StatisticsUpdated;
    public event EventHandler<FileLoadedEventArgs> OnLoadedFileChanged;
    public event Action<string> OnStatusMessageChanged;
    public event Action<LayoutObject> OnCurrentObjectChanged;
    public event EventHandler<OpenFileEventArgs> OpenFileRequested;
    public event EventHandler<SaveFileEventArgs> SaveFileRequested;

    #endregion

    public EditorCanvasAnnoAdapter(
        BuildingPresets buildingPresets,
        Dictionary<string, IconImage> icons,
        IAppSettings appSettings,
        ICoordinateHelper coordinateHelper,
        IBrushCache brushCache,
        IPenCache penCache,
        IUndoManager undoManager)
    {
        _buildingPresets = buildingPresets;
        _icons = icons;
        _undoManager = undoManager;

        _editorCanvas = new EditorCanvas.EditorCanvas();

        // Sync with system theme via WPF-UI dynamic resources
        _editorCanvas.SetResourceReference(EditorCanvas.EditorCanvas.BackgroundBrushProperty,
            "ApplicationBackgroundBrush");
        _editorCanvas.SetResourceReference(EditorCanvas.EditorCanvas.GridLineBrushProperty,
            "ControlStrokeColorDefaultBrush");
        _editorCanvas.SetResourceReference(EditorCanvas.EditorCanvas.SelectionStrokeBrushProperty,
            "SystemAccentColorPrimaryBrush");
        // ponytail: ObjectStrokeBrush left as default (Black, 1px) for building borders.
        // ObjectFillBrush left null so per-object FillColor is used.
        _editorCanvas.ObjectStrokeBrush = System.Windows.Media.Brushes.Black;
        _editorCanvas.ObjectFillBrush = System.Windows.Media.Brushes.Transparent;

        // Configure for Anno grid: 1 world-unit = 1 grid cell, zoom = GridSize pixels per cell
        if (_editorCanvas.Preferences != null)
        {
            _editorCanvas.Preferences.GridSpacing = 1.0;    // Grid lines every 1 world-unit
            _editorCanvas.Preferences.SubGridVisible = false;
            _editorCanvas.Preferences.SnapToGrid = true;
            _editorCanvas.Preferences.DefaultZoom = _gridSize;  // pixels per grid cell
            _editorCanvas.Preferences.MinZoom = 8.0;
            _editorCanvas.Preferences.MaxZoom = 120.0;
        }

        // Disable guidelines (crosshair dashes) — not useful for Anno tile-based placement
        _editorCanvas.ShowGuides = false;

        // Register Anno-specific tools
        _editorCanvas.ToolManager?.RegisterTool(new EditorCanvas.Tooling.RoadPlacementTool(
            _editorCanvas.ObjectManager, _editorCanvas,
            objs => _editorCanvas.SetSelection(objs?.ToList() ?? new()),
            () => _editorCanvas.InvalidateVisual()));

        // Set initial zoom to match Anno GridSize
        if (_editorCanvas.TransformService != null)
            _editorCanvas.TransformService.Zoom = _gridSize;

        // ponytail: Zoom might not apply if canvas isn't laid out yet. Re-apply on Loaded.
        _editorCanvas.Loaded += (s, e) =>
        {
            if (_editorCanvas.TransformService != null && _editorCanvas.TransformService.Zoom < _gridSize)
                _editorCanvas.TransformService.Zoom = _gridSize;
            _editorCanvas.InvalidateVisual();
        };

        // Configure grid layer cell size to 1 world-unit
        var gridLayer = _editorCanvas.LayeredRenderer?.Layers
            ?.OfType<EditorCanvas.Core.Layers.GridLayer>().FirstOrDefault();
        if (gridLayer != null)
            gridLayer.CellSize = 1;

        _adapter = new AnnoEditorAdapter(
            _editorCanvas,
            coordinateHelper,
            brushCache,
            penCache,
            () => _gridSize,
            icons);

        Content = CreateContentWithToolbox();

        _placedObjects = new QuadTree<LayoutObject>(new Rect(-500, -500, 1000, 1000));
        _selectedObjects = new HashSet<LayoutObject>();

        _editorCanvas.SelectionChanged += OnEditorSelectionChanged;
    }

    private UIElement CreateContentWithToolbox()
    {
        var dock = new DockPanel { LastChildFill = true };

        // Toolbox panel (left side, narrow strip)
        var toolbox = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Vertical,
            Width = 32,
            VerticalAlignment = VerticalAlignment.Top,
        };
        toolbox.SetResourceReference(StackPanel.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");

        var tools = new (string Name, string Label, string Hotkey)[]
        {
            ("Selection", "⇱", "V"),
            ("RectSelect", "⬚", "R"),
            ("LassoSelect", "◇", "L"),
            ("Placement", "⊞", "P"),
            ("Transform", "↔", "M"),
            ("LineDraw", "╱", "N"),
            ("RoadPlacement", "═", "⇧R"),
        };

        foreach (var (name, label, hotkey) in tools)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = label,
                ToolTip = $"{name} ({hotkey})",
                Width = 28,
                Height = 28,
                Margin = new Thickness(2, 2, 2, 0),
                Padding = new Thickness(0),
                FontSize = 14,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var toolName = name;
            btn.Click += (s, e) => _editorCanvas.ToolManager?.Activate(toolName);
            toolbox.Children.Add(btn);
        }

        DockPanel.SetDock(toolbox, Dock.Left);
        dock.Children.Add(toolbox);
        dock.Children.Add(_editorCanvas); // fills remaining space

        return dock;
    }

    #region IAnnoCanvas Properties

    public QuadTree<LayoutObject> PlacedObjects
    {
        get => _placedObjects;
        set
        {
            _placedObjects = value;
            _adapter.ClearAll();
            if (value != null)
            {
                foreach (var obj in value)
                    _adapter.AddLayoutObject(obj);
            }
        }
    }

    public HashSet<LayoutObject> SelectedObjects
    {
        get => _selectedObjects;
        set => _selectedObjects = value ?? new HashSet<LayoutObject>();
    }

    public List<LayoutObject> CurrentObjects => _currentObjects;

    public BuildingPresets BuildingPresets => _buildingPresets;

    public Dictionary<string, IconImage> Icons => _icons;

    public IUndoManager UndoManager => _undoManager;

    public bool RenderGrid
    {
        get => _editorCanvas.ShowGrid;
        set => _editorCanvas.ShowGrid = value;
    }

    public bool RenderInfluences { get; set; }
    public bool RenderTrueInfluenceRange { get; set; }
    public bool RenderHarborBlockedArea { get; set; }
    public bool RenderPanorama { get; set; }
    public bool RenderLabel { get; set; } = true;
    public bool RenderIcon { get; set; } = true;

    public string LoadedFile
    {
        get => _loadedFile;
        set => _loadedFile = value;
    }

    public int GridSize
    {
        get => _gridSize;
        set => _gridSize = value;
    }

    public ICommand RotateCommand => _rotateCommand ??= new SimpleRelayCommand(_ => { /* ponytail: stub — rotation not wired yet */ });
    private ICommand _rotateCommand;

    #endregion

    #region IAnnoCanvas Methods

    public void ForceRendering() => _editorCanvas.InvalidateVisual();

    public void SetCurrentObject(LayoutObject obj)
    {
        _currentObjects.Clear();
        if (obj != null)
        {
            _currentObjects.Add(obj);

            // Activate placement tool on the EditorCanvas with the object as template
            var placementTool = _editorCanvas.ToolManager?.RegisteredTools
                ?.OfType<EditorCanvas.Tooling.PlacementTool>().FirstOrDefault();
            if (placementTool != null)
            {
                var template = new CanvasObject
                {
                    Bounds = new Rect(0, 0, obj.Size.Width, obj.Size.Height),
                    Identifier = obj.Identifier,
                    FillColor = obj.Color.MediaColor,
                    Label = obj.WrappedAnnoObject?.Label,
                    IconName = obj.WrappedAnnoObject?.Icon,
                    IsRoad = obj.WrappedAnnoObject?.Road ?? false,
                    IsBorderless = obj.WrappedAnnoObject?.Borderless ?? false,
                    Tag = obj.WrappedAnnoObject,
                    ShapeType = "Rectangle"
                };
                placementTool.SetTemplate(template);
                _editorCanvas.ToolManager.Activate("Placement");
            }
        }
        OnCurrentObjectChanged?.Invoke(obj);
    }

    public void ResetZoom()
    {
        _editorCanvas.TransformService?.Reset();
        _editorCanvas.InvalidateVisual();
    }

    public void Normalize() => Normalize(0);

    public void Normalize(int border)
    {
        if (_placedObjects == null || _placedObjects.Count == 0) return;

        var bounds = ComputeBoundingRect(_placedObjects);
        var offset = new Vector(-bounds.X + border, -bounds.Y + border);

        foreach (var obj in _placedObjects.ToList())
        {
            var oldBounds = obj.Bounds;
            obj.Position = new Point(obj.Position.X + offset.X, obj.Position.Y + offset.Y);
            _placedObjects.ReIndex(obj, oldBounds);
        }

        _adapter.SyncAllFromSource();
        ForceRendering();
    }

    public void ResetViewport()
    {
        _editorCanvas.TransformService?.Reset();
        _editorCanvas.InvalidateVisual();
    }

    public void CenterViewportOnRect(Rect gridRect)
    {
        // ponytail: basic center — just reset for now. Ceiling: proper viewport centering.
        ResetViewport();
    }

    public void RaiseStatisticsUpdated(UpdateStatisticsEventArgs args)
    {
        StatisticsUpdated?.Invoke(this, args);
    }

    public void RaiseColorsInLayoutUpdated()
    {
        ColorsInLayoutUpdated?.Invoke(this, EventArgs.Empty);
    }

    public Rect ComputeBoundingRect(IEnumerable<LayoutObject> objects)
    {
        var rect = Rect.Empty;
        foreach (var obj in objects)
        {
            rect.Union(obj.Bounds);
        }
        return rect;
    }

    public Task<bool> CheckUnsavedChanges()
    {
        // ponytail: delegate to UndoManager.IsDirty — if not dirty, allow close
        return Task.FromResult(!_undoManager.IsDirty);
    }

    public Task CheckUnsavedChangesBeforeCrash()
    {
        // ponytail: no-op for MVP
        return Task.CompletedTask;
    }

    #endregion

    #region IHotkeySource

    public HotkeyCommandManager HotkeyCommandManager { get; set; }

    public void RegisterHotkeys(HotkeyCommandManager manager)
    {
        // ponytail: stub — hotkeys not wired to EditorCanvas yet
        HotkeyCommandManager = manager;
    }

    #endregion

    #region Internal wiring

    private void OnEditorSelectionChanged(IReadOnlyList<CanvasObject> selected)
    {
        _selectedObjects.Clear();
        foreach (var canvasObj in selected)
        {
            if (canvasObj.Tag is LayoutObject lo)
                _selectedObjects.Add(lo);
        }
    }

    /// <summary>
    /// Loads a layout into the adapter and syncs the QuadTree.
    /// Called externally after loading a file.
    /// </summary>
    public void LoadLayoutObjects(IEnumerable<LayoutObject> objects)
    {
        _placedObjects.Clear();
        _adapter.ClearAll();

        foreach (var obj in objects)
        {
            _placedObjects.Add(obj);
            _adapter.AddLayoutObject(obj);
        }

        ForceRendering();
        RaiseStatisticsUpdated(UpdateStatisticsEventArgs.All);
        RaiseColorsInLayoutUpdated();
    }

    #endregion

    /// <summary>
    /// Minimal ICommand for stubs. Avoids conflict with CommunityToolkit.Mvvm.Input.RelayCommand.
    /// </summary>
    private sealed class SimpleRelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public SimpleRelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
    }
}
