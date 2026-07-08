using System;
using Microsoft.Extensions.DependencyInjection;
using AnnoDesigner.Controls.Canvas.Services.Contracts;
using AnnoDesigner.Services.Undo;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Models;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IDocumentServices"/> backed by an <see cref="IServiceScope"/>.
    /// Disposing this instance will dispose the underlying scope.
    /// </summary>
    internal class DocumentServices : IDocumentServices
    {
        private readonly IServiceScope _scope;

        public DocumentServices(IServiceScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public IUndoManager CreateUndoManager()
        {
            return _scope.ServiceProvider.GetRequiredService<IUndoManager>();
        }

        public IAppSettings AppSettings => _scope.ServiceProvider.GetRequiredService<IAppSettings>();
        public ICoordinateHelper CoordinateHelper => _scope.ServiceProvider.GetRequiredService<ICoordinateHelper>();
        public IBrushCache BrushCache => _scope.ServiceProvider.GetRequiredService<IBrushCache>();
        public IPenCache PenCache => _scope.ServiceProvider.GetRequiredService<IPenCache>();
        public IMessageBoxService MessageBoxService => _scope.ServiceProvider.GetRequiredService<IMessageBoxService>();
        public ILocalizationHelper LocalizationHelper => _scope.ServiceProvider.GetRequiredService<ILocalizationHelper>();
        public IClipboardService ClipboardService => _scope.ServiceProvider.GetRequiredService<IClipboardService>();
        public ICommons Commons => _scope.ServiceProvider.GetRequiredService<ICommons>();

        // LayoutService is a small helper that wraps ILayoutLoader into async save operations
        public ILayoutService LayoutService => _scope.ServiceProvider.GetRequiredService<ILayoutService>();

        public IFileDialogService FileDialogService => _scope.ServiceProvider.GetService<IFileDialogService>();

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
