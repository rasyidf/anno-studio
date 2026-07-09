using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders blocked harbor/quay areas as hatched red overlays.
    /// Supports Direction (Up/Down/Left/Right) to position the blocked rect
    /// relative to the parent object.
    /// Order 260 → renders in world space (below objects at 300, above influence at 250).
    /// </summary>
    public class BlockedAreaRenderLayer : RenderLayerBase
    {
        private static readonly Brush BlockedFill;
        private static readonly Pen BlockedBorderPen;
        private static readonly Brush HatchBrush;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;

        static BlockedAreaRenderLayer()
        {
            BlockedFill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));
            BlockedFill.Freeze();

            BlockedBorderPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 180, 0, 0)), 1);
            BlockedBorderPen.Freeze();

            // Hatched pattern: diagonal lines in a DrawingBrush tile
            HatchBrush = CreateHatchBrush();
        }

        private static Brush CreateHatchBrush()
        {
            const double tileSize = 8.0;
            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(100, 200, 0, 0)), 1);
            linePen.Freeze();

            var drawing = new DrawingGroup();
            // Background fill
            var bgBrush = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0));
            bgBrush.Freeze();
            drawing.Children.Add(new GeometryDrawing(bgBrush, null, new RectangleGeometry(new Rect(0, 0, tileSize, tileSize))));

            // Diagonal line from bottom-left to top-right
            var lineGeo = new LineGeometry(new Point(0, tileSize), new Point(tileSize, 0));
            lineGeo.Freeze();
            drawing.Children.Add(new GeometryDrawing(null, linePen, lineGeo));

            drawing.Freeze();

            var brush = new DrawingBrush(drawing)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, tileSize, tileSize),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, tileSize, tileSize),
                ViewboxUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            return brush;
        }

        public BlockedAreaRenderLayer(Func<IEnumerable<LayoutObject>> getObjects, int order = 260)
            : base("BlockedArea", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            var gridSpacing = canvas.Preferences.GridSpacing;

            foreach (var obj in objects)
            {
                var annoObj = obj.WrappedAnnoObject;
                if (annoObj.BlockedAreaLength <= 0) continue;

                var gridRect = obj.GridRect;
                var blockedLength = annoObj.BlockedAreaLength;
                var blockedWidth = obj.BlockedAreaWidth;

                // Compute blocked rect in grid units based on Direction
                Rect blockedGrid;
                switch (annoObj.Direction)
                {
                    case GridDirection.Up:
                        blockedGrid = new Rect(
                            gridRect.X + (gridRect.Width - blockedWidth) / 2.0,
                            gridRect.Y - blockedLength,
                            blockedWidth,
                            blockedLength);
                        break;

                    case GridDirection.Right:
                        blockedGrid = new Rect(
                            gridRect.Right,
                            gridRect.Y + (gridRect.Height - blockedWidth) / 2.0,
                            blockedLength,
                            blockedWidth);
                        break;

                    case GridDirection.Left:
                        blockedGrid = new Rect(
                            gridRect.X - blockedLength,
                            gridRect.Y + (gridRect.Height - blockedWidth) / 2.0,
                            blockedLength,
                            blockedWidth);
                        break;

                    default: // GridDirection.Down
                        blockedGrid = new Rect(
                            gridRect.X + (gridRect.Width - blockedWidth) / 2.0,
                            gridRect.Bottom,
                            blockedWidth,
                            blockedLength);
                        break;
                }

                // Convert to world coordinates
                var worldRect = new Rect(
                    blockedGrid.X * gridSpacing,
                    blockedGrid.Y * gridSpacing,
                    blockedGrid.Width * gridSpacing,
                    blockedGrid.Height * gridSpacing);

                // Draw hatched fill + border
                dc.DrawRectangle(HatchBrush, BlockedBorderPen, worldRect);
            }
        }
    }
}
