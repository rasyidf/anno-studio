using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using NLog;

namespace AnnoDesigner.Core.Presets.Loader
{
    public class IconLoader
    {
        private readonly FileSystem _fileSystem;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public IconLoader()
        {
            _fileSystem = new FileSystem();
        }
        public Dictionary<string, IconImage> Load(string pathToIconFolder, IconMappingPresets iconNameMapping)
        {
            // If there's no icon folder available (e.g. in test environment), return an empty collection and log a warning.
            if (string.IsNullOrWhiteSpace(pathToIconFolder) || !_fileSystem.Directory.Exists(pathToIconFolder))
            {
                logger.Warn("Icon folder not found: {0}. Returning empty icon set.", pathToIconFolder);
                return new Dictionary<string, IconImage>(StringComparer.OrdinalIgnoreCase);
            }

            Dictionary<string, IconImage> result = new Dictionary<string, IconImage>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var path in _fileSystem.Directory.EnumerateFiles(pathToIconFolder, CoreConstants.IconFolderFilter))
                {
                    var filenameWithoutExt = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrWhiteSpace(filenameWithoutExt))
                    {
                        continue;
                    }

                    var filenameWithExt = Path.GetFileName(path);

                    // try mapping to the icon translations
                    Dictionary<string, string> localizations = null;
                    if (iconNameMapping?.IconNameMappings != null)
                    {
                        var map = iconNameMapping.IconNameMappings.Find(x => string.Equals(x.IconFilename, filenameWithExt, StringComparison.OrdinalIgnoreCase));
                        if (map != null)
                        {
                            localizations = map.Localizations.Dict;
                        }
                    }

                    // add the current icon
                    result[filenameWithoutExt] = new IconImage(filenameWithoutExt, localizations, path);
                }

                // sort icons by their DisplayName
                result = result.OrderBy(x => x.Value.DisplayName).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);//make sure ContainsKey is caseInSensitive
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading the icons.");
                // don't rethrow to avoid breaking tests when icons are missing/corrupted - return what we have or empty
            }

            return result;
        }
    }
}
