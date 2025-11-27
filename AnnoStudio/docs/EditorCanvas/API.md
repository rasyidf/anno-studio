# Editor Canvas API Reference

## Core Interfaces

### IEditorTool

Represents an interactive editing tool.

```csharp
public interface IEditorTool
{
    /// <summary>
    /// Unique identifier for the tool
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Icon path or resource key
    /// </summary>
    string Icon { get; }
    
    /// <summary>
    /// Description shown in UI
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Cursor to display when tool is active
    /// </summary>
    ToolCursor Cursor { get; }
    
    /// <summary>
    /// Keyboard shortcut for activating tool
    /// </summary>
    KeyGesture Shortcut { get; }
    
    /// <summary>
    /// Called when pointer is pressed on canvas
    /// </summary>
    void OnPointerPressed(PointerEventArgs args, ICanvasContext context);
    
    /// <summary>
    /// Called when pointer moves over canvas
    /// </summary>
    void OnPointerMoved(PointerEventArgs args, ICanvasContext context);
    
    /// <summary>
    /// Called when pointer is released
    /// </summary>
    void OnPointerReleased(PointerEventArgs args, ICanvasContext context);
    
    /// <summary>
    /// Called when tool becomes active
    /// </summary>
    void OnActivated(ICanvasContext context);
    
    /// <summary>
    /// Called when tool becomes inactive
    /// </summary>
    void OnDeactivated(ICanvasContext context);
    
    /// <summary>
    /// Render tool-specific overlay
    /// </summary>
    void Render(SKCanvas canvas, ICanvasContext context);
    
    /// <summary>
    /// Called on key press while tool is active
    /// </summary>
    bool OnKeyDown(KeyEventArgs args);
    
    /// <summary>
    /// Called on key release while tool is active
    /// </summary>
    bool OnKeyUp(KeyEventArgs args);
}
```

### ICanvasContext

Provides access to canvas state and services.

```csharp
public interface ICanvasContext
{
    /// <summary>
    /// Current viewport transformation
    /// </summary>
    ViewportTransform Viewport { get; }
    
    /// <summary>
    /// Grid settings and helper methods
    /// </summary>
    IGridSystem Grid { get; }
    
    /// <summary>
    /// Currently selected objects
    /// </summary>
    ISelectionService Selection { get; }
    
    /// <summary>
    /// All objects on canvas
    /// </summary>
    IObjectCollection Objects { get; }
    
    /// <summary>
    /// Command history for undo/redo
    /// </summary>
    ICommandHistory History { get; }
    
    /// <summary>
    /// Event bus for publishing events
    /// </summary>
    ICanvasEventBus EventBus { get; }
    
    /// <summary>
    /// Settings and preferences
    /// </summary>
    EditorSettings Settings { get; }
    
    /// <summary>
    /// Convert screen point to canvas coordinates
    /// </summary>
    SKPoint ScreenToCanvas(Point screenPoint);
    
    /// <summary>
    /// Convert canvas point to screen coordinates
    /// </summary>
    Point CanvasToScreen(SKPoint canvasPoint);
    
    /// <summary>
    /// Request canvas redraw
    /// </summary>
    void Invalidate();
    
    /// <summary>
    /// Request redraw of specific region
    /// </summary>
    void Invalidate(SKRect region);
}
```

### ICanvasObject

Represents an object that can be placed on the canvas.

```csharp
public interface ICanvasObject : ISerializable, INotifyPropertyChanged
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Type identifier for deserialization
    /// </summary>
    string Type { get; }
    
    /// <summary>
    /// Display name
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Bounding rectangle in canvas coordinates
    /// </summary>
    SKRect Bounds { get; }
    
    /// <summary>
    /// 2D transformation matrix
    /// </summary>
    Transform2D Transform { get; set; }
    
    /// <summary>
    /// Layer assignment
    /// </summary>
    string Layer { get; set; }
    
    /// <summary>
    /// Visibility flag
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Lock prevents modification
    /// </summary>
    bool IsLocked { get; set; }
    
    /// <summary>
    /// Z-order within layer
    /// </summary>
    int ZOrder { get; set; }
    
    /// <summary>
    /// Test if point hits this object
    /// </summary>
    bool HitTest(SKPoint point);
    
    /// <summary>
    /// Render the object
    /// </summary>
    void Render(SKCanvas canvas, RenderContext context);
    
    /// <summary>
    /// Create deep copy
    /// </summary>
    ICanvasObject Clone();
    
    /// <summary>
    /// Get custom properties for serialization
    /// </summary>
    Dictionary<string, object> GetProperties();
    
    /// <summary>
    /// Set custom properties from deserialization
    /// </summary>
    void SetProperties(Dictionary<string, object> properties);
}
```

### ILayer

Represents a rendering layer.

```csharp
public interface ILayer
{
    /// <summary>
    /// Layer identifier
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Z-index for rendering order
    /// </summary>
    int ZIndex { get; }
    
    /// <summary>
    /// Visibility toggle
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Layer opacity (0.0 - 1.0)
    /// </summary>
    float Opacity { get; set; }
    
    /// <summary>
    /// Blend mode for compositing
    /// </summary>
    SKBlendMode BlendMode { get; set; }
    
    /// <summary>
    /// Whether layer needs redraw
    /// </summary>
    bool IsDirty { get; }
    
    /// <summary>
    /// Render layer content
    /// </summary>
    void Render(SKCanvas canvas, ICanvasContext context);
    
    /// <summary>
    /// Update layer state (called per frame)
    /// </summary>
    void Update(TimeSpan deltaTime);
    
    /// <summary>
    /// Mark layer as needing redraw
    /// </summary>
    void Invalidate();
    
    /// <summary>
    /// Called when layer is added to canvas
    /// </summary>
    void OnAttached(ICanvasContext context);
    
    /// <summary>
    /// Called when layer is removed from canvas
    /// </summary>
    void OnDetached();
}
```

### ITransformOperation

Represents a transformation that can be applied to objects.

```csharp
public interface ITransformOperation : ICommand
{
    /// <summary>
    /// Operation identifier
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Display name for UI
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Icon for UI
    /// </summary>
    string Icon { get; }
    
    /// <summary>
    /// Check if operation can execute on given objects
    /// </summary>
    bool CanExecute(IEnumerable<ICanvasObject> objects);
    
    /// <summary>
    /// Execute transformation
    /// </summary>
    void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters);
    
    /// <summary>
    /// Undo transformation
    /// </summary>
    void Undo();
    
    /// <summary>
    /// Redo transformation
    /// </summary>
    void Redo();
}
```

## Core Services

### IToolRegistry

Manages tool registration and activation.

```csharp
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool type
    /// </summary>
    void RegisterTool<T>() where T : IEditorTool, new();
    
    /// <summary>
    /// Register a tool instance
    /// </summary>
    void RegisterTool(IEditorTool tool);
    
    /// <summary>
    /// Unregister a tool
    /// </summary>
    void UnregisterTool(string toolName);
    
    /// <summary>
    /// Get tool by name
    /// </summary>
    IEditorTool GetTool(string name);
    
    /// <summary>
    /// Get all registered tools
    /// </summary>
    IEnumerable<IEditorTool> GetAllTools();
    
    /// <summary>
    /// Currently active tool
    /// </summary>
    IEditorTool ActiveTool { get; }
    
    /// <summary>
    /// Set active tool by name
    /// </summary>
    void SetActiveTool(string name);
    
    /// <summary>
    /// Event raised when active tool changes
    /// </summary>
    event EventHandler<ToolChangedEventArgs> ActiveToolChanged;
}
```

### ITransformRegistry

Manages transform operation registration.

```csharp
public interface ITransformRegistry
{
    /// <summary>
    /// Register a transform type
    /// </summary>
    void RegisterTransform<T>() where T : ITransformOperation, new();
    
    /// <summary>
    /// Register a transform instance
    /// </summary>
    void RegisterTransform(ITransformOperation transform);
    
    /// <summary>
    /// Get transform by name
    /// </summary>
    ITransformOperation GetTransform(string name);
    
    /// <summary>
    /// Execute a registered transform
    /// </summary>
    void Execute(string transformName, IEnumerable<ICanvasObject> objects, TransformParameters parameters);
    
    /// <summary>
    /// Get all registered transforms
    /// </summary>
    IEnumerable<ITransformOperation> GetAllTransforms();
}
```

### ISelectionService

Manages object selection.

```csharp
public interface ISelectionService
{
    /// <summary>
    /// Currently selected objects
    /// </summary>
    IReadOnlyList<ICanvasObject> SelectedObjects { get; }
    
    /// <summary>
    /// Selection count
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// Check if object is selected
    /// </summary>
    bool IsSelected(ICanvasObject obj);
    
    /// <summary>
    /// Select single object (clears previous selection)
    /// </summary>
    void Select(ICanvasObject obj);
    
    /// <summary>
    /// Add object to selection
    /// </summary>
    void AddToSelection(ICanvasObject obj);
    
    /// <summary>
    /// Remove object from selection
    /// </summary>
    void RemoveFromSelection(ICanvasObject obj);
    
    /// <summary>
    /// Toggle object selection state
    /// </summary>
    void ToggleSelection(ICanvasObject obj);
    
    /// <summary>
    /// Select multiple objects (clears previous selection)
    /// </summary>
    void SelectMultiple(IEnumerable<ICanvasObject> objects);
    
    /// <summary>
    /// Clear all selections
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Select all objects
    /// </summary>
    void SelectAll();
    
    /// <summary>
    /// Get bounding box of all selected objects
    /// </summary>
    SKRect GetSelectionBounds();
    
    /// <summary>
    /// Event raised when selection changes
    /// </summary>
    event EventHandler<SelectionChangedEventArgs> SelectionChanged;
}
```

### IGridSystem

Provides grid functionality.

```csharp
public interface IGridSystem
{
    /// <summary>
    /// Grid settings
    /// </summary>
    GridSettings Settings { get; set; }
    
    /// <summary>
    /// Snap point to grid
    /// </summary>
    SKPoint SnapToGrid(SKPoint point);
    
    /// <summary>
    /// Snap rectangle to grid
    /// </summary>
    SKRect SnapToGrid(SKRect rect);
    
    /// <summary>
    /// Get grid cell at point
    /// </summary>
    GridCell GetCellAt(SKPoint point);
    
    /// <summary>
    /// Convert grid coordinates to canvas coordinates
    /// </summary>
    SKPoint GridToCanvas(int gridX, int gridY);
    
    /// <summary>
    /// Convert canvas coordinates to grid coordinates
    /// </summary>
    (int x, int y) CanvasToGrid(SKPoint canvasPoint);
    
    /// <summary>
    /// Check if grid is enabled
    /// </summary>
    bool IsEnabled { get; set; }
}
```

### ICanvasSerializer

Handles serialization/deserialization.

```csharp
public interface ICanvasSerializer
{
    /// <summary>
    /// Serialize document to JSON string
    /// </summary>
    string Serialize(CanvasDocument document);
    
    /// <summary>
    /// Deserialize JSON string to document
    /// </summary>
    CanvasDocument Deserialize(string json);
    
    /// <summary>
    /// Serialize document to stream asynchronously
    /// </summary>
    Task SerializeAsync(CanvasDocument document, Stream stream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserialize stream to document asynchronously
    /// </summary>
    Task<CanvasDocument> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Serialize single object
    /// </summary>
    string SerializeObject(ICanvasObject obj);
    
    /// <summary>
    /// Deserialize single object
    /// </summary>
    ICanvasObject DeserializeObject(string json);
}
```

### ISettingsService

Manages application settings.

```csharp
public interface ISettingsService
{
    /// <summary>
    /// Get settings of specific type
    /// </summary>
    T GetSettings<T>() where T : class, new();
    
    /// <summary>
    /// Save settings of specific type
    /// </summary>
    void SaveSettings<T>(T settings) where T : class;
    
    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    void ResetToDefaults<T>() where T : class, new();
    
    /// <summary>
    /// Watch for settings changes
    /// </summary>
    IObservable<T> WatchSettings<T>() where T : class;
    
    /// <summary>
    /// Check if settings exist
    /// </summary>
    bool HasSettings<T>() where T : class;
    
    /// <summary>
    /// Event raised when any settings change
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}
```

## Data Models

### CanvasDocument

```csharp
public class CanvasDocument
{
    public string Version { get; set; } = "1.0";
    public DocumentMetadata Metadata { get; set; }
    public EditorSettings Settings { get; set; }
    public List<ICanvasObject> Objects { get; set; }
    public List<Layer> Layers { get; set; }
}
```

### Transform2D

```csharp
public struct Transform2D
{
    public SKPoint Position { get; set; }
    public float Rotation { get; set; } // degrees
    public SKPoint Scale { get; set; }
    public SKPoint Pivot { get; set; }
    
    public SKMatrix ToMatrix();
    public Transform2D Inverse();
    public SKPoint TransformPoint(SKPoint point);
}
```

### TransformParameters

```csharp
public class TransformParameters
{
    public SKPoint? DeltaPosition { get; set; }
    public float? DeltaRotation { get; set; }
    public SKPoint? DeltaScale { get; set; }
    public SKPoint? Pivot { get; set; }
    public bool SnapToGrid { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; }
}
```

### GridSettings

```csharp
public class GridSettings
{
    public float GridSize { get; set; } = 16f;
    public GridDisplayMode DisplayMode { get; set; } = GridDisplayMode.Lines;
    public SKColor Color { get; set; } = SKColors.Gray;
    public float Opacity { get; set; } = 0.3f;
    public bool SnapEnabled { get; set; } = true;
    public SKPoint Offset { get; set; }
}
```

### RenderSettings

```csharp
public class RenderSettings
{
    public SKColor BackgroundColor { get; set; } = SKColors.White;
    public bool AntiAlias { get; set; } = true;
    public SKFilterQuality FilterQuality { get; set; } = SKFilterQuality.High;
    public bool ShowBounds { get; set; } = false;
    public bool ShowOrigin { get; set; } = true;
}
```

## Events

### CanvasEvents

```csharp
public record ObjectAddedEvent(ICanvasObject Object) : ICanvasEvent;
public record ObjectRemovedEvent(ICanvasObject Object) : ICanvasEvent;
public record ObjectTransformedEvent(ICanvasObject Object, Transform2D OldTransform) : ICanvasEvent;
public record SelectionChangedEvent(IReadOnlyList<ICanvasObject> Selection) : ICanvasEvent;
public record ToolChangedEvent(IEditorTool OldTool, IEditorTool NewTool) : ICanvasEvent;
public record ViewportChangedEvent(ViewportTransform Viewport) : ICanvasEvent;
public record LayerChangedEvent(ILayer Layer, string PropertyName) : ICanvasEvent;
```

## Usage Examples

### Registering a Custom Tool

```csharp
public class MyCustomTool : IEditorTool
{
    public string Name => "MyTool";
    public string Icon => "Assets/Icons/my_tool.png";
    // ... implement interface
}

// Register
toolRegistry.RegisterTool<MyCustomTool>();
```

### Creating and Adding Objects

```csharp
var building = new BuildingObject
{
    Name = "Warehouse",
    Transform = new Transform2D
    {
        Position = new SKPoint(100, 100),
        Scale = new SKPoint(1, 1)
    }
};

context.Objects.Add(building);
```

### Executing Transforms

```csharp
var selectedObjects = selectionService.SelectedObjects;
transformRegistry.Execute("Move", selectedObjects, new TransformParameters
{
    DeltaPosition = new SKPoint(10, 0),
    SnapToGrid = true
});
```

### Serialization

```csharp
// Serialize
var document = new CanvasDocument
{
    Objects = context.Objects.ToList(),
    Settings = context.Settings
};
string json = serializer.Serialize(document);

// Deserialize
var loadedDocument = serializer.Deserialize(json);
```
