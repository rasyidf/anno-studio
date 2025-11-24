# AnnoCanvas Responsibility Clusters (analysis)

This document captures responsibility clusters found in `AnnoCanvas.xaml.cs` with identifier names and the exact lineblocks where the code is currently located.

Each cluster includes:
- identifierName — short label
- lineblock — inclusive start..end line numbers in `AnnoCanvas.xaml.cs`
- description — what the cluster is responsible for
- recommendedDestination — where the code should be moved during the extraction
- movedAlready — whether code has been moved yet

---

1) identifierName: Rendering
- lineblock: #region Rendering
- description: OnRender & all rendering helpers (grid, objects, selection, influences, debug drawing, cached drawing groups)
- recommendedDestination: Controls/Canvas/Services/CanvasRenderer (fully implemented with RenderCore and specialized Draw methods)
- movedAlready: Yes — OnRender delegates to CanvasRenderer.Render, which contains the full rendering logic in RenderCore method. Specific drawing methods like DrawSelectionGroup, DrawObjectInfluenceRadius, and DrawPanoramaText have been extracted into CanvasRenderer.

2) identifierName: CoordinateAndTransforms
- lineblock: #region Coordinate and rectangle conversions
- description: Coordinate helpers (Rotate, coordinate conversions), transform math
- recommendedDestination: Controls/Canvas/Services/TransformService (fully implemented with Rotate and coordinate conversion methods)
- movedAlready: Yes — implemented in TransformService with ICoordinateHelper injection.

3) identifierName: Input_MouseHandling
- lineblock: #region Mouse
- description: Mouse event flow (OnMouseEnter/Leave/Wheel/Down/Move/Up), drag & drop logic, placement, hit testing calls
- recommendedDestination: Expand Controls/Canvas/Services/InputInteractionService (partially implemented with HandleMouseWheel and DecideOnMouseDown) for high-level decisions; delegate low-level math to TransformService
- movedAlready: Partial — OnMouseWheel handler and OnMouseDown decision logic extracted into InputInteractionService (wired in AnnoCanvas). Next: extract OnMouseMove/OnMouseUp.

4) identifierName: Input_KeyboardHandling
- lineblock: #region Keyboard
- description: keyboard event handling & helpers for modifier checks
- recommendedDestination: Integrate with Controls/Canvas/Services/InputInteractionService or existing HotkeyCommandManager for hotkey delegations
- movedAlready: No

5) identifierName: CollisionHandling
- lineblock: #region Collision handling
- description: collision tests, TryPlaceCurrentObjects, GetObjectAt, ComputeBoundingRect — geometry & collision checks
- recommendedDestination: Controls/Canvas/Services/SelectionService and LayoutModelService (fully implemented with selection logic, QuadTree management, collision checks, placement, and bounding rect calculations)
- movedAlready: Yes — implemented in LayoutModelService (QuadTree, TryPlaceCurrentObjects, GetObjectAt, ComputeBoundingRect) and SelectionService (selection management).

6) identifierName: PublicAPI
- lineblock: #region API
- description: public methods for consumers (SetCurrentObject / ResetZoom / Normalize / ResetViewport / RegisterHotkeys)
- recommendedDestination: Keep on AnnoCanvas surface (wiring) — delegate heavy work to services like TransformService for zoom/normalize logic
- movedAlready: No

7) identifierName: Persistence_FileIO
- lineblock: #region New/Save/Load/Export methods
- description: New/Save/Load/Export methods and unsaved-changes checks
- recommendedDestination: Controls/Canvas/Services/LayoutFileService (fully implemented with dialogs and unsaved-changes handling) and FileDialogService; keep only UI wiring on AnnoCanvas
- movedAlready: Partial — Methods delegate to LayoutFileService for dialogs and checks, but implementation still in AnnoCanvas.

8) identifierName: Commands_HotkeysAndHandlers
- lineblock: #region Commands
- description: Hotkey handling, command registrations and command implementations (Rotate/Copy/Paste/Delete/Duplicate/Undo/Redo/Select etc.)
- recommendedDestination: Use Controls/Canvas/Services/CommandHistoryService (basic undo/redo) for history; refactor implementation logic into InputInteractionService, LayoutModelService, and CanvasRenderer; keep small wiring on AnnoCanvas
- movedAlready: No

9) identifierName: HelperMethods
- lineblock: #region Helper methods
- description: clone helpers, update scroll bar visibility
- recommendedDestination: Create a dedicated UtilityService in Controls/Canvas/Services or move to AnnoDesigner.Core utilities
- movedAlready: No

10) identifierName: Scrolling_IScrollInfo
- lineblock: #region IScrollInfo
- description: IScrollInfo implementation (viewport, offsets, scrolling actions)
- recommendedDestination: Could remain on AnnoCanvas or create a ViewportService in Controls/Canvas/Services for scroll logic
- movedAlready: No

---

Notes:
- The above lineblocks were collected from the current `AnnoCanvas.xaml.cs` (branch: upgrade-to-NET10) — keep them as a reference during extraction.
- "movedAlready" is set to No for everything — initial skeleton services were created in `Controls/Canvas/Services` and `Controls/Canvas/Models` to start extraction work.

Next step: Move pieces incrementally — start with rendering extraction into `CanvasRenderer`, verify build and UI behavior, add unit tests for transform math.
