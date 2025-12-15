using System;
using System.Collections.Generic;
using System.IO;
using AnnoDesigner.Core.Presets.Loader;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Core;

namespace AnnoDesigner.Services
{
    public class SharedResourceManager : ISharedResourceManager
    {
        public SharedResourceManager(
            ILayoutLoader layoutLoader = null,
            ICoordinateHelper coordinateHelper = null,
            IBrushCache brushCache = null,
            IPenCache penCache = null)
        {
            LayoutLoader = layoutLoader ?? new LayoutLoader();
            CoordinateHelper = coordinateHelper ?? new AnnoDesigner.Helper.CoordinateHelper();
            BrushCache = brushCache ?? new AnnoDesigner.Helper.BrushCache();
            PenCache = penCache ?? new AnnoDesigner.Helper.PenCache();

            // Load building presets and icons once for the app
            try
            {
                var loader = new BuildingPresetsLoader();
                BuildingPresets = loader.Load(Path.Combine(App.ApplicationPath, CoreConstants.PresetsFiles.BuildingPresetsFile));
            }
            catch
            {
                BuildingPresets = new BuildingPresets();
            }

            try
            {
                var mappingLoader = new IconMappingPresetsLoader();
                var iconNameMapping = mappingLoader.LoadFromFile(Path.Combine(App.ApplicationPath, CoreConstants.PresetsFiles.IconNameFile));
                var iconLoader = new AnnoDesigner.Core.Presets.Loader.IconLoader();
                Icons = iconLoader.Load(Path.Combine(App.ApplicationPath, Constants.IconFolder), iconNameMapping);
            }
            catch
            {
                Icons = new Dictionary<string, IconImage>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public BuildingPresets BuildingPresets { get; }

        public Dictionary<string, IconImage> Icons { get; }

        public ICoordinateHelper CoordinateHelper { get; }

        public IBrushCache BrushCache { get; }

        public IPenCache PenCache { get; }

        public ILayoutLoader LayoutLoader { get; }
    }
}
