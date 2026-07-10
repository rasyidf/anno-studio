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
        private static readonly Brush PolygonFill;
        private static readonly Pen PolygonPen;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;
        private readonly Func<LayoutObject, IEnumerable<Point>> _getInfluencePolygonPoints;

        // ponytail: cache polygon geometry per object to avoid recomputing every frame.
        // Ceiling: unbounded cache — if layout changes frequently, add invalidation via a version stamp.
        private readonly Dictionary<LayoutObject, StreamGeometry> _polygonCache = new();

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

            // Semi-transparent green for road-based polygon
            PolygonFill = new SolidColorBrush(Color.FromArgb(50, 30, 200, 60));
            PolygonFill.Freeze();
            PolygonPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 30, 200, 60)), 1.5);
            PolygonPen.Freeze();
        }

        public InfluenceRenderLayer(
            Func<IEnumerable<LayoutObject>> getObjects,
            int order = 250,
            Func<LayoutObject, IEnumerable<Point>> getInfluencePolygonPoints = null)
            : base("Influence", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
            _getInfluencePolygonPoints = getInfluencePolygonPoints;
        }

        /// <summary>
        /// Invalidates cached polygon geometry. Call when the layout changes (objects added/removed/moved).
        /// </summary>
        public void InvalidatePolygonCache() => _polygonCache.Clear();

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

                // Try road-based polygon rendering first (only for objects with PavedStreet or influence > 0)
                if (_getInfluencePolygonPoints != null && annoObj.InfluenceRange > 0)
                {
                    if (TryDrawPolygon(dc, obj, gridSpacing))
                        goto skipCircle; // polygon drawn, skip the circle fallback for InfluenceRange
                }

                // Draw InfluenceRange circle (blue) — range measured from building edge
                if (annoObj.InfluenceRange > 0)
                {
                    var effectiveRadius = (Math.Max(gridRect.Width, gridRect.Height) / 2.0 + annoObj.InfluenceRange) * gridSpacing;
                    dc.DrawEllipse(InfluenceRangeFill, InfluenceRangePen, center, effectiveRadius, effectiveRadius);
                }

                skipCircle:

                // Draw Radius circle (green) — pure radius from center
                if (annoObj.Radius > 0)
                {
                    var radiusWorld = annoObj.Radius * gridSpacing;
                    dc.DrawEllipse(RadiusFill, RadiusPen, center, radiusWorld, radiusWorld);
                }
            }
        }

        private bool TryDrawPolygon(DrawingContext dc, LayoutObject obj, double gridSpacing)
        {
            if (!_polygonCache.TryGetValue(obj, out var geometry))
            {
                var points = _getInfluencePolygonPoints(obj);
                if (points == null)
                    return false;

                geometry = BuildPolygonGeometry(points, gridSpacing);
                if (geometry == null)
                    return false;

                _polygonCache[obj] = geometry;
            }

            dc.DrawGeometry(PolygonFill, PolygonPen, geometry);
            return true;
        }

        private static StreamGeometry BuildPolygonGeometry(IEnumerable<Point> points, double gridSpacing)
        {
            var sg = new StreamGeometry();
            using (var ctx = sg.Open())
            {
                var first = true;
                foreach (var pt in points)
                {
                    var worldPt = new Point(pt.X * gridSpacing, pt.Y * gridSpacing);
                    if (first)
                    {
                        ctx.BeginFigure(worldPt, isFilled: true, isClosed: true);
                        first = false;
                    }
                    else
                    {
                        ctx.LineTo(worldPt, isStroked: true, isSmoothJoin: true);
                    }
                }

                if (first) return null; // no points
            }

            if (sg.CanFreeze) sg.Freeze();
            return sg;
        }
    }
}
