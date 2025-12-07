using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Freehand pencil drawing tool that commits a bounding box for now.
    /// </summary>
    public class PencilDrawTool : ITool
    {
        public string Name => "PencilDraw";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private readonly List<Point> _points = new();
        private bool _isDrawing;

        public PencilDrawTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate, Action? afterCommit = null)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            _afterCommit = afterCommit ?? (() => { });
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
            _isDrawing = true;
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDrawing || e == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var pt = e.GetPosition(_owner);
            if (_points.Count == 0 || DistanceSquared(_points[^1], pt) > 1)
            {
                _points.Add(pt);
                _invalidate();
            }
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isDrawing || e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            _isDrawing = false;
            Commit();
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
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.MediumPurple, 1.5);
            var geo = new System.Windows.Media.StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(_points[0], false, false);
                ctx.PolyLineTo(_points.Skip(1).ToList(), true, true);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }

        private void Commit()
        {
            if (_points.Count < 2) return;
            double minX = _points.Min(p => p.X);
            double maxX = _points.Max(p => p.X);
            double minY = _points.Min(p => p.Y);
            double maxY = _points.Max(p => p.Y);
            var bounds = new Rect(new Point(minX, minY), new Point(maxX, maxY));
            var obj = new CanvasObject
            {
                Bounds = bounds,
                Identifier = "Pencil",
                IsSelectable = true
            };
            _objectManager.Add(obj);
            _setSelection(new[] { obj });
            _afterCommit?.Invoke();
        }

        private static double DistanceSquared(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private void Reset()
        {
            _points.Clear();
            _isDrawing = false;
        }
    }
}
