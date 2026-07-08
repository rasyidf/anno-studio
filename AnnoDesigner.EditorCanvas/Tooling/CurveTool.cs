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
    /// Bezier curve drawing tool: click to place anchor points, drag to set control handles,
    /// double-click or Enter to commit, Escape to cancel.
    /// </summary>
    public class CurveTool : ITool
    {
        public string Name => "Curve";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private readonly List<(Point Point, Point? ControlIn, Point? ControlOut)> _points = new();
        private Point? _currentMouse;
        private bool _isDragging;
        private Point _dragAnchor;

        public CurveTool(
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

        private Point ToWorld(Point screenPoint) => (_owner is EditorCanvas ec) ? ec.ScreenToWorld(screenPoint) : screenPoint;

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

            var pt = ToWorld(e.GetPosition(_owner));

            // Double-click commits
            if (e.ClickCount >= 2)
            {
                Commit();
                return;
            }

            // Place anchor and start potential drag for control handle
            _isDragging = true;
            _dragAnchor = pt;

            // Auto-mirror: set ControlIn from previous point's ControlOut
            Point? controlIn = null;
            if (_points.Count > 0)
            {
                var prev = _points[^1];
                if (prev.ControlOut.HasValue)
                {
                    // Mirror the previous control-out around the new anchor
                    var mirror = new Point(
                        2 * pt.X - (pt.X + (prev.ControlOut.Value.X - prev.Point.X)),
                        2 * pt.Y - (pt.Y + (prev.ControlOut.Value.Y - prev.Point.Y)));
                    // ponytail: simplified — controlIn mirrors prev's controlOut direction relative to this point
                    // Actually the controlIn should just be the reflection of the vector from prev's ControlOut to prev's Point, placed at the new point.
                    // Standard smooth behavior: controlIn = pt - (prevControlOut - prevPoint) normalized to same length
                    var vec = prev.ControlOut.Value - prev.Point;
                    controlIn = new Point(pt.X - vec.X, pt.Y - vec.Y);
                }
            }

            _points.Add((pt, controlIn, null));
            _invalidate();
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;

            var pt = ToWorld(e.GetPosition(_owner));
            _currentMouse = pt;

            if (_isDragging && _points.Count > 0)
            {
                // User is dragging to set control-out handle
                var last = _points[^1];
                var controlOut = pt;
                _points[^1] = (last.Point, last.ControlIn, controlOut);
            }

            _invalidate();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left) return;

            if (_isDragging)
            {
                _isDragging = false;
                var pt = ToWorld(e.GetPosition(_owner));

                // If the user didn't drag far enough, treat as a click (no control handle)
                if (_points.Count > 0)
                {
                    var last = _points[^1];
                    if (last.ControlOut.HasValue)
                    {
                        var delta = last.ControlOut.Value - last.Point;
                        if (delta.Length < 3)
                        {
                            // ponytail: threshold for "no drag" — 3 world units
                            _points[^1] = (last.Point, last.ControlIn, null);
                        }
                    }
                }
                _invalidate();
            }
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

        public void OnKeyUp(KeyEventArgs e) { }

        public void Render(DrawingContext dc)
        {
            if (_points.Count == 0) return;

            var curvePen = new Pen(Brushes.MediumPurple, 2);
            var dashPen = new Pen(Brushes.MediumPurple, 1.5) { DashStyle = DashStyles.Dash };
            var handlePen = new Pen(Brushes.Gray, 1);
            const double anchorRadius = 4;
            const double handleDiamondSize = 3;

            // Draw bezier curve path
            if (_points.Count >= 2)
            {
                var pathFigure = new PathFigure { StartPoint = _points[0].Point, IsFilled = false };

                for (int i = 1; i < _points.Count; i++)
                {
                    var prev = _points[i - 1];
                    var curr = _points[i];

                    var cp1 = prev.ControlOut ?? prev.Point;
                    var cp2 = curr.ControlIn ?? curr.Point;

                    pathFigure.Segments.Add(new BezierSegment(cp1, cp2, curr.Point, true));
                }

                var pathGeometry = new PathGeometry(new[] { pathFigure });
                dc.DrawGeometry(null, curvePen, pathGeometry);
            }

            // Draw anchor points and control handles
            foreach (var (point, controlIn, controlOut) in _points)
            {
                // Anchor circle
                dc.DrawEllipse(Brushes.White, new Pen(Brushes.MediumPurple, 1.5), point, anchorRadius, anchorRadius);

                // Control-out handle line + diamond
                if (controlOut.HasValue)
                {
                    dc.DrawLine(handlePen, point, controlOut.Value);
                    DrawDiamond(dc, controlOut.Value, handleDiamondSize);
                }

                // Control-in handle line + diamond
                if (controlIn.HasValue)
                {
                    dc.DrawLine(handlePen, point, controlIn.Value);
                    DrawDiamond(dc, controlIn.Value, handleDiamondSize);
                }
            }

            // Dashed line from last anchor to current mouse
            if (_currentMouse.HasValue && !_isDragging)
            {
                dc.DrawLine(dashPen, _points[^1].Point, _currentMouse.Value);
            }
        }

        private static void DrawDiamond(DrawingContext dc, Point center, double size)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(center.X, center.Y - size), true, true);
                ctx.LineTo(new Point(center.X + size, center.Y), true, false);
                ctx.LineTo(new Point(center.X, center.Y + size), true, false);
                ctx.LineTo(new Point(center.X - size, center.Y), true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(Brushes.Orange, null, geometry);
        }

        private void Commit()
        {
            if (_points.Count < 2)
            {
                Reset();
                _invalidate();
                return;
            }

            var allPts = _points.Select(p => p.Point).ToList();
            double minX = allPts.Min(p => p.X);
            double maxX = allPts.Max(p => p.X);
            double minY = allPts.Min(p => p.Y);
            double maxY = allPts.Max(p => p.Y);
            var bounds = new Rect(new Point(minX, minY), new Point(maxX, maxY));
            // ponytail: inflate zero-area bounds so the object is always hittable
            if (bounds.Width < 1) bounds.Inflate(1, 0);
            if (bounds.Height < 1) bounds.Inflate(0, 1);

            var obj = new CanvasObject
            {
                Bounds = bounds,
                ShapeType = "Curve",
                BezierPoints = new List<(Point, Point?, Point?)>(_points),
                Identifier = "Curve",
                IsSelectable = true
            };

            if (_owner is EditorCanvas ec) ec.AddObjectWithUndo(obj);
            else _objectManager.Add(obj);
            _setSelection(new[] { obj });
            Reset();
            _invalidate();
            _afterCommit();
        }

        private void Reset()
        {
            _points.Clear();
            _currentMouse = null;
            _isDragging = false;
        }
    }
}
