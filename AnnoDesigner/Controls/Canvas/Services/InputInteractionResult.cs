using System.Windows;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal record MouseWheelResult(int NewGridSize, double NewViewportLeft, double NewViewportTop);

    internal enum MouseDownAction
    {
        None,
        DragAllStartAndRegisterMove,
        PlaceCurrentObjects,
        SelectionRectStart,
        DragSelectionStart,
        DragSingleStart
    }

    internal record MouseDownDecision(MouseDownAction Action, LayoutObject ClickedObject);

    // Mouse Enter/Leave decisions
    internal enum MouseEnterAction
    {
        SetMouseWithinControl
    }

    internal record MouseEnterDecision(MouseEnterAction Action);

    internal enum MouseLeaveAction
    {
        ClearMouseWithinControl,
        ClearSelectionRect,
        ReindexMovedObjects
    }

    internal record MouseLeaveDecision(MouseLeaveAction[] Actions);

    // Mouse Move decisions
    internal enum MouseMoveAction
    {
        None,
        TransitionToSelectionRect,
        TransitionToDragSelection,
        TransitionToDragSingleObject,
        TransitionToDragAll,
        DragAllViewport,
        PlaceObjectsContinuous,
        UpdateSelectionRect,
        DragSelectedObjects
    }

    internal record MouseMoveDecision(MouseMoveAction Action, object Data = null);

    // Mouse Up decisions
    internal enum MouseUpAction
    {
        None,
        EndDragAll,
        ToggleObjectSelection,
        ClearSelection,
        EndSelectionRect,
        EndDragSelection,
        CancelPlacement,
        TransitionToPlaceObjects,
        TransitionToStandard
    }

    internal record MouseUpDecision(MouseUpAction[] Actions, object Data = null);

    // Key Down decisions
    internal enum KeyDownAction
    {
        None,
        Handled
    }

    internal record KeyDownDecision(KeyDownAction Action);
}
