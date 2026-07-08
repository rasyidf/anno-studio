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
    /// Path edit tool with full bezier handle editing.
    /// For Line/Path: drag anchor points, double-click to add, right-click to remove.
    /// For Curve: drag anchor points AND control handles (ControlIn/ControlOut).
    /// Shift+drag a handle breaks smooth symmetry; without Shift, the opposite handle mirrors.
    /// </summary>
    public class PathEditTool : ITool
    {
        public string Name => "PathEdit";

        private readonly IInputElement _owner;
        private readonly Func<IReadOnlyList<CanvasObject>> _selectionProvider;
        private readonly Action _invalidate;

        private const double HitRadius = 6.0;
        private const double SegmentHitDistance = 6.0;

        private enum HandleType { None, Anchor, ControlIn, ControlOut }

        private HandleType _dragType = HandleType.None;
        private int _dragIndex = -1;
        private bool _isDragging;

        public Cursor? SuggestedCursor { get; private set; }
        public event Action? CursorChanged;

        public PathEditTool(IInputElement owner, Func<IReadOnlyList<CanvasObject>> selectionProvider, Action invalidate)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _selectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        public void Activate() => ResetState();
        public void Deactivate() => ResetState();

        public void OnCancel()
        {
            ResetState();
            _invalidate();
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e == null) return;
            var target = GetEditableObject();
            if (target == null) return;

            var pos = ToWorld(e.GetPosition(_owner));

            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount >= 2)
                {
                    // Double-click → insert point on nearest segment
                    HandleDoubleClick(target, pos);
                    e.Handled = true;
                    return;
                }

                // Try hitting a control handle first (curves only), then anchor
                if (target.ShapeType == "Curve" && target.BezierPoints != null)
                {
                    if (TryHitBezierHandle(target.BezierPoints, pos, out var idx, out var type))
                    {
                        _dragIndex = idx;
                        _dragType = type;
                        _isDragging = true;
                        e.Handled = true;
                        return;
                    }
                }

                // Hit-test anchors
                var points = GetAnchorPoints(target);
                if (points != null)
                {
                    int hitIdx = HitTestPoint(points, pos);
                    if (hitIdx >= 0)
                    {
                        _dragIndex = hitIdx;
                        _dragType = HandleType.Anchor;
                        _isDragging = true;
                        e.Handled = true;
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                // Right-click → delete point
                var points = GetAnchorPoints(target);
                if (points != null && points.Count > 2)
                {
                    int hitIdx = HitTestPoint(points, pos);
                    if (hitIdx >= 0)
                    {
                        DeletePoint(target, hitIdx);
                        _invalidate();
                        e.Handled = true;
                    }
                }
            }
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;
            var target = GetEditableObject();
            if (target == null) return;

            var pos = ToWorld(e.GetPosition(_owner));

            if (_isDragging && _dragIndex >= 0)
            {
                bool smooth = !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift);
                ApplyDrag(target, _dragIndex, _dragType, pos, smooth);
                _invalidate();
                e.Handled = true;
            }
            else
            {
                // Update cursor hover
                UpdateCursor(target, pos);
            }
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _dragIndex = -1;
                _dragType = HandleType.None;
                // Recompute bounds after editing
                var target = GetEditableObject();
                if (target != null) RecomputeBounds(target);
                _invalidate();
            }
        }

        public void OnKeyDown(KeyEventArgs e) { }
        public void OnKeyUp(KeyEventArgs e) { }

        public void Render(DrawingContext dc)
        {
            var target = GetEditableObject();
            if (target == null) return;

            var anchorPen = new Pen(Brushes.DodgerBlue, 1.5);
            var handlePen = new Pen(Brushes.Gray, 1);
            var segmentPen = new Pen(Brushes.DodgerBlue, 1) { DashStyle = DashStyles.Dash };
            const double anchorSize = 4;
            const double handleSize = 3.5;

            if (target.ShapeType == "Curve" && target.BezierPoints is { Count: > 0 })
            {
                // Draw curve segments as dashes
                for (int i = 0; i < target.BezierPoints.Count - 1; i++)
                {
                    var a = target.BezierPoints[i];
                    var b = target.BezierPoints[i + 1];
                    var cp1 = a.ControlOut ?? a.Point;
                    var cp2 = b.ControlIn ?? b.Point;
                    var fig = new PathFigure { StartPoint = a.Point, IsFilled = false };
                    fig.Segments.Add(new BezierSegment(cp1, cp2, b.Point, true));
                    dc.DrawGeometry(null, segmentPen, new PathGeometry(new[] { fig }));
                }

                // Draw handles and anchors
                foreach (var (point, controlIn, controlOut) in target.BezierPoints)
                {
                    // ControlIn handle
                    if (controlIn.HasValue)
                    {
                        dc.DrawLine(handlePen, point, controlIn.Value);
                        DrawDiamond(dc, controlIn.Value, handleSize, Brushes.LightGray, handlePen);
                    }

                    // ControlOut handle
                    if (controlOut.HasValue)
                    {
                        dc.DrawLine(handlePen, point, controlOut.Value);
                        DrawDiamond(dc, controlOut.Value, handleSize, Brushes.Orange, handlePen);
                    }

                    // Anchor point (filled circle)
                    dc.DrawEllipse(Brushes.White, anchorPen, point, anchorSize, anchorSize);
                }
            }
            else
            {
                // Line or Path: draw simple point-to-point segments
                var points = GetAnchorPoints(target);
                if (points == null || points.Count == 0) return;

                for (int i = 0; i < points.Count - 1; i++)
                    dc.DrawLine(segmentPen, points[i], points[i + 1]);

                foreach (var pt in points)
                {
                    var r = new Rect(pt.X - anchorSize, pt.Y - anchorSize, anchorSize * 2, anchorSize * 2);
                    dc.DrawRectangle(Brushes.White, anchorPen, r);
                }
            }
        }

        #region Drag application

        private static void ApplyDrag(CanvasObject obj, int index, HandleType type, Point pos, bool mirrorOpposite)
        {
            switch (obj.ShapeType)
            {
                case "Curve" when obj.BezierPoints != null && index < obj.BezierPoints.Count:
                    var entry = obj.BezierPoints[index];
                    switch (type)
                    {
                        case HandleType.Anchor:
                            // Move anchor and its handles together
                            var delta = pos - entry.Point;
                            var newCtrlIn = entry.ControlIn.HasValue ? (Point?)(entry.ControlIn.Value + delta) : null;
                            var newCtrlOut = entry.ControlOut.HasValue ? (Point?)(entry.ControlOut.Value + delta) : null;
                            obj.BezierPoints[index] = (pos, newCtrlIn, newCtrlOut);
                            break;
                        case HandleType.ControlIn:
                            Point? mirroredOut = entry.ControlOut;
                            if (mirrorOpposite)
                            {
                                // Mirror: reflect ControlIn around anchor to get ControlOut
                                var vec = pos - entry.Point;
                                mirroredOut = new Point(entry.Point.X - vec.X, entry.Point.Y - vec.Y);
                            }
                            obj.BezierPoints[index] = (entry.Point, pos, mirroredOut);
                            break;
                        case HandleType.ControlOut:
                            Point? mirroredIn = entry.ControlIn;
                            if (mirrorOpposite)
                            {
                                var vec = pos - entry.Point;
                                mirroredIn = new Point(entry.Point.X - vec.X, entry.Point.Y - vec.Y);
                            }
                            obj.BezierPoints[index] = (entry.Point, mirroredIn, pos);
                            break;
                    }
                    break;

                case "Line":
                    if (type == HandleType.Anchor)
                    {
                        if (index == 0) obj.LineStart = pos;
                        else if (index == 1) obj.LineEnd = pos;
                    }
                    break;

                case "Path":
                    if (type == HandleType.Anchor && obj.PathPoints != null && index < obj.PathPoints.Count)
                        obj.PathPoints[index] = pos;
                    break;
            }
        }

        #endregion

        #region Insertion and deletion

        private void HandleDoubleClick(CanvasObject target, Point pos)
        {
            if (target.ShapeType == "Curve" && target.BezierPoints != null)
            {
                // Find nearest segment and insert
                int seg = HitTestCurveSegment(target.BezierPoints, pos);
                if (seg >= 0)
                {
                    target.BezierPoints.Insert(seg + 1, (pos, null, null));
                    _invalidate();
                }
            }
            else
            {
                var points = GetAnchorPoints(target);
                if (points == null) return;
                int seg = HitTestSegment(points, pos);
                if (seg >= 0)
                {
                    InsertAnchor(target, seg + 1, pos);
                    _invalidate();
                }
            }
        }

        private static void InsertAnchor(CanvasObject obj, int insertAt, Point pos)
        {
            switch (obj.ShapeType)
            {
                case "Path":
                    obj.PathPoints?.Insert(insertAt, pos);
                    break;
                case "Line":
                    obj.ShapeType = "Path";
                    var pts = new List<Point>();
                    if (obj.LineStart.HasValue) pts.Add(obj.LineStart.Value);
                    if (obj.LineEnd.HasValue) pts.Add(obj.LineEnd.Value);
                    pts.Insert(insertAt, pos);
                    obj.PathPoints = pts;
                    obj.LineStart = null;
                    obj.LineEnd = null;
                    break;
            }
        }

        private static void DeletePoint(CanvasObject obj, int index)
        {
            switch (obj.ShapeType)
            {
                case "Path":
                    obj.PathPoints?.RemoveAt(index);
                    break;
                case "Curve":
                    obj.BezierPoints?.RemoveAt(index);
                    break;
            }
        }

        #endregion

        #region Hit testing

        private static bool TryHitBezierHandle(
            List<(Point Point, Point? ControlIn, Point? ControlOut)> bezierPoints,
            Point pos, out int index, out HandleType type)
        {
            // Check handles first (they're smaller targets, prioritize them)
            for (int i = 0; i < bezierPoints.Count; i++)
            {
                var entry = bezierPoints[i];
                if (entry.ControlOut.HasValue && Distance(pos, entry.ControlOut.Value) <= HitRadius)
                {
                    index = i;
                    type = HandleType.ControlOut;
                    return true;
                }
                if (entry.ControlIn.HasValue && Distance(pos, entry.ControlIn.Value) <= HitRadius)
                {
                    index = i;
                    type = HandleType.ControlIn;
                    return true;
                }
            }

            // Then check anchors
            for (int i = 0; i < bezierPoints.Count; i++)
            {
                if (Distance(pos, bezierPoints[i].Point) <= HitRadius)
                {
                    index = i;
                    type = HandleType.Anchor;
                    return true;
                }
            }

            index = -1;
            type = HandleType.None;
            return false;
        }

        private static int HitTestPoint(List<Point> points, Point pos)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Distance(pos, points[i]) <= HitRadius) return i;
            }
            return -1;
        }

        private static int HitTestSegment(List<Point> points, Point pos)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (DistanceToSegment(pos, points[i], points[i + 1]) <= SegmentHitDistance)
                    return i;
            }
            return -1;
        }

        private static int HitTestCurveSegment(
            List<(Point Point, Point? ControlIn, Point? ControlOut)> bezierPoints, Point pos)
        {
            // Approximate: check distance to the straight line between consecutive anchors
            for (int i = 0; i < bezierPoints.Count - 1; i++)
            {
                if (DistanceToSegment(pos, bezierPoints[i].Point, bezierPoints[i + 1].Point) <= SegmentHitDistance * 2)
                    return i;
            }
            return -1;
        }

        #endregion

        #region Helpers

        private CanvasObject? GetEditableObject()
        {
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0) return null;
            var obj = selection[0];
            return obj.ShapeType is "Line" or "Path" or "Curve" ? obj : null;
        }

        private static List<Point>? GetAnchorPoints(CanvasObject obj)
        {
            return obj.ShapeType switch
            {
                "Line" => new List<Point> { obj.LineStart ?? obj.Bounds.TopLeft, obj.LineEnd ?? obj.Bounds.BottomRight },
                "Path" => obj.PathPoints,
                "Curve" => obj.BezierPoints?.Select(b => b.Point).ToList(),
                _ => null
            };
        }

        private static void RecomputeBounds(CanvasObject obj)
        {
            var pts = new List<Point>();
            switch (obj.ShapeType)
            {
                case "Line":
                    if (obj.LineStart.HasValue) pts.Add(obj.LineStart.Value);
                    if (obj.LineEnd.HasValue) pts.Add(obj.LineEnd.Value);
                    break;
                case "Path":
                    if (obj.PathPoints != null) pts.AddRange(obj.PathPoints);
                    break;
                case "Curve":
                    if (obj.BezierPoints != null)
                    {
                        foreach (var (p, ci, co) in obj.BezierPoints)
                        {
                            pts.Add(p);
                            if (ci.HasValue) pts.Add(ci.Value);
                            if (co.HasValue) pts.Add(co.Value);
                        }
                    }
                    break;
            }
            if (pts.Count == 0) return;
            double minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
            double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
            var bounds = new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
            obj.Bounds = bounds;
        }

        private void UpdateCursor(CanvasObject target, Point pos)
        {
            Cursor? newCursor = null;

            if (target.ShapeType == "Curve" && target.BezierPoints != null)
            {
                if (TryHitBezierHandle(target.BezierPoints, pos, out _, out var type))
                {
                    newCursor = type == HandleType.Anchor ? Cursors.Cross : Cursors.Hand;
                }
            }
            else
            {
                var points = GetAnchorPoints(target);
                if (points != null && HitTestPoint(points, pos) >= 0)
                    newCursor = Cursors.Cross;
            }

            if (newCursor != SuggestedCursor)
            {
                SuggestedCursor = newCursor;
                CursorChanged?.Invoke();
            }
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double DistanceToSegment(Point p, Point a, Point b)
        {
            var ab = b - a;
            var ap = p - a;
            double lengthSq = ab.X * ab.X + ab.Y * ab.Y;
            if (lengthSq == 0) return Distance(p, a);
            double t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / lengthSq, 0, 1);
            var proj = new Point(a.X + t * ab.X, a.Y + t * ab.Y);
            return Distance(p, proj);
        }

        private static void DrawDiamond(DrawingContext dc, Point center, double size, Brush fill, Pen pen)
        {
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new Point(center.X, center.Y - size), true, true);
                ctx.LineTo(new Point(center.X + size, center.Y), true, false);
                ctx.LineTo(new Point(center.X, center.Y + size), true, false);
                ctx.LineTo(new Point(center.X - size, center.Y), true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(fill, pen, geo);
        }

        private void ResetState()
        {
            _isDragging = false;
            _dragIndex = -1;
            _dragType = HandleType.None;
            SuggestedCursor = null;
        }

        private Point ToWorld(Point screenPoint)
            => (_owner is EditorCanvas ec) ? ec.ScreenToWorld(screenPoint) : screenPoint;

        #endregion
    }
}
