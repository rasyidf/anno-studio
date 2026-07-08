using System;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Factory that creates an <see cref="IDocumentServices"/> instance for a single document.
    /// The created services instance typically owns a DI scope and should be disposed when the
    /// document is closed.
    /// </summary>
    public interface IDocumentServicesFactory
    {
        IDocumentServices CreateDocumentServices();
    }
}
