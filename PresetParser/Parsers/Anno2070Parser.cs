using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Anno1404_Anno2070;
using PresetParser.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Parser for Anno 2070 game data. Shares most logic with Anno 1404 via Anno1404_2070ParserHelper.
/// </summary>
public class Anno2070Parser : GameParserBase
{
    public override string Version => Constants.ANNO_VERSION_2070;

    private static readonly List<string> ExcludeNameList =
    [
        "ruin_residence", "monument_unfinished", "town_center_variation",
        "nuclearpowerplant_destroyed", "limestone_quarry", "markethouse2",
        "markethouse3", "warehouse2", "warehouse3", "cybernatic_factory", "electronic_recycler"
    ];

    private static readonly List<string> ExcludeTemplateList = ["OrnamentBuilding", "Ark"];
    private static readonly List<string> ExcludeFactionList = ["third party"];

    private readonly LocalizationHelper _localizationHelper;

    public Anno2070Parser(IFileSystem fileSystem = null) : base(fileSystem)
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
            ExcludeNameList, ExcludeTemplateList, ExcludeFactionList, Languages);

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

    private Dictionary<string, Dictionary<string, PathRef[]>> GetVersionPaths()
    {
        var paths = new Dictionary<string, Dictionary<string, PathRef[]>>
        {
            [Version] = new Dictionary<string, PathRef[]>
            {
                ["icons"] = [new PathRef("data/config/game/icons.xml", "/Icons/i", "", "")],
                ["localisation"] = [new PathRef("data/config/game/icons.xml", "/Icons/i", "IconFilename", "")],
                ["assets"] =
                [
                    new PathRef("data/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
                    new PathRef("addondata/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard")
                ]
            }
        };
        return paths;
    }
}
