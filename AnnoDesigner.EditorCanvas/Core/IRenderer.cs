namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    /// <summary>
    /// Minimal renderer interface for the EditorCanvas pipeline.
    /// Concrete renderers will be provided later (WPF DrawingContext, DirectX, etc.).
    /// </summary>
    public interface IRenderer
    {
        /// <summary>Request a redraw of the canvas or affected region.</summary>
        void Invalidate();

        /// <summary>Render using a backend-specific drawing context object.</summary>
        void Render(object drawingContext);
    }
}
