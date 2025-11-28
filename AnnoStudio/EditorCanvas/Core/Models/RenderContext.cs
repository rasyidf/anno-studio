using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Context information for rendering operations.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Current viewport transformation.
    /// </summary>
    public ViewportTransform Viewport { get; init; } = new();

    /// <summary>
    /// Rendering settings.
    /// </summary>
    public RenderSettings Settings { get; init; } = new();

    /// <summary>
    /// Grid size for reference.
    /// </summary>
    public float GridSize { get; init; } = 16f;

    /// <summary>
    /// Current selection service.
    /// </summary>
    public ISelectionService? Selection { get; init; }

    /// <summary>
    /// Visible viewport rectangle in canvas coordinates.
    /// </summary>
    public SKRect VisibleRect { get; set; }

    /// <summary>
    /// Whether rendering is for export/print (affects quality).
    /// </summary>
    public bool IsExporting { get; set; } = false;

    /// <summary>
    /// Delta time since last frame (for animations).
    /// </summary>
    public float DeltaTime { get; set; }
}
