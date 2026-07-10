namespace AnnoDesigner.Models;

/// <summary>
/// Settings for image export operations.
/// </summary>
public class ExportSettings
{
    /// <summary>Whether to use the current zoom level (vs. default grid size).</summary>
    public bool UseCurrentZoom { get; init; }

    /// <summary>Whether to render selection highlights.</summary>
    public bool RenderSelectionHighlights { get; init; }

    /// <summary>Whether to render the layout version text.</summary>
    public bool RenderVersion { get; init; }

    /// <summary>Whether the statistics panel is visible and should be included.</summary>
    public bool RenderStatistics { get; init; }
}
