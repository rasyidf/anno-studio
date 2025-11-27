using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;
using AnnoStudio.EditorCanvas.Core.Services;
using AnnoStudio.EditorCanvas.Rendering.Layers;

namespace AnnoStudio.EditorCanvas.Controls;

/// <summary>
/// Main editor canvas control integrating all systems.
/// </summary>
public partial class EditorCanvas : UserControl, ICanvasContext
{
    private SkiaCanvasControl? _skiaCanvas;
    private IEditorTool? _activeTool;
    private readonly List<ILayer> _layers = new();
    private bool _isPanning;
    private SKPoint _panStartPos;
    // cursor position is published via EventBus; layers/services can track it themselves

    // ICanvasContext implementation
    public ViewportTransform Viewport { get; } = new();
    public IGridSystem Grid { get; }
    public ISelectionService Selection { get; }
    public IObjectCollection Objects { get; }
    public ICommandHistory History { get; }
    public ICanvasEventBus EventBus { get; }
    public EditorSettings Settings { get; }
    
    // New services
    public KeyboardShortcutManager Shortcuts { get; }
    public ContextMenuService ContextMenus { get; }
    public IOverlayService OverlayService { get; }

    // Properties
    public IEditorTool? ActiveTool
    {
        get => _activeTool;
        set
        {
            if (_activeTool == value)
                return;

            _activeTool?.OnDeactivated(this);
            _activeTool = value;
            _activeTool?.OnActivated(this);
        }
    }

    public IReadOnlyList<ILayer> Layers => _layers.AsReadOnly();

    public EditorCanvas()
    {
        // Initialize services
        Settings = new EditorSettings();
        Grid = new GridSystem();
        Selection = new SelectionService();
        Objects = new ObjectCollection();
        History = new CommandHistory();
        EventBus = new CanvasEventBus();
        Shortcuts = new KeyboardShortcutManager();
        ContextMenus = new ContextMenuService();

        // Initialize layers
        _layers.Add(new GridLayer());
        _layers.Add(new ObjectLayer());
        _layers.Add(new SelectionLayer());
        _layers.Add(new ToolOverlayLayer());

        // Overlay service used by layers for debugging / overlays
        OverlayService = new OverlayService();

        // Attach layers so they can use the canvas context (e.g. events)
        foreach (var layer in _layers)
            layer.OnAttached(this);

        InitializeComponent();
        SetupEventHandlers();
        InitializeDefaultShortcuts();
        InitializeContextMenus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _skiaCanvas = this.FindControl<SkiaCanvasControl>("SkiaCanvas");

        if (_skiaCanvas != null)
        {
            _skiaCanvas.PaintSurface += OnPaintSurface;
        }
        
        // Ensure canvas gets focus when attached to visual tree
        AttachedToVisualTree += (s, e) => 
        {
            Focus();
        };
    }

    private void SetupEventHandlers()
    {
        // Viewport changed
        Viewport.Changed += (s, e) => Invalidate();

        // Selection changed
        Selection.SelectionChanged += (s, e) => Invalidate();

        // Objects changed
        Objects.CollectionChanged += (s, e) => Invalidate();

        // Pointer events
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;

        // Keyboard events
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

    private void OnPaintSurface(SKCanvas canvas)
    {
        if (canvas == null)
            return;

        // Clear background
        canvas.Clear(Settings.Render.BackgroundColor);

        // Save canvas state
        canvas.Save();

        // Apply viewport transform
        var matrix = Viewport.GetMatrix();
        canvas.SetMatrix(matrix);

        // Render layers in order
        foreach (var layer in _layers.OrderBy(l => l.ZIndex))
        {
            if (layer.IsVisible)
            {
                canvas.Save();
                layer.Render(canvas, this);
                canvas.Restore();
            }
        }

        // Active tool overlay and debug overlays are rendered by ToolOverlayLayer

        // Optional debug overlay (canvas-space) - shows pointer canvas coordinate and grid cell
        // overlays handled by layer

        // Restore canvas state
        canvas.Restore();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        
        var props = e.GetCurrentPoint(this).Properties;
        
        // Middle mouse button for panning
        if (props.IsMiddleButtonPressed)
        {
            _isPanning = true;
            var pos = e.GetPosition(this);
            _panStartPos = new SKPoint((float)pos.X, (float)pos.Y);
            e.Handled = true;
            return;
        }
        
        _activeTool?.OnPointerPressed(e, this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // Track cursor position for status bar
        var position = e.GetPosition(this);
        var canvasPos = ScreenToCanvas(position);
        // publishing position to the event bus so overlay layer/service can render
        // and optionally request invalidation there
        EventBus.PublishCursorPositionChanged(canvasPos);
        
        // Handle panning
        if (_isPanning)
        {
            var currentPos = e.GetPosition(this);
            var currentPosF = new SKPoint((float)currentPos.X, (float)currentPos.Y);
            var delta = new SKPoint(
                currentPosF.X - _panStartPos.X,
                currentPosF.Y - _panStartPos.Y
            );
            
            Viewport.Pan = new SKPoint(
                Viewport.Pan.X + delta.X,
                Viewport.Pan.Y + delta.Y
            );
            
            _panStartPos = currentPosF;
            e.Handled = true;
            return;
        }
        
        _activeTool?.OnPointerMoved(e, this);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Handled = true;
            return;
        }
        
        _activeTool?.OnPointerReleased(e, this);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Get mouse position relative to canvas
        var mousePos = e.GetPosition(this);
        var mousePosF = new SKPoint((float)mousePos.X, (float)mousePos.Y);
        
        // Get canvas position before zoom
        var canvasPosBefore = ScreenToCanvas(mousePos);

        // Zoom with mouse wheel
        var delta = e.Delta.Y;
        var zoomFactor = delta > 0 ? 1.1f : 0.9f;
        Viewport.Zoom *= zoomFactor;

        // Get canvas position after zoom
        var canvasPosAfter = ScreenToCanvas(mousePos);
        
        // Adjust pan to keep mouse position fixed
        var offset = new SKPoint(
            canvasPosAfter.X - canvasPosBefore.X,
            canvasPosAfter.Y - canvasPosBefore.Y
        );

        Viewport.Pan = new SKPoint(
            Viewport.Pan.X - offset.X * Viewport.Zoom,
            Viewport.Pan.Y - offset.Y * Viewport.Zoom
        );

        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // First, try global shortcuts
        if (Shortcuts.HandleKeyDown(e))
        {
            e.Handled = true;
            return;
        }

        // Then, give active tool a chance to handle
        var handled = _activeTool?.OnKeyDown(e) ?? false;
        if (handled)
        {
            e.Handled = true;
            return;
        }

        // Handle arrow key nudging
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            var nudgeDistance = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10f : 1f;
            var nudgeOffset = SKPoint.Empty;

            switch (e.Key)
            {
                case Key.Left:
                    nudgeOffset = new SKPoint(-nudgeDistance, 0);
                    break;
                case Key.Right:
                    nudgeOffset = new SKPoint(nudgeDistance, 0);
                    break;
                case Key.Up:
                    nudgeOffset = new SKPoint(0, -nudgeDistance);
                    break;
                case Key.Down:
                    nudgeOffset = new SKPoint(0, nudgeDistance);
                    break;
            }

            if (nudgeOffset != SKPoint.Empty && Selection.SelectedObjects.Any())
            {
                NudgeSelected(nudgeOffset);
                e.Handled = true;
                return;
            }
        }

        
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        _activeTool?.OnKeyUp(e);
    }

    private void DeleteSelected()
    {
        var selected = Selection.SelectedObjects.ToList();
        foreach (var obj in selected)
        {
            Objects.Remove(obj);
        }
        Selection.Clear();
    }

    // ICanvasContext methods
    public SKPoint ScreenToCanvas(Point screenPoint)
    {
        return Viewport.ScreenToCanvas(new SKPoint((float)screenPoint.X, (float)screenPoint.Y));
    }

    public Point CanvasToScreen(SKPoint canvasPoint)
    {
        var screen = Viewport.CanvasToScreen(canvasPoint);
        return new Point(screen.X, screen.Y);
    }

    public void Invalidate()
    {
        _skiaCanvas?.InvalidateVisual();
    }

    public void Invalidate(SKRect region)
    {
        // For simplicity, invalidate entire canvas
        Invalidate();
    }

    // Document management
    public CanvasDocument GetDocument()
    {
        return new CanvasDocument
        {
            Objects = Objects.ToList()
        };
    }

    public void LoadDocument(CanvasDocument document)
    {
        Objects.Clear();
        Selection.Clear();

        foreach (var obj in document.Objects)
        {
            Objects.Add(obj);
        }

        Invalidate();
    }

    public void Clear()
    {
        Objects.Clear();
        Selection.Clear();
        History.Clear();
        Invalidate();
    }

    private void InitializeDefaultShortcuts()
    {
        // Edit shortcuts
        Shortcuts.RegisterShortcut(Key.Z, KeyModifiers.Control, () => History.Undo(), "Undo", "Undo last action");
        Shortcuts.RegisterShortcut(Key.Y, KeyModifiers.Control, () => History.Redo(), "Redo", "Redo last undone action");
        Shortcuts.RegisterShortcut(Key.Z, KeyModifiers.Control | KeyModifiers.Shift, () => History.Redo(), "RedoAlt", "Redo (alternative)");
        
        // Selection shortcuts
        Shortcuts.RegisterShortcut(Key.A, KeyModifiers.Control, () => Selection.SelectAll(Objects), "SelectAll", "Select all objects");
        Shortcuts.RegisterShortcut(Key.D, KeyModifiers.Control, DuplicateSelected, "Duplicate", "Duplicate selected objects");
        Shortcuts.RegisterShortcut(Key.Delete, DeleteSelected, "Delete", "Delete selected objects");
        Shortcuts.RegisterShortcut(Key.Back, DeleteSelected, "DeleteAlt", "Delete selected objects (backspace)");
        
        // Escape to clear selection
        Shortcuts.RegisterShortcut(Key.Escape, () => Selection.Clear(), "ClearSelection", "Clear selection");
    }

    private void InitializeContextMenus()
    {
        // Object context menu actions
        ContextMenus.RegisterObjectAction("Delete", "Delete", 
            (obj, ctx) => { Objects.Remove(obj); Selection.Clear(); },
            obj => true, "delete_icon");
            
        ContextMenus.RegisterObjectAction("Duplicate", "Duplicate",
            (obj, ctx) => DuplicateObject(obj),
            obj => true, "duplicate_icon");
            
        ContextMenus.RegisterObjectAction("BringToFront", "Bring to Front",
            (obj, ctx) => BringToFront(obj),
            obj => true, "bring_to_front_icon");
            
        ContextMenus.RegisterObjectAction("SendToBack", "Send to Back",
            (obj, ctx) => SendToBack(obj),
            obj => true, "send_to_back_icon");
        
        ContextMenus.RegisterObjectAction("Properties", "Properties...",
            (obj, ctx) => ShowObjectProperties(obj),
            obj => true, "properties_icon");

        // Canvas context menu actions
        ContextMenus.RegisterCanvasAction("Paste", "Paste",
            ctx => PasteFromClipboard(),
            ctx => true, "paste_icon");
            
        ContextMenus.RegisterCanvasAction("SelectAll", "Select All",
            ctx => Selection.SelectAll(Objects),
            ctx => Objects.Any(), "select_all_icon");
    }

    private void NudgeSelected(SKPoint offset)
    {
        foreach (var obj in Selection.SelectedObjects)
        {
            obj.Transform = new Transform2D
            {
                Position = new SKPoint(
                    obj.Transform.Position.X + offset.X,
                    obj.Transform.Position.Y + offset.Y
                ),
                Rotation = obj.Transform.Rotation,
                Scale = obj.Transform.Scale
            };
        }
        Invalidate();
    }

    private void DuplicateSelected()
    {
        var duplicates = new List<ICanvasObject>();
        foreach (var obj in Selection.SelectedObjects.ToList())
        {
            var duplicate = DuplicateObject(obj);
            duplicates.Add(duplicate);
        }
        
        Selection.Clear();
        foreach (var dup in duplicates)
        {
            Selection.AddToSelection(dup);
        }
    }

    private ICanvasObject DuplicateObject(ICanvasObject original)
    {
        // This is a simple clone - may need to be more sophisticated based on object type
        var duplicate = (ICanvasObject)Activator.CreateInstance(original.GetType())!;
        
        // Copy transform with offset
        duplicate.Transform = new Transform2D
        {
            Position = new SKPoint(
                original.Transform.Position.X + 20,
                original.Transform.Position.Y + 20
            ),
            Rotation = original.Transform.Rotation,
            Scale = original.Transform.Scale
        };
        
        Objects.Add(duplicate);
        return duplicate;
    }

    private void BringToFront(ICanvasObject obj)
    {
        // Move object to end of collection (drawn last = on top)
        if (Objects.Contains(obj))
        {
            Objects.Remove(obj);
            Objects.Add(obj);
            Invalidate();
        }
    }

    private void SendToBack(ICanvasObject obj)
    {
        // Move object to beginning (drawn first = in back)
        // Note: This is simplified - a proper implementation would need indexed collection
        if (Objects.Contains(obj))
        {
            var allObjects = Objects.ToList();
            Objects.Clear();
            Objects.Add(obj);
            foreach (var other in allObjects.Where(o => o != obj))
            {
                Objects.Add(other);
            }
            Invalidate();
        }
    }

    private void ShowObjectProperties(ICanvasObject obj)
    {
        // Raise event that can be handled by the view model
        // TODO: Implement event bus method for properties request
        // EventBus.PublishObjectPropertiesRequested(obj);
    }

    private void PasteFromClipboard()
    {
        // TODO: Implement clipboard paste
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        // Right-click for context menu
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            var position = e.GetPosition(this);
            var worldPos = ScreenToCanvas(position);

            // Check if clicking on an object
            var clickedObject = Objects
                .Reverse()
                .FirstOrDefault(obj => obj.HitTest(worldPos));

            ContextMenu? menu;
            if (clickedObject != null)
            {
                // Select the object if not already selected
                if (!Selection.IsSelected(clickedObject))
                {
                    Selection.Clear();
                    Selection.AddToSelection(clickedObject);
                }
                
                menu = ContextMenus.BuildObjectContextMenu(clickedObject, this);
            }
            else
            {
                menu = ContextMenus.BuildCanvasContextMenu(this);
            }

            if (menu.ItemsSource is not null)
            {
                menu.Open(this);
            }
            
            e.Handled = true;
        }
    }
}
