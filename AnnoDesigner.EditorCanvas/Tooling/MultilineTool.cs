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
    /// Polyline drawing tool: click to add points, double-click or Enter to commit, Escape to cancel.
    /// </summary>
    public class MultilineTool : ITool
    {
        public string Name => "Multiline";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private readonly List<Point> _points = new();
        private Point? _currentMouse;

        public MultilineTool(
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
            _currentMouse = ToWorld(e.GetPosition(_owner));
            if (_points.Count > 0)
                _invalidate();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            // no-op: placement is click-based, not drag-based
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
            if (_points.Count == 0) return;

            var solidPen = new Pen(Brushes.DodgerBlue, 2);
            var dashPen = new Pen(Brushes.DodgerBlue, 1.5) { DashStyle = DashStyles.Dash };
            const double handleSize = 4;

            // Draw solid lines between placed points
            for (int i = 0; i < _points.Count - 1; i++)
            {
                dc.DrawLine(solidPen, _points[i], _points[i + 1]);
            }

            // Draw small squares at each placed point
            foreach (var pt in _points)
            {
                dc.DrawRectangle(Brushes.DodgerBlue, null,
                    new Rect(pt.X - handleSize, pt.Y - handleSize, handleSize * 2, handleSize * 2));
            }

            // Dashed line from last point to current cursor position
            if (_currentMouse.HasValue)
            {
                dc.DrawLine(dashPen, _points[^1], _currentMouse.Value);
            }
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
                ShapeType = "Path",
                PathPoints = new List<Point>(_points),
                Identifier = "Multiline",
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
        }
    }
}
