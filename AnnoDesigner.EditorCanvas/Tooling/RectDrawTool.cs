using System;
using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Draws rectangles by drag gesture and commits a CanvasObject.
    /// </summary>
    public class RectDrawTool : ITool
    {
        public string Name => "RectDraw";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private Point? _start;
        private Rect _preview;

        public RectDrawTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate, Action? afterCommit = null)
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
            _start = e.GetPosition(_owner);
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (!_start.HasValue || e == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var current = e.GetPosition(_owner);
            _preview = Normalize(_start.Value, current);
            _invalidate();
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_start.HasValue || e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            var end = e.GetPosition(_owner);
            var rect = Normalize(_start.Value, end);
            Commit(rect);
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
            if (_preview.IsEmpty) return;
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0, 120, 255));
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DodgerBlue, 1);
            dc.DrawRectangle(brush, pen, _preview);
        }

        private void Commit(Rect rect)
        {
            if (rect.Width < 1 || rect.Height < 1) return;
            var obj = new CanvasObject
            {
                Bounds = rect,
                Identifier = "Rect",
                IsSelectable = true
            };
            _objectManager.Add(obj);
            _setSelection(new[] { obj });
            _afterCommit?.Invoke();
        }

        private static Rect Normalize(Point a, Point b)
        {
            return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        }

        private void Reset()
        {
            _start = null;
            _preview = Rect.Empty;
        }
    }
}
