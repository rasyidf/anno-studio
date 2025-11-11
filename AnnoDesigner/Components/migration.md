## 🤖 AI Refactor Plan: AnnoCanvas God Class

***Important: Preserve v1 (AnnoCanvas) intact. Do not edit or refactor the existing AnnoCanvas control in-place.***

- Rationale: v1 must remain a stable reference and fallback. Every removed/relocated responsibility must be moved to a new file or folder under CanvasV2 or to a clearly marked Legacy/ directory. Keep the original AnnoCanvas.xaml(.cs) untouched; create a copy under /Legacy/AnnoCanvasV1/ if you need to patch or instrument it for migration tests.

---

Current migration status (this branch)

- New V2 structure created:
  - CanvasV2/AnnoCanvasViewModel.cs (INotifyPropertyChanged + IScrollInfo)
  - CanvasV2/AnnoCanvasV2.xaml/.cs (UserControl, IScrollInfo)
  - CanvasV2/Integration/CanvasV2Adapter.cs (IAnnoCanvas adapter for MainViewModel)
  - CanvasV2/Input/CanvasInputHandler.cs (input state machine)
  - CanvasV2/Rendering/LayoutRenderer.cs (rendering logic)
  - CanvasV2/Rendering/RenderState.cs (immutable render snapshot)
  - CanvasV2/FeatureFlags/* (already present; consumed from VM)
- v1 AnnoCanvas remains unchanged.
- Feature flags are used to provide a snapshot (CanvasFeatureFlags) to the renderer.
- IScrollInfo is implemented on the ViewModel; the view pass-throughs to VM.
- Commands migrated (subset): Rotate, Copy, Paste, Delete, Undo, Redo.
- Input handlers migrated: MouseWheel, KeyDown, MouseDown, MouseMove, MouseUp with full state machine.
- Rendering features migrated: Grid, objects, icons, labels, selection, influences (basic + advanced with RoadSearchHelper), harbor blocked areas.
- Cache management: RendererCaches class with version-based invalidation.
- MainWindow integration: V2 canvas is swapped in based on a feature flag.
- Context menu: Added with common operations (Copy, Paste, Delete, Rotate, Undo, Redo).
- Core functionality complete: Object placement with proper grid snapping, hover feedback, save/load with unsaved changes checking.
- Not done yet: Panorama mode, debug visualizations.

---

### Known Issues & Missing Features (V2 vs V1 Parity)
- ~~**No Hover Feedback**: The grid does not show hover highlights when placing objects (like roads), making placement unintuitive.~~ **FIXED**: CurrentObjects now render with 50% opacity.
- ~~**Incorrect Object Placement**: Placed objects (especially roads) default to `(0,0)` instead of the mouse cursor position. The `MoveCurrentObjectsToMouse` logic needs to be fixed.~~ **FIXED**: Implemented proper grid-snapping logic from v1.
- ~~**Broken/Placeholder Implementations**: Several methods in `CanvasV2Adapter` and `AnnoCanvasViewModel` are placeholders (`CheckUnsavedChanges`, `CheckUnsavedChangesBeforeCrash`) and need to be implemented by porting logic from `AnnoCanvas` v1.~~ **FIXED**: All methods now properly implemented.
- ~~**Incomplete Influence Rendering**: The advanced influence range rendering (using `RoadSearchHelper`) is not ported. Only basic radius is shown.~~ **FIXED**: Full influence rendering with RoadSearchHelper is implemented and working.
- ~~**Context Menu**: Right-click context menu is not implemented.~~ **FIXED**: Added context menu with common operations (Copy, Paste, Delete, Rotate, Undo, Redo).
- **Panorama Mode**: The panorama/isometric view feature is not implemented in the V2 renderer.
- **Debug Visualizations**: Debug rendering modes from v1 are missing.

---

**Objective:** Decompose `AnnoCanvas` (God Class) into a modular, extensible, and maintainable structure (`CanvasV2`) based on Separation of Concerns (SoC). The goal is to provide a safe migration path that keeps v1 operational while enabling iterative extraction and testing of V2 components.

Target Folder Structure (V2):
/CanvasV2/
├── AnnoCanvasV2.xaml
├── AnnoCanvasV2.xaml.cs       (View: Dumb control, input/render dispatcher)
├── AnnoCanvasViewModel.cs     (ViewModel: State, logic, commands, IScrollInfo logic)
├── FeatureFlags/              (centralized runtime feature flags)
│   ├── IFeatureFlags.cs
│   └── CanvasFeatureFlags.cs
├── Input/
│   ├── CanvasInputHandler.cs  (Controller: Input state machine, mouse/key logic)
│   ├── IInputHandlerHost.cs   (Interface: ViewModel -> InputHandler)
└── Rendering/
    ├── LayoutRenderer.cs      (ViewLogic: All DrawingContext operations)
    ├── RenderState.cs         (record struct: Snapshot of VM state for a single frame)
    └── RendererCaches.cs      (optional: caches and DrawingGroup owners)
-----

Phase overview (high level):
- Phase 0: Preparation & Safeguards (copy v1, tests baseline, create feature flags)
- Phase 1: ViewModel Extraction (state & logic core)
- Phase 2: Input Controller Extraction (input handling)
- Phase 3: Renderer Extraction (rendering logic)
- Phase 4: Re-implement View (thin control)
- Phase 5: Integration, tests, performance tuning and rollback plan

-----

Phase 0 — Preparation & Safeguards

- Ensure AnnoCanvas (v1) is never modified. Create a read-only copy:
  - /Legacy/AnnoCanvasV1/AnnoCanvas.xaml
  - /Legacy/AnnoCanvasV1/AnnoCanvas.xaml.cs
- Add feature-flagging scaffolding (see FeatureFlags folder) so V2 can be enabled incrementally at runtime.
- Add automated tests that capture current behaviour of v1 (golden images for render, unit tests for commands/hotkeys, input behaviours).
- Add CI step to run both v1 and v2 tests while migration is in progress.

Why this matters
- Allows safe, incremental migration; if V2 introduces regressions we can toggle back to v1 instantly.
- Keeps a single source of truth for behaviour until V2 is feature-complete.

-----

Phase 1 — ViewModel Extraction (State & Logic Core)

Target: `AnnoCanvasViewModel.cs`
Approach: Create an INotifyPropertyChanged ViewModel. Migrate state, services and business logic from the code-behind while keeping public API parity where needed.

Key tasks:
- Move state properties (as-is) to ViewModel:
  - Core: QuadTree<LayoutObject> PlacedObjects, HashSet<LayoutObject> SelectedObjects, List<LayoutObject> CurrentObjects.
  - Config: BuildingPresets, Dictionary<string, IconImage> Icons.
  - View state: GridSize, RenderGrid, RenderInfluences, RenderLabel, RenderIcon, RenderTrueInfluenceRange, RenderHarborBlockedArea, RenderPanorama.
  - Status: StatusMessage, LoadedFile.
  - Services: IUndoManager, IClipboardService, IAppSettings, IBrushCache, IPenCache, ICoordinateHelper, ILayoutLoader, etc.
- Move ICommand properties and command implementations (RotateCommand, CopyCommand, DeleteCommand, etc.) into the ViewModel.
- Move hotkey registration into a HotkeyService and expose it as a dependency on the ViewModel. Register hotkeys through HotkeyService rather than View code-behind.
- Implement IScrollInfo on the ViewModel, not the view control. Keep ScrollOwner interactions via the control but compute offsets in ViewModel. Expose an event ScrollInvalidated to notify the view.
- Provide RenderInvalidated event to notify view to InvalidateVisual().

Notes & modern C# tips:
- Use file-scoped namespace declarations.
- Use nullable reference annotations and init-only properties where appropriate.
- Use records/record structs for small DTOs (e.g., RenderState -> record struct) to get value-based equality and succinct syntax.
- Use pattern matching, target-typed new, and expression-bodied members to keep code concise.

Moved items (do not delete):
- Anything removed from AnnoCanvas.xaml.cs must be moved to a new V2 file or to Legacy/ folder. Keep a one-line forwarder in v1 only if necessary for compatibility (prefer composition over modification).

Status:
- [x] AnnoCanvasViewModel.cs created with INotifyPropertyChanged and IScrollInfo.
- [x] Core state moved (PlacedObjects, SelectedObjects, CurrentObjects), configuration placeholders, status.
- [x] Commands (Rotate, Copy, Paste, Delete, Undo, Redo) migrated to ViewModel.
- [x] Events exposed: RenderInvalidated, ScrollInvalidated, StatisticsUpdated, ColorsInLayoutUpdated, OnLoadedFileChanged.
- [ ] Hotkey registration abstraction (“HotkeyService”) — currently using existing HotkeyCommandManager directly.

-----

Phase 2 — Input Controller Extraction (Input Handling)

Targets: Input/CanvasInputHandler.cs, Input/IInputHandlerHost.cs
Approach: Extract input state machine to a controller that depends only on an abstraction (IInputHandlerHost).

Key tasks:
- Create IInputHandlerHost implemented by AnnoCanvasViewModel. Minimal surface area: PlacedObjects, SelectedObjects, CurrentObjects, GridSize, Viewport, UndoManager, GetObjectAt(Point), ComputeBoundingRect, UpdateStatistics(), NotifyRenderInvalidate(), AddSelectedObject(..), RemoveSelectedObject(..).
- Move MouseMode enum and input state fields into CanvasInputHandler.
- Create public handlers: HandleMouseDown(MouseButtonEventArgs), HandleMouseMove(...), HandleMouseUp(...), HandleMouseWheel(...), HandleKeyDown(...).
- Replace any direct access to view fields with IInputHandlerHost calls.
- Move keyboard logic / hotkeys out of the control into the HotkeyService; input handler should ask HotkeyService whether a key combination corresponds to an action.

Status:
- [x] IInputHandlerHost interface added and implemented by ViewModel.
- [x] MouseMode enum moved to CanvasV2.Input namespace with full documentation.
- [x] CanvasInputHandler created with complete handlers (MouseWheel, KeyDown, MouseDown/Move/Up).
- [x] Full input state machine ported: selection rect, drag selection, drag all (pan), object placement.
- [x] Helper methods: IsControlPressed, IsShiftPressed, ShouldAffectObjectsWithIdentifier.
- [ ] Unit tests against mock IInputHandlerHost.

-----

Phase 3 — Renderer Extraction (Rendering Logic)

Targets: Rendering/LayoutRenderer.cs, Rendering/RenderState.cs
Approach: Isolate DrawingContext and WPF-specific rendering into a renderer class that accepts an immutable snapshot (RenderState) per frame.

Key tasks:
- Create RenderState as a record struct (C# 10).
- Move brushes, pens and DrawingGroup caches into LayoutRenderer (or a RendererCaches helper owned by LayoutRenderer). Keep the caches private to the renderer.
- Provide a single Render(DrawingContext dc, RenderState state) entry point. The renderer must be pure (no mutation of ViewModel state) except for internal caches.
- Use record structs for immutable frame snapshots: they are allocation-friendly and easy to test.

Status:
- [x] RenderState record struct created with expanded properties.
- [x] LayoutRenderer created with comprehensive drawing (background, grid, objects, icons, labels, selection, influences).
- [x] RendererCaches helper class created for pen/brush management with version tokens.
- [x] CurrentObjects now render with 50% opacity for hover feedback.
- [x] Full influence range polygons with RoadSearchHelper implemented and working.
- [ ] Port remaining v1 rendering: panorama mode, debug visualizations.
- [ ] Unit render tests or visual smoke tests.

-----

Phase 4 — Re-Implement View (The "Dumb" Control)

Target: AnnoCanvasV2.xaml.cs
Approach: Control acts as a thin dispatcher only; no business logic.

Key tasks:
- DataContext is AnnoCanvasViewModel.
- The view creates a LayoutRenderer instance once (or receives via DI) and calls renderer.Render in OnRender.
- OnRender gathers a RenderState snapshot from ViewModel (do not reference ViewModel fields after snapshot is taken).
- Override input events and delegate to ViewModel.InputHandler.HandleXXX.
- Implement IScrollInfo on the control as simple pass-throughs to ViewModel.
- Subscribe to ViewModel.RenderInvalidated -> call InvalidateVisual(); subscribe to ScrollInvalidated -> ScrollOwner?.InvalidateScrollInfo().

Status:
- [x] AnnoCanvasV2.xaml and AnnoCanvasV2.xaml.cs implemented as a thin control.
- [x] IScrollInfo pass-throughs to VM implemented.
- [x] Notifications wired (RenderInvalidated -> InvalidateVisual, ScrollInvalidated -> InvalidateScrollInfo).
- [x] E2E behind feature flag wiring in MainWindow / composition root.
- [x] Context menu added with common operations.

-----

Centralized Feature Flags

Replace ad-hoc per-property toggles with a centralized feature-flag system for the canvas. This provides a single, discoverable place to turn on/off features, simplifies RenderState and keeps toggles testable.

Design suggestions:
- IFeatureFlags interface:
  - bool IsEnabled(string name);
  - bool TryGet<T>(string name, out T value);
  - event Action<string, object?>? FeatureChanged; // optional: runtime toggles
- CanvasFeatureFlags (strongly-typed wrapper for canvas features) contains properties like:
  - bool RenderGrid { get; init; }
  - bool RenderInfluences { get; init; }
  - bool RenderIcons { get; init; }
  - bool RenderTrueInfluenceRange { get; init; }
  - ...
- Source of truth: IAppSettings or a FeatureFlagsService loaded at startup; allow runtime toggles via UI or developer tools.
- The ViewModel accepts IFeatureFlags and exposes CanvasFeatureFlags Snapshot for rendering.
- The Renderer uses this snapshot only.

Benefits:
- Centralized discovery and documentation of features.
- Easier A/B testing and gradual rollout.
- Runtime toggles for debugging and performance profiling.

-----

Checklist — Migration Plan (progress tracker)

Phase 0 — Preparation
- [x] Create /Legacy/AnnoCanvasV1/ copy of the original control (xaml + cs).
- [x] Add integration tests capturing v1 behaviour (render golden images, input flows, command outcomes).
- [x] Add FeatureFlags scaffolding and wire to app settings.

Phase 1 — ViewModel
- [x] Create AnnoCanvasViewModel.cs with INotifyPropertyChanged and IScrollInfo.
- [x] Move state properties to ViewModel.
- [x] Move commands and business logic to ViewModel; using existing HotkeyCommandManager for now.
- [x] Expose events: RenderInvalidated, ScrollInvalidated, StatisticsUpdated, ColorsInLayoutUpdated, OnLoadedFileChanged.
- [x] Implement CheckUnsavedChanges, Save, SaveAs methods.
- [x] Implement MoveCurrentObjectsToMouse with proper grid snapping.
- [x] Implement ReindexMovedObjects, RecalculateSelectionContainsNotIgnoredObject, InvalidateBounds.
- [x] Use actual scrollable bounds for ExtentWidth/ExtentHeight.
- [ ] Unit tests for command behaviours and IScrollInfo computations.

Phase 2 — Input
- [x] Add IInputHandlerHost and CanvasInputHandler.
- [x] Move MouseMode enum and input state to CanvasInputHandler.
- [x] Implement full public input handlers (MouseDown/Move/Up, MouseWheel, KeyDown) with complete state machine.
- [ ] Unit tests against mock host.
- [ ] Integrate full CanvasInputHandler logic into the ViewModel (if not already done).

Phase 3 — Renderer
- [x] Create RenderState record struct and LayoutRenderer.
- [x] Comprehensive Render(DrawingContext, RenderState) implemented (objects, icons, labels, selection, basic influences).
- [x] Create RendererCaches helper class for pen/brush management.
- [x] Move pens/brushes/caches into renderer with version-based invalidation.
- [ ] Complete influence range polygon rendering (complex, requires RoadSearchHelper integration).
- [ ] Unit render tests or visual smoke tests.

Phase 4 — View
- [x] Implement AnnoCanvasV2.xaml and AnnoCanvasV2.xaml.cs as thin control.
- [x] Implement IScrollInfo pass-throughs to VM.
- [x] Hook notifications (RenderInvalidated -> InvalidateVisual()).
- [ ] E2E tests: replace v1 with v2 behind a feature flag, run tests.

Phase 5 — Integration & Cleanup
- [ ] Performance profiling and cache invalidation checks.
- [ ] Remove duplication and move shared utilities under /Shared or keep in Legacy for historical reference.
- [ ] Finalize documentation and developer notes about migration.
- [ ] When V2 is verified, update docs and deprecate Legacy/AnnoCanvasV1 in a separate PR (do not delete immediately).

-----

Suggested improvements and fixes while migrating

- Hotkeys: centralize into a HotkeyService so hotkey mapping is testable and not mixed with control code.
- File I/O: keep file operations out of the canvas. Expose events and let a MainViewModel or service handle persistence.
- Small helpers: move small pure helpers (Normalize, ComputeBoundingRect, GetObjectAt, Rotate math) into static, well-tested helper classes in Core.
- Undo/Redo: keep an IUndoManager interface and make it owned by the ViewModel. Provide additional unit tests for composite operations (AsSingleUndoableOperation).
- Renderer caches: use a version token (int or GUID) to invalidate renderer caches on configuration changes instead of relying on subtle side-effects.
- DI and wiring: prefer constructor injection for V2 classes. For WPF-only components, factory methods or service locators can be used at the control boundary.
- Use C# 10+ features:
  - file-scoped namespaces
  - record struct for RenderState
  - target-typed new and pattern matching
  - global using directives for common namespaces in the project

-----

Migration rules (strict):
- Never change AnnoCanvas v1 files. All logic removed from v1 must be moved into new files (V2) or Legacy/ folder. If behavior needs to be preserved during migration, create small forwarders in v1 that call into the new services but keep v1 sources unchanged in the mainline until you decide to fully switch.
- Each moved method or field must be placed with a comment: "migrated from AnnoCanvas.xaml.cs — <original member name>".
- Every behavioral change must be accompanied by a unit or integration test that asserts the old behaviour remains the same (or document the intended change explicitly).

-----

Rollback & Release strategy

- Deploy V2 behind a runtime feature flag. Keep v1 as default.
- Run A/B or Canary with a subset of users or as a dev-only toggle.
- If regressions are found, flip the feature flag back and fix V2 iteratively.

-----

Notes about what to move and where

- UI-only rendering code -> Rendering/LayoutRenderer.cs and Rendering/RenderState.cs (record struct)
- Input state machine -> Input/CanvasInputHandler.cs and IInputHandlerHost.cs
- State, commands, hotkey registration -> AnnoCanvasViewModel.cs (hotkeys wired through HotkeyService or HotkeyCommandManager)
- IScrollInfo implementation -> AnnoCanvasViewModel.cs (control passes through calls)
- File I/O (Save/Open/New) -> Remove from canvas and raise events OpenFileRequested/SaveFileRequested that MainViewModel subscribes to. Move actual Save/Open implementation to MainViewModel or a dedicated LayoutPersistenceService.
- Small pure helpers -> move to AnnoDesigner.Core.Helpers namespace and unit test them.

-----

Final remarks

- This migration plan prioritizes incremental, test-driven refactoring while keeping the legacy control untouched.
- Centralized feature flags reduce complexity and make it easier to toggle behaviour during migration and after release.
- Use modern C# constructs (record structs, file-scoped namespaces, target-typed new) to keep V2 concise and idiomatic.

Good luck — follow the checklist and keep commits small and test-covered. When in doubt, prefer copying behaviour into V2 rather than altering the existing V1 sources.