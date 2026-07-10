using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Presets.Models;

namespace PresetParser
{
    /// <summary>
    /// Splits a monolithic presets.json into per-game files and writes a manifest.
    /// </summary>
    public static class SplitPresets
    {
        // ponytail: header→filename map is hardcoded; add entries here if new games appear.
        private static readonly Dictionary<string, string> HeaderToFilename = new()
        {
            ["(A4) Anno 1404"] = "presets_1404.json",
            ["(A5) Anno 2070"] = "presets_2070.json",
            ["(A6) Anno 2205"] = "presets_2205.json",
            ["(A7) Anno 1800"] = "presets_1800.json",
            ["(A8) Anno 117"] = "presets_117.json",
        };

        /// <summary>
        /// Reads a monolithic presets file, splits it by game header, and writes
        /// per-game JSON files plus a presets_manifest.json to <paramref name="outputDirectory"/>.
        /// </summary>
        public static void SplitPresetsFile(string inputPath, string outputDirectory)
        {
            var presets = SerializationHelper.LoadFromFile<BuildingPresets>(inputPath);
            if (presets?.Buildings == null || presets.Buildings.Count == 0)
            {
                Console.WriteLine("SplitPresets: No buildings found in input file.");
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            var groups = presets.Buildings.GroupBy(b => b.Header ?? string.Empty);
            var manifestEntries = new List<PresetsManifestEntry>();

            foreach (var group in groups)
            {
                if (!HeaderToFilename.TryGetValue(group.Key, out var filename))
                {
                    Console.WriteLine($"SplitPresets: Unknown header '{group.Key}', skipping {group.Count()} buildings.");
                    continue;
                }

                var splitPreset = new BuildingPresets
                {
                    Version = presets.Version,
                    Buildings = group.ToList()
                };

                var outputPath = Path.Combine(outputDirectory, filename);
                SerializationHelper.SaveToFile(splitPreset, outputPath);

                manifestEntries.Add(new PresetsManifestEntry
                {
                    Header = group.Key,
                    Filename = filename,
                    BuildingCount = splitPreset.Buildings.Count
                });

                Console.WriteLine($"SplitPresets: Wrote {splitPreset.Buildings.Count} buildings to {filename}");
            }

            var manifest = new PresetsManifest
            {
                Version = presets.Version,
                Files = manifestEntries
            };

            var manifestPath = Path.Combine(outputDirectory, "presets_manifest.json");
            SerializationHelper.SaveToFile(manifest, manifestPath);
            Console.WriteLine($"SplitPresets: Wrote manifest with {manifestEntries.Count} entries to presets_manifest.json");
        }
    }

    public class PresetsManifest
    {
        public string Version { get; set; }
        public List<PresetsManifestEntry> Files { get; set; }
    }

    public class PresetsManifestEntry
    {
        public string Header { get; set; }
        public string Filename { get; set; }
        public int BuildingCount { get; set; }
    }
}
