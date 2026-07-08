# Multi-Document & Docking Roadmap (MDT) — Iteration v2 (Rewritten)

## Purpose
This document restates and expands the MDT work to include a Dock Manager for extensible panes, a clear separation of canvas ownership from UI surface and `MainViewModel`, and a concrete migration plan to enable multi-document tabs while preserving existing behavior for users.

## High-level goals
- Introduce a robust per-document model: each document owns its `AnnoCanvas`, file metadata, undo history, and export behavior.
- Add a `DocumentManager` to manage document lifecycle, active document selection, and persistence operations.
- Introduce a `DockManager` (or integrate with existing docking) to host property panes, tools, and per-document panels so multiple documents can share the same UI layout.
- Isolate `MainViewModel` from direct file I/O and canvas lifecycle; make it an orchestrator for UI concerns only.
- Provide safe, render-time helpers (`CanvasRenderHelper` / `CanvasFactory`) so exports and thumbnails do not rely on visible UI elements or UI thread assumptions.
- Keep the changes incremental, well-tested, and reversible until coverage and UX signoff are complete.

## Scope
- Components: `IAnnoDocument`, `AnnoDocument`, `DocumentManager`, `DockManager` (or Docking integration), `CanvasRenderHelper`/`CanvasFactory`, changes to `MainViewModel`, and a small tabbed UI.
- Non-goals: full-feature parity for complex multi-window workflows in the first iteration (we'll aim for a conservative, well-tested MVP).

## Architecture overview
- `AnnoDocument`: owns a single `AnnoCanvas` instance, file path, dirty-state, open/save/close logic, and exposes events (e.g., `LoadedFileChanged`, `StatisticsUpdated`, `StatusMessageChanged`).
- `DocumentManager`: keeps `ObservableCollection<IAnnoDocument> Documents`, `IAnnoDocument ActiveDocument`, and APIs `OpenAsync`, `SaveAsync`, `New`, `CloseAsync`.
- `DockManager`: provides a way to register panes and associate their context with the `ActiveDocument` (so property panes reflect the active document without re-creating UI components on each tab change).
- `CanvasRenderHelper`/`CanvasFactory`: creates off-screen canvases for rendering/exporting and lightweight snapshots used by the UI without touching the live canvas UI thread state.
- `MainViewModel`: no longer performs file load/save directly; it binds to `DocumentManager.ActiveDocument` and to dock manager state and routes UI commands (New/Open/Save/Close/Export) through `DocumentManager`.

## Design principles
- Keep single-document behavior unchanged through the Stabilize phase.
- Make every change testable and add unit tests before removing fallbacks.
- Prefer composition over inheritance when integrating with existing docking systems.
- Minimize UI-thread work for render and export operations.

## Phased migration plan

Phase 0 — Stabilize (current)
- Add `IAnnoDocument` and `AnnoDocument` with basic open/save/dirty-state and event forwarding.
- Add `CanvasRenderHelper` and `CanvasFactory` for off-screen rendering.
- Add a lightweight `DocumentManager` (helpers only) and wire `MainViewModel` to register a canvas as a document (preserve fallback behavior).

Phase 1 — Document-first refactor (short-term)
- Move file load/save, clipboard import/export, and layout serialization into `AnnoDocument`.
- Consolidate `DocumentManager` into a single class and mark it as the app-wide service (register in DI / service locator).
- Replace `MainViewModel` direct file operations with calls to `DocumentManager` and subscribe only to `ActiveDocument` events.
- Add unit tests covering `AnnoDocument` open/save/dirty behavior and `DocumentManager` operations.

Phase 2 — Docking and UI (medium-term)
- Add/Integrate `DockManager` and refactor panes (properties, layers, hierarchy, assets) to bind to `DocumentManager.ActiveDocument` and not directly to `MainViewModel` internals.
- Implement a minimal tab bar UI with the following actions: New, Open (in new tab), Close (with unsaved prompt), Duplicate, Rename, Reorder.
- Make export use `CanvasRenderHelper` so it can operate without a visible canvas.

Phase 3 — Hardening & polish (long-term)
- Add integration tests (tab interactions, docking, concurrent document operations).
- Audit threading and memory usage; fix leak or cross-thread access issues.
- Improve UX: persistent layout per workspace, tab restoration, and per-document settings.

## Acceptance criteria
- Existing single-document flows remain unchanged for end-users.
- `AnnoDocument` owns file/undo/serialization concerns and is covered by unit tests.
- `DocumentManager` provides reliable open/save/new/close operations and emits events suitable for MVVM binding.
- `DockManager` and property panes update correctly on `ActiveDocument` changes.
- A minimal tab UI exists and supports core workflows with correct unsaved prompts.

## Testing strategy
- Unit tests for `AnnoDocument` (open/save, serialization, dirty flag, events).
- Unit tests for `DocumentManager` (open/save/close semantics, active document switching, collection updates).
- UI/integration tests for tab interactions and `DockManager`-driven pane updates (smoke tests initially).
- Performance tests for export and large-document memory usage.

## Risk register & mitigations
- Duplicate/partial `DocumentManager` definitions — mitigate by consolidating early and adding a compatibility layer.
- Export depending on UI thread — mitigate by implementing `CanvasRenderHelper` that uses off-screen rendering patterns and minimal shallow-copy models for images.
- Regressions in `MainViewModel` behavior — keep fallbacks for the Stabilize and Refactor phases and remove only when tests and manual QA pass.

## Implementation checklist (developer tasks)
- Add: `IAnnoDocument`, `AnnoDocument`, `DocumentManager` (single class), `CanvasRenderHelper`, `CanvasFactory`, `DockManager` integration hooks.
- Modify: `MainViewModel` to rely on `DocumentManager.ActiveDocument` and to move UI-only responsibilities there.
- Add unit tests and basic UI tests.
- Implement tab UI and wire to `DocumentManager`.
 

## Files (expected to be added / modified)
- Add: `AnnoDesigner/Models/Interface/IAnnoDocument.cs`, `AnnoDesigner/Models/AnnoDocument.cs`.
- Add/Modify: `AnnoDesigner/Services/DocumentManager.cs` (consolidate into one file).
- Add: `AnnoDesigner/Helpers/CanvasRenderHelper.cs`, `AnnoDesigner/Helpers/CanvasFactory.cs`.
- Add/Modify: Docking integration or `DockManager` extension points (folder: `AnnoDesigner/UI/DockManager/`).
- Modify: `AnnoDesigner/ViewModels/MainViewModel.cs` (remove direct file logic, subscribe to `ActiveDocument`).

## Immediate next steps (what I will do next)
1. Produce the first focused PR adding `IAnnoDocument`, `AnnoDocument`, and `CanvasRenderHelper` with unit tests for document-level operations.
2. Consolidate `DocumentManager` into a single file and add unit tests for its core API.
3. Add basic tab UI wired to `DocumentManager` for reviewers to exercise the flow.

## Signals that we are done
- All acceptance criteria are met and covered by automated tests.
- No regressions in single-document workflows (manual smoke tests pass).
- Dock panes correctly reflect `ActiveDocument` context.

## Contact / ownership
- Author: AnnoDesigner roadmap and implementation (iteration authored/maintained by the development team). Reach out on the repo's issue tracker for discussion.

---

Markdown generated: `docs/mdt_v2.md` — please review and tell me whether you want a branch/PR for Phase 0 (stabilize) created next.
