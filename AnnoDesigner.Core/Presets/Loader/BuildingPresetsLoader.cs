using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Presets.Models;
using NLog;

namespace AnnoDesigner.Core.Presets.Loader
{
    public class BuildingPresetsLoader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BuildingPresets Load(string pathToBuildingPresetsFile)
        {
            BuildingPresets result;
            try
            {
                result = SerializationHelper.LoadFromFile<BuildingPresets>(pathToBuildingPresetsFile);
                if (result != null)
                {
                    logger.Debug($"Loaded building presets version: {result.Version}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading the buildings.");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Loads building presets from a manifest file, merging all per-game preset files.
        /// </summary>
        public BuildingPresets LoadFromManifest(string manifestPath)
        {
            return LoadFromManifest(manifestPath, gameFilters: null);
        }

        /// <summary>
        /// Loads building presets from a manifest file, optionally filtering to specific games.
        /// </summary>
        /// <param name="manifestPath">Path to presets_manifest.json</param>
        /// <param name="gameFilters">Game names to load (e.g. "Anno 1800"). If empty/null, loads all.</param>
        public BuildingPresets LoadFromManifest(string manifestPath, params string[] gameFilters)
        {
            try
            {
                var manifest = SerializationHelper.LoadFromFile<PresetsManifest>(manifestPath);
                if (manifest == null)
                {
                    throw new InvalidOperationException("Failed to deserialize presets manifest.");
                }

                logger.Debug($"Loaded presets manifest version: {manifest.Version}, {manifest.Games.Count} game(s)");

                var manifestDir = Path.GetDirectoryName(manifestPath);
                var allBuildings = new List<BuildingInfo>();

                var entries = manifest.Games;
                if (gameFilters != null && gameFilters.Length > 0)
                {
                    var filterSet = new HashSet<string>(gameFilters, StringComparer.OrdinalIgnoreCase);
                    entries = entries.Where(e => filterSet.Contains(e.Game)).ToList();
                }

                foreach (var entry in entries)
                {
                    var gameFile = Path.Combine(manifestDir, entry.FileName);
                    var gamePresets = SerializationHelper.LoadFromFile<BuildingPresets>(gameFile);
                    if (gamePresets?.Buildings != null)
                    {
                        allBuildings.AddRange(gamePresets.Buildings);
                        logger.Debug($"Loaded {gamePresets.Buildings.Count} buildings from {entry.FileName} ({entry.Game})");
                    }
                }

                return new BuildingPresets
                {
                    Version = manifest.Version,
                    Buildings = allBuildings
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading buildings from manifest.");
                throw;
            }
        }
    }
}
