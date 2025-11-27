using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Grid display and behavior settings.
/// </summary>
public class GridSettings
{
    /// <summary>
    /// Size of each grid cell.
    /// </summary>
    public float GridSize { get; set; } = 16f;

    /// <summary>
    /// Grid display mode.
    /// </summary>
    public GridDisplayMode DisplayMode { get; set; } = GridDisplayMode.Crosses;

    /// <summary>
    /// Grid line color.
    /// </summary>
    public SKColor Color { get; set; } = SKColors.Gray;

    /// <summary>
    /// Grid opacity (0.0 - 1.0).
    /// </summary>
    public float Opacity { get; set; } = 0.3f;

    /// <summary>
    /// Whether snap-to-grid is enabled.
    /// </summary>
    public bool SnapEnabled { get; set; } = true;

    /// <summary>
    /// Grid offset from origin.
    /// </summary>
    public SKPoint Offset { get; set; } = SKPoint.Empty;

    /// <summary>
    /// Major grid line interval (0 = disabled).
    /// </summary>
    public int MajorGridInterval { get; set; } = 5;

    /// <summary>
    /// Major grid line color.
    /// </summary>
    public SKColor MajorGridColor { get; set; } = SKColors.DarkGray;

    /// <summary>
    /// Major grid line thickness.
    /// </summary>
    public float MajorGridThickness { get; set; } = 1.5f;

    /// <summary>
    /// Minor grid line thickness.
    /// </summary>
    public float MinorGridThickness { get; set; } = 1f;
}

/// <summary>
/// Grid display modes.
/// </summary>
public enum GridDisplayMode
{
    /// <summary>
    /// No grid displayed.
    /// </summary>
    None,

    /// <summary>
    /// Grid as dots at intersections.
    /// </summary>
    Dots,

    /// <summary>
    /// Grid as horizontal and vertical lines.
    /// </summary>
    Lines,

    /// <summary>
    /// Grid as crosses at intersections.
    /// </summary>
    Crosses
}
