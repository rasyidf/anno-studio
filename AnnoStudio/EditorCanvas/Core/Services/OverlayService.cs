using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services
{
    /// <summary>
    /// Simple overlay renderer for debug visuals and origin marker.
    /// </summary>
    public class OverlayService : IOverlayService
    {
        public void DrawDebugOverlay(SKCanvas canvas, ICanvasContext context, SKPoint? cursorCanvasPosition)
        {
            if (canvas == null || context == null || cursorCanvasPosition == null)
                return;

            var viewport = context.Viewport;
            var settings = context.Settings;

            // Only draw when requested
            if (settings.Debug?.ShowOverlay != true)
                return;

            try
            {
                var gridSize = context.Grid.Settings.GridSize;
                using var paint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 / viewport.Zoom, IsAntialias = true };

                // small cross at cursor
                var pos = cursorCanvasPosition.Value;
                canvas.DrawLine(pos.X - 6 / viewport.Zoom, pos.Y, pos.X + 6 / viewport.Zoom, pos.Y, paint);
                canvas.DrawLine(pos.X, pos.Y - 6 / viewport.Zoom, pos.X, pos.Y + 6 / viewport.Zoom, paint);

                using var textPaint = new SKPaint { Color = SKColors.OrangeRed, TextSize = 12f / viewport.Zoom, IsAntialias = true };
                var (gx, gy) = context.Grid.CanvasToGrid(pos);
                var info = $"Canvas: ({pos.X:F1},{pos.Y:F1}) Grid: ({gx},{gy})";
                canvas.DrawText(info, pos.X + 8 / viewport.Zoom, pos.Y - 8 / viewport.Zoom, textPaint);
            }
            catch
            {
                // defensive - don't break paint
            }
        }

        public void DrawOriginMarker(SKCanvas canvas, ICanvasContext context)
        {
            if (canvas == null || context == null)
                return;

            var viewport = context.Viewport;
            var settings = context.Settings;

            var showDebug = settings.Debug?.ShowOverlay == true;
            var showOrigin = settings.Render?.ShowOrigin == true;

            if (!showDebug && !showOrigin)
                return;

            try
            {
                var origin = new SKPoint(0, 0);
                using var originPaint = new SKPaint { Color = SKColors.Lime, StrokeWidth = 2 / viewport.Zoom, IsAntialias = true };
                // Crosshair at origin
                canvas.DrawLine(origin.X - 8 / viewport.Zoom, origin.Y, origin.X + 8 / viewport.Zoom, origin.Y, originPaint);
                canvas.DrawLine(origin.X, origin.Y - 8 / viewport.Zoom, origin.X, origin.Y + 8 / viewport.Zoom, originPaint);

                if (showDebug)
                {
                    using var textPaint2 = new SKPaint { Color = SKColors.Lime, TextSize = 12f / viewport.Zoom, IsAntialias = true };
                    canvas.DrawText("0,0", origin.X + 10 / viewport.Zoom, origin.Y - 10 / viewport.Zoom, textPaint2);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
