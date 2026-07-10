using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Shared parsing logic for Anno 1404 and Anno 2070 asset files.
/// ponytail: extracted from Program.ParseAssetsFile/ParseBuilding — same logic, instance state instead of globals.
/// Upgrade path: if versions diverge significantly, split into per-version overrides.
/// </summary>
internal sealed class Anno1404_2070ParserHelper
{
    private readonly string _basePath;
    private readonly string _annoVersion;
    private readonly BuildingBlockProvider _buildingBlockProvider;
    private readonly IconFileNameHelper _iconFileNameHelper;
    private readonly List<string> _excludeNameList;
    private readonly List<string> _excludeTemplateList;
    private readonly List<string> _excludeFactionList;
    private readonly string[] _languages;

    // Instance dedup state (replaces Program.annoBuildingLists / annoBuildingsListCount)
    private readonly List<string> _seenBuildings = [];
    private int _buildingCount;

    public Anno1404_2070ParserHelper(
        string basePath,
        string annoVersion,
        BuildingBlockProvider buildingBlockProvider,
        IconFileNameHelper iconFileNameHelper,
        List<string> excludeNameList,
        List<string> excludeTemplateList,
        List<string> excludeFactionList,
        string[] languages)
    {
        _basePath = basePath;
        _annoVersion = annoVersion;
        _buildingBlockProvider = buildingBlockProvider;
        _iconFileNameHelper = iconFileNameHelper;
        _excludeNameList = excludeNameList;
        _excludeTemplateList = excludeTemplateList;
        _excludeFactionList = excludeFactionList ?? [];
        _languages = languages;
    }

    /// <summary>
    /// Number of buildings successfully parsed (informational).
    /// </summary>
    public int BuildingCount => _buildingCount;

    /// <summary>
    /// Loads icon nodes from the version-specific icon paths.
    /// </summary>
    public static List<XmlNode> LoadIconNodes(string basePath, PathRef[] iconPaths)
    {
        var iconsDocument = new XmlDocument();
        List<XmlNode> iconNodes = [];
        foreach (var p in iconPaths)
        {
            iconsDocument.Load(basePath + p.Path);
            iconNodes.AddRange(iconsDocument.SelectNodes(p.XPath).Cast<XmlNode>());
        }
        return iconNodes;
    }

    /// <summary>
    /// Parses a single assets XML file and extracts buildings from it.
    /// </summary>
    public void ParseAssetsFile(
        string filename,
        string xPathToBuildingsNode,
        string yPath,
        List<IBuildingInfo> buildings,
        IEnumerable<XmlNode> iconNodes,
        Dictionary<string, SerializableDictionary<string>> localizations,
        string innerNameTag)
    {
        var assetsDocument = new XmlDocument();
        assetsDocument.Load(filename);
        var buildingNodes = assetsDocument.SelectNodes(xPathToBuildingsNode)
            .Cast<XmlNode>().Single(n => n["Name"].InnerText == innerNameTag);

        foreach (var buildingNode in buildingNodes.SelectNodes(yPath).Cast<XmlNode>())
        {
            ParseBuilding(buildings, buildingNode, iconNodes, localizations);
        }
    }

    private void ParseBuilding(
        List<IBuildingInfo> buildings,
        XmlNode buildingNode,
        IEnumerable<XmlNode> iconNodes,
        Dictionary<string, SerializableDictionary<string>> localizations)
    {
        // skip invalid elements
        if (buildingNode["Template"] == null)
            return;

        #region Get valid Building Information

        var values = buildingNode["Values"];
        var nameValue = values["Standard"]["Name"].InnerText;
        var templateValue = buildingNode["Template"].InnerText;

        var guidValue = values["Standard"]?["GUID"]?.InnerText;
        if (string.IsNullOrEmpty(guidValue))
            guidValue = "0";

        #region Skip Unused buildings

        var isExcludedFaction = false;

        // to get 2 buildings from OrnamentFeedbackBuilding, all other OrnamentFeedbackBuildings will be skipped
        if (templateValue == "OrnamentFeedbackBuilding" && _annoVersion == Constants.ANNO_VERSION_2070)
        {
            if (guidValue is not "7110000" and not "7110001")
                return;
            else
                templateValue = "Statistics_Building";
        }

        bool isExcludedName;
        bool isExcludedTemplate;

        if (_annoVersion == Constants.ANNO_VERSION_1404)
        {
            isExcludedName = nameValue.Contains(_excludeNameList);
            isExcludedTemplate = templateValue.Contains(_excludeTemplateList);
        }
        else // 2070
        {
            isExcludedName = nameValue.Contains(_excludeNameList);
            isExcludedTemplate = templateValue.Contains(_excludeTemplateList);

            var factionValue = buildingNode.ParentNode.ParentNode.ParentNode.ParentNode["Name"].InnerText;
            isExcludedFaction = factionValue.Contains(_excludeFactionList);
        }

        if (isExcludedName || isExcludedTemplate || isExcludedFaction)
            return;

        #endregion

        #region Skip Double Database Buildings

        if (nameValue != "underwater markethouse")
        {
            if (nameValue.IsPartOf(_seenBuildings))
                return;
        }

        var identifierName = values["Standard"]["Name"].InnerText;

        if (nameValue == "underwater markethouse" && nameValue.IsPartOf(_seenBuildings))
            identifierName = "underwater markethouse II";

        #endregion

        // Parse faction/group from XML tree
        var factionName = buildingNode.ParentNode.ParentNode.ParentNode.ParentNode["Name"].InnerText.FirstCharToUpper();
        var groupName = buildingNode.ParentNode.ParentNode["Name"].InnerText.FirstCharToUpper();

        #region Regrouping several faction and group names

        if (_annoVersion == Constants.ANNO_VERSION_1404)
        {
            if (factionName == "Farm") { factionName = "Production"; }
            if (identifierName == "Hospice") { factionName = "Public"; groupName = "Special"; }
        }
        else if (_annoVersion == Constants.ANNO_VERSION_2070)
        {
            if (factionName == "Ecos") { factionName = "(1) Ecos"; }
            if (factionName == "Tycoons") { factionName = "(2) Tycoons"; }
            if (factionName == "Techs") { factionName = "(3) Techs"; }
            if (factionName == "(3) Techs" && identifierName == "underwater markethouse II") { factionName = "Others"; }
            if (identifierName == "techs_academy") { groupName = "Public"; }
            if (identifierName == "vineyard") { identifierName = "A5_vineyard"; }
            if (groupName is "Farmfields" or "Farmfield") { groupName = "Farm Fields"; }
            if (factionName == "Others" && identifierName.Contains("black_smoker_miner") == true) { groupName = "Black Smokers (Normal)"; }
            if (factionName == "(3) Techs" && identifierName == "black_smoker_miner_platinum")
            {
                factionName = "Others";
                groupName = "Black Smokers (Deep Sea)";
            }
        }

        #endregion

        #region Set Header Name

        var headerName = _annoVersion == Constants.ANNO_VERSION_1404
            ? "(A4) Anno " + Constants.ANNO_VERSION_1404
            : "(A5) Anno " + Constants.ANNO_VERSION_2070;

        #endregion

        IBuildingInfo b = new BuildingInfo
        {
            Header = headerName,
            Faction = factionName,
            Group = groupName,
            Template = templateValue,
            Identifier = identifierName,
            Guid = Convert.ToInt32(guidValue),
        };

        // Place both statistic Buildings into Others > Statistic Buildings (anno 2070)
        if ((b.Guid == 7110000 || b.Guid == 7110001) && _annoVersion == Constants.ANNO_VERSION_2070)
        {
            b.Faction = "Others";
            b.Group = "Statistic Buildings";
        }

        // Skip unused Underwater Warehouse (3x6)
        if (_annoVersion == Constants.ANNO_VERSION_2070 && b.Guid == 10035)
            return;

        Console.WriteLine(b.Identifier + " -- " + Convert.ToString(b.Guid));

        #endregion

        #region Get Building Blocker Information
        if (!_buildingBlockProvider.GetBuildingBlocker(_basePath, b, values["Object"]["Variations"].FirstChild["Filename"].InnerText, _annoVersion))
            return;
        #endregion

        #region Get IconFilename from icons.xml

        if (iconNodes != null)
        {
            var buildingGuid = values["Standard"]["GUID"].InnerText;
            var icon = iconNodes.FirstOrDefault(n => n["GUID"].InnerText == buildingGuid);
            if (icon != null)
            {
                b.IconFileName = _iconFileNameHelper.GetIconFilename(icon["Icons"].FirstChild, _annoVersion);
            }
        }

        #endregion

        #region Get Influence Radius
        try
        {
            b.InfluenceRadius = Convert.ToInt32(values["Influence"]?["InfluenceRadius"]?.InnerText);
        }
        catch (NullReferenceException) { }
        b.InfluenceRange = 0;
        #endregion

        #region Get Translations for Building Names
        var languageCount = 0;
        var buildingGuidStr = values["Standard"]["GUID"].InnerText;
        if (localizations.TryGetValue(buildingGuidStr, out var locValue))
        {
            b.Localization = locValue;

            if (_annoVersion == Constants.ANNO_VERSION_2070)
            {
                // Add extra name to Rice Paddles for the Distillery
                if (b.Guid == 10047)
                {
                    languageCount = 0;
                    foreach (var _ in _languages)
                    {
                        b.Localization[_languages[languageCount]] = b.Localization[_languages[languageCount]] + languageCount switch
                        {
                            0 => " (Distillery)",
                            1 => " (Spirituosenfabrik)",
                            2 => " (Distillerie)",
                            3 => " (Destylarnia)",
                            4 => " (Перегонный завод)",
                            5 => " (Destilería)",
                            _ => ""
                        };
                        languageCount++;
                    }
                }

                // put tier number before translation of residences
                string tierPrefix = b.Guid switch
                {
                    10011 or 10021 or 10088 => "(1) ",
                    10013 or 10076 or 10209 => "(2) ",
                    10119 or 10116 or 40000006 => "(3) ",
                    10117 or 10118 => "(4) ",
                    _ => null
                };

                if (tierPrefix != null)
                {
                    languageCount = 0;
                    foreach (var _ in _languages)
                    {
                        b.Localization[_languages[languageCount]] = tierPrefix + b.Localization[_languages[languageCount]];
                        languageCount++;
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("No Translation found, it will be set to Identifier.");
            b.Localization = new SerializableDictionary<string>();
            var translation = values["Standard"]["Name"].InnerText;
            languageCount = 0;
            foreach (var _ in _languages)
            {
                b.Localization.Dict.Add(_languages[languageCount], translation);
                languageCount++;
            }
        }

        // removing the iconFileName for the Quay Walls (on Identifier)
        if (b.Identifier.Equals("harboursystem", StringComparison.OrdinalIgnoreCase))
            b.IconFileName = null;

        #endregion

        // GUIDs are not used in 1404 and 2070 presets
        b.Guid = 0;

        // add building to the list(s)
        _buildingCount++;
        _seenBuildings.Add(values["Standard"]["Name"].InnerText);
        buildings.Add(b);
    }
}
