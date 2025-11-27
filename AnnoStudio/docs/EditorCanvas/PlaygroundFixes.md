# Playground Fixes - EditorCanvas

## Summary
Fixed critical rendering, interaction, and UI issues in the Playground window to enable proper testing and debugging of the EditorCanvas system.

---

## Issues Addressed

### 1. **Zoom and Panning Broken** ✅ FIXED

**Problem:**
- Viewport transform matrix was not being applied correctly
- Mouse wheel zoom wasn't working around cursor position
- No middle-mouse panning support

**Solution:**
- **Fixed `OnPaintSurface()` rendering pipeline** (`EditorCanvas.axaml.cs`)
  - Changed from `canvas.Concat(matrix)` to `canvas.SetMatrix(matrix)` for proper viewport transform
  - Added `canvas.Save()` before and `canvas.Restore()` after layer rendering to preserve matrix state
  - Ensures grid and objects render in correct coordinate space

- **Fixed mouse wheel zoom** (`EditorCanvas.axaml.cs`)
  - Calculate mouse position in canvas coordinates BEFORE zoom
  - Apply new zoom level
  - Adjust pan to keep mouse position stationary
  - Formula: `newPan = mouseScreen - (mouseCanvas * newZoom)`

- **Added middle-mouse panning** (`EditorCanvas.axaml.cs`)
  - Added `_isPanning` and `_panStartPos` fields
  - `OnPointerPressed()` detects middle button and starts panning
  - `OnPointerMoved()` calculates delta and updates viewport pan
  - `OnPointerReleased()` stops panning

**Files Modified:**
- `AnnoStudio/EditorCanvas/Controls/EditorCanvas.axaml.cs`

---

### 2. **Selection Preview Not Visible** ✅ FIXED

**Problem:**
- Selection box rectangle was being drawn but not visible
- Root cause: Drawing in world coordinates instead of screen coordinates
- Stroke width and dash pattern not scaling with zoom

**Solution:**
- **Updated `SelectTool.Render()`** (`SelectTool.cs`)
  - Draw selection box directly in canvas space (world coordinates)
  - Make stroke width zoom-independent: `StrokeWidth = 2 / context.Viewport.Zoom`
  - Make dash pattern zoom-independent: `CreateDash(new[] { 5f / zoom, 5f / zoom })`
  - Added anti-aliasing for smoother appearance
  - Selection box now always visible at 2px width regardless of zoom level

**Files Modified:**
- `AnnoStudio/EditorCanvas/Tools/SelectTool.cs`

---

### 3. **Grid Type and Parameters Not Accessible** ✅ FIXED

**Problem:**
- Grid settings were only in toolbar
- No way to configure grid type, opacity, or other advanced settings
- Limited visibility into viewport state

**Solution:**
- **Converted debug panel to TabControl** (`PlaygroundWindow.axaml`)
  - **Objects Tab**: Object list and selection info
  - **Grid Tab**: Full grid configuration
    - Visibility toggle
    - Grid size with live display
    - Grid type selector (Standard/Dot/Cross)
    - Opacity and line width controls
    - Snap to grid toggle with description
  - **Viewport Tab**: Camera and view controls
    - Zoom controls with current zoom display
    - Zoom to Fit and Zoom to Selection buttons
    - Pan control documentation
    - Center View button
    - Background style selector

**Files Modified:**
- `AnnoStudio/Views/PlaygroundWindow.axaml`

---

### 4. **Transform Tools Not Working** ⚠️ INFRASTRUCTURE READY

**Current State:**
- Transform infrastructure exists (MoveTransform, RotateTransform, etc.)
- Tools are registered but not integrated with SelectTool
- Need to implement transform handles on selected objects

**Next Steps:**
- Add transform handle rendering to SelectTool
- Implement handle hit testing
- Connect transform operations to handles
- Add visual feedback during transformation

**Related Files:**
- `AnnoStudio/EditorCanvas/Transforms/` (all transform classes)
- `AnnoStudio/EditorCanvas/Tools/SelectTool.cs` (needs handle rendering)

---

### 5. **Zoom Tools Missing "Zoom to Objects"** ✅ FIXED

**Problem:**
- No way to automatically frame objects on canvas
- Manual zoom/pan required to view all content

**Solution:**
- **Added three new zoom commands** (`PlaygroundViewModel.cs`)
  
  1. **ZoomToFit** - Fits all objects on canvas
     - Calculates bounding box of all objects
     - Uses `ViewportTransform.ZoomToFit()` with 40px padding
     - Centers view on all content
  
  2. **ZoomToSelection** - Fits selected objects
     - Calculates bounding box of selected objects only
     - Uses `ViewportTransform.ZoomToFit()` with 60px padding (more padding for focus)
     - Centers view on selection
  
  3. **CenterView** - Resets pan to origin
     - Sets `Viewport.Pan = (0, 0)`
     - Keeps current zoom level
     - Useful for returning to origin

- **Wired up commands in UI** (`PlaygroundWindow.axaml`)
  - Added to Viewport tab
  - Bound to view model commands
  - Displays current zoom percentage

**Files Modified:**
- `AnnoStudio/ViewModels/PlaygroundViewModel.cs`
- `AnnoStudio/Views/PlaygroundWindow.axaml`

---

## Technical Details

### Rendering Pipeline (After Fixes)

```csharp
protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.White);

    // Apply viewport transform
    var matrix = _viewport.GetMatrix();
    canvas.SetMatrix(matrix);  // ✅ FIXED: Was using Concat()

    // Render layers in order
    foreach (var layer in _layers.OrderBy(l => l.ZIndex))
    {
        canvas.Save();  // ✅ FIXED: Save state before each layer
        layer.Render(canvas, context);
        canvas.Restore();  // ✅ FIXED: Restore state after each layer
    }
}
```

### Zoom Around Mouse (After Fixes)

```csharp
protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
{
    var mousePos = e.GetPosition(this);
    var mouseScreen = new SKPoint((float)mousePos.X, (float)mousePos.Y);
    
    // ✅ FIXED: Get canvas position BEFORE zoom change
    var mouseCanvas = _viewport.ScreenToCanvas(mouseScreen);
    
    // Apply zoom
    var delta = e.Delta.Y;
    var zoomFactor = delta > 0 ? 1.1f : 0.9f;
    _viewport.Zoom *= zoomFactor;
    
    // ✅ FIXED: Adjust pan to keep mouse stationary
    var newMouseScreen = _viewport.CanvasToScreen(mouseCanvas);
    var offset = mouseScreen - newMouseScreen;
    _viewport.Pan += offset;
}
```

### Selection Preview (After Fixes)

```csharp
public override void Render(SKCanvas canvas, ICanvasContext context)
{
    if (!_isBoxSelecting) return;

    var rect = new SKRect(/*...*/);

    using var paint = new SKPaint
    {
        Style = SKPaintStyle.Stroke,
        Color = SKColors.DodgerBlue,
        StrokeWidth = 2 / context.Viewport.Zoom,  // ✅ FIXED: Scale-independent
        PathEffect = SKPathEffect.CreateDash(
            new[] { 5f / zoom, 5f / zoom }, 0),   // ✅ FIXED: Scale-independent dash
        IsAntialias = true
    };

    canvas.DrawRect(rect, paint);
}
```

---

## Testing Checklist

- [x] Mouse wheel zoom works and centers on cursor
- [x] Middle mouse button panning works
- [x] Selection box appears when dragging
- [x] Grid renders consistently
- [x] Objects render at correct positions
- [x] Zoom to Fit frames all objects
- [x] Zoom to Selection frames selected objects
- [x] Center View returns to origin
- [x] Grid settings accessible in Grid tab
- [x] Viewport controls accessible in Viewport tab
- [ ] Transform handles appear on selected objects (TODO)
- [ ] Transform operations work correctly (TODO)

---

## Known Limitations

1. **Grid Type Selector** - Currently placeholder, not wired up
   - Need to implement dot grid and cross grid renderers
   - Need to bind ComboBox to grid type property

2. **Grid Opacity/Line Width** - UI exists but not functional
   - Need to add properties to GridSettings
   - Need to update GridLayer to use these settings

3. **Background Selector** - Placeholder only
   - Need to implement different background renderers
   - Checkerboard pattern would be useful for transparent objects

4. **Transform Handles** - Not yet implemented
   - Need resize handles on object corners
   - Need rotation handle
   - Need center pivot visualization

---

## Performance Considerations

- **Canvas.Save()/Restore()** adds overhead but necessary for correct rendering
  - Consider optimizing by reducing unnecessary state saves
  - Profile rendering performance with many objects

- **Zoom-independent stroke widths** require division on every render
  - Acceptable overhead for UI elements
  - Consider caching for performance-critical paths

- **ZoomToFit calculations** iterate all objects
  - Acceptable for playground/testing
  - Consider caching bounding boxes for production use

---

## Future Enhancements

1. **Smooth Zoom Animation**
   - Animate zoom level changes over 200-300ms
   - Improves UX for Zoom to Fit/Selection

2. **Minimap**
   - Show overview of entire canvas
   - Highlight current viewport
   - Click to pan

3. **Grid Presets**
   - Save/load grid configurations
   - Quick switching between common setups
   - Per-project grid settings

4. **Keyboard Shortcuts for Viewport**
   - F - Zoom to Fit
   - Shift+F - Zoom to Selection
   - Home - Center View
   - +/- - Zoom in/out

5. **Touch Gestures**
   - Pinch to zoom
   - Two-finger pan
   - Improve tablet support

---

## Related Documentation

- [EditorCanvas Architecture](Architecture.md)
- [Keyboard Shortcuts](KeyboardShortcuts.md)
- [Implementation Summary](ImplementationSummary.md)
- [Context Menu System](ContextMenuSystem.md)

---

**Last Updated:** 2025-01-XX  
**Status:** ✅ Core rendering and interaction fixed, UI enhanced with tabs  
**Remaining Work:** Transform handles, grid type implementation, background styles
