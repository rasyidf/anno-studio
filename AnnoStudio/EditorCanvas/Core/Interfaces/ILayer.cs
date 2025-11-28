using System;
using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Represents a rendering layer in the canvas.
/// </summary>
public interface ILayer
{
    /// <summary>
    /// Layer identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Z-index for rendering order (lower values render first).
    /// </summary>
    int ZIndex { get; }

    /// <summary>
    /// Visibility toggle.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Layer opacity (0.0 - 1.0).
    /// </summary>
    float Opacity { get; set; }

    /// <summary>
    /// Blend mode for compositing.
    /// </summary>
    SKBlendMode BlendMode { get; set; }

    /// <summary>
    /// Whether layer needs redraw.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Render layer content.
    /// </summary>
    void Render(SKCanvas canvas, ICanvasContext context);

    /// <summary>
    /// Update layer state (called per frame).
    /// </summary>
    void Update(TimeSpan deltaTime);

    /// <summary>
    /// Mark layer as needing redraw.
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Called when layer is added to canvas.
    /// </summary>
    void OnAttached(ICanvasContext context);

    /// <summary>
    /// Called when layer is removed from canvas.
    /// </summary>
    void OnDetached();
}
