# Editor Canvas Usage Guide

## Getting Started

### Basic Setup

1. **Add required NuGet packages** (if not already present):
```xml
<PackageReference Include="Avalonia" Version="11.0.*" />
<PackageReference Include="Avalonia.Skia" Version="11.0.*" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.*" />
<PackageReference Include="System.Text.Json" Version="8.0.*" />
```

2. **Register services in your App.axaml.cs**:
```csharp
public override void OnFrameworkInitializationCompleted()
{
    var services = new ServiceCollection();
    
    // Register core services
    services.AddSingleton<IToolRegistry, ToolRegistry>();
    services.AddSingleton<ITransformRegistry, TransformRegistry>();
    services.AddSingleton<ISelectionService, SelectionService>();
    services.AddSingleton<ICanvasSerializer, JsonCanvasSerializer>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<ICanvasEventBus, CanvasEventBus>();
    
    // Register default tools
    var toolRegistry = services.BuildServiceProvider().GetService<IToolRegistry>();
    toolRegistry.RegisterTool<DrawTool>();
    toolRegistry.RegisterTool<LineTool>();
    toolRegistry.RegisterTool<StampTool>();
    toolRegistry.RegisterTool<RectTool>();
    
    // Register default transforms
    var transformRegistry = services.BuildServiceProvider().GetService<ITransformRegistry>();
    transformRegistry.RegisterTransform<MoveTransform>();
    transformRegistry.RegisterTransform<RotateTransform>();
    transformRegistry.RegisterTransform<ResizeTransform>();
    transformRegistry.RegisterTransform<DuplicateTransform>();
    transformRegistry.RegisterTransform<SelectTransform>();
    
    base.OnFrameworkInitializationCompleted();
}
```

3. **Add EditorCanvas to your view**:
```xaml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ec="using:AnnoStudio.EditorCanvas.Controls"
        x:Class="AnnoStudio.Views.MainWindow">
    
    <ec:EditorCanvas x:Name="Canvas"
                     Background="White"
                     GridSize="16"
                     ShowGrid="True"
                     SnapToGrid="True" />
</Window>
```

## Working with Tools

### Activating Tools

```csharp
// From code-behind
var toolRegistry = ServiceLocator.Get<IToolRegistry>();
toolRegistry.SetActiveTool("Stamp");

// From ViewModel with MVVM
[RelayCommand]
private void ActivateTool(string toolName)
{
    _toolRegistry.SetActiveTool(toolName);
}
```

### Creating a Tool Palette

```xaml
<ItemsControl ItemsSource="{Binding AvailableTools}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Button Command="{Binding $parent[Window].DataContext.ActivateToolCommand}"
                    CommandParameter="{Binding Name}"
                    ToolTip.Tip="{Binding Description}">
                <Image Source="{Binding Icon}" Width="24" Height="24"/>
            </Button>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### Implementing a Custom Tool

```csharp
public class CircleTool : EditorToolBase
{
    private SKPoint? _startPoint;
    
    public override string Name => "Circle";
    public override string Icon => "Assets/Icons/circle.png";
    public override string Description => "Draw circles";
    
    public override void OnPointerPressed(PointerEventArgs args, ICanvasContext context)
    {
        var point = context.ScreenToCanvas(args.GetPosition(args.Source as Visual));
        _startPoint = context.Grid.SnapToGrid(point);
    }
    
    public override void OnPointerMoved(PointerEventArgs args, ICanvasContext context)
    {
        if (_startPoint.HasValue)
        {
            context.Invalidate(); // Request redraw for preview
        }
    }
    
    public override void OnPointerReleased(PointerEventArgs args, ICanvasContext context)
    {
        if (_startPoint.HasValue)
        {
            var endPoint = context.ScreenToCanvas(args.GetPosition(args.Source as Visual));
            var radius = SKPoint.Distance(_startPoint.Value, endPoint);
            
            var circle = new CircleObject
            {
                Center = _startPoint.Value,
                Radius = radius
            };
            
            context.Objects.Add(circle);
            _startPoint = null;
        }
    }
    
    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (_startPoint.HasValue && _currentPoint.HasValue)
        {
            var radius = SKPoint.Distance(_startPoint.Value, _currentPoint.Value);
            using var paint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(128),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            };
            
            canvas.DrawCircle(_startPoint.Value, radius, paint);
        }
    }
}
```

## Working with Objects

### Creating Custom Canvas Objects

```csharp
public class BuildingObject : CanvasObjectBase
{
    public override string Type => "Building";
    
    public string BuildingType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public SKBitmap Icon { get; set; }
    
    public override void Render(SKCanvas canvas, RenderContext context)
    {
        using var paint = new SKPaint { IsAntialias = true };
        
        // Draw building footprint
        var rect = new SKRect(0, 0, Width * context.GridSize, Height * context.GridSize);
        canvas.Save();
        canvas.Concat(Transform.ToMatrix());
        
        // Draw background
        paint.Color = SKColors.LightGray;
        canvas.DrawRect(rect, paint);
        
        // Draw icon
        if (Icon != null)
        {
            canvas.DrawBitmap(Icon, rect);
        }
        
        // Draw border
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = SKColors.Black;
        paint.StrokeWidth = 2;
        canvas.DrawRect(rect, paint);
        
        canvas.Restore();
    }
    
    public override bool HitTest(SKPoint point)
    {
        var rect = new SKRect(0, 0, Width, Height);
        var inverse = Transform.Inverse();
        var localPoint = inverse.TransformPoint(point);
        return rect.Contains(localPoint);
    }
    
    public override Dictionary<string, object> GetProperties()
    {
        return new Dictionary<string, object>
        {
            ["BuildingType"] = BuildingType,
            ["Width"] = Width,
            ["Height"] = Height
        };
    }
    
    public override void SetProperties(Dictionary<string, object> properties)
    {
        BuildingType = properties["BuildingType"]?.ToString();
        Width = Convert.ToInt32(properties["Width"]);
        Height = Convert.ToInt32(properties["Height"]);
    }
}
```

### Adding Objects to Canvas

```csharp
// Create object
var warehouse = new BuildingObject
{
    Name = "Warehouse",
    BuildingType = "Production",
    Width = 4,
    Height = 3,
    Transform = new Transform2D
    {
        Position = new SKPoint(100, 100),
        Scale = new SKPoint(1, 1),
        Rotation = 0
    }
};

// Add to canvas
canvasContext.Objects.Add(warehouse);

// Or from ViewModel
[RelayCommand]
private void AddBuilding()
{
    var building = CreateBuilding();
    Objects.Add(building);
    _eventBus.Publish(new ObjectAddedEvent(building));
}
```

## Working with Selection

### Basic Selection

```csharp
// Select single object
selectionService.Select(myObject);

// Add to selection
selectionService.AddToSelection(anotherObject);

// Select multiple
selectionService.SelectMultiple(new[] { obj1, obj2, obj3 });

// Clear selection
selectionService.Clear();
```

### Selection in ViewModel

```csharp
[ObservableObject]
public partial class EditorCanvasViewModel
{
    private readonly ISelectionService _selectionService;
    
    [ObservableProperty]
    private IReadOnlyList<ICanvasObject> _selectedObjects;
    
    public EditorCanvasViewModel(ISelectionService selectionService)
    {
        _selectionService = selectionService;
        _selectionService.SelectionChanged += OnSelectionChanged;
    }
    
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedObjects = _selectionService.SelectedObjects;
    }
    
    [RelayCommand]
    private void DeleteSelected()
    {
        foreach (var obj in SelectedObjects.ToList())
        {
            Objects.Remove(obj);
        }
        _selectionService.Clear();
    }
}
```

## Working with Transforms

### Applying Transforms

```csharp
// Move selected objects
var moveParams = new TransformParameters
{
    DeltaPosition = new SKPoint(10, 0),
    SnapToGrid = true
};
transformRegistry.Execute("Move", selectionService.SelectedObjects, moveParams);

// Rotate selected objects
var rotateParams = new TransformParameters
{
    DeltaRotation = 45,
    Pivot = selectionService.GetSelectionBounds().Center
};
transformRegistry.Execute("Rotate", selectionService.SelectedObjects, rotateParams);

// Duplicate selected objects
transformRegistry.Execute("Duplicate", selectionService.SelectedObjects, new TransformParameters());
```

### Creating Custom Transforms

```csharp
public class FlipHorizontalTransform : TransformOperationBase
{
    public override string Name => "FlipHorizontal";
    public override string DisplayName => "Flip Horizontal";
    
    private List<(ICanvasObject obj, Transform2D oldTransform)> _previousStates;
    
    public override bool CanExecute(IEnumerable<ICanvasObject> objects)
    {
        return objects?.Any() == true;
    }
    
    public override void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        _previousStates = new List<(ICanvasObject, Transform2D)>();
        
        foreach (var obj in objects)
        {
            _previousStates.Add((obj, obj.Transform));
            
            var transform = obj.Transform;
            transform.Scale = new SKPoint(-transform.Scale.X, transform.Scale.Y);
            obj.Transform = transform;
        }
    }
    
    public override void Undo()
    {
        foreach (var (obj, oldTransform) in _previousStates)
        {
            obj.Transform = oldTransform;
        }
    }
}
```

## Working with Layers

### Creating Custom Layers

```csharp
public class EffectLayer : LayerBase
{
    public override string Name => "Effects";
    public override int ZIndex => 50;
    
    private List<ICanvasObject> _objectsWithEffects;
    
    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!IsVisible) return;
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        
        foreach (var obj in _objectsWithEffects)
        {
            if (obj is IHasAreaOfEffect effectObj)
            {
                paint.Color = effectObj.EffectColor.WithAlpha((byte)(255 * Opacity));
                canvas.DrawCircle(obj.Transform.Position, effectObj.EffectRadius, paint);
            }
        }
    }
    
    public override void Update(TimeSpan deltaTime)
    {
        // Update effect animations if any
    }
}
```

### Managing Layers

```csharp
// Add layer to canvas
canvasRenderer.AddLayer(new EffectLayer());

// Toggle layer visibility
var gridLayer = canvasRenderer.GetLayer("Grid");
gridLayer.IsVisible = !gridLayer.IsVisible;

// Change layer opacity
var guidelinesLayer = canvasRenderer.GetLayer("Guidelines");
guidelinesLayer.Opacity = 0.5f;
```

## Working with Grid

### Grid Configuration

```csharp
// Configure grid via settings
var gridSettings = new GridSettings
{
    GridSize = 16f,
    DisplayMode = GridDisplayMode.Lines,
    Color = SKColors.Gray,
    Opacity = 0.3f,
    SnapEnabled = true
};

context.Grid.Settings = gridSettings;
```

### Grid Snapping

```csharp
// Snap point to grid
var snappedPoint = gridSystem.SnapToGrid(rawPoint);

// Snap rectangle
var snappedRect = gridSystem.SnapToGrid(rawRect);

// Convert between grid and canvas coordinates
var canvasPoint = gridSystem.GridToCanvas(5, 3);
var (gridX, gridY) = gridSystem.CanvasToGrid(new SKPoint(100, 100));
```

## Serialization and Persistence

### Saving Canvas

```csharp
public async Task SaveCanvasAsync(string filePath)
{
    var document = new CanvasDocument
    {
        Metadata = new DocumentMetadata
        {
            Created = DateTime.UtcNow,
            Author = Environment.UserName
        },
        Settings = _canvasContext.Settings,
        Objects = _canvasContext.Objects.ToList()
    };
    
    using var stream = File.Create(filePath);
    await _serializer.SerializeAsync(document, stream);
}
```

### Loading Canvas

```csharp
public async Task LoadCanvasAsync(string filePath)
{
    using var stream = File.OpenRead(filePath);
    var document = await _serializer.DeserializeAsync(stream);
    
    // Apply settings
    _canvasContext.Settings = document.Settings;
    
    // Load objects
    Objects.Clear();
    foreach (var obj in document.Objects)
    {
        Objects.Add(obj);
    }
}
```

### Export/Import Specific Objects

```csharp
// Export selected objects to clipboard
var selectedJson = _serializer.Serialize(new CanvasDocument
{
    Objects = _selectionService.SelectedObjects.ToList()
});
await Clipboard.SetTextAsync(selectedJson);

// Import from clipboard
var json = await Clipboard.GetTextAsync();
var document = _serializer.Deserialize(json);
foreach (var obj in document.Objects)
{
    Objects.Add(obj);
}
```

## Settings Management

### Accessing Settings

```csharp
// Get settings
var renderSettings = settingsService.GetSettings<RenderSettings>();

// Modify and save
renderSettings.AntiAlias = true;
renderSettings.FilterQuality = SKFilterQuality.High;
settingsService.SaveSettings(renderSettings);

// Watch for changes
settingsService.WatchSettings<RenderSettings>()
    .Subscribe(settings =>
    {
        // React to settings changes
        UpdateRenderQuality(settings);
    });
```

### Custom Settings

```csharp
public class MyCustomSettings
{
    public bool EnableFeature { get; set; }
    public int MaxObjects { get; set; } = 1000;
    public Dictionary<string, string> CustomData { get; set; }
}

// Use it
var mySettings = settingsService.GetSettings<MyCustomSettings>();
```

## Event Handling

### Subscribing to Events

```csharp
// Subscribe to specific event
eventBus.Subscribe<ObjectAddedEvent>(e =>
{
    Console.WriteLine($"Object added: {e.Object.Name}");
});

// Subscribe in ViewModel
public class CanvasViewModel : ViewModelBase
{
    private readonly IDisposable _subscription;
    
    public CanvasViewModel(ICanvasEventBus eventBus)
    {
        _subscription = eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
    }
    
    private void OnSelectionChanged(SelectionChangedEvent e)
    {
        SelectedCount = e.Selection.Count;
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Publishing Events

```csharp
// Publish event
eventBus.Publish(new ObjectTransformedEvent(myObject, oldTransform));

// Custom events
public record CustomValidationEvent(bool IsValid, string Message) : ICanvasEvent;

eventBus.Publish(new CustomValidationEvent(true, "Validation passed"));
```

## Advanced Scenarios

### Multi-Selection with Marquee

```csharp
public class SelectTool : EditorToolBase
{
    private SKPoint? _startPoint;
    private SKRect _selectionRect;
    
    public override void OnPointerPressed(PointerEventArgs args, ICanvasContext context)
    {
        _startPoint = context.ScreenToCanvas(args.GetPosition(args.Source as Visual));
    }
    
    public override void OnPointerMoved(PointerEventArgs args, ICanvasContext context)
    {
        if (_startPoint.HasValue)
        {
            var currentPoint = context.ScreenToCanvas(args.GetPosition(args.Source as Visual));
            _selectionRect = new SKRect(
                Math.Min(_startPoint.Value.X, currentPoint.X),
                Math.Min(_startPoint.Value.Y, currentPoint.Y),
                Math.Max(_startPoint.Value.X, currentPoint.X),
                Math.Max(_startPoint.Value.Y, currentPoint.Y)
            );
            context.Invalidate();
        }
    }
    
    public override void OnPointerReleased(PointerEventArgs args, ICanvasContext context)
    {
        if (_startPoint.HasValue)
        {
            var objectsInRect = context.Objects
                .Where(obj => _selectionRect.IntersectsWith(obj.Bounds))
                .ToList();
            
            context.Selection.SelectMultiple(objectsInRect);
            _startPoint = null;
        }
    }
    
    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (_startPoint.HasValue)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(64),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(_selectionRect, paint);
            
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.Blue;
            paint.StrokeWidth = 1;
            canvas.DrawRect(_selectionRect, paint);
        }
    }
}
```

### Undo/Redo Implementation

```csharp
public interface ICommandHistory
{
    void Execute(ICommand command);
    void Undo();
    void Redo();
    bool CanUndo { get; }
    bool CanRedo { get; }
}

// Usage
commandHistory.Execute(new MoveCommand(selectedObjects, delta));

// In ViewModel
[RelayCommand(CanExecute = nameof(CanUndo))]
private void Undo()
{
    _commandHistory.Undo();
}

[RelayCommand(CanExecute = nameof(CanRedo))]
private void Redo()
{
    _commandHistory.Redo();
}
```

## Performance Tips

1. **Use dirty rectangle invalidation**:
```csharp
context.Invalidate(obj.Bounds); // Instead of context.Invalidate()
```

2. **Implement object culling**:
```csharp
var visibleObjects = Objects.Where(obj => viewport.Intersects(obj.Bounds));
```

3. **Cache rendered layers**:
```csharp
public class CachedLayer : LayerBase
{
    private SKBitmap _cache;
    
    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (IsDirty || _cache == null)
        {
            RenderToCache(context);
        }
        canvas.DrawBitmap(_cache, 0, 0);
    }
}
```

4. **Batch object updates**:
```csharp
// Bad: Multiple invalidations
foreach (var obj in objects)
{
    obj.Position += delta;
    context.Invalidate();
}

// Good: Single invalidation
foreach (var obj in objects)
{
    obj.Position += delta;
}
context.Invalidate();
```
