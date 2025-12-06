using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AnnoDesigner.Core;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class InputInteractionService : IInputInteractionService
    {
        // Will encapsulate hit testing and input decision logic

        public MouseWheelResult HandleMouseWheel(int delta, System.Windows.Point mousePosition, int currentGridSize, bool useZoomToPoint, double zoomSensitivityPercentage, Viewport viewport, bool placedObjectsEmpty, ICoordinateHelper coordinateHelper)
        {
            // Compute zoom factor using the same logic as the original AnnoCanvas handler
            var zoomFactor = ((Constants.ZoomSensitivitySliderMaximum + 1 - zoomSensitivityPercentage) * Constants.ZoomSensitivityCoefficient) + Constants.ZoomSensitivityMinimum;
            var change = (int)(delta / zoomFactor);
            if (change == 0)
            {
                change = delta > 0 ? 1 : -1;
            }

            var newGridSize = currentGridSize;
            double newLeft = viewport.Left;
            double newTop = viewport.Top;

            if (!useZoomToPoint)
            {
                newGridSize += change;
            }
            else
            {
                var preZoomPosition = coordinateHelper.ScreenToFractionalGrid(mousePosition, currentGridSize);
                newGridSize += change;
                var postZoomPosition = coordinateHelper.ScreenToFractionalGrid(mousePosition, newGridSize);
                var diff = preZoomPosition - postZoomPosition;
                newLeft += diff.X;
                newTop += diff.Y;
            }

            if (placedObjectsEmpty)
            {
                newLeft = viewport.HorizontalAlignmentValue >= 0 ? 1 - viewport.HorizontalAlignmentValue : System.Math.Abs(viewport.HorizontalAlignmentValue);
                newTop = viewport.VerticalAlignmentValue >= 0 ? 1 - viewport.VerticalAlignmentValue : System.Math.Abs(viewport.VerticalAlignmentValue);
            }

            return new MouseWheelResult(newGridSize, newLeft, newTop);
        }

        public MouseDownDecision DecideOnMouseDown(bool leftPressed, bool rightPressed, Point mousePosition, bool currentModeWasDragSelection, int currentObjectsCount, Func<Point, LayoutObject> getObjectAt, Func<LayoutObject, bool> selectedContains, bool isControlPressed, bool isShiftPressed)
        {
            // Both mouse buttons pressed
            if (leftPressed && rightPressed)
            {
                if (currentModeWasDragSelection)
                {
                    // signal caller to register move operation and switch to DragAllStart
                    return new MouseDownDecision(MouseDownAction.DragAllStartAndRegisterMove, null);
                }

                return new MouseDownDecision(MouseDownAction.DragAllStartAndRegisterMove, null);
            }

            // Left button pressed and we have current objects -> place
            if (leftPressed && currentObjectsCount != 0)
            {
                return new MouseDownDecision(MouseDownAction.PlaceCurrentObjects, null);
            }

            // Left button pressed and no current objects -> selection/drag single
            if (leftPressed && currentObjectsCount == 0)
            {
                var obj = getObjectAt(mousePosition);
                if (obj is null)
                {
                    return new MouseDownDecision(MouseDownAction.SelectionRectStart, null);
                }

                // if modifier keys are pressed we don't change selection mode here
                if (!(isControlPressed || isShiftPressed))
                {
                    if (selectedContains(obj))
                    {
                        return new MouseDownDecision(MouseDownAction.DragSelectionStart, obj);
                    }

                    return new MouseDownDecision(MouseDownAction.DragSingleStart, obj);
                }
            }

            return new MouseDownDecision(MouseDownAction.None, null);
        }

        public void HandleDragStartCheck(ref MouseMode currentMode, Point mousePosition, Point mouseDragStart, HashSet<LayoutObject> selectedObjects, Func<Point, LayoutObject> getObjectAt, out List<(LayoutObject, Rect)> oldObjectPositions)
        {
            oldObjectPositions = null;
            if (Math.Abs(mouseDragStart.X - mousePosition.X) >= 1 || Math.Abs(mouseDragStart.Y - mousePosition.Y) >= 1)
            {
                switch (currentMode)
                {
                    case MouseMode.SelectionRectStart:
                        currentMode = MouseMode.SelectionRect;
                        break;
                    case MouseMode.DragSelectionStart:
                        currentMode = MouseMode.DragSelection;
                        oldObjectPositions = selectedObjects.Select(obj => (obj, obj.GridRect)).ToList();
                        break;
                    case MouseMode.DragSingleStart:
                        selectedObjects.Clear();
                        var obj = getObjectAt(mouseDragStart);
                        if (obj != null)
                        {
                            selectedObjects.Add(obj);
                            oldObjectPositions = new List<(LayoutObject, Rect)> { (obj, obj.GridRect) };
                        }
                        currentMode = MouseMode.DragSelection;
                        break;
                    case MouseMode.DragAllStart:
                        currentMode = MouseMode.DragAll;
                        break;
                }
            }
        }

        public void HandleDragAll(Point mousePosition, ref Point mouseDragStart, int gridSize, Viewport viewport, ICoordinateHelper coordinateHelper, IAppSettings appSettings, out bool invalidateScroll)
        {
            var dx = (int)coordinateHelper.ScreenToGrid(mousePosition.X - mouseDragStart.X, gridSize);
            var dy = (int)coordinateHelper.ScreenToGrid(mousePosition.Y - mouseDragStart.Y, gridSize);

            if (appSettings.InvertPanningDirection)
            {
                viewport.Left -= dx;
                viewport.Top -= dy;
            }
            else
            {
                viewport.Left += dx;
                viewport.Top += dy;
            }

            mouseDragStart.X += coordinateHelper.GridToScreen(dx, gridSize);
            mouseDragStart.Y += coordinateHelper.GridToScreen(dy, gridSize);

            invalidateScroll = true;
        }

        public void HandleSelectionRect(Point mousePosition, Point mouseDragStart, int gridSize, Viewport viewport, QuadTree<LayoutObject> placedObjects, HashSet<LayoutObject> selectedObjects, ICoordinateHelper coordinateHelper, bool isControlPressed, bool isShiftPressed, bool shouldAffectObjectsWithIdentifier, out bool statisticsUpdated)
        {
            statisticsUpdated = false;
            if (isControlPressed || isShiftPressed)
            {
                // remove previously selected by the selection rect
                if (shouldAffectObjectsWithIdentifier)
                {
                    selectedObjects.RemoveWhere(_ => _.CalculateScreenRect(gridSize).IntersectsWith(new Rect(mouseDragStart, mousePosition)));
                }
                else
                {
                    selectedObjects.ExceptWith(selectedObjects.Where(x => x.CalculateScreenRect(gridSize).IntersectsWith(new Rect(mouseDragStart, mousePosition))));
                }
            }
            else
            {
                selectedObjects.Clear();
            }

            // adjust rect
            var selectionRect = new Rect(mouseDragStart, mousePosition);
            // select intersecting objects
            var selectionRectGrid = coordinateHelper.ScreenToGrid(selectionRect, gridSize);
            selectionRectGrid = viewport.OriginToViewport(selectionRectGrid);
            selectedObjects.UnionWith(placedObjects.GetItemsIntersecting(selectionRectGrid));
            statisticsUpdated = true;
        }

        public void HandleDragSelection(Point mousePosition, ref Point mouseDragStart, int gridSize, ref List<(LayoutObject Item, Rect OldGridRect)> oldObjectPositions, ref Rect collisionRect, HashSet<LayoutObject> selectedObjects, QuadTree<LayoutObject> placedObjects, ICoordinateHelper coordinateHelper, out bool invalidateScroll, out bool statisticsUpdated, out bool forceRendering)
        {
            invalidateScroll = false;
            statisticsUpdated = false;
            forceRendering = false;

            if (oldObjectPositions.Count == 0)
            {
                oldObjectPositions.AddRange(selectedObjects.Select(obj => (obj, obj.GridRect)));
            }

            // move all selected objects
            var dx = (int)coordinateHelper.ScreenToGrid(mousePosition.X - mouseDragStart.X, gridSize);
            var dy = (int)coordinateHelper.ScreenToGrid(mousePosition.Y - mouseDragStart.Y, gridSize);

            // check if the mouse has moved at least one grid cell in any direction
            if (dx == 0 && dy == 0)
            {
                return;
            }

            // Recompute unselectedObjects
            var offsetCollisionRect = collisionRect;
            offsetCollisionRect.Offset(dx, dy);
            var unselectedObjects = placedObjects.GetItemsIntersecting(offsetCollisionRect).Where(_ => !selectedObjects.Contains(_)).ToList();

            var collisionsExist = false;
            // temporarily move each object and check if collisions with unselected objects exist
            foreach (var curLayoutObject in selectedObjects)
            {
                var originalPosition = curLayoutObject.Position;
                // move object                                
                curLayoutObject.Position = new Point(curLayoutObject.Position.X + dx, curLayoutObject.Position.Y + dy);
                // check for collisions                                
                var collides = unselectedObjects.Any(_ => ObjectIntersectionExists(curLayoutObject, _));
                curLayoutObject.Position = originalPosition;
                if (collides)
                {
                    collisionsExist = true;
                    break;
                }
            }

            // if no collisions were found, permanently move all selected objects
            if (!collisionsExist)
            {
                foreach (var curLayoutObject in selectedObjects)
                {
                    curLayoutObject.Position = new Point(curLayoutObject.Position.X + dx, curLayoutObject.Position.Y + dy);
                }
                // adjust the drag start to compensate the amount we already moved
                mouseDragStart.X += coordinateHelper.GridToScreen(dx, gridSize);
                mouseDragStart.Y += coordinateHelper.GridToScreen(dy, gridSize);

                // update collision rect, so that collisions are correctly computed on next run
                collisionRect.X += dx;
                collisionRect.Y += dy;

                statisticsUpdated = true;
                invalidateScroll = true; // bounds may change
            }

            forceRendering = true;
        }

        public void HandleMouseUpDragSelection(List<(LayoutObject Item, Rect OldGridRect)> oldObjectPositions, HashSet<LayoutObject> selectedObjects, bool isRightButton, out bool registerUndo, out bool reindex, out bool clearSelection)
        {
            registerUndo = true;
            reindex = true;
            clearSelection = isRightButton || selectedObjects.Count == 1;
        }

        public MouseEnterDecision HandleMouseEnter()
        {
            return new MouseEnterDecision(MouseEnterAction.SetMouseWithinControl);
        }

        public MouseLeaveDecision HandleMouseLeave(MouseMode currentMode)
        {
            // Always clear mouse within control, selection rect, and reindex moved objects
            return new MouseLeaveDecision(new[]
            {
                MouseLeaveAction.ClearMouseWithinControl,
                MouseLeaveAction.ClearSelectionRect,
                MouseLeaveAction.ReindexMovedObjects
            });
        }

        public MouseMoveDecision HandleMouseMove(Point mousePosition, Point mouseDragStart, MouseMode currentMode, MouseButtonState leftButtonState, int currentObjectsCount, bool isControlPressed, bool isShiftPressed)
        {
            // Check if user begins to drag (movement threshold)
            var hasMoved = Math.Abs(mouseDragStart.X - mousePosition.X) >= 1 || Math.Abs(mouseDragStart.Y - mousePosition.Y) >= 1;

            if (hasMoved)
            {
                switch (currentMode)
                {
                    case MouseMode.SelectionRectStart:
                        return new MouseMoveDecision(MouseMoveAction.TransitionToSelectionRect);
                    case MouseMode.DragSelectionStart:
                        return new MouseMoveDecision(MouseMoveAction.TransitionToDragSelection);
                    case MouseMode.DragSingleStart:
                        return new MouseMoveDecision(MouseMoveAction.TransitionToDragSingleObject);
                    case MouseMode.DragAllStart:
                        return new MouseMoveDecision(MouseMoveAction.TransitionToDragAll);
                }
            }

            // Handle active dragging modes
            if (currentMode == MouseMode.DragAll)
            {
                return new MouseMoveDecision(MouseMoveAction.DragAllViewport);
            }
            else if (leftButtonState == MouseButtonState.Pressed)
            {
                if (currentObjectsCount != 0)
                {
                    return new MouseMoveDecision(MouseMoveAction.PlaceObjectsContinuous);
                }
                else
                {
                    switch (currentMode)
                    {
                        case MouseMode.SelectionRect:
                            return new MouseMoveDecision(MouseMoveAction.UpdateSelectionRect);
                        case MouseMode.DragSelection:
                            return new MouseMoveDecision(MouseMoveAction.DragSelectedObjects);
                    }
                }
            }

            return new MouseMoveDecision(MouseMoveAction.None);
        }

        public MouseUpDecision HandleMouseUp(MouseButton changedButton, MouseButtonState leftButtonState, MouseButtonState rightButtonState, MouseMode currentMode, int currentObjectsCount, Point mousePosition, bool isControlPressed, bool isShiftPressed, Func<Point, LayoutObject> getObjectAt, Func<LayoutObject, bool> selectedContains)
        {
            // Handle DragAll mode
            if (currentMode == MouseMode.DragAll)
            {
                if (leftButtonState == MouseButtonState.Released && rightButtonState == MouseButtonState.Released)
                {
                    return new MouseUpDecision(new[] { MouseUpAction.EndDragAll });
                }
                return new MouseUpDecision(new[] { MouseUpAction.None });
            }

            // Handle left button release
            if (changedButton == MouseButton.Left && currentObjectsCount == 0)
            {
                switch (currentMode)
                {
                    case MouseMode.SelectSameIdentifier:
                        return new MouseUpDecision(new[] { MouseUpAction.TransitionToStandard });

                    case MouseMode.SelectionRect:
                        return new MouseUpDecision(new[] { MouseUpAction.EndSelectionRect });

                    case MouseMode.DragSelection:
                        return new MouseUpDecision(new[] { MouseUpAction.EndDragSelection }, false); // false = not right button

                    default:
                        // Standard click - toggle object selection
                        var obj = getObjectAt(mousePosition);
                        if (obj != null)
                        {
                            return new MouseUpDecision(new[] { MouseUpAction.ToggleObjectSelection }, obj);
                        }
                        else if (!(isControlPressed || isShiftPressed))
                        {
                            return new MouseUpDecision(new[] { MouseUpAction.ClearSelection, MouseUpAction.TransitionToStandard });
                        }
                        return new MouseUpDecision(new[] { MouseUpAction.TransitionToStandard });
                }
            }
            else if (changedButton == MouseButton.Left && currentObjectsCount != 0)
            {
                return new MouseUpDecision(new[] { MouseUpAction.TransitionToPlaceObjects });
            }
            else if (changedButton == MouseButton.Right)
            {
                switch (currentMode)
                {
                    case MouseMode.PlaceObjects:
                    case MouseMode.DeleteObject:
                    case MouseMode.Standard:
                        if (currentObjectsCount != 0)
                        {
                            return new MouseUpDecision(new[] { MouseUpAction.CancelPlacement });
                        }
                        return new MouseUpDecision(new[] { MouseUpAction.TransitionToStandard });

                    case MouseMode.DragSelection:
                        return new MouseUpDecision(new[] { MouseUpAction.EndDragSelection, MouseUpAction.CancelPlacement }, true); // true = right button

                    case MouseMode.SelectSameIdentifier:
                        return new MouseUpDecision(new[] { MouseUpAction.TransitionToStandard });
                }
            }

            return new MouseUpDecision(new[] { MouseUpAction.None });
        }

        public KeyDownDecision HandleKeyDown()
        {
            // Keyboard handling is delegated to HotkeyCommandManager
            // This service just signals that it was handled
            return new KeyDownDecision(KeyDownAction.Handled);
        }

        private static bool ObjectIntersectionExists(LayoutObject a, LayoutObject b)
        {
            return a.CollisionRect.IntersectsWith(b.CollisionRect);
        }
    }
}
