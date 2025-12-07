using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Duplicates current selection once when activated.
    /// </summary>
    public class DuplicateTool : ITool
    {
        public string Name => "Duplicate";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly Func<IReadOnlyList<CanvasObject>> _selectionProvider;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;
        private readonly Action _afterCommit;
        private readonly Vector _offset = new Vector(12, 12);

        public DuplicateTool(Content.IObjectManager<CanvasObject> objectManager, Func<IReadOnlyList<CanvasObject>> selectionProvider, Action<IEnumerable<CanvasObject>> setSelection, Action invalidate, Action? afterCommit = null)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _selectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            _afterCommit = afterCommit ?? (() => { });
        }

        public void Activate()
        {
            var selection = _selectionProvider();
            if (selection == null || selection.Count == 0) return;

            var duplicates = new List<CanvasObject>();
            foreach (var item in selection)
            {
                if (item == null) continue;
                var clone = item.Clone();
                var bounds = clone.Bounds;
                clone.Bounds = new Rect(bounds.X + _offset.X, bounds.Y + _offset.Y, bounds.Width, bounds.Height);
                clone.ZIndex = item.ZIndex + 1;
                _objectManager.Add(clone);
                duplicates.Add(clone);
            }
            if (duplicates.Count > 0)
            {
                _setSelection(duplicates);
                _invalidate();
                _afterCommit?.Invoke();
            }
        }

        public void Deactivate()
        {
            // no-op
        }

        public void OnCancel()
        {
            // duplication is atomic; nothing to cancel
        }

        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        public void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
        }

        public void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
        }

        public void Render(System.Windows.Media.DrawingContext dc)
        {
            // no overlay
        }
    }
}
