using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Road placement tool: click to add angle-constrained road points, double-click or Enter to commit.
    /// Snaps to 45° increments and existing road endpoints.
    /// </summary>
    public class RoadPlacementTool : ITool
    {
        public string Name => "RoadPlacement";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private readonly List<Point> _points = new();
        private Point? _currentSnapped;
        private Point? _activeSnapTarget;

        // ponytail: 8 cardinal/diagonal angles, precomputed as unit vectors for snapping
        private static readonly double[] AllowedAnglesRad =
        {
            0, Math.PI / 4, Math.PI / 2, 3 * Math.PI / 4,
            Math.PI, 5 * Math.PI / 4, 3 * Math.PI / 2, 7 * Math.PI / 4
        };

        private const double EndpointSnapRadius = 0.5;

        /// <summary>
        /// Road width in grid units. Controls preview line thickness and committed road width.
        /// </summary>
        public double RoadWidth { get; set; } = 1.0;

        public RoadPlacementTool(
            Content.IObjectManager<CanvasObject> objectManager,
            IInputElement owner,
            Action<IEnumerable<CanvasObject>> setSelection,
            Action invalidate,
            Action? afterCommit = null)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            _afterCommit = afterCommit ?? (() => { });
        }

        private EditorCanvas? Canvas => _owner as EditorCanvas;

        private Point ToWorld(Point screenPoint) => Canvas?.ScreenToWorld(screenPoint) ?? screenPoint;

        public void Activate() => Reset();

        public void Deactivate() => Reset();

        public void OnCancel()
        {
            Reset();
            _invalidate();
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left) return;

            var rawWorld = ToWorld(e.GetPosition(_owner));
            var pt = ComputeSnappedPoint(rawWorld);

            // Double-click commits
            if (e.ClickCount >= 2)
            {
                // ponytail: don't add duplicate point on double-click, just commit
                Commit();
                return;
            }

            _points.Add(pt);
            _invalidate();
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;

            var rawWorld = ToWorld(e.GetPosition(_owner));
            _currentSnapped = ComputeSnappedPoint(rawWorld);

            if (_points.Count > 0)
                _invalidate();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            // no-op: placement is click-based
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e == null) return;

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                Commit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                OnCancel();
                e.Handled = true;
            }
        }

        public void OnKeyUp(KeyEventArgs e)
        {
            // no-op
        }

        public void Render(DrawingContext dc)
        {
            if (_points.Count == 0 && !_currentSnapped.HasValue) return;

            var previewBrush = new SolidColorBrush(Color.FromArgb(140, 255, 180, 0));
            previewBrush.Freeze();
            var previewPen = new Pen(previewBrush, RoadWidth);
            previewPen.Freeze();

            var dashBrush = new SolidColorBrush(Color.FromArgb(100, 255, 180, 0));
            dashBrush.Freeze();
            var dashPen = new Pen(dashBrush, RoadWidth) { DashStyle = DashStyles.Dash };
            dashPen.Freeze();

            var snapDotBrush = new SolidColorBrush(Color.FromArgb(200, 0, 255, 120));
            snapDotBrush.Freeze();

            // Draw solid segments between committed points
            for (int i = 0; i < _points.Count - 1; i++)
            {
                dc.DrawLine(previewPen, _points[i], _points[i + 1]);
            }

            // Draw dots at committed points
            foreach (var pt in _points)
            {
                dc.DrawEllipse(previewBrush, null, pt, RoadWidth * 0.6, RoadWidth * 0.6);
            }

            // Dashed line from last committed point to current snapped cursor
            if (_points.Count > 0 && _currentSnapped.HasValue)
            {
                dc.DrawLine(dashPen, _points[^1], _currentSnapped.Value);
            }

            // Show snap target indicator (green dot at nearby road endpoint)
            if (_activeSnapTarget.HasValue)
            {
                dc.DrawEllipse(snapDotBrush, null, _activeSnapTarget.Value, RoadWidth * 0.8, RoadWidth * 0.8);
            }

            // Draw dots at existing road endpoints that are within visual range as potential snap targets
            DrawNearbyEndpointHints(dc, snapDotBrush);
        }

        /// <summary>
        /// Computes the final snapped point, applying endpoint snapping first, then angle constraint.
        /// </summary>
        private Point ComputeSnappedPoint(Point rawWorld)
        {
            _activeSnapTarget = null;

            // 1. Try to snap to an existing road endpoint
            var endpointSnap = FindNearestRoadEndpoint(rawWorld);
            if (endpointSnap.HasValue)
            {
                _activeSnapTarget = endpointSnap.Value;
                return endpointSnap.Value;
            }

            // 2. If we have previous points, constrain to allowed angles
            if (_points.Count > 0)
            {
                return ConstrainToAngle(_points[^1], rawWorld);
            }

            // 3. First point — no constraint
            return rawWorld;
        }

        /// <summary>
        /// Constrains <paramref name="target"/> to the nearest 45° angle from <paramref name="origin"/>.
        /// </summary>
        private static Point ConstrainToAngle(Point origin, Point target)
        {
            var dx = target.X - origin.X;
            var dy = target.Y - origin.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001) return origin;

            var angle = Math.Atan2(dy, dx);
            // Normalize to [0, 2π)
            if (angle < 0) angle += 2 * Math.PI;

            // Find nearest allowed angle
            double bestAngle = 0;
            double bestDiff = double.MaxValue;
            foreach (var allowed in AllowedAnglesRad)
            {
                var diff = AngleDiff(angle, allowed);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestAngle = allowed;
                }
            }

            return new Point(
                origin.X + distance * Math.Cos(bestAngle),
                origin.Y + distance * Math.Sin(bestAngle));
        }

        /// <summary>
        /// Shortest angular distance between two angles in radians.
        /// </summary>
        private static double AngleDiff(double a, double b)
        {
            var diff = Math.Abs(a - b) % (2 * Math.PI);
            return diff > Math.PI ? 2 * Math.PI - diff : diff;
        }

        /// <summary>
        /// Finds the nearest road endpoint within <see cref="EndpointSnapRadius"/> of <paramref name="point"/>.
        /// </summary>
        private Point? FindNearestRoadEndpoint(Point point)
        {
            double bestDist = EndpointSnapRadius;
            Point? best = null;

            foreach (var obj in _objectManager.GetAll())
            {
                if (!obj.IsRoad || obj.PathPoints == null || obj.PathPoints.Count < 2)
                    continue;

                // Check first and last points of each road object
                var first = obj.PathPoints[0];
                var last = obj.PathPoints[^1];

                var d1 = Distance(point, first);
                if (d1 < bestDist)
                {
                    bestDist = d1;
                    best = first;
                }

                var d2 = Distance(point, last);
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = last;
                }
            }

            return best;
        }

        /// <summary>
        /// Draws small dots at nearby road endpoints to hint snap targets.
        /// </summary>
        private void DrawNearbyEndpointHints(DrawingContext dc, Brush brush)
        {
            // ponytail: only scan if we have a cursor position to be near
            if (!_currentSnapped.HasValue) return;

            const double hintRadius = 3.0; // world units visual range for hints
            var center = _currentSnapped.Value;

            foreach (var obj in _objectManager.GetAll())
            {
                if (!obj.IsRoad || obj.PathPoints == null || obj.PathPoints.Count < 2)
                    continue;

                var first = obj.PathPoints[0];
                var last = obj.PathPoints[^1];

                if (Distance(center, first) < hintRadius)
                    dc.DrawEllipse(null, new Pen(brush, 0.5), first, 0.3, 0.3);

                if (Distance(center, last) < hintRadius)
                    dc.DrawEllipse(null, new Pen(brush, 0.5), last, 0.3, 0.3);
            }
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void Commit()
        {
            if (_points.Count < 2)
            {
                Reset();
                _invalidate();
                return;
            }

            double minX = _points.Min(p => p.X);
            double maxX = _points.Max(p => p.X);
            double minY = _points.Min(p => p.Y);
            double maxY = _points.Max(p => p.Y);
            var bounds = new Rect(new Point(minX, minY), new Point(maxX, maxY));
            // ponytail: inflate zero-area bounds so the object is always hittable
            if (bounds.Width < 1) bounds.Inflate(1, 0);
            if (bounds.Height < 1) bounds.Inflate(0, 1);

            var obj = new CanvasObject
            {
                Bounds = bounds,
                ShapeType = "Multiline",
                PathPoints = new List<Point>(_points),
                IsRoad = true,
                IsBorderless = true,
                Identifier = "Road",
                IsSelectable = true
            };

            if (Canvas != null) Canvas.AddObjectWithUndo(obj);
            else _objectManager.Add(obj);

            _setSelection(new[] { obj });
            Reset();
            _invalidate();
            _afterCommit();
        }

        private void Reset()
        {
            _points.Clear();
            _currentSnapped = null;
            _activeSnapTarget = null;
        }
    }
}
