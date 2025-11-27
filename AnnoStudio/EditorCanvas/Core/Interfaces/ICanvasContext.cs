using Avalonia;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Provides access to canvas state and services.
/// </summary>
public interface ICanvasContext
{
    /// <summary>
    /// Current viewport transformation.
    /// </summary>
    ViewportTransform Viewport { get; }

    /// <summary>
    /// Grid system for snapping and alignment.
    /// </summary>
    IGridSystem Grid { get; }

    /// <summary>
    /// Selection management service.
    /// </summary>
    ISelectionService Selection { get; }

    /// <summary>
    /// Collection of all canvas objects.
    /// </summary>
    IObjectCollection Objects { get; }

    /// <summary>
    /// Command history for undo/redo.
    /// </summary>
    ICommandHistory History { get; }

    /// <summary>
    /// Event bus for canvas events.
    /// </summary>
    ICanvasEventBus EventBus { get; }

    /// <summary>
    /// Editor settings and preferences.
    /// </summary>
    EditorSettings Settings { get; }

    /// <summary>
    /// The currently active tool (optional) so layers and services can render tool overlays.
    /// </summary>
    IEditorTool? ActiveTool { get; }

    /// <summary>
    /// Overlay rendering / debug helper service.
    /// </summary>
    IOverlayService OverlayService { get; }

    /// <summary>
    /// Convert screen coordinates to canvas coordinates.
    /// </summary>
    SKPoint ScreenToCanvas(Point screenPoint);

    /// <summary>
    /// Convert canvas coordinates to screen coordinates.
    /// </summary>
    Point CanvasToScreen(SKPoint canvasPoint);

    /// <summary>
    /// Request full canvas redraw.
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Request redraw of specific region.
    /// </summary>
    void Invalidate(SKRect region);
}
