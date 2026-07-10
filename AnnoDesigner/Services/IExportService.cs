using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Services;

/// <summary>
/// Handles layout export to image and clipboard operations.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Shows a save dialog and renders the current layout to an image file.
    /// </summary>
    void ExportImage(IAnnoCanvas canvas, ExportSettings settings);

    /// <summary>
    /// Copies the current layout as JSON to the clipboard.
    /// </summary>
    void CopyLayoutToClipboard(IAnnoCanvas canvas);

    /// <summary>
    /// Prepares a canvas element sized for export rendering.
    /// </summary>
    FrameworkElement PrepareCanvasForRender(
        IEnumerable<AnnoObject> placedObjects,
        IEnumerable<AnnoObject> selectedObjects,
        int border,
        CanvasRenderSetting renderSettings = null);
}
