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
    /// Selection tool: click to select single object, drag to rubber-band multi-select.
    /// Supports Ctrl (toggle) and Shift (add) modifiers during drag selection.
    /// </summary>
    public class SelectionTool : ITool
    {
        public string Name => "Selection";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly IInputElement _owner;
        private readonly Action<IEnumerable<CanvasObject>>? _setSelection;
        private readonly Func<IReadOnlyList<CanvasObject>>? _getSelection;
        private readonly Action? _invalidate;

        public event Action<CanvasObject>? ObjectSelected;

        private bool _isDragging;
        private Point _dragStart;
        private Rect _selectionRect;

        public SelectionTool(Content.IObjectManager<CanvasObject> objectManager, IInputElement owner)
            : this(objectManager, owner, null, null, null)
        {
        }

        public SelectionTool(
            Content.IObjectManager<CanvasObject> objectManager,
            IInputElement owner,
            Action<IEnumerable<CanvasObject>>? setSelection,
            Func<IReadOnlyList<CanvasObject>>? getSelection,
            Action? invalidate)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection;
            _getSelection = getSelection;
            _invalidate = invalidate;
        }

        private Point ToWorld(Point screenPoint) => (_owner is EditorCanvas ec) ? ec.ScreenToWorld(screenPoint) : screenPoint;

        public void Activate()
        {
            ClearDragState();
        }

        public void Deactivate()
        {
            ClearDragState();
        }

        public void OnCancel()
        {
            ClearDragState();
            _invalidate?.Invoke();
        }

        private bool _isMoving;
        private Point _moveStart;
        private List<(CanvasObject obj, Rect originalBounds)> _moveTargets = new();

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left) return;
            var screenPt = e.GetPosition(_owner);
            var pt = ToWorld(screenPt);

            // Check if we hit an existing object
            var hits = _objectManager.GetObjectsAt(pt);
            foreach (var hit in hits)
            {
                // If hit object is already selected, start move
                var currentSelection = _getSelection?.Invoke();
                if (currentSelection != null && currentSelection.Contains(hit))
                {
                    _isMoving = true;
                    _moveStart = pt;
                    _moveTargets = currentSelection.Select(o => (o, o.Bounds)).ToList();
                    return;
                }

                // Otherwise, select it (with Ctrl toggle)
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    var sel = currentSelection?.ToList() ?? new();
                    if (sel.Contains(hit)) sel.Remove(hit);
                    else sel.Add(hit);
                    _setSelection?.Invoke(sel);
                }
                else
                {
                    _setSelection?.Invoke(new[] { hit });
                }
                _invalidate?.Invoke();
                return;
            }

            // No hit — start drag selection
            _isDragging = true;
            _dragStart = pt;
            _selectionRect = new Rect(pt, new Size(0, 0));
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;
            var current = ToWorld(e.GetPosition(_owner));

            if (_isMoving && e.LeftButton == MouseButtonState.Pressed)
            {
                var delta = current - _moveStart;
                // Snap delta to grid
                var ec = _owner as EditorCanvas;
                if (ec?.TransformService != null)
                {
                    var snapped = ec.TransformService.SnapToGrid(new Point(delta.X, delta.Y));
                    delta = new Vector(Math.Round(snapped.X), Math.Round(snapped.Y));
                }

                foreach (var (obj, orig) in _moveTargets)
                {
                    obj.Bounds = new Rect(orig.X + delta.X, orig.Y + delta.Y, orig.Width, orig.Height);
                }
                _invalidate?.Invoke();
                return;
            }

            if (!_isDragging || e.LeftButton != MouseButtonState.Pressed) return;
            _selectionRect = NormalizeRect(_dragStart, current);
            _invalidate?.Invoke();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left) return;

            if (_isMoving)
            {
                _isMoving = false;
                _moveTargets.Clear();
                _invalidate?.Invoke();
                return;
            }

            if (!_isDragging) return;

            var end = ToWorld(e.GetPosition(_owner));
            var finalRect = NormalizeRect(_dragStart, end);

            var hits = _objectManager.GetObjectsInRegion(finalRect)
                .Where(o => o.IsSelectable)
                .ToList();

            var modifiers = Keyboard.Modifiers;

            if (_setSelection != null)
            {
                if ((modifiers & ModifierKeys.Control) != 0)
                {
                    // Ctrl: toggle each hit in/out of current selection
                    var current = new List<CanvasObject>(_getSelection?.Invoke() ?? Array.Empty<CanvasObject>());
                    foreach (var hit in hits)
                    {
                        if (current.Contains(hit))
                            current.Remove(hit);
                        else
                            current.Add(hit);
                    }
                    _setSelection(current);
                }
                else if ((modifiers & ModifierKeys.Shift) != 0)
                {
                    // Shift: add to current selection
                    var current = new List<CanvasObject>(_getSelection?.Invoke() ?? Array.Empty<CanvasObject>());
                    foreach (var hit in hits)
                    {
                        if (!current.Contains(hit))
                            current.Add(hit);
                    }
                    _setSelection(current);
                }
                else
                {
                    // No modifier: replace selection
                    _setSelection(hits);
                }
            }
            else
            {
                // Fallback for legacy constructor: raise event for first hit
                RaiseSelected(hits.FirstOrDefault());
            }

            ClearDragState();
            _invalidate?.Invoke();
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            // no-op — modifiers checked on mouse up
        }

        public void OnKeyUp(KeyEventArgs e)
        {
            // no-op
        }

        public void Render(DrawingContext dc)
        {
            if (!_isDragging || _selectionRect.IsEmpty) return;

            var fill = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
            fill.Freeze();
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 1)
            {
                DashStyle = DashStyles.Dash
            };
            pen.Freeze();

            dc.DrawRectangle(fill, pen, _selectionRect);
        }

        protected void RaiseSelected(CanvasObject? obj)
        {
            ObjectSelected?.Invoke(obj);
        }

        private void ClearDragState()
        {
            _isDragging = false;
            _isMoving = false;
            _moveTargets.Clear();
            _selectionRect = Rect.Empty;
        }

        private static Rect NormalizeRect(Point a, Point b)
        {
            return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        }
    }
}
