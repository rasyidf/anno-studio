# AnnoCanvas Migration Plan

## 1. Preparation

* Create folders: `Canvas/Services`, `Canvas/Models`, `Canvas/Commands`.
* Add empty service interfaces and classes: `ICanvasRenderer`, `IInputInteractionService`, `ISelectionService`, `ITransformService`, `ILayoutModelService`, `ICommandHistoryService`.
* Add basic model types: `CanvasItem`, `LayoutModel`, `SelectionState`, `CanvasTransformState`.

## 2. Identify responsibility clusters in AnnoCanvas.xaml.cs

Group methods by:

* Rendering
* Mouse/keyboard interaction
* Selection and dragging
* Coordinate transforms
* Model mutations (add/remove/move/rotate)
* Undo/redo logic
* Persistence calls

Document these areas in a temporary file.

## 3. Extract rendering

* Locate OnRender and all drawing helpers.
* Move drawing code to `CanvasRenderer.Render`.
* Replace inline drawing calls with `_renderer.Render`.

## 4. Extract transform logic

* Find zoom, pan, and coordinate conversion functions.
* Move math into `TransformService` (zoom handling, ScreenToCanvas, CanvasToScreen).
* Replace calls inside AnnoCanvas with `_transform.Method(...)`.

## 5. Extract selection logic

* Identify fields related to selection, dragging, hover.
* Move state to `SelectionService.Current`.
* Move operations like Select(), Clear(), SetDragStart(), UpdateDrag() into service.
* Update event handlers to call selection service methods.

## 6. Extract input handling

* Identify MouseDown, MouseMove, MouseUp, MouseWheel.
* Move decision logic (hit testing, drag initiation, drag updates) into `InputInteractionService`.
* Event handlers in AnnoCanvas become single-line delegates to this service.

## 7. Extract model operations

* Identify code manipulating placed objects: add, remove, move, rotate.
* Move all modifications to `LayoutModelService`.
* Ensure AnnoCanvas keeps no direct mutation logic.

## 8. Extract undo/redo

* Identify command-like logic.
* Create simple command classes: MoveItemCommand, ResizeItemCommand, RotateItemCommand.
* Move undo/redo stacks to `CommandHistoryService`.
* Update input/selection services to wrap changes in commands.

## 9. Replace direct field access

* Remove fields from AnnoCanvas that represent state now stored in services.
* Replace direct reads with service properties:

  * selection state
  * transform state
  * model

## 10. Reduce AnnoCanvas to wiring

* Remove all logic not related to:

  * hosting the canvas control
  * event subscription
  * delegating work to services
* OnRender calls only renderer.
* Mouse/keyboard events call only input/transform services.

## 11. Verification

* Build after each extraction step.
* Write unit tests for:

  * transform math
  * selection logic
  * model operations
  * command history
* Smoke test UI after each completed responsibility extraction.

## 12. Removal of dead code

* After all responsibilities are extracted, delete:

  * helper methods now moved into services
  * unused fields
  * old interaction logic

## 13. Final cleanup

* Split any oversized service into smaller parts if necessary.
* Add XML docs for public interfaces.
* Add internal visibility for implementation classes if not required publicly.
 