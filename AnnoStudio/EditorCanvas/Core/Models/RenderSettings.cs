using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Rendering quality and display settings.
/// </summary>
public class RenderSettings
{
    /// <summary>
    /// Background color of the canvas.
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.White;

    /// <summary>
    /// Enable anti-aliasing.
    /// </summary>
    public bool AntiAlias { get; set; } = true;

    /// <summary>
    /// Bitmap filter quality.
    /// </summary>
    public SKFilterQuality FilterQuality { get; set; } = SKFilterQuality.High;

    /// <summary>
    /// Show object bounding boxes.
    /// </summary>
    public bool ShowBounds { get; set; } = false;

    /// <summary>
    /// Show canvas origin marker.
    /// </summary>
    public bool ShowOrigin { get; set; } = true;

    /// <summary>
    /// Show transform handles on selected objects.
    /// </summary>
    public bool ShowTransformHandles { get; set; } = true;

    /// <summary>
    /// Selection box color.
    /// </summary>
    public SKColor SelectionColor { get; set; } = new SKColor(0, 120, 215);

    /// <summary>
    /// Selection box thickness.
    /// </summary>
    public float SelectionThickness { get; set; } = 2f;

    /// <summary>
    /// Enable dirty rectangle optimization.
    /// </summary>
    public bool UseDirtyRectangle { get; set; } = true;

    /// <summary>
    /// Enable object culling (don't render objects outside viewport).
    /// </summary>
    public bool EnableCulling { get; set; } = true;

    /// <summary>
    /// Frame rate limit (0 = unlimited).
    /// </summary>
    public int TargetFrameRate { get; set; } = 60;
}
