using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    /// <summary>
    /// Represents a single renderable layer in the EditorCanvas pipeline.
    /// Layers are intended to be independently registered and upgraded.
    /// </summary>
    public interface IRenderLayer
    {
        string Name { get; }
        int Order { get; }
        bool Enabled { get; set; }

        /// <summary>
        /// Render the layer using a WPF DrawingContext. Implementations should be lightweight and
        /// rely on the given canvas for state and brushes.
        /// </summary>
        void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip);
    }
}
