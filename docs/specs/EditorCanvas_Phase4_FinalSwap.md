# Phase 4 — EditorCanvas Final Swap

## Overview
Replace the old `AnnoCanvas` (v1) with `EditorCanvas` (v2) as the primary canvas control in the application. This is the culmination of Phases 1-3 which established the bridge layer, Anno-specific features, and diagonal road support.

## Prerequisites (✓ Complete)
- Phase 1: Bridge layer — CanvasObject ↔ AnnoObject sync, serialization
- Phase 2: Anno features — influence, blocked area, scroll, clipboard, collision, select-same
- Phase 3: Diagonal roads — RoadPlacementTool, Bresenham connectivity, BFS integration

## Migration Steps

### Step 1: Wire EditorCanvas into DocumentViewModel
- `DocumentViewModel.InitializeCanvas()` currently creates an `AnnoCanvas` instance
- Change to create an `EditorCanvas` wrapped by `AnnoEditorAdapter`
- Expose the adapter as the document's canvas interface
- Keep `IAnnoCanvas` interface for backward compat — implement it on the adapter or create `EditorCanvasAnnoAdapter : IAnnoCanvas`

### Step 2: Implement IAnnoCanvas on AnnoEditorAdapter
The old `IAnnoCanvas` interface exposes:
- `PlacedObjects`, `SelectedObjects` (collections)
- `GridSize`, `RenderGrid`, `RenderIcon`, etc. (display toggles)
- `SetCurrentObject`, `ForceRendering`, `Normalize`, `ResetViewport`
- `UndoManager`, `LoadedFile`, `BuildingPresets`, `Icons`
- Events: `StatisticsUpdated`, `ColorsInLayoutUpdated`, `OnCurrentObjectChanged`, etc.

Implement all of these on `AnnoEditorAdapter`, delegating to EditorCanvas internals:
- `PlacedObjects` → adapter's `_wrapperMap` exposed as compatible collection
- `SelectedObjects` → map EditorCanvas selections back to LayoutObjects
- Display toggles → EditorCanvas layer enable/disable + PreferencesService
- `Normalize` → compute bounding rect, offset all objects
- Events → fire when EditorCanvas selection/content changes

### Step 3: Update XAML Layout
- `MainWindow.xaml` currently hosts `<canvas:AnnoCanvas x:Name="annoCanvas"/>`
- Replace with `<editorCanvas:EditorCanvas x:Name="editorCanvas"/>`
- Update DataContext bindings (the adapter wraps EditorCanvas and implements IAnnoCanvas)
- Ensure DockPanel layout (statistics, version bar) still works with EditorCanvas

### Step 4: Register All Tools
In the `AnnoEditorAdapter` or `DocumentViewModel`, register:
- `RoadPlacementTool` with hotkey (e.g., Shift+R)
- Ensure `PlacementTool` uses Anno-specific validation (collision + grid snap)
- Map old hotkeys: Rotate → TransformTool rotate, Delete → remove, etc.

### Step 5: Command Parity
Verify all old commands work through the new canvas:
- Rotate (single + group)
- Align / Distribute / Flip (ITransformationService — already wired)
- Undo/Redo (EditorCanvas has its own UndoManager)
- Export image (ExportService.PrepareCanvasForRender still uses old AnnoCanvas — needs update or keep as-is)
- Merge Roads (RoadMergeHelper — needs to work on new adapter)

### Step 6: Feature Flag
Add a toggle in settings: `UseNewCanvas` (default: true for dev builds, false for release)
```csharp
if (_appSettings.UseNewCanvas)
    document.InitializeEditorCanvas();
else
    document.InitializeLegacyCanvas();
```
This allows A/B comparison and safe rollback.

### Step 7: Integration Testing
- Load every test layout from `Tests\AnnoDesigner.Tests\TestData\`
- Verify: render output matches (screenshot comparison)
- Verify: all hotkeys work
- Verify: undo/redo chain survives complex edit sessions
- Verify: save → reload produces identical layout
- Verify: export image produces valid PNG

### Step 8: Performance Validation
- Load a large layout (500+ buildings)
- Profile render loop — target: <16ms per frame (60fps)
- Profile spatial queries — target: <1ms for selection rect
- If needed: implement render caching (DrawingGroup per layer)

### Step 9: Delete Old Canvas
Once feature flag is proven stable:
- Remove `AnnoDesigner\Controls\Canvas\AnnoCanvas.xaml.cs` (1855 LOC)
- Remove `AnnoDesigner\Controls\Canvas\AnnoCanvas.Commands.cs` (703 LOC)
- Remove `AnnoDesigner\Controls\Canvas\AnnoCanvas.Constants.cs` (35 LOC)
- Remove `AnnoDesigner\Controls\Canvas\Services\` (CanvasRenderer, InputInteractionService, etc.)
- Remove `AnnoDesigner\Models\Interface\IAnnoCanvas.cs` (replace with EditorCanvas API)
- Update all references throughout the codebase

## Risk Assessment
| Risk | Mitigation |
|------|-----------|
| Rendering differences | Side-by-side mode via feature flag |
| Performance regression | Profile before deletion; EditorCanvas layered renderer should be faster |
| Broken hotkeys | Map all old bindings explicitly in Step 4 |
| Export image broken | Keep ExportService using temporary old AnnoCanvas for now |
| Selection behavior different | Test with complex multi-select scenarios |

## Estimated Effort
- Steps 1-3: ~2 days (adapter implementation + XAML changes)
- Steps 4-5: ~1 day (hotkey mapping + command verification)
- Step 6: ~0.5 day (feature flag)
- Steps 7-8: ~1 day (manual testing + profiling)
- Step 9: ~0.5 day (deletion + cleanup)
- **Total: ~5 days**

## Dependencies
- No external dependencies
- Can be done incrementally behind the feature flag
- Does not block other development (preset parser, new game support)
