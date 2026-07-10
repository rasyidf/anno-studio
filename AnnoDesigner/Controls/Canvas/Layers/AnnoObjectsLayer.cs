using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders placed LayoutObjects with their color, border, and label.
    /// Replaces the generic ObjectsLayer for Anno-specific rendering.
    /// </summary>
    public class AnnoObjectsLayer : RenderLayerBase
    {
        private static readonly Pen BorderPen;
        private static readonly Typeface LabelTypeface;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;
        private readonly Func<int> _getGridSize;

        static AnnoObjectsLayer()
        {
            BorderPen = new Pen(Brushes.Black, 1);
            BorderPen.Freeze();
            LabelTypeface = new Typeface("Segoe UI");
        }

        public AnnoObjectsLayer(
            Func<IEnumerable<LayoutObject>> getObjects,
            Func<int> getGridSize,
            int order = 300)
            : base("AnnoObjects", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
            _getGridSize = getGridSize ?? throw new ArgumentNullException(nameof(getGridSize));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            var gridSize = _getGridSize();

            foreach (var obj in objects)
            {
                var screenRect = obj.CalculateScreenRect(gridSize);
                if (!clip.IntersectsWith(screenRect)) continue;

                // Fill with the object's cached render brush
                dc.DrawRectangle(obj.RenderBrush, BorderPen, screenRect);

                // ponytail: Icon rendering is a future enhancement.
                // Upgrade path: draw obj.Icon centered in screenRect when available.

                // Draw label if Identifier is not empty
                var identifier = obj.Identifier;
                if (!string.IsNullOrEmpty(identifier))
                {
                    var pixelsPerDip = VisualTreeHelper.GetDpi(canvas).PixelsPerDip;
                    var text = obj.GetFormattedText(
                        TextAlignment.Center,
                        CultureInfo.CurrentCulture,
                        LabelTypeface,
                        pixelsPerDip,
                        screenRect.Width,
                        screenRect.Height);

                    var textOrigin = new Point(
                        screenRect.X,
                        screenRect.Y + (screenRect.Height - text.Height) / 2);

                    dc.DrawText(text, textOrigin);
                }
            }
        }
    }
}
