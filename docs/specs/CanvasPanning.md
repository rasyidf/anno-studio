# Canvas Panning  One-Page Specification

Purpose
- Define expected canvas panning UX, API contracts and a safe, testable implementation plan.

Scope
- Mouse-drag panning (both-buttons gesture currently used), scrollbars and wheel/trackpad panning (not zoom).
- Preserve existing selection, placement and rendering behavior. Keep the UX identical during incremental refactors.

Relevant components
- Control: `AnnoDesigner/Controls/Canvas/AnnoCanvas.xaml.cs`  mouse event handling, `_viewport`, `InvalidateScroll()`, `InvalidateVisual()`.
- Service: `Controls/Canvas/Services/InputInteractionService` / `IInputInteractionService`  contains `HandleDragAll`, `HandleDragSelection`, `HandleMouseWheel`.
- Coordinate helper: `ICoordinateHelper`  conversions between screen/grid coordinates.
- Settings: `IAppSettings.InvertPanningDirection`  toggles panning direction.

Expected UX behavior
- Entry: `DragAllStartAndRegisterMove` decision on both-buttons mouse-down leads to `MouseMode.DragAllStart`.
- Start panning: when pointer moves > 1px switch to `MouseMode.DragAll`.
- Motion: compute integer grid deltas from screen delta, update `_viewport.Left/Top` by delta (negated when inverted), and update the drag-start position by the corresponding screen amount so subsequent deltas are relative to the remaining motion.
- Exit: on both buttons released return to `MouseMode.Standard`.

API contract for `HandleDragAll`
- Signature (already present):
  - `void HandleDragAll(Point mousePosition, ref Point mouseDragStart, int gridSize, Viewport viewport, ICoordinateHelper coordinateHelper, IAppSettings appSettings, out bool invalidateScroll)`
- Semantics:
  - `mousePosition`: current screen point.
  - `mouseDragStart` (ref): updated by service to compensate for applied integer-grid shift (in screen units).
  - `gridSize`: zoom level (grid cell size).
  - `viewport`: mutated in-place; update `Left/Top` by integer grid shift.
  - `appsettings.InvertPanningDirection`: flips sign of applied shift.
  - `invalidateScroll` (out): true when control should call `InvalidateScroll()` to recompute scroll bounds.

Sequence example
1. Both-button mouse down -> `DecideOnMouseDown` -> `DragAllStartAndRegisterMove` -> `CurrentMode = DragAllStart`.
2. OnMouseMove beyond threshold -> `CurrentMode = DragAll`.
3. Each OnMouseMove while `DragAll`:
   - dx = (int)ScreenToGrid(mousePosition.X - mouseDragStart.X, gridSize)
   - dy = (int)ScreenToGrid(mousePosition.Y - mouseDragStart.Y, gridSize)
   - apply `viewport.Left += (invert ? -dx : dx)` and same for Top
   - `mouseDragStart.X += GridToScreen(dx, gridSize)` and same for Y
   - return `invalidateScroll` true only when layout/scrollable bounds changed
4. Mouse up (both released) -> `CurrentMode = Standard`.

Testing checklist (unit-level)
- `HandleDragAll` tests:
  - Basic pan: verify dx/dy and viewport update for given screen movement.
  - Invert flag toggles sign.
  - Zero-movement yields no changes.
  - Repeated small moves accumulate to integer grid step without drift.
- Integration tests:
  - Simulated mouse down -> move -> up sequence results in expected `CurrentMode` and `_viewport` values.

Implementation plan (safe, incremental)
1. Create tests for `InputInteractionService.HandleDragAll` to lock in expected behavior.
2. Add control-level integration tests that simulate `AnnoCanvas` mouse sequences using fakes for `ICoordinateHelper`/viewport.
3. After tests pass, refactor `AnnoCanvas` to delegate panning to `HandleDragAll` (preserve existing math and update `mouseDragStart` behavior), and only call `InvalidateScroll()` when `invalidateScroll` is true.
4. Manual QA: verify mouse-drag panning, scrollbars, trackpad behaviour, zoom interactions and `InvertPanningDirection` toggle.

Deliverables
- `docs/specs/CanvasPanning.md` (this file).
- Unit tests for `HandleDragAll` (added in a follow-up change on request).
- Small, documented refactor of `AnnoCanvas` that delegates to the service (performed only after approval).

Open questions
- Keep both-buttons drag as canonical panning gesture or add middle-button / space+drag? (I recommend keeping current gesture and optionally adding alternatives behind a setting.)
- Trackpad inertia / gesture smoothing: out of scope for now.

Approval
- Reply with "approve" to proceed with tests + small refactor, or reply with "tests-only" to add tests first and stop before refactor. Reply with any edits to the spec if you'd like adjustments.
