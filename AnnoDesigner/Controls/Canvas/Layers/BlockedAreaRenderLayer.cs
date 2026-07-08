using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders blocked harbor areas as semi-transparent red overlays
    /// for objects with BlockedAreaLength > 0.
    /// </summary>
    public class BlockedAreaRenderLayer : RenderLayerBase
    {
        private static readonly Brush BlockedBrush;
        private static readonly Pen BlockedPen;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;

        static BlockedAreaRenderLayer()
        {
            BlockedBrush = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0));
            BlockedBrush.Freeze();
            BlockedPen = new Pen(Brushes.DarkRed, 1);
            BlockedPen.Freeze();
        }

        public BlockedAreaRenderLayer(Func<IEnumerable<LayoutObject>> getObjects, int order = 550)
            : base("BlockedArea", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            foreach (var obj in objects)
            {
                if (obj.WrappedAnnoObject.BlockedAreaLength <= 0) continue;

                // ponytail: draws blocked rect below the object using grid coords.
                // Upgrade path: use ICoordinateHelper.GridToScreen for proper screen-space rendering.
                var gridRect = obj.GridRect;
                var blockedLength = obj.WrappedAnnoObject.BlockedAreaLength;

                var blockedRect = new Rect(
                    gridRect.X,
                    gridRect.Bottom,
                    gridRect.Width,
                    blockedLength);

                dc.DrawRectangle(BlockedBrush, BlockedPen, blockedRect);
            }
        }
    }
}
