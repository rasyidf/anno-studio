using System;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Rendering.Layers;

/// <summary>
/// Renders the background grid.
/// </summary>
public class GridLayer : LayerBase
{
    public override string Name => "Grid";
    public override int ZIndex => -100;

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!IsVisible || !context.Grid.IsEnabled)
            return;

        var settings = context.Grid.Settings;
        var viewport = context.Viewport;
        
        // Calculate visible grid bounds using the viewport transform (more robust than manual math)
        var topLeft = viewport.ScreenToCanvas(new SKPoint(0, 0));
        var bottomRight = viewport.ScreenToCanvas(new SKPoint(canvas.LocalClipBounds.Width, canvas.LocalClipBounds.Height));

        // Normalize so Left < Right and Top < Bottom
        var left = Math.Min(topLeft.X, bottomRight.X);
        var right = Math.Max(topLeft.X, bottomRight.X);
        var top = Math.Min(topLeft.Y, bottomRight.Y);
        var bottom = Math.Max(topLeft.Y, bottomRight.Y);

        var viewportBounds = new SKRect(left, top, right - left, bottom - top);

        var gridSize = settings.GridSize;
        // Expand by one extra grid at each edge to avoid artifacts where the edges aren't covered
        var startX = (int)Math.Floor(viewportBounds.Left / gridSize) * gridSize - (int)gridSize;
        var startY = (int)Math.Floor(viewportBounds.Top / gridSize) * gridSize - (int)gridSize;
        var endX = (int)Math.Ceiling(viewportBounds.Right / gridSize) * gridSize + (int)gridSize;
        var endY = (int)Math.Ceiling(viewportBounds.Bottom / gridSize) * gridSize + (int)gridSize;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = settings.Color.WithAlpha((byte)(255 * settings.Opacity * Opacity))
        };

        switch (settings.DisplayMode)
        {
            case GridDisplayMode.Dots:
                RenderDots(canvas, startX, startY, endX, endY, gridSize, settings, paint);
                break;

            case GridDisplayMode.Lines:
                RenderLines(canvas, startX, startY, endX, endY, gridSize, settings, paint);
                break;

            case GridDisplayMode.Crosses:
                RenderCrosses(canvas, startX, startY, endX, endY, gridSize, settings, paint);
                break;
        }

        ClearDirtyFlag();
    }

    private void RenderDots(SKCanvas canvas, float startX, float startY, float endX, float endY, 
        float gridSize, GridSettings settings, SKPaint paint)
    {
        paint.StrokeWidth = settings.MinorGridThickness;
        
        for (float x = startX; x <= endX; x += gridSize)
        {
            for (float y = startY; y <= endY; y += gridSize)
            {
                canvas.DrawCircle(x, y, 1f, paint);
            }
        }
    }

    private void RenderLines(SKCanvas canvas, float startX, float startY, float endX, float endY,
        float gridSize, GridSettings settings, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;

        int lineCount = 0;

        // Vertical lines
        for (float x = startX; x <= endX; x += gridSize)
        {
            bool isMajor = settings.MajorGridInterval > 0 && lineCount % settings.MajorGridInterval == 0;
            
            if (isMajor)
            {
                paint.Color = settings.MajorGridColor.WithAlpha((byte)(255 * settings.Opacity * Opacity));
                paint.StrokeWidth = settings.MajorGridThickness;
            }
            else
            {
                paint.Color = settings.Color.WithAlpha((byte)(255 * settings.Opacity * Opacity));
                paint.StrokeWidth = settings.MinorGridThickness;
            }

            canvas.DrawLine(x, startY, x, endY, paint);
            lineCount++;
        }

        lineCount = 0;

        // Horizontal lines
        for (float y = startY; y <= endY; y += gridSize)
        {
            bool isMajor = settings.MajorGridInterval > 0 && lineCount % settings.MajorGridInterval == 0;
            
            if (isMajor)
            {
                paint.Color = settings.MajorGridColor.WithAlpha((byte)(255 * settings.Opacity * Opacity));
                paint.StrokeWidth = settings.MajorGridThickness;
            }
            else
            {
                paint.Color = settings.Color.WithAlpha((byte)(255 * settings.Opacity * Opacity));
                paint.StrokeWidth = settings.MinorGridThickness;
            }

            canvas.DrawLine(startX, y, endX, y, paint);
            lineCount++;
        }
    }

    private void RenderCrosses(SKCanvas canvas, float startX, float startY, float endX, float endY,
        float gridSize, GridSettings settings, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = settings.MinorGridThickness;
        
        float crossSize = 3f;

        for (float x = startX; x <= endX; x += gridSize)
        {
            for (float y = startY; y <= endY; y += gridSize)
            {
                canvas.DrawLine(x - crossSize, y, x + crossSize, y, paint);
                canvas.DrawLine(x, y - crossSize, x, y + crossSize, paint);
            }
        }
    }
}
