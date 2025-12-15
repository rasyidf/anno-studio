using System;
using System.Linq;
using System.Threading.Tasks;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Services
{
    public class LayoutService : ILayoutService
    {
        private readonly ILayoutLoader _layoutLoader;

        public LayoutService(ILayoutLoader layoutLoader)
        {
            _layoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
        }

        public Task SaveLayoutAsync(IAnnoCanvas canvas, string filePath)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            var layoutToSave = new LayoutFile(canvas.PlacedObjects.Select(x => x.WrappedAnnoObject));
            return Task.Run(() => _layoutLoader.SaveLayout(layoutToSave, filePath));
        }
    }
}
