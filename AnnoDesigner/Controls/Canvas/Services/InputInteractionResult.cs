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
}
