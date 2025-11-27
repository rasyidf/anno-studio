using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Rendering.Layers;

/// <summary>
/// Renders the active tool's overlay.
/// </summary>
public class ToolOverlayLayer : LayerBase
{
    public override string Name => "ToolOverlay";
    public override int ZIndex => 150;

    private SKPoint? _lastCursorCanvas;
    private bool _subscribed;

    public override void OnAttached(ICanvasContext context)
    {
        base.OnAttached(context);

        // Subscribe to cursor events so this layer can render debug overlays
        if (!_subscribed && context.EventBus != null)
        {
            context.EventBus.CursorPositionChanged += OnCursorChanged;
            _subscribed = true;
        }
    }

    public override void OnDetached()
    {
        if (_subscribed && Context?.EventBus != null)
        {
            Context.EventBus.CursorPositionChanged -= OnCursorChanged;
            _subscribed = false;
        }

        base.OnDetached();
    }

    private void OnCursorChanged(object? sender, SKPoint pos)
    {
        _lastCursorCanvas = pos;
        // Mark layer dirty so it repaints
        Invalidate();
    }

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!IsVisible)
            return;

        // Render the active tool overlay (tools draw in canvas-space by default)
        context.ActiveTool?.Render(canvas, context);

        // Draw debug overlays via the overlay service (origin + cursor info)
        context.OverlayService?.DrawDebugOverlay(canvas, context, _lastCursorCanvas);
        context.OverlayService?.DrawOriginMarker(canvas, context);

        ClearDirtyFlag();
    }
}
