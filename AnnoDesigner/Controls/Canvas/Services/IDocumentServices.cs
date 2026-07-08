
using System;
using AnnoDesigner.Controls.Canvas.Services.Contracts;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services.Undo;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Provides scoped and shared services for a single document instance.
    /// </summary>
    public interface IDocumentServices : IDisposable
    {
        // Scoped services (unique per document)
        IUndoManager CreateUndoManager();

        // Shared services (singleton across application)
        IAppSettings AppSettings { get; }
        ICoordinateHelper CoordinateHelper { get; }
        IBrushCache BrushCache { get; }
        IPenCache PenCache { get; }
        IMessageBoxService MessageBoxService { get; }
        ILocalizationHelper LocalizationHelper { get; }
        IClipboardService ClipboardService { get; }
        ICommons Commons { get; }
        ILayoutService LayoutService { get; }
        IFileDialogService FileDialogService { get; }
    }
}
