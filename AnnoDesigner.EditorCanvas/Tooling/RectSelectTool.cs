using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Rectangle selection tool (click-drag rubber band).
    /// </summary>
    public class RectSelectTool : ITool
    {
        public string Name => "RectSelect";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;

        private Point? _start;
        private Rect _rubberBand;

        public RectSelectTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        public void Activate()
        {
            _start = null;
            _rubberBand = Rect.Empty;
        }

        public void Deactivate()
        {
            _start = null;
            _rubberBand = Rect.Empty;
        }

        public void OnCancel()
        {
            _start = null;
            _rubberBand = Rect.Empty;
            _invalidate();
        }

        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            _start = e.GetPosition(_owner);
            _rubberBand = Rect.Empty;
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (!_start.HasValue || e == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var current = e.GetPosition(_owner);
            _rubberBand = NormalizeRect(_start.Value, current);
            _invalidate();
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_start.HasValue || e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            var end = e.GetPosition(_owner);
            var selectionRect = NormalizeRect(_start.Value, end);
            var hits = _objectManager.GetAll()?.Where(o => o != null && o.Bounds.IntersectsWith(selectionRect)).ToList() ?? new List<CanvasObject>();
            _setSelection(hits);
            _start = null;
            _rubberBand = Rect.Empty;
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
            if (_rubberBand.IsEmpty) return;
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Orange, 1)
            {
                DashStyle = System.Windows.Media.DashStyles.Dash
            };
            dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, pen, _rubberBand);
        }

        private static Rect NormalizeRect(Point a, Point b)
        {
            return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        }
    }
}
