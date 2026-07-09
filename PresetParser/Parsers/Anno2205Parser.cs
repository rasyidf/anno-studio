using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
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

    private static readonly List<string> ExcludeNameList =
    [
        "Placeholder", "voting", "CTU Reactor 2 (decommissioned)", "CTU Reactor 3 (decommissioned)",
        "CTU Reactor 4 (decommissioned)", "CTU Reactor 5 (decommissioned)", "CTU Reactor 6 (decommissioned)",
        "CTU Reactor 2 (active!)", "CTU Reactor 3 (active!)", "CTU Reactor 4 (active!)",
        "CTU Reactor 5 (active!)", "CTU Reactor 6 (active!)", "orbit module 07 (unused)"
    ];

    private static readonly List<string> ExcludeTemplateList = ["SpacePort", "BridgeWithUpgrade", "DistributionBuilding"];

    private static readonly List<string> TestGUIDNames = ["NODOUBLES YET"];

    // ponytail: language documents cached per parse run to avoid repeated file I/O
    private readonly Dictionary<string, XmlDocument> _langDocuments = new();

    // Instance state replacing Program.* statics
    private readonly HashSet<string> _seenBuildings = new();
    private int _printTestText;

    public Anno2205Parser(IFileSystem fileSystem = null) : base(fileSystem) { }

    public override List<IBuildingInfo> ParseBuildings()
    {
        if (string.IsNullOrEmpty(BasePath))
            throw new InvalidOperationException("Configure() must be called before ParseBuildings().");

        Console.WriteLine($"[{Version}] Parsing language files...");
        LoadLanguageFiles();

        Console.WriteLine($"[{Version}] Parsing assets...");
        var buildings = new List<IBuildingInfo>();
        _seenBuildings.Clear();
        _printTestText = 0;

        var assetPaths = GetAssetPaths();

        foreach (var p in assetPaths)
        {
            ParseAssetsFile(BasePath + p.Path, p.XPath, p.YPath, buildings, p.InnerNameTag);
        }

        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings.");
        return buildings;
    }

    private void ParseAssetsFile(string filename, string xPathToBuildingsNode, string yPath, List<IBuildingInfo> buildings, string innerNameTag)
    {
        var assetsDocument = new XmlDocument();
        assetsDocument.Load(filename);
        var buildingNodes = assetsDocument.SelectNodes(xPathToBuildingsNode)
            .Cast<XmlNode>().Single(_ => _["Name"].InnerText == innerNameTag);

        foreach (var buildingNode in buildingNodes.SelectNodes(yPath).Cast<XmlNode>())
        {
            ParseBuilding(buildings, buildingNode);
        }
    }

    private void ParseBuilding(List<IBuildingInfo> buildings, XmlNode buildingNode)
    {
        // skip invalid elements
        if (buildingNode["Template"] == null)
        {
            return;
        }

        #region Get valid Building Information

        var values = buildingNode["Values"];
        var nameValue = values["Standard"]["Name"].InnerText;
        var templateValue = buildingNode["Template"].InnerText;
        var guidValue = values["Standard"]?["GUID"].InnerText;
        if (string.IsNullOrEmpty(guidValue))
        {
            guidValue = "0";
        }

        #region Skip Unused buildings in Anno Designer List
        // Skip Energy Connector Top Object (no field, nor object | 01-07-2022)
        if (guidValue is "1003535" or "1002878" or "1001410" or "13000158" or "13000424")
        {
            return;
        }

        var isExcludedName = nameValue.Contains(ExcludeNameList);
        var isExcludedTemplate = templateValue.Contains(ExcludeTemplateList);
        if (isExcludedName || isExcludedTemplate)
        {
            return;
        }

        #endregion

        #region Skip Double Database Buildings

        isExcludedName = nameValue.IsPartOf(_seenBuildings);
        if (isExcludedName)
        {
            return;
        }

        #endregion

        var buildingGuid = values["Standard"]["GUID"].InnerText;

        #region TEST SECTION OF GUID CHECK

        if (!TestMode && buildingGuid.Contains(ExcludeGUIDList))
        {
            return;
        }
        else
        {
            if (TestMode)
            {
                if (_printTestText == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Testing GUID Result :");
                    _printTestText = 1;
                }

                if (buildingGuid.Contains(ExcludeGUIDList))
                {
                    Console.WriteLine("GUID : {0} (Checked GUID)", buildingGuid);
                    Console.WriteLine("Name : {0}", nameValue);
                }
                else
                {
                    Console.WriteLine("GUID : {0} <<-- NOT IN GUID CHECK", buildingGuid);
                    Console.WriteLine("Name : {0}", nameValue);
                }
            }
        }

        #endregion

        // parse stuff
        var identifierName = values["Standard"]["Name"].InnerText;

        var factionName = buildingNode.ParentNode.ParentNode.ParentNode.ParentNode["Name"].InnerText.FirstCharToUpper();
        var groupName = buildingNode.ParentNode.ParentNode["Name"].InnerText.FirstCharToUpper();

        #region Regrouping several faction or group names

        switch (factionName)
        {
            case "Earth": factionName = "(1) Earth"; break;
            case "Arctic": factionName = "(2) Arctic"; break;
            case "Moon": factionName = "(3) Moon"; break;
            case "Tundra": factionName = "(4) Tundra"; break;
            case "Orbit": factionName = "(5) Orbit"; break;
        }

        if (identifierName == "orbit connection 01") { groupName = "Special"; }

        #endregion

        var headerName = "(A6) Anno " + Constants.ANNO_VERSION_2205;

        IBuildingInfo b = new BuildingInfo
        {
            Header = headerName,
            Faction = factionName,
            Group = groupName,
            Template = templateValue,
            Identifier = identifierName,
            Guid = Convert.ToInt32(guidValue),
        };

        // print progress
        if (!TestMode)
        {
            Console.WriteLine(b.Identifier + " -- " + b.Guid);
        }

        #endregion

        #region Get/Set InfluenceRange information

        // Head shield generation into radius parameter, on request #296
        b.InfluenceRadius = Convert.ToInt32(values?["ShieldGenerator"]?["ShieldedRadius"]?.InnerText);
        if (string.IsNullOrEmpty(Convert.ToString(b.InfluenceRadius)) || b.InfluenceRadius == 0)
        {
            b.InfluenceRadius = Convert.ToInt32(values?["Energy"]?["RadiusUsed"]?.InnerText) / 4096;
        }
        if (string.IsNullOrEmpty(Convert.ToString(b.InfluenceRadius)))
        {
            b.InfluenceRadius = 0;
        }

        #endregion

        #region Get BuildBlockers information

        if (values["Object"] != null)
        {
            if (values["Object"]?["Variations"]?.FirstChild["Filename"]?.InnerText != null)
            {
                if (!BuildingBlockProvider.GetBuildingBlocker(BasePath, b, values["Object"]["Variations"].FirstChild["Filename"].InnerText, Version))
                {
                    Console.WriteLine("-<BuidBlocker> Tag not found in Object File!");
                    return;
                }
            }
            else
            {
                Console.WriteLine("-BuildBlocker not found, skipping: Missing Object File");
                return;
            }
        }
        else
        {
            Console.WriteLine("-BuildBlocker not found, skipping: Object File Information not fount");
            return;
        }

        #endregion

        #region Get IconFilenames

        string icon = null;
        if (values["Standard"]?["IconFilename"]?.InnerText != null)
        {
            icon = values["Standard"]["IconFilename"].InnerText;
        }

        if (icon != null)
        {
            b.IconFileName = icon.Split('/').LastOrDefault().Replace("icon_", "A6_");

            if (b.Faction == "Facility Modules")
            {
                b.IconFileName = b.IconFileName.Replace(".png", "_module.png");
            }
        }
        else
        {
            b.IconFileName = null;
        }

        #endregion

        #region Get localizations

        var langNodeStartPath = "/TextExport/Texts/Text";
        var langNodeDepth = "Text";
        var languageCount = 0;
        b.Localization = new SerializableDictionary<string>();

        foreach (var language in Languages)
        {
            var langDocument = _langDocuments[language];
            var translation = "";

            // To get the right residence building inhabitants name
            if (!string.IsNullOrEmpty(values?["Residence"]?["PopulationLevel"].InnerText))
            {
                buildingGuid = values["Residence"]["PopulationLevel"].InnerText;
            }

            var translationNodes = langDocument.SelectNodes(langNodeStartPath)
                .Cast<XmlNode>().SingleOrDefault(_ => _["GUID"].InnerText == buildingGuid);

            if (translationNodes != null)
            {
                translation = translationNodes?.SelectNodes(langNodeDepth)?.Item(0).InnerText;
                if (buildingGuid == "7000422")
                {
                    if (languageCount == 0) { translation = "Storage Depot (4x4)"; }
                    if (languageCount == 1) { translation = "Lager (4x4)"; }
                    if (languageCount == 2) { translation = "Magazyn (4x4)"; }
                    if (languageCount == 3) { translation = "Хранилище (4x4)"; }
                    if (languageCount == 4) { translation = "Almacén de depósito (4x4)"; }
                }
                if (buildingGuid == "7000426")
                {
                    if (languageCount == 0) { translation = "Storage Depot (2x2)"; }
                    if (languageCount == 1) { translation = "Lager (2x2)"; }
                    if (languageCount == 2) { translation = "Magazyn (2x2)"; }
                    if (languageCount == 3) { translation = "Хранилище (2x2)"; }
                    if (languageCount == 4) { translation = "Almacén de depósito (2x2)"; }
                }

                #region Set tier numbers and measurements on the residence buildings
                if (b.Guid is 1000005 or 1000152 or 1000153 or 1000154)
                {
                    translation += " (3x3)";
                }
                if (b.Guid is 1000151 or 1000192 or 1000193 or 1000194 or 13000388)
                {
                    translation += " (6x6)";
                }
                if (b.Guid is 1000005 or 1000151 or 1000247 or 1000183 or 7000007)
                {
                    translation = "(1) " + translation;
                }
                if (b.Guid is 1000152 or 1000192 or 1000248 or 1000184 or 7000008)
                {
                    translation = "(2) " + translation;
                }
                if (b.Guid is 1000153 or 1000193)
                {
                    translation = "(3) " + translation;
                }
                if (b.Guid is 1000154 or 1000194)
                {
                    translation = "(4) " + translation;
                }
                if (b.Guid == 13000388)
                {
                    translation = "(5) " + translation;
                }
                #endregion

                if (translation == null)
                {
                    throw new InvalidOperationException("Cannot get translation, text node not found");
                }

                while (translation.Contains("GUIDNAME"))
                {
                    var nextGuid = translation[1..^1].Replace("GUIDNAME", "").Trim();
                    translationNodes = langDocument.SelectNodes(langNodeStartPath)
                        .Cast<XmlNode>().SingleOrDefault(_ => _["GUID"].InnerText == nextGuid);
                    translation = translationNodes?.SelectNodes(langNodeDepth)?.Item(0).InnerText;
                }
            }
            else
            {
                if (languageCount < 1)
                {
                    Console.WriteLine("No Translation found, it will set to Identifier.");
                }

                translation = values["Standard"]["Name"].InnerText;
            }

            b.Localization.Dict.Add(Languages[languageCount], translation);

            if (TestMode && languageCount == 0)
            {
                Console.WriteLine("ENG name: {0}", translation);

                if (translation.IsPartOf(TestGUIDNames))
                {
                    Console.WriteLine(">>------------------------------------------------------------------------<<");
                    _ = Console.ReadKey();

                    if (buildingGuid.Contains(ExcludeGUIDList))
                    {
                        return;
                    }
                }
            }

            languageCount++;
        }

        #endregion

        // set the Building GUID back to 0, as we not need to use them anymore
        b.Guid = 0;

        // ponytail: ValidateIconFile skipped — debug output only, no functional effect
        // add building to the list
        _seenBuildings.Add(values["Standard"]["Name"].InnerText);
        buildings.Add(b);
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
