# Generic Editor Canvas Component - Architecture Documentation

## Overview

The Generic Editor Canvas Component is a modular, extensible canvas-based editing system built with Avalonia and SKIA, specifically designed for Anno Layout Design but architected for reusability across different scenarios.

## Core Design Principles

### 1. Modularity
- Tool registration system allows dynamic addition of editing tools
- Layer-based rendering system with independent layer implementations
- Command pattern for all transformations and operations
- Dependency injection for service registration

### 2. Separation of Concerns
- **Rendering Layer**: SKIA-based rendering engine completely separate from business logic
- **Tool System**: Independent tool implementations that don't depend on each other
- **Serialization**: Completely detached from file system operations
- **Settings**: Centralized preferences system independent of canvas logic

### 3. Extensibility
- Interface-based design for all major components
- Registry pattern for tools and commands
- Observable collections for reactive UI updates
- Event-driven architecture for tool communication

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  (Avalonia Controls, ViewModels with CommunityToolkit.MVVM) │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│         (Canvas Orchestration, Tool Management)              │
└─────────────────────────────────────────────────────────────┘
                            │
┌────────────────────┬────────────────────┬────────────────────┐
│   Rendering Engine │    Tool System     │  Transform System  │
│   (SKIA Layers)    │  (Modular Tools)   │   (Operations)     │
└────────────────────┴────────────────────┴────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                        Core Domain                           │
│     (Canvas Objects, Grid System, Serialization)             │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│      (Settings, Preferences, JSON Serialization)             │
└─────────────────────────────────────────────────────────────┘
```

## Component Structure

### 1. Core Interfaces

#### IEditorTool
Base interface for all editing tools (Draw, Line, Stamp, Rect, etc.)
```csharp
public interface IEditorTool
{
    string Name { get; }
    string Icon { get; }
    ToolCursor Cursor { get; }
    
    void OnPointerPressed(PointerEventArgs args, ICanvasContext context);
    void OnPointerMoved(PointerEventArgs args, ICanvasContext context);
    void OnPointerReleased(PointerEventArgs args, ICanvasContext context);
    void OnActivated(ICanvasContext context);
    void OnDeactivated(ICanvasContext context);
    void Render(SKCanvas canvas, ICanvasContext context);
}
```

#### ITransformOperation
Base interface for transform operations (Move, Rotate, Duplicate, Resize, Select)
```csharp
public interface ITransformOperation
{
    string Name { get; }
    bool CanExecute(IEnumerable<ICanvasObject> objects);
    void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters);
    void Undo();
}
```

#### ILayer
Base interface for rendering layers
```csharp
public interface ILayer
{
    string Name { get; }
    int ZIndex { get; }
    bool IsVisible { get; set; }
    float Opacity { get; set; }
    
    void Render(SKCanvas canvas, ICanvasContext context);
    void Update(TimeSpan deltaTime);
}
```

#### ICanvasObject
Base interface for objects on the canvas
```csharp
public interface ICanvasObject : ISerializable
{
    Guid Id { get; }
    string Type { get; }
    SKRect Bounds { get; }
    Transform2D Transform { get; set; }
    
    bool HitTest(SKPoint point);
    void Render(SKCanvas canvas, RenderContext context);
    ICanvasObject Clone();
}
```

### 2. Layer System

The rendering system is organized in layers with specific z-order:

1. **GridLayer** (Z: -100): Renders the background grid
2. **GuidelinesLayer** (Z: -50): Renders alignment guidelines
3. **ObjectLayer** (Z: 0): Renders canvas objects (buildings, decorations)
4. **EffectLayer** (Z: 50): Renders area of effect circles/radii anchored to objects
5. **SelectionLayer** (Z: 100): Renders selection boxes and handles
6. **ToolOverlayLayer** (Z: 150): Renders active tool preview/overlay
7. **DebugLayer** (Z: 200): Optional debug information

Each layer is independently toggleable and configurable.

### 3. Grid System

The grid-based system supports:
- Configurable grid size (default: aligned to Anno building units)
- Snap-to-grid functionality
- Grid offset and rotation
- Multiple grid display modes (dots, lines, cross)
- Isometric and orthographic projections

### 4. Tool Registration System

Tools are registered through a centralized registry:

```csharp
public interface IToolRegistry
{
    void RegisterTool<T>() where T : IEditorTool;
    void RegisterTool(IEditorTool tool);
    IEditorTool GetTool(string name);
    IEnumerable<IEditorTool> GetAllTools();
    void SetActiveTool(string name);
}
```

Default tools include:
- **DrawTool**: Freehand drawing
- **LineTool**: Straight line drawing
- **StampTool**: Place predefined objects
- **RectTool**: Rectangle/building placement

### 5. Transform System

Transform operations are registered as commands:

```csharp
public interface ITransformRegistry
{
    void RegisterTransform<T>() where T : ITransformOperation;
    void RegisterTransform(ITransformOperation transform);
    ITransformOperation GetTransform(string name);
    void Execute(string transformName, IEnumerable<ICanvasObject> objects, TransformParameters parameters);
}
```

Available transforms:
- **MoveTransform**: Translate objects
- **RotateTransform**: Rotate objects around pivot
- **DuplicateTransform**: Clone objects
- **ResizeTransform**: Scale objects
- **SelectTransform**: Multi-selection with marquee

### 6. Settings and Preferences

Centralized settings system using JSON serialization:

```csharp
public class EditorSettings
{
    public GridSettings Grid { get; set; }
    public RenderSettings Render { get; set; }
    public ToolSettings Tools { get; set; }
    public KeyBindings KeyBindings { get; set; }
    public ThemeSettings Theme { get; set; }
}
```

Settings are managed through:
```csharp
public interface ISettingsService
{
    T GetSettings<T>() where T : class;
    void SaveSettings<T>(T settings) where T : class;
    void ResetToDefaults<T>() where T : class;
    IObservable<T> WatchSettings<T>() where T : class;
}
```

### 7. Serialization System

Complete separation from file system:

```csharp
public interface ICanvasSerializer
{
    string Serialize(CanvasDocument document);
    CanvasDocument Deserialize(string json);
    
    // Stream-based for large files
    Task SerializeAsync(CanvasDocument document, Stream stream);
    Task<CanvasDocument> DeserializeAsync(Stream stream);
}
```

The serialization format is version-tagged for forward compatibility:
```json
{
  "version": "1.0",
  "metadata": {
    "created": "2025-11-27T10:30:00Z",
    "modified": "2025-11-27T11:45:00Z",
    "author": "user"
  },
  "settings": {
    "gridSize": 16,
    "snapToGrid": true
  },
  "objects": [
    {
      "id": "guid",
      "type": "Building",
      "transform": {...},
      "properties": {...}
    }
  ]
}
```

## Event System

The canvas uses an event-driven architecture for communication:

```csharp
public interface ICanvasEventBus
{
    void Publish<T>(T eventData) where T : ICanvasEvent;
    IDisposable Subscribe<T>(Action<T> handler) where T : ICanvasEvent;
}
```

Key events:
- `ObjectAddedEvent`
- `ObjectRemovedEvent`
- `ObjectTransformedEvent`
- `SelectionChangedEvent`
- `ToolChangedEvent`
- `ViewportChangedEvent`

## MVVM Integration

Using CommunityToolkit.MVVM for ViewModels:

```csharp
[ObservableObject]
public partial class EditorCanvasViewModel
{
    [ObservableProperty]
    private ObservableCollection<ICanvasObject> _objects;
    
    [ObservableProperty]
    private IEditorTool _activeTool;
    
    [RelayCommand]
    private void AddObject(ICanvasObject obj)
    {
        Objects.Add(obj);
        _eventBus.Publish(new ObjectAddedEvent(obj));
    }
}
```

## Performance Considerations

1. **Dirty Rectangle Rendering**: Only redraw changed regions
2. **Object Culling**: Don't render objects outside viewport
3. **Layer Caching**: Cache layer renders when unchanged
4. **Virtual Scrolling**: For large object collections in panels
5. **Debounced Updates**: For settings changes and property updates

## Extensibility Points

The system can be extended through:

1. **Custom Tools**: Implement `IEditorTool`
2. **Custom Layers**: Implement `ILayer`
3. **Custom Objects**: Implement `ICanvasObject`
4. **Custom Transforms**: Implement `ITransformOperation`
5. **Custom Serializers**: Implement `ICanvasSerializer` for different formats
6. **Plugin System**: Load tools and layers from external assemblies

## Future Considerations

- Collaborative editing support
- Undo/Redo stack persistence
- Animation timeline for object properties
- Custom shader support for effects
- WebGL/WebGPU export for web preview
- Scripting API for automation

## Dependencies

- Avalonia 11.x
- SkiaSharp
- CommunityToolkit.MVVM
- System.Text.Json
- Microsoft.Extensions.DependencyInjection

## File Organization

```
AnnoStudio/
├── EditorCanvas/
│   ├── Core/
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   └── Services/
│   ├── Rendering/
│   │   ├── Layers/
│   │   ├── RenderContext.cs
│   │   └── CanvasRenderer.cs
│   ├── Tools/
│   │   ├── Base/
│   │   ├── DrawTool.cs
│   │   ├── LineTool.cs
│   │   ├── StampTool.cs
│   │   └── RectTool.cs
│   ├── Transforms/
│   │   ├── MoveTransform.cs
│   │   ├── RotateTransform.cs
│   │   ├── DuplicateTransform.cs
│   │   ├── ResizeTransform.cs
│   │   └── SelectTransform.cs
│   ├── Serialization/
│   │   ├── JsonCanvasSerializer.cs
│   │   └── Converters/
│   ├── Settings/
│   │   ├── EditorSettings.cs
│   │   └── SettingsService.cs
│   ├── ViewModels/
│   │   └── EditorCanvasViewModel.cs
│   └── Controls/
│       └── EditorCanvas.axaml(.cs)
└── docs/
    └── EditorCanvas/
        ├── Architecture.md
        ├── API.md
        └── Usage.md
```
