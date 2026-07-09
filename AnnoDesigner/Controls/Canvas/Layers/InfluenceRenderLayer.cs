using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders influence circles for objects that have InfluenceRange or Radius > 0.
    /// Order 250 → renders in world space (below objects at 300).
    /// Coordinates are grid units × GridSpacing.
    /// </summary>
    public class InfluenceRenderLayer : RenderLayerBase
    {
        private static readonly Brush InfluenceRangeFill;
        private static readonly Pen InfluenceRangePen;
        private static readonly Brush RadiusFill;
        private static readonly Pen RadiusPen;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;

        static InfluenceRenderLayer()
        {
            // Semi-transparent blue for InfluenceRange
            InfluenceRangeFill = new SolidColorBrush(Color.FromArgb(40, 30, 80, 220));
            InfluenceRangeFill.Freeze();
            InfluenceRangePen = new Pen(new SolidColorBrush(Color.FromArgb(120, 30, 80, 220)), 1.5);
            InfluenceRangePen.Freeze();

            // Semi-transparent green for Radius
            RadiusFill = new SolidColorBrush(Color.FromArgb(40, 30, 180, 60));
            RadiusFill.Freeze();
            RadiusPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 30, 180, 60)), 1.5);
            RadiusPen.Freeze();
        }

        public InfluenceRenderLayer(Func<IEnumerable<LayoutObject>> getObjects, int order = 250)
            : base("Influence", order)
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
                var gridRect = obj.GridRect;

                // Center of the object in world coordinates
                var centerX = (gridRect.X + gridRect.Width / 2.0) * gridSpacing;
                var centerY = (gridRect.Y + gridRect.Height / 2.0) * gridSpacing;
                var center = new Point(centerX, centerY);

                // Draw InfluenceRange circle (blue) — range measured from building edge
                if (annoObj.InfluenceRange > 0)
                {
                    // Radius in world units: half the building diagonal extent + range
                    // Anno convention: influence range extends from each edge, so effective circle
                    // radius from center = max(width, height)/2 + InfluenceRange
                    var effectiveRadius = (Math.Max(gridRect.Width, gridRect.Height) / 2.0 + annoObj.InfluenceRange) * gridSpacing;

                    dc.DrawEllipse(InfluenceRangeFill, InfluenceRangePen, center, effectiveRadius, effectiveRadius);
                }

                // Draw Radius circle (green) — pure radius from center
                if (annoObj.Radius > 0)
                {
                    var radiusWorld = annoObj.Radius * gridSpacing;

                    dc.DrawEllipse(RadiusFill, RadiusPen, center, radiusWorld, radiusWorld);
                }

                // TODO ponytail: Phase 3 — render true influence polygon when road connectivity
                // data is available via RoadSearchHelper. The polygon would show actual reachable
                // tiles via road network flood-fill rather than a simple circle approximation.
                // Upgrade path: accept an optional Func<LayoutObject, Geometry?> that returns
                // the flood-fill polygon, then dc.DrawGeometry(fill, pen, polygon) here.
            }
        }
    }
}
