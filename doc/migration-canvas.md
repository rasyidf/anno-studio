# Canvas V2 Migration — Status, Checklist & Tracking

Date: 2025-11-10

This document tracks the current state of Canvas V2 in the repository, feature-parity with the v1 `AnnoCanvas`, outstanding gaps, recommended adapter/API changes, and a checklist to follow during migration.

## TL;DR
- Canvas V2 is functional and has a ViewModel, renderer and an adapter (`CanvasV2Adapter`).
- Several integration surface items are missing or only partially implemented (export path, CheckUnsavedChanges, full Undo/Clipboard exposure, event parity, DPI handling and some UI wiring).
- Next step: formalize an `IAnnoCanvas` compatibility interface, update `CanvasV2Adapter` to implement it, add factory wiring and tests, then finish feature parity (export, clipboard, flags, hotkeys, events).

---

## Migration checklist (tracking)
- [x] Create migration docs — document current state, parity, missing items, adapter redesign and next steps. (Completed)
 - [x] Define `IAnnoCanvas` contract (interface for adapter parity)
 - [x] Audit feature parity (v1 vs v2)
 - [x] Define `IAnnoCanvas` contract (interface for adapter parity)
 - [x] Audit feature parity (v1 vs v2)
- [ ] Adapter refactor plan (make `CanvasV2Adapter` independent and implement `IAnnoCanvas`)
- [ ] Implementation tasks (wire hotkeys, export, CheckUnsavedChanges, DPI, presets/icons sync)
- [ ] Validation & tests (unit + integration + smoke export tests)
- [ ] CI + docs (hook tests and publish migration guide)

Notes: the above checklist mirrors the tracked todo list used by the team; each item should be updated in the central todo tracker when started/completed.

---

## Current repository facts (quick)
Files of interest (under `AnnoDesigner/CanvasV2`):
- `AnnoCanvasV2.xaml` / `AnnoCanvasV2.xaml.cs`
- `AnnoCanvasViewModel.cs`
- `CanvasV2Adapter.cs`
- `Rendering/` (`LayoutRenderer.cs`, `RenderState.cs`, `RendererCaches.cs`)
- `Input/` (`CanvasInputHandler.cs`, `MouseMode.cs`)
- `FeatureFlags/` (`IFeatureFlags`, `SimpleFeatureFlags`, `CanvasFeatureFlagNames`)

`MainWindow` currently performs special-case wiring when the `UseCanvasV2` flag is enabled: it constructs the V2 viewmodel, copies `BuildingPresets` and `Icons` from the v1 canvas to the v2 viewmodel, creates `CanvasV2Adapter`, assigns it to `_mainViewModel.AnnoCanvas` and registers hotkeys. There's additional ad-hoc wiring from `MainViewModel.PropertyChanged` into `SimpleFeatureFlags`.

---

## Feature-parity summary
Status codes: Present / Partial / Missing

- Undo/Redo + Dirty tracking: Partial — v2 accepts `UndoManager` but adapter must expose it to callers.
- Clipboard (copy/paste layout): Partial — viewmodel accepts `clipboardService`; adapter must expose and ensure format parity.
- Building presets & icons: Partial — `MainWindow` currently copies presets into v2; adapter should accept updates and refresh caches.
- Hotkeys: Partial — `RegisterHotkeys` is called; verify mapping parity and focus handling.
- Feature toggles (grid, icons, labels, influences): Present (flags) / Partial (render completeness).
- Rendering & Export: Partial/Missing — renderer exists but adapter lacks `PrepareCanvasForRender` / `RenderToFile` parity used by CLI/export flows.
- DPI handling: Partial/Unknown — ensure renderer caches respect `App.DpiScale` and handle `DpiChanged`.
- Input behavior (selection, keyboard navigation): Partial — implemented but needs parity tests against v1.
- CheckUnsavedChanges (WindowClosing): Missing — adapter must implement async check used by `MainWindow`.
- CLI export path (ExportArgs): Missing — integrate v2 into headless export flow.

---

## Detailed feature parity audit (expanded)
Legend: Present = fully implemented & verified. Partial = implemented but needs verification or has gaps. Missing = not yet implemented in V2 path.

| Category | v1 Status | v2 Status | Gap Summary | Recommended Actions |
|----------|-----------|-----------|-------------|--------------------|
| Core Rendering (objects, grid) | Present | Present | ViewModel snapshot vs direct drawing grouping differs; need perf comparison | Add perf benchmark + compare frame times; optimize renderer caches if needed |
| Influence Radius/Range | Present | Partial | Range logic ported; true range & offscreen object influence needs visual parity tests | Add visual regression tests; ensure offscreen influence union logic matches v1 |
| Panorama Text | Present | Partial | Logic for skyscraper regex & panorama exists in v1 only; v2 flags present but feature not rendered | Port panorama calculation/render from v1 to renderer or VM; add sample test layout |
| Harbor Blocked Area | Present | Partial | Flag exists; blocked area brush logic not confirmed in v2 | Validate rendering; port missing blocked area rectangle logic if absent |
| Labels & Icons | Present | Present | Icon duplication prevention logic differs; ensure identical fallbacks on missing icon | Add test for missing icon logs/status message |
| Selection Rectangle & Multi-Select | Present | Present | Behavior mostly ported to `CanvasInputHandler`; collision recalculation simplified | Add parity tests for drag, additive selection (Ctrl/Shift), identifier grouping |
| Drag & Move Selection | Present | Partial | Collision + rollback implemented; need undo op parity and tree reindex optimization | Validate undo stacks; implement selective QuadTree reindex as optimization |
| Continuous Placement | Present | Partial | V2 copies current objects each click; test for multi-object templates | Add tests for continuous draw of multi-cell objects |
| Rotate (single/multi) | Present | Present | Direction/size rotate logic ported | Verify rotate-all hotkey; add tests |
| Duplicate (double-click) | Present | Missing | Hotkey and gesture not mapped in v2 | Add duplicate command & hotkey binding in VM/Input handler |
| Delete Object Under Cursor (Right-click) | Present | Partial | Right-click resets mode; specific targeted delete missing | Implement targeted delete when right-click over object (match v1 behavior) |
| Select All Same Identifier | Present | Missing | Gesture (Ctrl+Shift+Click) logic absent | Port logic to CanvasInputHandler; add test |
| Undo/Redo | Present | Partial | Operation coverage narrower (rotate-all, duplicate) | Add missing operations and tests for each command |
| Clipboard Copy/Paste | Present | Partial | Format parity likely OK, need multi-object copy orientation tests | Add round-trip tests; ensure selection after paste matches v1 |
| Layout Serialization (Load/Save) | Present | Partial | VM raises request events; file path update logic relies on host | Ensure host fully sets LoadedFile and resets Dirty state; integration test |
| Presets & Icons Loading | Present | Partial | Adapter constructor handles initial load; runtime refresh missing | Implement adapter refresh methods + tests |
| Statistics / ColorsInLayout events | Present | Partial | Events forwarded but recalculation triggers differ | Add tests verifying events fire after add/delete/move |
| Hotkeys Registration | Present | Partial | Command bindings mapped; missing some (duplicate, select same identifier, enable debug mode) | Add missing hotkeys & ensure idempotent registration |
| Debug Mode Overlays | Present | Missing | No debug flag logic in v2 renderer | Defer (low priority) or add flag-driven overlay rendering |
| DPI Awareness / Scaling | Present | Partial | V2 renders based on GridSize; no explicit DPI invalidation wiring | Hook into App DPI change event, invalidate caches; test high DPI export |
| Export (PrepareCanvasForRender) | Present | Missing | Adapter only has `RenderToFile`; headless multi-pane export absent | Implement PrepareCanvasForRender parity (statistics/version panes) |
| Export Threading (STA) | Present | Missing | V2 export runs inline; no background STA thread | Wrap export in STA thread for clipboard/image encoding parity |
| Window Closing Unsaved Check | Present | Present | Routing improved; verify main uses adapter property consistently | Ensure MainWindow only references `_mainViewModel.AnnoCanvas` |
| CLI Export Args Flow | Present | Missing | Currently hardwired to v1 pipeline | Detect v2 adapter and route to its export methods |
| Normalize / ResetZoom | Present | Present | Behavior ported; layout bounds recalculation simplified | Add tests for border normalization equality |
| Viewport Scrolling & ScrollInfo | Present | Partial | Basic scroll info present; fine-grained invalidation differs | Stress-test large layouts; optimize scroll invalidation |
| Recent Files Integration | Present | Missing | VM lacks recent-files helper integration | Expose events or injection point for recent files service |
| Language Change / Localization Hooks | Present | Missing | VM doesn't handle dynamic localization refresh | Optional: integrate localization refresh into VM; low priority |
| Preferences / Keybindings UI | Present | Missing | V2 doesn’t populate manage keybindings page yet | Populate from HotkeyCommandManager; list v2-only commands |
| Performance (Drawing groups caching) | Present | Partial | V2 snapshot approach may re-render more often | Profile & introduce selective caching if needed |
| QuadTree Optimized Reindex | Present | Missing | V2 rebuilds entire QuadTree on move | Implement selective `ReIndex` logic similar to v1 |
| Selection Influence Highlight Filtering | Present | Partial | Logic ported but offscreen object highlight parity unverified | Add tests for influence across viewport boundaries |
| Blocked Harbor Area Rendering | Present | Partial | Brush logic may not be applied identically | Confirm and port if gap |
| Road / True Influence Range BFS | Present | Partial | BFS port incomplete or untested for performance | Add stress test with large road network |

### High-priority partial/missing items
1. Export pipeline parity (PrepareCanvasForRender, CLI integration, statistics/version overlays).
2. Missing interaction commands (Duplicate, SelectSameIdentifier, DeleteObjectUnderCursor specifics).
3. Hotkey parity for newly added or unmapped commands.
4. Clipboard & undo operation completeness.
5. Performance: selective QuadTree reindex & renderer caching under large layouts.

### Medium priority
1. DPI invalidation & high DPI export verification.
2. Panorama & harbor blocked area visual parity.
3. Presets/icons runtime refresh API.
4. Influence visualization correctness (offscreen objects, true range BFS performance).

### Low priority / optional
1. Debug overlays.
2. Recent files integration.
3. Preferences/Keybindings UI population.
4. Localization live refresh.

### Recommended Test Matrix
| Test Type | Scenario | Assertion |
|----------|----------|-----------|
| Unit | Rotate multi-object selection | All objects rotated; undo reverts |
| Unit | Clipboard round-trip (multi + single) | Pasted layout objects equal original (identifier, size, orientation) |
| Unit | Duplicate command | New instance appears offset; undo removes duplicate |
| Unit | Select same identifier gesture | All matching identifiers selected; deselect works with modifier removal |
| Unit | Normalize with border | Min position >= border; layout bounding rect stable |
| Unit | Undo move after drag | Positions revert exactly; QuadTree count stable |
| Integration | Export with statistics/version | Image includes overlay panes with correct sizes |
| Integration | High DPI export | Image scaled; grid lines crisp (no blurring) |
| Integration | Influence visualization across viewport | Offscreen influencer still renders influence area |
| Performance | Large layout render (N objects) | Frame time under threshold; memory stable |
| Performance | Continuous placement stress | No exponential undo growth; acceptable GC stats |

### Implementation Sprint Backlog (Ordered)
1. Adapter: Add `PrepareCanvasForRender` parity method (statistics/version overlays).
2. Export: STA-threaded export + CLI routing for V2.
3. Interaction: Duplicate, SelectSameIdentifier, DeleteObjectUnderCursor (specific object) implementation & hotkeys.
4. QuadTree selective reindex (replace full rebuild in `ReindexMovedObjects`).
5. Presets/Icons runtime refresh API + tests.
6. Panorama/Harbor blocked area parity visual tests & renderer adjustments.
7. DPI change hook invalidation (subscribe to app DPI events). 
8. Influence range validation & performance test (road BFS optimization).
9. Recent files + localization refresh hooks (optional).

### Done Definition for Parity Completion
All High-priority backlog items completed & tested, no Missing statuses remain in table, all Partial items have either tests or explicit deferral ticket.

The table below lists important app features, current V2 status and recommended next steps to reach parity with v1.

- Undo/Redo & Dirty tracking: Partial
   - Status: V2 `AnnoCanvasViewModel` accepts an `IUndoManager` and uses it for operations. `CanvasV2Adapter` exposes `UndoManager` via the `IAnnoCanvas` contract.
   - Gaps: Ensure `UndoManager` instance is the one used by the rest of the app (MainWindow passes v1's manager when migrating). Verify all undoable operations register the same operation types and UI command states (CanUndo/CanRedo).
   - Action: Add unit tests for a simple operation (add object -> undo -> redo) via adapter.

- Clipboard (copy/paste layout): Partial
   - Status: ViewModel uses an `IClipboardService` and exposes Copy/Paste commands. Adapter accepts and forwards the service.
   - Gaps: Verify clipboard format parity with v1 and that paste placement behavior is identical (current objects and selection updates). Also expose `ClipboardService` on adapter if callers expect direct access.
   - Action: Add round-trip clipboard tests and a smoke manual test for copy/paste with preset objects.

- Building presets & Icons: Partial
   - Status: MainWindow used to copy `BuildingPresets` and `Icons` into v2 viewmodel; adapter constructor now accepts optional presets/icons and initializes VM.
   - Gaps: Ensure v2 caches are refreshed when presets/icons are updated at runtime (e.g. LoadPresets flow). Confirm search and tree interactions behave the same.
   - Action: Add adapter methods `UpdatePresets(BuildingPresets)` and `UpdateIcons(Dictionary<...>)` (or make properties settable) and add tests.

- Hotkeys: Partial
   - Status: Adapter and VM expose `HotkeyCommandManager` and `RegisterHotkeys` is already used by MainWindow.
   - Gaps: Confirm hotkey registration is idempotent and focus/priority behavior matches v1. Ensure commands map the same names and handlers.
   - Action: Test global hotkeys and in-canvas hotkeys (rotate, delete, copy/paste, undo/redo).

- Feature toggles (grid, labels, icons, influences, panorama): Partial
   - Status: FeatureFlag subsystem exists and `AnnoCanvasViewModel` reads a `FeatureSnapshot`. MainWindow still mutates `SimpleFeatureFlags` in response to viewmodel changes.
   - Gaps: Prefer adapter/VM to listen to `IFeatureFlags` changes or provide `SetFeatureFlag(name, bool)` so MainViewModel doesn't need to mutate internals.
   - Action: Expose a small API on adapter to set flags, and update MainWindow to call that API (or pass the same `IFeatureFlags` instance into the adapter/VM).

- Rendering & Export (PrepareCanvasForRender/RenderToFile): Partial/Missing
   - Status: `LayoutRenderer` and render pipeline exist; however the v1 export helper `PrepareCanvasForRender(...).RenderToFile(...)` is not yet provided by adapter.
   - Gaps: CLI `ExportArgs` path currently uses v1 `PrepareCanvasForRender`. We must provide an adapter method with the same signature/behavior or make `_mainViewModel.PrepareCanvasForRender` route to v2 renderer when adapter is active.
   - Action: Implement `PrepareCanvasForRender` on adapter and hook CLI export flow to use it.

- DPI & Renderer Caches: Partial
   - Status: App sets `App.DpiScale` and `DpiChanged` event is handled in MainWindow; renderer caches exist in `RendererCaches`.
   - Gaps: Verify caches are invalidated on DPI change and that `RenderState` uses scaled transforms correctly.
   - Action: Add unit test for DPI change (invoke `MainWindow_DpiChanged`) and visually verify export scaling.

- Input handling (selection, drag, keyboard navigation): Partial
   - Status: Input handler exists (`CanvasInputHandler`) and VM provides IInputHandlerHost hooks.
   - Gaps: Behavioral parity with v1 needs to be validated (caret, continuous draw, drag thresholds, keyboard nav, text input focus handling).
   - Action: Test interactive flows manually and add automated UI tests if possible.

- Selection UI, highlights, labels and statistics integration: Partial
   - Status: VM fires `StatisticsUpdated`, `ColorsInLayoutUpdated` events and exposes selection. Adapter forwards these.
   - Gaps: Confirm `StatisticsViewModel` updates and selection-driven UI controls behave as before.
   - Action: Add integration tests that perform selection changes and assert `StatisticsViewModel` changes.

- CheckUnsavedChanges (WindowClosing): Present (VM), Adapter: Present
   - Status: `AnnoCanvasViewModel.CheckUnsavedChanges()` exists. `CanvasV2Adapter` forwards `CheckUnsavedChanges()` to the VM; MainWindow's closing handler still calls v1 `annoCanvas.CheckUnsavedChanges()` — this must be conditional or route to `_mainViewModel.AnnoCanvas.CheckUnsavedChanges()` when using v2.
   - Gaps: `WindowClosing` still references `annoCanvas` directly. Update to use `_mainViewModel.AnnoCanvas.CheckUnsavedChanges()` so both canvases are handled uniformly.
   - Action: Patch `WindowClosing` to call through the `MainViewModel` canvas property instead of v1 control.

- CLI Export (ExportArgs headless path): Missing for v2
   - Status: MainWindow export code currently calls v1 pipeline. Adapter must expose the same export API or MainViewModel should detect v2 and route accordingly.
   - Action: Implement `RenderToFile` / `PrepareCanvasForRender` on adapter and update `MainWindow` CLI export path.

- Serialization / LayoutLoader compatibility: Partial
   - Status: `AnnoCanvasViewModel` accepts an `ILayoutLoader` and the same `LayoutFile` model is used by renderer.
   - Gaps: Verify any v1-specific assumptions in renderer (e.g., roads handling) are preserved.
   - Action: Run export on representative layouts and compare outputs.

- Panorama & Advanced Influence rendering: Partial
   - Status: Flags exist; renderer has infrastructure but feature parity needs visual verification.
   - Action: Test with layouts using panorama and influence features.

- Misc UI helpers (Normalize, ResetZoom, MergeRoads): Partial
   - Status: Normalize/ResetZoom exist on VM and adapter; MergeRoads is a tool on MainViewModel which expects behavior from canvas.
   - Gaps: Ensure these commands produce identical results and are exposed on adapter.
   - Action: Add functional tests for Normalize/ResetZoom and MergeRoads.

---

## Audit conclusion
Canvas V2 covers the majority of core functionality (rendering, undo, basic input, hotkey plumbing, feature flags) but some integration surface pieces are still missing or need small wiring changes:

- Export/PrepareCanvasForRender parity for CLI is the top blocker for headless export flows.
- `WindowClosing` must be updated to call `_mainViewModel.AnnoCanvas.CheckUnsavedChanges()` instead of directly referencing the v1 `annoCanvas` control.
- Runtime presets/icons updates, DPI cache invalidation, and full input/selection parity need verification via tests and manual checks.

Priority next actions (short):
1. Update `WindowClosing` to call `_mainViewModel.AnnoCanvas.CheckUnsavedChanges()`.
2. Implement `PrepareCanvasForRender`/`RenderToFile` on adapter and route CLI export flows to it.
3. Add adapter APIs for updating presets/icons at runtime and tests for undo/clipboard/hotkeys.


---

## Concrete actions (short)
1. Define `IAnnoCanvas` interface capturing the minimal compatibility surface:
   - Properties: `UndoManager`, `ClipboardService`, `BuildingPresets`, `Icons`.
   - Methods: `RegisterHotkeys`, `Task<bool> CheckUnsavedChangesAsync()`, `ResetZoom()`, `Normalize()`, `RenderToFileAsync(...)` or `PrepareCanvasForRender(...)`.
   - Events: `DirtyStateChanged`, `SelectionChanged`.

2. Update `CanvasV2Adapter` to implement `IAnnoCanvas` and avoid referencing `MainWindow`. Make it accept the `AnnoCanvasViewModel` + control (or create them internally via a factory).

3. Add `IAnnoCanvasFactory` (optional) to centralize construction and wiring.

4. Implement `PrepareCanvasForRender` / `RenderToFile` in v2 to match CLI/export behavior.

5. Expose `CheckUnsavedChangesAsync` which mirrors current `annoCanvas.CheckUnsavedChanges()` behavior used during `WindowClosing`.

6. Add unit tests for adapter behavior and a small integration test exercising undo/redo, clipboard, and export.

---

## Suggested timeline & priorities
1. (High) `IAnnoCanvas` + `CanvasV2Adapter` changes so `MainWindow` can use adapter without bespoke wiring.
2. (High) `CheckUnsavedChangesAsync` + `RenderToFile` parity for CLI and closing flows.
3. (Medium) Hotkey parity, Clipboard exposure, Preset/Icon sync and migration to factory/DI.
4. (Low) Performance tuning, renderer cache invalidation, additional UI polish.

---

## Smoke test checklist (manual)
- [ ] Build solution and run app with `UseCanvasV2` feature flag enabled.
- [ ] Verify canvas displays and does not throw on load.
- [ ] Make a change, verify undo/redo and title unsaved indicator toggle.
- [ ] Copy/paste layout and verify contents.
- [ ] Trigger Export via menu and confirm an image file is produced.
- [ ] Close the app with unsaved changes and verify confirmation dialog.

 