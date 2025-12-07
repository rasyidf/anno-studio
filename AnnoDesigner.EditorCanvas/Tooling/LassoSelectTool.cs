using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Freeform lasso selection tool using a polyline.
    /// </summary>
    public class LassoSelectTool : ITool
    {
        public string Name => "LassoSelect";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;

        private readonly List<Point> _points = new();
        private bool _isCapturing;

        public LassoSelectTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        public void Activate()
        {
            Reset();
        }

        public void Deactivate()
        {
            Reset();
        }

        public void OnCancel()
        {
            Reset();
            _invalidate();
        }

        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            _points.Clear();
            _points.Add(e.GetPosition(_owner));
            _isCapturing = true;
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (!_isCapturing || e == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var pt = e.GetPosition(_owner);
            if (_points.Count == 0 || DistanceSquared(_points[^1], pt) > 2)
            {
                _points.Add(pt);
                _invalidate();
            }
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isCapturing || e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            _isCapturing = false;
            if (_points.Count > 2)
            {
                var polygon = _points.ToArray();
                var hits = _objectManager.GetAll()?.Where(o => o != null && LassoHit(polygon, o.Bounds)).ToList() ?? new List<CanvasObject>();
                _setSelection(hits);
            }
            Reset();
            _invalidate();
        }

        public void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // no-op
        }

        public void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            // no-op
        }

        public void Render(System.Windows.Media.DrawingContext dc)
        {
            if (_points.Count < 2) return;
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.OrangeRed, 1)
            {
                DashStyle = System.Windows.Media.DashStyles.Dot
            };
            var geo = new System.Windows.Media.StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(_points[0], false, false);
                ctx.PolyLineTo(_points.Skip(1).ToList(), true, true);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }

        private bool LassoHit(IReadOnlyList<Point> polygon, Rect target)
        {
            var corners = new[]
            {
                new Point(target.Left, target.Top),
                new Point(target.Right, target.Top),
                new Point(target.Right, target.Bottom),
                new Point(target.Left, target.Bottom),
                new Point(target.Left + target.Width/2, target.Top + target.Height/2)
            };

            foreach (var corner in corners)
            {
                if (PointInPolygon(polygon, corner)) return true;
            }
            return false;
        }

        private static bool PointInPolygon(IReadOnlyList<Point> polygon, Point testPoint)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                var intersect = ((pi.Y > testPoint.Y) != (pj.Y > testPoint.Y)) &&
                                (testPoint.X < (pj.X - pi.X) * (testPoint.Y - pi.Y) / (pj.Y - pi.Y + double.Epsilon) + pi.X);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private static double DistanceSquared(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private void Reset()
        {
            _isCapturing = false;
            _points.Clear();
        }
    }
}
