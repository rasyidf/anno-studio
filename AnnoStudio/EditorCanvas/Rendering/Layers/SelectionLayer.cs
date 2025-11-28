using System.Linq;
using System;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Rendering.Layers;

/// <summary>
/// Renders selection boxes and handles.
/// </summary>
public class SelectionLayer : LayerBase
{
    public override string Name => "Selection";
    public override int ZIndex => 100;

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!IsVisible)
            return;

        var selectedObjects = context.Selection.SelectedObjects;
        if (!selectedObjects.Any())
            return;

        var settings = context.Settings.Render;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = settings.SelectionColor.WithAlpha((byte)(255 * Opacity)),
            StrokeWidth = settings.SelectionThickness
        };

        // Draw selection for each object
        foreach (var obj in selectedObjects)
        {
            var bounds = obj.Bounds;
            
            // Inflate bounds slightly
            bounds.Inflate(2, 2);
            
            canvas.DrawRect(bounds, paint);

            // Draw transform handles if enabled
            if (settings.ShowTransformHandles)
            {
                DrawTransformHandles(canvas, bounds, paint, context);
            }
        }

        // Draw selection bounds if multiple objects selected
        if (selectedObjects.Count > 1)
        {
            var selectionBounds = context.Selection.GetSelectionBounds();
            selectionBounds.Inflate(4, 4);
            
            paint.PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0);
            canvas.DrawRect(selectionBounds, paint);
        }

        ClearDirtyFlag();
    }

    private void DrawTransformHandles(SKCanvas canvas, SKRect bounds, SKPaint paint, ICanvasContext context)
    {
        // Prefer a fixed screen sized handle so handles don't grow/shrink with zoom.
        var baseHandlePixel = 8f;
        var handleSize = Math.Max(2f, baseHandlePixel / Math.Max(0.0001f, context.Viewport.Zoom));
        var originalStyle = paint.Style;

        // Draw corner handles
        var handles = new[]
        {
            new SKPoint(bounds.Left, bounds.Top),
            new SKPoint(bounds.Right, bounds.Top),
            new SKPoint(bounds.Right, bounds.Bottom),
            new SKPoint(bounds.Left, bounds.Bottom),
            new SKPoint(bounds.MidX, bounds.Top),
            new SKPoint(bounds.MidX, bounds.Bottom),
            new SKPoint(bounds.Left, bounds.MidY),
            new SKPoint(bounds.Right, bounds.MidY)
        };

        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColors.White;

        foreach (var handle in handles)
        {
            var handleRect = new SKRect(
                handle.X - handleSize / 2,
                handle.Y - handleSize / 2,
                handle.X + handleSize / 2,
                handle.Y + handleSize / 2
            );
            
            canvas.DrawRect(handleRect, paint);
        }

        // Draw handle borders with consistent 1px screen stroke
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = context.Settings.Render.SelectionColor.WithAlpha((byte)(255 * Opacity));
        paint.StrokeWidth = 1f / Math.Max(0.0001f, context.Viewport.Zoom);

        foreach (var handle in handles)
        {
            var handleRect = new SKRect(
                handle.X - handleSize / 2,
                handle.Y - handleSize / 2,
                handle.X + handleSize / 2,
                handle.Y + handleSize / 2
            );
            
            canvas.DrawRect(handleRect, paint);
        }

        paint.Style = originalStyle;
    }
}
