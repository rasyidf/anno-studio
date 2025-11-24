using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class TransformService : ITransformService
    {
        private readonly ICoordinateHelper _coordinateHelper;

        public TransformService(ICoordinateHelper coordinateHelper)
        {
            _coordinateHelper = coordinateHelper ?? throw new System.ArgumentNullException(nameof(coordinateHelper));
        }

        public TransformService()
        {
            _coordinateHelper = null;
        }

        public double Zoom { get; set; } = 1.0;

        public Point ScreenToCanvas(Point screenPoint)
        {
            // Convert a point from screen coordinates to canvas coordinates using current zoom.
            // When zoom > 1, screen coordinates are scaled up; to map back to canvas space divide by zoom.
            return new Point(screenPoint.X / Zoom, screenPoint.Y / Zoom);
        }

        public Point CanvasToScreen(Point canvasPoint)
        {
            // Convert a point from canvas coordinates to screen coordinates using current zoom.
            return new Point(canvasPoint.X * Zoom, canvasPoint.Y * Zoom);
        }

        /// <summary>
        /// Rotates a group of objects 90 degrees clockwise around point (0, 0).
        /// </summary>
        /// <param name="objects">The objects to rotate.</param>
        /// <returns>Lazily evaluated iterator which rotates each object and returns tuple of the rotated item and its old rectangle.</returns>
        public IEnumerable<(LayoutObject item, Rect oldRect)> Rotate(IEnumerable<LayoutObject> objects)
        {
            foreach (var item in objects)
            {
                var newRect = _coordinateHelper.Rotate(item.Bounds);
                var oldRect = item.Bounds;
                item.Bounds = newRect;
                item.Direction = _coordinateHelper.Rotate(item.Direction);
                yield return (item, oldRect);
            }
        }

        /// <summary>
        /// Converts a point from screen coordinates to grid coordinates.
        /// </summary>
        public Point ScreenToGrid(Point screenPoint, int gridSize)
        {
            return _coordinateHelper.ScreenToGrid(screenPoint, gridSize);
        }

        /// <summary>
        /// Converts a point from grid coordinates to screen coordinates.
        /// </summary>
        public Point GridToScreen(Point gridPoint, int gridSize)
        {
            return _coordinateHelper.GridToScreen(gridPoint, gridSize);
        }

        /// <summary>
        /// Converts a rectangle from screen coordinates to grid coordinates.
        /// </summary>
        public Rect ScreenToGrid(Rect screenRect, int gridSize)
        {
            return _coordinateHelper.ScreenToGrid(screenRect, gridSize);
        }

        /// <summary>
        /// Converts a rectangle from grid coordinates to screen coordinates.
        /// </summary>
        public Rect GridToScreen(Rect gridRect, int gridSize)
        {
            return _coordinateHelper.GridToScreen(gridRect, gridSize);
        }
    }
}
