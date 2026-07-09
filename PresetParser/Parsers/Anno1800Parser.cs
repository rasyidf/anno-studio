using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Xml;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Parser for Anno 1800 game data. The largest and most complex parser.
/// Uses XML language files and FileDB-based assets.
/// </summary>
public class Anno1800Parser : GameParserBase
{
    public override string Version => Constants.ANNO_VERSION_1800;

    // ponytail: language documents cached per parse run
    private readonly Dictionary<string, XmlDocument> _langDocuments = new();

    public Anno1800Parser(IFileSystem fileSystem = null) : base(fileSystem) { }

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
            Program.ParseAssetsFile1800(BasePath + p.Path, p.XPath, buildings);
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

            // ponytail: set global statics for backward compat with Program.ParseBuilding1800
            // Upgrade path: pass language docs as parameter, eliminate global state
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

    /// <summary>
    /// Returns all asset file paths for Anno 1800 (base game + all DLCs).
    /// ponytail: hardcoded for now. Upgrade path: discover DLC paths dynamically from game directory.
    /// </summary>
    private PathRef[] GetAssetPaths()
    {
        // ponytail: This is a simplified version. The full path list from Program.cs
        // includes 20+ DLC asset files. They'll be added incrementally.
        return
        [
            new PathRef("data/config/export/main/asset/assets.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc01.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc02.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc03.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc04.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc05.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc06.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc07.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc08.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc09.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc10.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc11.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
            new PathRef("data/config/export/main/asset/assets_dlc12.xml", "/AssetList/Groups/Group/Groups/Group/Groups/Group/Assets/Asset", "", ""),
        ];
    }
}
