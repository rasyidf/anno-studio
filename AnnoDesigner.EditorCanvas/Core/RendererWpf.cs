using System.Linq;
using System.Windows.Controls;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    /// <summary>
    /// Lightweight WPF renderer scaffold. Provides a minimal bridge for the EditorCanvas to request redraws.
    /// This is intentionally small — real rendering and layered draws will be implemented later.
    /// </summary>
    public class RendererWpf : ILayeredRenderer
    {
        private readonly UserControl _owner;
        private readonly object _layersLock = new object();
        private readonly System.Collections.Generic.List<IRenderLayer> _layers = new();

        public RendererWpf(UserControl owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            // Request a WPF redraw. Specific drawing will be performed by the control's OnRender or a DrawingVisual layer.
            _owner.Dispatcher?.Invoke(() => _owner.InvalidateVisual());
        }

        public void Render(object drawingContext)
        {
            if (drawingContext is not System.Windows.Media.DrawingContext dc) return;

            var canvas = _owner as AnnoDesigner.Controls.EditorCanvas.EditorCanvas;
            if (canvas == null) return;

            var width = _owner.ActualWidth;
            var height = _owner.ActualHeight;
            var clip = new System.Windows.Rect(0, 0, width, height);

            // Iterate registered layers in order
            IRenderLayer[] copy;
            lock (_layersLock)
            {
                copy = _layers.ToArray();
            }

            foreach (var layer in copy.OrderBy(l => l.Order))
            {
                if (layer == null || !layer.Enabled) continue;
                layer.Render(dc, canvas, clip);
            }
        }

        // The concrete drawing implementations moved to independent layer classes.

        // ILayeredRenderer implementation
        public void AddLayer(IRenderLayer layer)
        {
            if (layer == null) return;
            lock (_layersLock)
            {
                if (!_layers.Contains(layer)) _layers.Add(layer);
            }
        }

        public bool RemoveLayer(IRenderLayer layer)
        {
            if (layer == null) return false;
            lock (_layersLock)
            {
                return _layers.Remove(layer);
            }
        }

        public System.Collections.Generic.IEnumerable<IRenderLayer> Layers
        {
            get
            {
                lock (_layersLock)
                {
                    return _layers.ToArray();
                }
            }
        }
    }
}
