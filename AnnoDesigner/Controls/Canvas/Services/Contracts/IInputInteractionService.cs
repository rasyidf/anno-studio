using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface IInputInteractionService
    {
        // Handles high-level input decisions (hit testing, drag logic)
        MouseWheelResult HandleMouseWheel(int delta, Point mousePosition, int currentGridSize, bool useZoomToPoint, double zoomSensitivityPercentage, Viewport viewport, bool placedObjectsEmpty, ICoordinateHelper coordinateHelper);

        // Decide high-level action to take for a mouse-down. The control will apply the suggested action.
        MouseDownDecision DecideOnMouseDown(bool leftPressed, bool rightPressed, Point mousePosition, bool currentModeWasDragSelection, int currentObjectsCount, System.Func<Point, AnnoDesigner.Models.LayoutObject> getObjectAt, System.Func<AnnoDesigner.Models.LayoutObject, bool> selectedContains, bool isControlPressed, bool isShiftPressed);

        void HandleDragStartCheck(ref MouseMode currentMode, Point mousePosition, Point mouseDragStart, HashSet<AnnoDesigner.Models.LayoutObject> selectedObjects, System.Func<Point, AnnoDesigner.Models.LayoutObject> getObjectAt, out List<(AnnoDesigner.Models.LayoutObject Item, Rect OldGridRect)> oldObjectPositions);

        void HandleDragAll(Point mousePosition, ref Point mouseDragStart, int gridSize, Viewport viewport, ICoordinateHelper coordinateHelper, IAppSettings appSettings, out bool invalidateScroll);

        void HandleSelectionRect(Point mousePosition, Point mouseDragStart, int gridSize, Viewport viewport, QuadTree<AnnoDesigner.Models.LayoutObject> placedObjects, HashSet<AnnoDesigner.Models.LayoutObject> selectedObjects, ICoordinateHelper coordinateHelper, bool isControlPressed, bool isShiftPressed, bool shouldAffectObjectsWithIdentifier, out bool statisticsUpdated);

        void HandleDragSelection(Point mousePosition, ref Point mouseDragStart, int gridSize, ref List<(AnnoDesigner.Models.LayoutObject Item, Rect OldGridRect)> oldObjectPositions, ref Rect collisionRect, HashSet<AnnoDesigner.Models.LayoutObject> selectedObjects, QuadTree<AnnoDesigner.Models.LayoutObject> placedObjects, ICoordinateHelper coordinateHelper, out bool invalidateScroll, out bool statisticsUpdated, out bool forceRendering);

        void HandleMouseUpDragSelection(List<(AnnoDesigner.Models.LayoutObject Item, Rect OldGridRect)> oldObjectPositions, HashSet<AnnoDesigner.Models.LayoutObject> selectedObjects, bool isRightButton, out bool registerUndo, out bool reindex, out bool clearSelection);
    }
}
