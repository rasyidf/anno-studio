using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Models;

namespace PresetParser;

/// <summary>
/// Base class with shared parsing logic: extra presets, roads, blocking tiles, validation, output.
/// Per-game parsers inherit from this and implement <see cref="IGameParser.ParseBuildings"/>.
/// </summary>
public abstract class GameParserBase : IGameParser
{
    protected static readonly string[] Languages = ["eng", "ger", "fra", "pol", "rus", "esp"];
    protected static readonly string[] LanguageFilesModern = ["english", "german", "french", "polish", "russian", "spanish"];

    protected string BasePath { get; private set; }
    protected bool TestMode { get; private set; }
    protected IFileSystem FileSystem { get; }
    protected IconFileNameHelper IconFileNameHelper { get; }
    protected BuildingBlockProvider BuildingBlockProvider { get; }

    public abstract string Version { get; }

    protected GameParserBase(IFileSystem fileSystem = null)
    {
        FileSystem = fileSystem ?? new FileSystem();
        IconFileNameHelper = new IconFileNameHelper();
        BuildingBlockProvider = new BuildingBlockProvider(new IfoFileProvider());
    }

    public void Configure(string basePath, bool testMode = false)
    {
        // Ensure trailing directory separator
        if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
        {
            basePath += System.IO.Path.DirectorySeparatorChar;
        }

        BasePath = basePath;
        TestMode = testMode;
    }

    public abstract List<IBuildingInfo> ParseBuildings();

    /// <summary>
    /// Adds extra preset buildings for this version (manually-defined buildings not in game files).
    /// </summary>
    public void AddExtraPresets(List<IBuildingInfo> buildings)
    {
        foreach (var curExtraPreset in ExtraPresets.GetExtraPresets(Version))
        {
            IBuildingInfo buildingToAdd = new BuildingInfo
            {
                Header = curExtraPreset.Header,
                Faction = curExtraPreset.Faction,
                Group = curExtraPreset.Group,
                IconFileName = curExtraPreset.IconFileName,
                Identifier = curExtraPreset.Identifier,
                InfluenceRadius = curExtraPreset.InfluenceRadius,
                InfluenceRange = curExtraPreset.InfluenceRange,
                Template = curExtraPreset.Template,
                Road = false,
                Borderless = false,
                Guid = curExtraPreset.Guid,
            };

            buildingToAdd.BuildBlocker = new SerializableDictionary<double>();
            buildingToAdd.BuildBlocker["x"] = curExtraPreset.BuildBlockerX;
            buildingToAdd.BuildBlocker["z"] = curExtraPreset.BuildBlockerZ;

            buildingToAdd.Localization = new SerializableDictionary<string>();
            buildingToAdd.Localization["eng"] = curExtraPreset.LocaEng;
            buildingToAdd.Localization["ger"] = curExtraPreset.LocaGer;
            buildingToAdd.Localization["fra"] = curExtraPreset.LocaFra;
            buildingToAdd.Localization["pol"] = curExtraPreset.LocaPol;
            buildingToAdd.Localization["rus"] = curExtraPreset.LocaRus;
            buildingToAdd.Localization["esp"] = curExtraPreset.LocaEsp;

            Console.WriteLine("Extra Building: {0}", buildingToAdd.Identifier);
            buildings.Add(buildingToAdd);
        }
    }

    /// <summary>
    /// Adds extra road definitions shared across all versions.
    /// </summary>
    public static void AddExtraRoads(List<IBuildingInfo> buildings)
    {
        foreach (var curExtraRoad in ExtraPresets.GetExtraRoads())
        {
            IBuildingInfo buildingToAdd = new BuildingInfo
            {
                Header = curExtraRoad.Header,
                Faction = curExtraRoad.Faction,
                Group = curExtraRoad.Group,
                IconFileName = curExtraRoad.IconFileName,
                Identifier = curExtraRoad.Identifier,
                InfluenceRadius = curExtraRoad.InfluenceRadius,
                InfluenceRange = curExtraRoad.InfluenceRange,
                Template = curExtraRoad.Template,
                Road = curExtraRoad.Road,
                Borderless = curExtraRoad.Borderless,
            };

            buildingToAdd.BuildBlocker = new SerializableDictionary<double>();
            buildingToAdd.BuildBlocker["x"] = curExtraRoad.BuildBlockerX;
            buildingToAdd.BuildBlocker["z"] = curExtraRoad.BuildBlockerZ;

            buildingToAdd.Localization = new SerializableDictionary<string>();
            buildingToAdd.Localization["eng"] = curExtraRoad.LocaEng;
            buildingToAdd.Localization["ger"] = curExtraRoad.LocaGer;
            buildingToAdd.Localization["fra"] = curExtraRoad.LocaFra;
            buildingToAdd.Localization["pol"] = curExtraRoad.LocaPol;
            buildingToAdd.Localization["rus"] = curExtraRoad.LocaRus;
            buildingToAdd.Localization["esp"] = curExtraRoad.LocaEsp;

            Console.WriteLine("Extra Road Bar: {0}", buildingToAdd.Identifier);
            buildings.Add(buildingToAdd);
        }
    }

    /// <summary>
    /// Adds blocking tile definitions shared across all versions.
    /// </summary>
    public static void AddBlockingTiles(List<IBuildingInfo> buildings)
    {
        foreach (var curBlockingTile in ExtraPresets.GetBlockingTiles())
        {
            IBuildingInfo buildingToAdd = new BuildingInfo
            {
                Header = curBlockingTile.Header,
                Faction = curBlockingTile.Faction,
                Group = curBlockingTile.Group,
                IconFileName = curBlockingTile.IconFileName,
                Identifier = curBlockingTile.Identifier,
                Template = curBlockingTile.Template,
                Road = false,
                Borderless = false,
            };

            buildingToAdd.BuildBlocker = new SerializableDictionary<double>();
            buildingToAdd.BuildBlocker["x"] = curBlockingTile.BuildBlockerX;
            buildingToAdd.BuildBlocker["z"] = curBlockingTile.BuildBlockerZ;

            buildingToAdd.Localization = new SerializableDictionary<string>();
            buildingToAdd.Localization["eng"] = curBlockingTile.LocaEng;
            buildingToAdd.Localization["ger"] = curBlockingTile.LocaGer;
            buildingToAdd.Localization["fra"] = curBlockingTile.LocaFra;
            buildingToAdd.Localization["pol"] = curBlockingTile.LocaPol;
            buildingToAdd.Localization["rus"] = curBlockingTile.LocaRus;
            buildingToAdd.Localization["esp"] = curBlockingTile.LocaEsp;

            Console.WriteLine("Blocking Tile: {0}", buildingToAdd.Identifier);
            buildings.Add(buildingToAdd);
        }
    }

    /// <summary>
    /// Validates building list for duplicate identifiers.
    /// </summary>
    public static void ValidateBuildings(List<IBuildingInfo> buildings)
    {
        var knownDuplicates = new List<string>
        {
            "Logistic_02 (Warehouse I)", "Residence_Old_World", "Residence_tier02",
            "Residence_tier03", "Residence_tier04", "Residence_tier05", "Residence_tier05b",
            "Residence_New_World", "Residence_colony01_tier02", "Residence_Arctic_World",
            "Residence_arctic_tier02", "Residence_Africa_World", "Residence_colony02_tier02"
        };

        var validator = new Validator();
        var (isValid, duplicateIdentifiers) = validator.CheckForUniqueIdentifiers(buildings, knownDuplicates);

        if (!isValid)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"\n### There are duplicate identifiers ({duplicateIdentifiers.Count}) ###");
            foreach (var id in duplicateIdentifiers)
                Console.WriteLine(id);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nThere are no duplicate Identifiers.");
            Console.ResetColor();
        }
    }
}
