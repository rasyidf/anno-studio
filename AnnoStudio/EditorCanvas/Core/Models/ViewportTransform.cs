using SkiaSharp;
using System;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Viewport transformation for pan and zoom.
/// </summary>
public class ViewportTransform
{
    private SKPoint _pan = SKPoint.Empty;
    private float _zoom = 1.0f;

    /// <summary>
    /// Pan offset in screen coordinates.
    /// </summary>
    public SKPoint Pan
    {
        get => _pan;
        set
        {
            if (_pan != value)
            {
                _pan = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Zoom level (1.0 = 100%).
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Clamp(value, MinZoom, MaxZoom);
            if (Math.Abs(_zoom - clamped) > 0.001f)
            {
                _zoom = clamped;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Minimum zoom level.
    /// </summary>
    public float MinZoom { get; set; } = 0.1f;

    /// <summary>
    /// Maximum zoom level.
    /// </summary>
    public float MaxZoom { get; set; } = 10.0f;

    /// <summary>
    /// Event raised when viewport changes.
    /// </summary>
    public event EventHandler? Changed;

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get transformation matrix.
    /// </summary>
    public SKMatrix GetMatrix()
    {
        var matrix = SKMatrix.CreateScale(Zoom, Zoom);
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation(Pan.X, Pan.Y));
        return matrix;
    }

    /// <summary>
    /// Get inverse transformation matrix.
    /// </summary>
    public SKMatrix GetInverseMatrix()
    {
        var matrix = GetMatrix();
        matrix.TryInvert(out var inverted);
        return inverted;
    }

    /// <summary>
    /// Transform screen point to canvas coordinates.
    /// </summary>
    public SKPoint ScreenToCanvas(SKPoint screenPoint)
    {
        var inverted = GetInverseMatrix();
        return inverted.MapPoint(screenPoint);
    }

    /// <summary>
    /// Transform canvas point to screen coordinates.
    /// </summary>
    public SKPoint CanvasToScreen(SKPoint canvasPoint)
    {
        var matrix = GetMatrix();
        return matrix.MapPoint(canvasPoint);
    }

    /// <summary>
    /// Reset viewport to default.
    /// </summary>
    public void Reset()
    {
        Pan = SKPoint.Empty;
        Zoom = 1.0f;
    }

    /// <summary>
    /// Zoom to fit rectangle in viewport.
    /// </summary>
    public void ZoomToFit(SKRect rect, SKSize viewportSize, float padding = 20)
    {
        if (rect.IsEmpty || viewportSize.IsEmpty)
            return;

        var scaleX = (viewportSize.Width - padding * 2) / rect.Width;
        var scaleY = (viewportSize.Height - padding * 2) / rect.Height;
        Zoom = Math.Min(scaleX, scaleY);

        var centerX = rect.MidX * Zoom;
        var centerY = rect.MidY * Zoom;
        Pan = new SKPoint(
            viewportSize.Width / 2 - centerX,
            viewportSize.Height / 2 - centerY
        );
    }
}
