using System;
using Microsoft.Extensions.DependencyInjection;

namespace AnnoDesigner.Services
{
    public class DocumentServicesFactory : IDocumentServicesFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DocumentServicesFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IDocumentServices CreateDocumentServices()
        {
            // create a scope so scoped services (e.g. UndoManager) have their own lifetime
            var scope = _serviceProvider.CreateScope();
            return new DocumentServices(scope);
        }
    }
}
