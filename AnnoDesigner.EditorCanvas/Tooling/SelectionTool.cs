using System;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Basic selection tool scaffold. Will be extended to handle mouse interactions and multi-select.
    /// </summary>
    public class SelectionTool : ITool
    {
        public string Name => "Selection";

        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly System.Windows.IInputElement _owner;

        public event Action<CanvasObject> ObjectSelected;

        public SelectionTool(Content.IObjectManager<CanvasObject> objectManager, System.Windows.IInputElement owner)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Activate()
        {
            // nothing special for now
        }

        public void Deactivate()
        {
            // nothing special for now
        }

        public void OnCancel()
        {
            // cancel clears pending drag state; none currently
        }

        protected void RaiseSelected(CanvasObject obj)
        {
            ObjectSelected?.Invoke(obj);
        }

        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e == null) return;
            var pt = e.GetPosition(_owner);
            var hits = _objectManager.GetObjectsAt(pt);
            // pick the first hit (topmost logic not implemented yet)
            foreach (var hit in hits)
            {
                RaiseSelected(hit);
                return;
            }
            // no hit -> deselect (raise null?) We'll raise null to indicate none
            RaiseSelected(null);
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            // TODO: update drag selection rectangle
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: finalize selection
        }

        public void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // TODO: handle keyboard modifiers
        }

        public void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            // TODO: handle keyboard modifiers
        }

        public void Render(System.Windows.Media.DrawingContext dc)
        {
            // SelectionTool has no additional overlays yet; future handles/rects go here.
        }
    }
}
