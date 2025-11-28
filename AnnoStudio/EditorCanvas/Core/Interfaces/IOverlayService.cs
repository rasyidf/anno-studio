using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Interfaces
{
    /// <summary>
    /// Helper service for overlay rendering and debug overlays.
    /// Implementations should provide drawing helpers used by layers.
    /// </summary>
    public interface IOverlayService
    {
        /// <summary>
        /// Draw debug overlays (e.g. cursor position + grid cell) at the provided canvas-space position.
        /// </summary>
        void DrawDebugOverlay(SKCanvas canvas, ICanvasContext context, SKPoint? cursorCanvasPosition);

        /// <summary>
        /// Draw a small origin marker at canvas (0,0).
        /// </summary>
        void DrawOriginMarker(SKCanvas canvas, ICanvasContext context);
    }
}
