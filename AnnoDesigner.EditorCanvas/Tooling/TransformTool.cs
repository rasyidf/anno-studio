using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Transform tool: move (drag body), resize (drag corner handles), rotate (drag rotate handle).
    /// </summary>
    public class TransformTool : ITool
    {
        public string Name => "Transform";

        private readonly IInputElement _owner;
        private readonly Func<IReadOnlyList<CanvasObject>> _selectionProvider;
        private readonly Action _invalidate;

        private enum DragMode { None, Move, Resize, Rotate }

        private DragMode _dragMode;
        private Point _start;
        private Rect _selectionBounds;
        private int _activeHandleIndex = -1; // 0=TL, 1=TR, 2=BR, 3=BL
        private readonly Dictionary<CanvasObject, Rect> _originalBounds = new();
        private readonly Dictionary<CanvasObject, double> _originalRotation = new();
        private readonly Dictionary<CanvasObject, Point> _originalLineStarts = new();
        private readonly Dictionary<CanvasObject, Point> _originalLineEnds = new();
        private readonly Dictionary<CanvasObject, List<Point>> _originalPathPoints = new();
        private readonly Dictionary<CanvasObject, List<(Point, Point?, Point?)>> _originalBezierPoints = new();

        // Cursor communication
        public Cursor? SuggestedCursor { get; private set; }
        public event Action? CursorChanged;

        private const double HandleSize = 6;
        private const double RotateHandleDistance = 16;
        private const double RotateHitRadius = 7;

        public TransformTool(IInputElement owner, Func<IReadOnlyList<CanvasObject>> selectionProvider, Action invalidate)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _selectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        private Point ToWorld(Point screenPoint)
        {
            return (_owner is EditorCanvas ec) ? ec.ScreenToWorld(screenPoint) : screenPoint;
        }

        public void Activate() => ResetState();

        public void Deactivate() => ResetState();

        public void OnCancel()
        {
            if (_dragMode != DragMode.None)
                RestoreSelection();
            ResetState();
            _invalidate();
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0 || e == null) return;
            if (e.ChangedButton != MouseButton.Left) return;

            _selectionBounds = GetSelectionBounds(selection);
            _start = ToWorld(e.GetPosition(_owner));
            SnapshotSelection(selection);

            // Hit-test priority: rotate handle > corner handles > body
            if (HitTestRotateHandle(_selectionBounds, _start))
            {
                _dragMode = DragMode.Rotate;
            }
            else if (TryHitTestCornerHandle(_selectionBounds, _start, out var handleIndex))
            {
                _dragMode = DragMode.Resize;
                _activeHandleIndex = handleIndex;
            }
            else if (_selectionBounds.Contains(_start))
            {
                _dragMode = DragMode.Move;
            }
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0) return;

            var current = ToWorld(e.GetPosition(_owner));

            if (_dragMode == DragMode.None)
            {
                // Update cursor based on hover position
                UpdateCursorForPosition(current, GetSelectionBounds(selection));
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                // Button released without MouseUp event (edge case)
                _dragMode = DragMode.None;
                return;
            }

            switch (_dragMode)
            {
                case DragMode.Move:
                    ApplyMove(selection, current);
                    break;
                case DragMode.Resize:
                    ApplyResize(selection, current, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
                    break;
                case DragMode.Rotate:
                    ApplyRotation(selection, current);
                    break;
            }

            _invalidate();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            _dragMode = DragMode.None;
            _activeHandleIndex = -1;
            _originalBounds.Clear();
            _originalRotation.Clear();
        }

        public void OnKeyDown(KeyEventArgs e) { }
        public void OnKeyUp(KeyEventArgs e) { }

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
            foreach (var handle in GetHandleRects(bounds))
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.White, new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGoldenrod, 1), handle);
            }

            // draw rotate handle
            var rotateAnchor = new Point(bounds.X + bounds.Width / 2, bounds.Y - RotateHandleDistance);
            dc.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGoldenrod, 1), new Point(bounds.X + bounds.Width / 2, bounds.Y), rotateAnchor);
            dc.DrawEllipse(System.Windows.Media.Brushes.DarkGoldenrod, null, rotateAnchor, 5, 5);
        }

        #region Drag operations

        private void ApplyMove(IReadOnlyList<CanvasObject> selection, Point current)
        {
            var delta = current - _start;
            foreach (var item in selection)
            {
                if (!_originalBounds.TryGetValue(item, out var original)) continue;

                // Restore to original position first, then offset
                item.Bounds = original;
                if (_originalLineStarts.TryGetValue(item, out var ls)) item.LineStart = ls;
                if (_originalLineEnds.TryGetValue(item, out var le)) item.LineEnd = le;
                if (_originalPathPoints.TryGetValue(item, out var pp)) item.PathPoints = new List<Point>(pp);
                if (_originalBezierPoints.TryGetValue(item, out var bp))
                    item.BezierPoints = new List<(Point, Point?, Point?)>(bp);

                item.OffsetBy(new Vector(delta.X, delta.Y));
            }
        }

        private void ApplyResize(IReadOnlyList<CanvasObject> selection, Point current, bool keepAspectRatio)
        {
            if (_selectionBounds.Width == 0 || _selectionBounds.Height == 0) return;

            // Determine the anchor (opposite corner) and compute new bounds
            var anchor = GetOppositeCorner(_selectionBounds, _activeHandleIndex);
            double newWidth = Math.Abs(current.X - anchor.X);
            double newHeight = Math.Abs(current.Y - anchor.Y);

            if (keepAspectRatio)
            {
                double aspect = _selectionBounds.Width / _selectionBounds.Height;
                double candidateHeight = newWidth / aspect;
                if (candidateHeight > newHeight)
                    newWidth = newHeight * aspect;
                else
                    newHeight = candidateHeight;
            }

            // Prevent degenerate sizes
            if (newWidth < 1) newWidth = 1;
            if (newHeight < 1) newHeight = 1;

            double scaleX = newWidth / _selectionBounds.Width;
            double scaleY = newHeight / _selectionBounds.Height;

            // New top-left based on anchor position
            double newLeft = Math.Min(anchor.X, anchor.X + (current.X > anchor.X ? 0 : -newWidth));
            double newTop = Math.Min(anchor.Y, anchor.Y + (current.Y > anchor.Y ? 0 : -newHeight));

            // ponytail: simpler — just use min of anchor and current
            newLeft = current.X < anchor.X ? anchor.X - newWidth : anchor.X;
            newTop = current.Y < anchor.Y ? anchor.Y - newHeight : anchor.Y;

            foreach (var item in selection)
            {
                if (!_originalBounds.TryGetValue(item, out var orig)) continue;

                // Proportionally reposition and resize within the new selection bounds
                double relX = (orig.X - _selectionBounds.X) / _selectionBounds.Width;
                double relY = (orig.Y - _selectionBounds.Y) / _selectionBounds.Height;
                double relW = orig.Width / _selectionBounds.Width;
                double relH = orig.Height / _selectionBounds.Height;

                item.Bounds = new Rect(
                    newLeft + relX * newWidth,
                    newTop + relY * newHeight,
                    relW * newWidth,
                    relH * newHeight);
            }
        }

        private void ApplyRotation(IReadOnlyList<CanvasObject> selection, Point current)
        {
            var center = new Point(
                _selectionBounds.X + _selectionBounds.Width / 2,
                _selectionBounds.Y + _selectionBounds.Height / 2);
            var angle = AngleBetween(_start, current, center);

            foreach (var item in selection)
            {
                if (!_originalRotation.TryGetValue(item, out var original)) continue;
                item.RotationDegrees = original + angle;
            }
        }

        #endregion

        #region Cursor management

        private void UpdateCursorForPosition(Point pos, Rect bounds)
        {
            Cursor? newCursor;

            if (HitTestRotateHandle(bounds, pos))
            {
                newCursor = Cursors.Hand;
            }
            else if (TryHitTestCornerHandle(bounds, pos, out var handleIdx))
            {
                // TL(0) and BR(2) = NWSE, TR(1) and BL(3) = NESW
                newCursor = (handleIdx == 0 || handleIdx == 2) ? Cursors.SizeNWSE : Cursors.SizeNESW;
            }
            else if (bounds.Contains(pos))
            {
                newCursor = Cursors.SizeAll;
            }
            else
            {
                newCursor = null;
            }

            if (newCursor != SuggestedCursor)
            {
                SuggestedCursor = newCursor;
                CursorChanged?.Invoke();
            }
        }

        #endregion

        #region Hit testing

        private static bool HitTestRotateHandle(Rect bounds, Point pos)
        {
            var rotateCenter = new Point(bounds.X + bounds.Width / 2, bounds.Y - RotateHandleDistance);
            var dx = pos.X - rotateCenter.X;
            var dy = pos.Y - rotateCenter.Y;
            return (dx * dx + dy * dy) <= RotateHitRadius * RotateHitRadius;
        }

        private static bool TryHitTestCornerHandle(Rect bounds, Point pos, out int handleIndex)
        {
            int idx = 0;
            foreach (var handleRect in GetHandleRects(bounds))
            {
                if (handleRect.Contains(pos))
                {
                    handleIndex = idx;
                    return true;
                }
                idx++;
            }
            handleIndex = -1;
            return false;
        }

        #endregion

        #region Helpers

        private void SnapshotSelection(IReadOnlyList<CanvasObject> selection)
        {
            _originalBounds.Clear();
            _originalRotation.Clear();
            _originalLineStarts.Clear();
            _originalLineEnds.Clear();
            _originalPathPoints.Clear();
            _originalBezierPoints.Clear();
            foreach (var item in selection)
            {
                _originalBounds[item] = item.Bounds;
                _originalRotation[item] = item.RotationDegrees;
                if (item.LineStart.HasValue) _originalLineStarts[item] = item.LineStart.Value;
                if (item.LineEnd.HasValue) _originalLineEnds[item] = item.LineEnd.Value;
                if (item.PathPoints != null) _originalPathPoints[item] = new List<Point>(item.PathPoints);
                if (item.BezierPoints != null) _originalBezierPoints[item] = new List<(Point, Point?, Point?)>(item.BezierPoints);
            }
        }

        private void RestoreSelection()
        {
            var selection = _selectionProvider();
            if (selection == null) return;
            foreach (var item in selection)
            {
                if (_originalBounds.TryGetValue(item, out var rect)) item.Bounds = rect;
                if (_originalRotation.TryGetValue(item, out var rot)) item.RotationDegrees = rot;
                if (_originalLineStarts.TryGetValue(item, out var ls)) item.LineStart = ls;
                if (_originalLineEnds.TryGetValue(item, out var le)) item.LineEnd = le;
                if (_originalPathPoints.TryGetValue(item, out var pp)) item.PathPoints = new List<Point>(pp);
                if (_originalBezierPoints.TryGetValue(item, out var bp)) item.BezierPoints = new List<(Point, Point?, Point?)>(bp);
            }
        }

        private void ResetState()
        {
            _dragMode = DragMode.None;
            _activeHandleIndex = -1;
            _originalBounds.Clear();
            _originalRotation.Clear();
            _originalLineStarts.Clear();
            _originalLineEnds.Clear();
            _originalPathPoints.Clear();
            _originalBezierPoints.Clear();
            _selectionBounds = Rect.Empty;
        }

        private static Point GetOppositeCorner(Rect bounds, int handleIndex)
        {
            // 0=TL→BR, 1=TR→BL, 2=BR→TL, 3=BL→TR
            return handleIndex switch
            {
                0 => bounds.BottomRight,
                1 => new Point(bounds.Left, bounds.Bottom),
                2 => bounds.TopLeft,
                3 => new Point(bounds.Right, bounds.Top),
                _ => bounds.TopLeft
            };
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

        private static IEnumerable<Rect> GetHandleRects(Rect bounds)
        {
            const double size = HandleSize;
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
            return (angle2 - angle1) * 180 / Math.PI;
        }

        #endregion
    }
}
