using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Xml;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Parser for Anno 2205 game data.
/// Uses XML language files (texts_english.xml etc.) for localizations.
/// </summary>
public class Anno2205Parser : GameParserBase
{
    public override string Version => Constants.ANNO_VERSION_2205;

    private static readonly List<string> ExcludeGUIDList =
    [
        "1001178", "1000737", "7000274", "1001175", "1000736", "7000275",
        "1000672", "1000755", "7000273", "1001171", "1000703", "7000272",
        "7000420", "7000421", "7000423", "7000424", "7000425", "7000427",
        "7000428", "7000429", "7000430", "7000431", "12000009", "12000010",
        "12000011", "12000020", "12000036", "1000063", "1000170", "1000212",
        "1000213", "1000174", "1000215", "1000217", "1000224", "1000250",
        "1000332", "1000886", "7001466", "7001467", "7001470", "7001471",
        "7001472", "7001473", "7001877", "7001878", "7001879", "7001880",
        "7001881", "7001882", "7001883", "7001884", "7001885", "7000310",
        "7000311", "7000315", "7000316", "7000313", "7000263", "7000262",
        "7000305", "7000306"
    ];

    // ponytail: language documents cached per parse run to avoid repeated file I/O
    private readonly Dictionary<string, XmlDocument> _langDocuments = new();

    public Anno2205Parser(IFileSystem fileSystem = null) : base(fileSystem) { }

    public override List<IBuildingInfo> ParseBuildings()
    {
        if (string.IsNullOrEmpty(BasePath))
            throw new InvalidOperationException("Configure() must be called before ParseBuildings().");

        Console.WriteLine($"[{Version}] Parsing language files...");
        LoadLanguageFiles();

        Console.WriteLine($"[{Version}] Parsing assets...");
        var buildings = new List<IBuildingInfo>();
        var assetPaths = GetAssetPaths();

        foreach (var p in assetPaths)
        {
            Program.ParseAssetsFile2205(BasePath + p.Path, p.XPath, p.YPath, buildings, p.InnerNameTag, Version);
        }

        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings.");
        return buildings;
    }

    private void LoadLanguageFiles()
    {
        var languageFilePath = "data/config/gui/";
        var languageFileStart = "texts_";

        for (int i = 0; i < Languages.Length; i++)
        {
            var fileName = BasePath + languageFilePath + languageFileStart + LanguageFilesModern[i] + ".xml";
            var doc = new XmlDocument();
            doc.Load(fileName);
            _langDocuments[Languages[i]] = doc;

            // ponytail: also set global statics for backward compat with Program.ParseBuilding2205
            // Upgrade path: pass language docs as parameter to parse methods
            switch (i)
            {
                case 0: Program.langDocument_english = doc; break;
                case 1: Program.langDocument_german = doc; break;
                case 2: Program.langDocument_french = doc; break;
                case 3: Program.langDocument_polish = doc; break;
                case 4: Program.langDocument_russian = doc; break;
                case 5: Program.langDocument_spanish = doc; break;
            }
        }
    }

    private PathRef[] GetAssetPaths()
    {
        return
        [
            new PathRef("data/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/dlc01/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/dlc02/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/dlc03/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/dlc04/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/FCP01/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
            new PathRef("data/FCP02/config/game/assets.xml", "/AssetList/Groups/Group/Groups/Group", "Groups/Group/Assets/Asset", "Standard"),
        ];
    }
}
