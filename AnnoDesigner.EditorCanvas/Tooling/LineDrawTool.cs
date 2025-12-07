using System;
using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Draws simple lines by click-drag.
    /// </summary>
    public class LineDrawTool : ITool
    {
        public string Name => "LineDraw";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;

        private Point? _start;
        private Point? _current;

        public LineDrawTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate, Action? afterCommit = null)
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
            _current = _start;
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (!_start.HasValue || e == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            _current = e.GetPosition(_owner);
            _invalidate();
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_start.HasValue || e == null || e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
            _current = e.GetPosition(_owner);
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
            if (!_start.HasValue || !_current.HasValue) return;
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.ForestGreen, 2);
            dc.DrawLine(pen, _start.Value, _current.Value);
        }

        private void Commit()
        {
            if (!_start.HasValue || !_current.HasValue) return;
            var rect = new Rect(_start.Value, _current.Value);
            rect.Inflate(1, 1);
            var obj = new CanvasObject
            {
                Bounds = rect,
                Identifier = "Line",
                IsSelectable = true
            };
            _objectManager.Add(obj);
            _setSelection(new[] { obj });
            _afterCommit?.Invoke();
        }

        private void Reset()
        {
            _start = null;
            _current = null;
        }
    }
}
