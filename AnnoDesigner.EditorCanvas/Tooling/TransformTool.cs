using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Simple transform tool: move with left drag, rotate with right drag on the selection.
    /// </summary>
    public class TransformTool : ITool
    {
        public string Name => "Transform";

        private readonly System.Windows.IInputElement _owner;
        private readonly Func<IReadOnlyList<CanvasObject>> _selectionProvider;
        private readonly Action _invalidate;

        private bool _isDragging;
        private bool _isRotating;
        private Point _start;
        private Rect _selectionBounds;
        private readonly Dictionary<CanvasObject, Rect> _originalBounds = new();
        private readonly Dictionary<CanvasObject, double> _originalRotation = new();

        public TransformTool(System.Windows.IInputElement owner, Func<IReadOnlyList<CanvasObject>> selectionProvider, Action invalidate)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _selectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        public void Activate()
        {
            ResetState();
        }

        public void Deactivate()
        {
            ResetState();
        }

        public void OnCancel()
        {
            if (_isDragging || _isRotating)
            {
                RestoreSelection();
            }
            ResetState();
            _invalidate();
        }

        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0 || e == null) return;

            _selectionBounds = GetSelectionBounds(selection);
            _start = e.GetPosition(_owner);
            _originalBounds.Clear();
            _originalRotation.Clear();
            foreach (var item in selection)
            {
                _originalBounds[item] = item.Bounds;
                _originalRotation[item] = item.RotationDegrees;
            }

            if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                _isRotating = true;
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                _isDragging = true;
            }
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (e == null) return;
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0) return;

            if (_isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var current = e.GetPosition(_owner);
                var delta = current - _start;
                foreach (var item in selection)
                {
                    if (!_originalBounds.TryGetValue(item, out var original)) continue;
                    item.Bounds = new Rect(original.X + delta.X, original.Y + delta.Y, original.Width, original.Height);
                }
                _invalidate();
            }
            else if (_isRotating && e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var center = new Point(_selectionBounds.X + _selectionBounds.Width / 2, _selectionBounds.Y + _selectionBounds.Height / 2);
                var current = e.GetPosition(_owner);
                var angle = AngleBetween(_start, current, center);
                foreach (var item in selection)
                {
                    if (!_originalRotation.TryGetValue(item, out var original)) continue;
                    item.RotationDegrees = original + angle;
                }
                _invalidate();
            }
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isRotating = false;
            _originalBounds.Clear();
            _originalRotation.Clear();
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
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0) return;

            var bounds = GetSelectionBounds(selection);
            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Goldenrod, 1)
            {
                DashStyle = System.Windows.Media.DashStyles.Dot
            };
            dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, pen, bounds);

            // draw corner handles
            foreach (var handle in GetHandles(bounds))
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.White, new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGoldenrod, 1), handle);
            }

            // draw rotate handle
            var rotateAnchor = new Point(bounds.X + bounds.Width / 2, bounds.Y - 16);
            dc.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGoldenrod, 1), new Point(bounds.X + bounds.Width / 2, bounds.Y), rotateAnchor);
            dc.DrawEllipse(System.Windows.Media.Brushes.DarkGoldenrod, null, rotateAnchor, 5, 5);
        }

        private void RestoreSelection()
        {
            var selection = _selectionProvider();
            if (selection == null) return;
            foreach (var item in selection)
            {
                if (_originalBounds.TryGetValue(item, out var rect)) item.Bounds = rect;
                if (_originalRotation.TryGetValue(item, out var rot)) item.RotationDegrees = rot;
            }
        }

        private void ResetState()
        {
            _isDragging = false;
            _isRotating = false;
            _originalBounds.Clear();
            _originalRotation.Clear();
            _selectionBounds = Rect.Empty;
        }

        private static Rect GetSelectionBounds(IReadOnlyCollection<CanvasObject> selection)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var obj in selection)
            {
                minX = Math.Min(minX, obj.Bounds.X);
                minY = Math.Min(minY, obj.Bounds.Y);
                maxX = Math.Max(maxX, obj.Bounds.Right);
                maxY = Math.Max(maxY, obj.Bounds.Bottom);
            }
            if (minX == double.MaxValue) return Rect.Empty;
            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        private static IEnumerable<Rect> GetHandles(Rect bounds)
        {
            const double size = 6;
            yield return new Rect(bounds.TopLeft - new Vector(size / 2, size / 2), new Size(size, size));
            yield return new Rect(new Point(bounds.Right - size / 2, bounds.Top - size / 2), new Size(size, size));
            yield return new Rect(new Point(bounds.Right - size / 2, bounds.Bottom - size / 2), new Size(size, size));
            yield return new Rect(new Point(bounds.Left - size / 2, bounds.Bottom - size / 2), new Size(size, size));
        }

        private static double AngleBetween(Point start, Point current, Point center)
        {
            var v1 = start - center;
            var v2 = current - center;
            var angle1 = Math.Atan2(v1.Y, v1.X);
            var angle2 = Math.Atan2(v2.Y, v2.X);
            var delta = (angle2 - angle1) * 180 / Math.PI;
            return delta;
        }
    }
}
