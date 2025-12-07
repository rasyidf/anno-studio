# EditorCanvas Hotkeys and Tooling Plan

## Goals
- Provide a production-ready annotation tooling stack for `EditorCanvas` that matches or exceeds canvas v1 capabilities.
- Introduce a flexible HotkeyManager that supports dynamic mouse + keyboard combos and cancel behavior.
- Add richer selection (click, rectangle, lasso) and drawing (rect, line, pencil) tools plus transform (move/rotate) and duplicate tooling.
- Ensure rendering shows tool affordances (selection outlines, handles, previews, rubber bands) without breaking existing layers.

## Scope (this iteration)
- New HotkeyManager inside `EditorCanvas` with API to register/unregister combos, bind to tools/commands, expose cancel hotkey.
- Tool additions: RectSelect, LassoSelect, RectDraw, LineDraw, PencilDraw (freehand), TransformTool (move/rotate on selected), DuplicateTool.
- Selection tool upgrades: multi-select, box-select, lasso-select, shift/ctrl modifiers, clear on cancel.
- Rendering updates: selection adorners, drag rectangles, lasso polyline, transform handles/rotation gizmo, draw previews.
- Demo updates: hotkey editing UI surface, quick actions to pick tools, instructions overlay.

## Out-of-scope (for now)
- Persistence/serialization of hotkey profiles to disk.
- Undo/redo for hotkey edits (tool operations should still integrate e  xisting undo once available).
- Advanced snapping (beyond simple guideline/grid snap already available) and constraints.

## Design Overview
- **HotkeyManager** (new):
  - Lives inside `EditorCanvas`; integrates with `InputInteractionService` to observe keyboard/mouse state.
  - API: register/unregister bindings (id, gesture, trigger type), set active profile, query active tool binding, raise events on match.
  - Supports combined mouse + modifier keys (e.g., Ctrl+Shift+LeftMouse drag) and keyboard-only shortcuts.
  - Cancel hotkey (configurable, default `Esc`) clears current tool state and deselects transient previews.
  - Exposes simple DTO for bindings to allow demo UI editing.
- **ToolManager interplay**:
  - HotkeyManager maps gestures → tool activations or commands (cancel, duplicate selection, rotate, switch tool).
  - ToolManager remains source of truth for active tool; tools gain optional hooks for cancel/reset.
- **Tools taxonomy & behaviors**:
  - *SelectionTool* (existing): extend for click + box select; add modifier handling; expose `SetSelection`/`ClearSelection` helpers in canvas API.
  - *RectSelectTool* (new or mode of SelectionTool): click-drag axis-aligned rectangle; hit-test via `ObjectManagerQuadTree`; multi-select.
  - *LassoSelectTool*: freeform polyline capture; compute polygon hit-test against object bounds; finish on mouse up; preview polyline.
  - *RectDrawTool*: click-drag to define rect; on release, emit `CanvasObject` with computed bounds.
  - *LineDrawTool*: click-drag line; draws thin rect or line geometry; may emit a `CanvasObject` representing a line.
  - *PencilDrawTool*: capture freehand points; simplify optionally; on release, emit polyline/geometry object.
  - *TransformTool*: when selection present, enable drag-move, rotate via handle; supports constrain (Shift) and snap to grid/guides.
  - *DuplicateTool*: duplicate current selection (respect z-index offset), optionally offset by grid step; integrate with selection to select duplicates.
- **Rendering layers** (build on existing RendererWpf layering):
  - Grid / Guidelines (unchanged).
  - Objects (unchanged ordering by ZIndex asc for compositing; selection overlays drawn after objects).
  - Tool overlays: selection rectangles, lasso polyline, draw previews, transform handles (corners, rotate handle), pivot marker.
  - Overlay text: tool hints, hotkey hints.

## Data/API changes
- `EditorCanvas` public API:
  - `HotkeyManager Hotkeys { get; }` to manage bindings.
  - `void SetSelection(IEnumerable<CanvasObject> objects)` and `void ClearSelection()` for external control.
  - Events: `SelectionChanged`, `ToolChanged`, `HotkeyBindingChanged` (optional).
  - Properties: `ShowToolOverlays` toggle.
- `ITool` contract additions:
  - `void OnCancel()` for cancel hotkey.
  - Optional: `bool CanStart(InputState state)` for gesture gating.
- `InputInteractionService`:
  - Surface combined gesture info (mouse button + modifiers) to HotkeyManager.
- `CanvasObject` (if needed):
  - Support for rotation/origin (e.g., `double RotationDegrees`, `Point RotationCenter`) for transform tool.

## Hotkey model
- Binding structure: `Id`, `Gesture` (e.g., `Ctrl+Shift+LButton`, `Ctrl+D`, `Esc`), `ActionType` (ActivateTool/Command), `TargetId` (tool key or command key), `Scope` (global vs tool-local), `DisplayName`.
- Defaults (proposed):
  - Esc: Cancel current tool / clear transient state.
  - V: Selection tool; M: Move/Transform; R: Rect select; L: Lasso select; D: Rect draw; N: Line draw; P: Pencil; Alt+D: Duplicate; Delete: delete selection; Ctrl+R: rotate selection (Transform mode).
  - Mouse: Left drag = primary action for active tool; Right click = cancel/finish (tool-specific); Shift modifies (constrain angle/axis), Ctrl toggles add/remove selection.
- API calls: `RegisterBinding`, `UnregisterBinding`, `ReplaceBindings(IEnumerable<Binding>)`, `TryMatch(InputState state)`.

## Interaction flows
- **Rect selection**: Mouse down → start rect; drag → show rubber band; mouse up → hit-test rect vs objects; set selection; modifiers: Ctrl toggles add/remove, Shift add.
- **Lasso selection**: Mouse down → start polyline; move → append points (throttled); mouse up → close polygon; hit-test vs objects; set selection.
- **Draw tools**: Mouse down (or drag) → preview shape; mouse up → commit new object into `ObjectManagerQuadTree` and selection; Esc cancels preview.
- **Transform**: Click selection to show handles; drag inside = move; drag rotate handle = rotate; handles show during render; Esc cancels drag; Enter/MouseUp commits.
- **Duplicate**: Hotkey or tool action duplicates current selection, offsets, reselects duplicates; uses existing object manager to insert.
- **Cancel**: Cancel hotkey resets active tool state, clears transient geometries, keeps selection unless tool owns it (configurable; default keep selection).

## Rendering specifics
- Selection overlays: stroked rect (dashed), lasso polyline, handles (small squares), rotate handle circle with connector, pivot marker.
- Draw previews: semi-transparent fill/stroke for pending rect/line/pencil path; snap indicators when snapping active.
- Interaction hints: small text overlay showing active tool and hotkey reminders (optional toggle).

## Testing & Demo
- Demo surface to edit hotkeys (simple list + editable text for gesture), with validation.
- Demo buttons to switch tools and show overlays; status bar showing active tool and cancel hint.
- Manual test matrix: selection modes, draw modes, transform (move/rotate) on single/multi selection, duplicate action, cancel behavior, hotkey rebinds.

## Risks / Considerations
- Hit-testing for lasso uses polygon intersection; ensure performance by reusing `ObjectManagerQuadTree` + bounding box prefilter.
- Rotation support may require extending `CanvasObject` to store rotation; rendering and hit-testing must respect rotation.
- Hotkey conflicts: need simple conflict detection in manager; for now, last-wins or warn via event.
- Undo/redo for new object creation/transform is not covered here; keep hooks to integrate later.

## Implementation steps (high level)
1) Add HotkeyManager class and hook into InputInteractionService and EditorCanvas APIs.
2) Extend ITool with cancel hook; update ToolManager to route hotkey actions.
3) Implement selection enhancements (rect, lasso) and selection API on canvas.
4) Implement draw tools (rect/line/pencil) with previews and commit to object manager.
5) Implement transform tool (move/rotate) and duplicate action; add rotation to CanvasObject if needed.
6) Update RendererWpf overlays for new tool visuals.
7) Wire default hotkeys and cancel behavior; expose demo UI to edit bindings.
8) Demo polish and smoke tests.
