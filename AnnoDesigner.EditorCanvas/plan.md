# EditorCanvas — Implementation Plan

This document captures the plan for the new `EditorCanvas` component. It is intended to cover and improve upon all functionality currently provided by `AnnoCanvas` (Canvas v1) and to provide a clear implementation roadmap.

## Goals
- Recreate parity with Canvas v1 features: selection, placement, transformations, rendering, undo/redo, file IO, tooling, and interaction.
- Improve modularity, testability, performance, and extensibility.
- Provide clear interfaces for tools, rendering backends, input handling, and content management.

## High-Level Architecture

- Layers:
  - Core: rendering pipeline, transforms, viewport management.
  - Interaction: input handling, gesture recognition, selection management.
  - Tooling: tools (selection, placement, rotate, measure), tool manager.
  - Content: object storage (quad-tree or spatial index), serialization, preset loading.
  - Services: undo/redo manager, file service, command history, clipboard.

## Implementation Phases & Tasks

Phase 1 — Scaffolding (current)
- Create `EditorCanvas` control (XAML + code-behind) as a drop-in, minimal UserControl.
- Add core interfaces: `IRenderer`, `IInputHandler`, `ITool`, `IObjectManager`.
- Add `plan.md` (this file).

Phase 2 — Core systems
- Implement `Viewport` and coordinate transformation helpers.
- Implement a `Renderer` that wraps WPF `DrawingContext` for now.
- Add `RenderLayer` management to separate background/grid, objects, overlays.

Phase 3 — Interaction & tools
- Implement `InputInteractionService` to convert UI events to higher-level actions.
- Implement `ToolManager` and sample tools: `SelectionTool`, `PlacementTool`, `TransformTool`.
- Implement selection rectangle, snapping to grid, and multi-select.

Phase 4 — Content & persistence
- Implement `ObjectManager` with spatial index (QuadTree) for efficient querying.
- Implement serialization/deserialization compatible with Canvas v1 layout format.
- Integrate `BuildingPresets` loader and icon management.

Phase 5 — Undo/Redo & Commands
- Integrate `IUndoManager` using Command/Memento patterns.
- Wire all tools and content modifications through the undo manager.

Phase 6 — QA, performance, migration
- Add unit and integration tests for core systems.
- Profile rendering and interaction; optimize hot paths.
- Migrate sample layouts and ensure parity with Canvas v1.

## File/Folder Layout (proposed)

```
AnnoDesigner/Controls/EditorCanvas/
  EditorCanvas.xaml
  EditorCanvas.xaml.cs
  plan.md
  Core/
    IRenderer.cs
    RendererWpf.cs
    Viewport.cs
  Interaction/
    IInputHandler.cs
    InputInteractionService.cs
    SelectionManager.cs
  Tooling/
    ITool.cs
    ToolManager.cs
    SelectionTool.cs
    PlacementTool.cs
  Content/
    IObjectManager.cs
    ObjectManagerQuadTree.cs
    Models/
  Services/
    Undo/
    FileDialogService.cs
    LayoutFileService.cs
```

## Design Notes & Patterns
- Use the Command pattern for undo/redo operations.
- Use Strategy for pluggable rendering backends.
- Use Observer/Events for ViewModel <-> Canvas notifications.
- Keep UI-specific code (WPF) isolated from core logic to ease testing.

## Developer Documentation
- Expand `plan.md` with class diagrams and sequence flows as implementation progresses.
- Keep XML doc comments on public APIs.

## Testing Strategy
- Unit tests for: Viewport transforms, InputInteractionService, ObjectManager quad-tree.
- Integration tests: ToolManager + tool interactions with ObjectManager.
- End-to-end: load/save layout, basic edit flows.

## Migration Plan
1. Implement parity features behind a feature flag.
2. Provide an import/compatibility layer that converts existing Canvas v1 layouts to the new format.
3. Run both controls in parallel for validation; swap once parity is confirmed.

## Next Immediate Steps (developer)
1. Implement a lightweight `RendererWpf` and wire it into `EditorCanvas`.
2. Implement `InputInteractionService` skeleton and connect UI events.
3. Add `ToolManager` and a `SelectionTool` that can highlight and select items (placeholder objects).

---

Keep this plan updated as implementation progresses. Each task should be converted into tracked TODOs and small PR-sized changes.
