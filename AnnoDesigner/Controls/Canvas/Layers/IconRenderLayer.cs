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
        private readonly Func<int> _getGridSize;
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
            Func<int> getGridSize,
            Dictionary<string, BitmapImage> iconLookup,
            int order = 350)
            : base("Icons", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
            _getGridSize = getGridSize ?? throw new ArgumentNullException(nameof(getGridSize));
            _iconLookup = iconLookup ?? throw new ArgumentNullException(nameof(iconLookup));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            var gridSize = _getGridSize();

            foreach (var obj in objects)
            {
                var iconRect = obj.GetIconRect(gridSize);
                if (iconRect.IsEmpty) continue;
                if (!clip.IntersectsWith(iconRect)) continue;

                var iconName = obj.WrappedAnnoObject?.Icon;
                var identifier = obj.Identifier;
                if (!string.IsNullOrEmpty(iconName) && _iconLookup.TryGetValue(iconName, out var icon))
                {
                    dc.DrawImage(icon, iconRect);
                }
                else if (identifier != null && _iconLookup.TryGetValue(identifier, out var iconByIdent))
                {
                    dc.DrawImage(iconByIdent, iconRect);
                }
                else
                {
                    dc.DrawRectangle(PlaceholderBrush, PlaceholderPen, iconRect);
                }
            }
        }
    }
}
