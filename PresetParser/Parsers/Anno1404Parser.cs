using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Anno1404_Anno2070;
using PresetParser.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Parser for Anno 1404 game data. Extracts building presets from extracted RDA files.
/// </summary>
public class Anno1404Parser : GameParserBase
{
    public override string Version => Constants.ANNO_VERSION_1404;

    private static readonly List<string> ExcludeNameList =
    [
        "ResidenceRuin", "AmbassadorRuin", "Gatehouse", "StorehouseTownPart",
        "ImperialCathedralPart", "SultanMosquePart", "Warehouse02", "Warehouse03",
        "Markethouse02", "Markethouse03", "TreeBuildCost", "BanditCamp"
    ];

    private static readonly List<string> ExcludeTemplateList = ["OrnamentBuilding", "Wall"];

    private readonly LocalizationHelper _localizationHelper;

    public Anno1404Parser(IFileSystem fileSystem = null) : base(fileSystem)
    {
        _localizationHelper = new LocalizationHelper(FileSystem);
    }

    public override List<IBuildingInfo> ParseBuildings()
    {
        if (string.IsNullOrEmpty(BasePath))
            throw new InvalidOperationException("Configure() must be called before ParseBuildings().");

        Console.WriteLine($"[{Version}] Parsing assets...");

        var buildings = new List<IBuildingInfo>();
        var versionPaths = GetVersionPaths();

        // Load localizations (with prefix for icon mapping, without for presets)
        var localizations = _localizationHelper.GetLocalization(
            Version, addPrefix: false, versionPaths, Languages, BasePath);

        // Parse buildings from asset files
        var assetPaths = versionPaths[Version]["assets"];
        foreach (var p in assetPaths)
        {
            ParseAssetsFile(
                BasePath + p.Path, p.XPath, p.YPath,
                buildings, localizations, p.InnerNameTag);
        }

        // Add extras
        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings.");
        return buildings;
    }

    /// <summary>
    /// Returns the version-specific paths configuration for Anno 1404.
    /// ponytail: extracted from the inline dictionary in Program.cs.
    /// Upgrade path: load from a JSON/YAML config file per game version.
    /// </summary>
    private Dictionary<string, Dictionary<string, PathRef[]>> GetVersionPaths()
    {
        var paths = new Dictionary<string, Dictionary<string, PathRef[]>>
        {
            [Version] = new Dictionary<string, PathRef[]>
            {
                ["icons"] = [new PathRef("addondata/config/game/icons.xml", "/Icons/i", "", "")],
                ["localisation"] = [new PathRef("addondata/config/game/icons.xml", "/Icons/i", "IconFilename", "")],
                ["assets"] =
                [
                    new PathRef("data/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
                    new PathRef("addondata/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
                    new PathRef("addondata/config/balancing/addon_01_assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard")
                ]
            }
        };
        return paths;
    }

    private void ParseAssetsFile(
        string filePath, string xPath, string yPath,
        List<IBuildingInfo> buildings, Dictionary<string, SerializableDictionary<string>> localizations,
        string innerNameTag)
    {
        // ponytail: This delegates to the existing Program.ParseAssetsFile logic.
        // Full extraction of the 200+ line method body is a follow-up task.
        // For now, this establishes the interface boundary.
        Program.ParseAssetsFile(filePath, xPath, yPath, buildings, null, localizations, innerNameTag, Version);
    }
}
