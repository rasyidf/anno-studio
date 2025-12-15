using System.Collections.Generic;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Services
{
    public interface ISharedResourceManager
    {
        BuildingPresets BuildingPresets { get; }
        Dictionary<string, AnnoDesigner.Core.Models.IconImage> Icons { get; }
        ICoordinateHelper CoordinateHelper { get; }
        IBrushCache BrushCache { get; }
        IPenCache PenCache { get; }
        ILayoutLoader LayoutLoader { get; }
    }
}
