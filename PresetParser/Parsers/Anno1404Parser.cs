using System;
using System.Collections.Generic;
using System.IO.Abstractions;
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

        var localizations = _localizationHelper.GetLocalization(
            Version, addPrefix: false, versionPaths, Languages, BasePath);

        var iconNodes = Anno1404_2070ParserHelper.LoadIconNodes(BasePath, versionPaths[Version]["icons"]);

        var helper = new Anno1404_2070ParserHelper(
            BasePath, Version, BuildingBlockProvider, IconFileNameHelper,
            ExcludeNameList, ExcludeTemplateList, excludeFactionList: null, Languages);

        var assetPaths = versionPaths[Version]["assets"];
        foreach (var p in assetPaths)
        {
            helper.ParseAssetsFile(
                BasePath + p.Path, p.XPath, p.YPath,
                buildings, iconNodes, localizations, p.InnerNameTag);
        }

        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings (helper count: {helper.BuildingCount}).");
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
}
