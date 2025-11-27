using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Controls;

/// <summary>
/// Custom control for rendering with SkiaSharp.
/// </summary>
public class SkiaCanvasControl : Control
{
    public event Action<SKCanvas>? PaintSurface;

    static SkiaCanvasControl()
    {
        AffectsRender<SkiaCanvasControl>(BoundsProperty);
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new SkiaDrawOperation(Bounds, PaintSurface));
    }

    private class SkiaDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly Action<SKCanvas>? _paintSurface;

        public SkiaDrawOperation(Rect bounds, Action<SKCanvas>? paintSurface)
        {
            _bounds = bounds;
            _paintSurface = paintSurface;
        }

        public Rect Bounds => _bounds;

        public void Dispose()
        {
            // Nothing to dispose
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return other is SkiaDrawOperation;
        }

        public bool HitTest(Point p) => _bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            if (_paintSurface == null)
                return;

            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature));
            if (leaseFeature is not ISkiaSharpApiLeaseFeature skiaFeature)
                return;

            using var lease = skiaFeature.Lease();
            var canvas = lease.SkCanvas;

            // Ensure the underlying surface is cleared first to avoid showing
            // stale or partially rendered content during partial redraws.
            canvas.Save();
            // Reset matrix so Clear affects pixel-space, not transformed space
            canvas.ResetMatrix();
            canvas.Clear();
            canvas.Restore();

            // Call the paint delegate with a saved state; delegate is responsible
            // for any transforms it needs (e.g. viewport matrix)
            canvas.Save();
            _paintSurface(canvas);
            canvas.Restore();
        }
    }
}
