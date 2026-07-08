using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders building icons on top of placed objects.
    /// Falls back to a small placeholder rect when the icon is not found.
    /// </summary>
    public class IconRenderLayer : RenderLayerBase
    {
        private static readonly Brush PlaceholderBrush;
        private static readonly Pen PlaceholderPen;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;
        private readonly Dictionary<string, BitmapImage> _iconLookup;

        static IconRenderLayer()
        {
            PlaceholderBrush = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128));
            PlaceholderBrush.Freeze();
            PlaceholderPen = new Pen(Brushes.Gray, 1);
            PlaceholderPen.Freeze();
        }

        public IconRenderLayer(
            Func<IEnumerable<LayoutObject>> getObjects,
            Dictionary<string, BitmapImage> iconLookup,
            int order = 500)
            : base("Icons", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
            _iconLookup = iconLookup ?? throw new ArgumentNullException(nameof(iconLookup));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            foreach (var obj in objects)
            {
                var gridRect = obj.GridRect;
                if (gridRect.IsEmpty) continue;

                // ponytail: icon rect is inset from grid rect; no scaling to screen coords yet.
                // Upgrade path: use canvas transform/grid-size to compute screen rect.
                var iconRect = new Rect(
                    gridRect.X + gridRect.Width * 0.25,
                    gridRect.Y + gridRect.Height * 0.25,
                    gridRect.Width * 0.5,
                    gridRect.Height * 0.5);

                var identifier = obj.Identifier;
                if (identifier != null && _iconLookup.TryGetValue(identifier, out var icon))
                {
                    dc.DrawImage(icon, iconRect);
                }
                else
                {
                    dc.DrawRectangle(PlaceholderBrush, PlaceholderPen, iconRect);
                }
            }
        }
    }
}
