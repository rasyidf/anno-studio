using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;

namespace PresetParser.Parsers;

/// <summary>
/// Parser for Anno 117 (Anno 8) game data.
/// Reads building presets from extracted XML asset files.
/// 
/// Data sources (from game installation):
/// - assets.xml: All asset definitions (buildings, items, etc.)
/// - templates.xml: Template inheritance
/// - texts_*.xml: Localization (6 languages)
/// - *.ifo files: BuildBlocker dimensions
/// </summary>
public class Anno117Parser : GameParserBase
{
    public override string Version => "117";
    private const string Header = "(A8) Anno 117";

    // Templates that represent placeable buildings
    private static readonly HashSet<string> BuildingTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "ResidenceBuilding", "Production", "Production Field", "Production Area",
        "Warehouse", "PublicServiceBuilding", "CityInstitutionBuilding",
        "Monument", "MilitaryWall", "MilitaryGate", "MilitaryTowerUnit",
        "SlotFactoryBuilding7", "AqueductProducer", "AqueductConnector",
        "AqueductDistributor", "VillaUrban", "GuestHouse", "Module Field"
    };

    // Templates to exclude (non-building assets)
    private static readonly HashSet<string> ExcludeTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssetPool", "Trigger", "Quest", "Reward", "Item", "Profile",
        "ConditionPlayerCounter", "ActionUnlockAsset"
    };

    // Population level GUID → (TierName, Region, TierNumber)
    private static readonly Dictionary<string, (string Name, string Region, int Tier)> PopulationLevels = new()
    {
        ["1499"] = ("Liberti", "Roman", 1),
        ["1496"] = ("Plebeians", "Roman", 2),
        ["1497"] = ("Equites", "Roman", 3),
        ["1498"] = ("Patricians", "Roman", 4),
        ["1500"] = ("Waders", "Celtic", 1),
        ["1501"] = ("Smiths", "Celtic", 2),
        ["1503"] = ("Mercators", "Romano-Celtic", 2),
        ["1502"] = ("Aldermen", "Celtic", 3),
        ["1504"] = ("Nobles", "Romano-Celtic", 3),
    };

    private readonly Dictionary<string, Dictionary<string, string>> _localizations = new();
    private XmlDocument _assetsDoc;

    public Anno117Parser(IFileSystem fileSystem = null) : base(fileSystem) { }

    public override List<IBuildingInfo> ParseBuildings()
    {
        if (string.IsNullOrEmpty(BasePath))
            throw new InvalidOperationException("Configure() must be called before ParseBuildings().");

        Console.WriteLine($"[{Version}] Loading localizations...");
        LoadLocalizations();

        Console.WriteLine($"[{Version}] Loading assets.xml...");
        _assetsDoc = new XmlDocument();
        var assetsPath = Path.Combine(BasePath, "data", "base", "config", "export", "assets.xml");
        _assetsDoc.Load(assetsPath);

        Console.WriteLine($"[{Version}] Parsing buildings...");
        var buildings = new List<IBuildingInfo>();

        var assetNodes = _assetsDoc.SelectNodes("//Asset");
        if (assetNodes == null)
        {
            Console.WriteLine($"[{Version}] WARNING: No Asset nodes found in assets.xml");
            return buildings;
        }

        foreach (XmlNode assetNode in assetNodes)
        {
            var building = TryParseBuilding(assetNode);
            if (building != null)
            {
                buildings.Add(building);
            }
        }

        // Add shared extras
        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings.");
        return buildings;
    }

    private IBuildingInfo TryParseBuilding(XmlNode assetNode)
    {
        var templateNode = assetNode.SelectSingleNode("Template");
        if (templateNode == null) return null;

        var template = templateNode.InnerText;
        if (!BuildingTemplates.Contains(template)) return null;

        var values = assetNode.SelectSingleNode("Values");
        if (values == null) return null;

        var standard = values.SelectSingleNode("Standard");
        if (standard == null) return null;

        var guid = standard.SelectSingleNode("GUID")?.InnerText;
        var name = standard.SelectSingleNode("Name")?.InnerText;
        var iconFilename = standard.SelectSingleNode("IconFilename")?.InnerText;

        if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(name)) return null;

        // Resolve classification
        var (faction, group) = ClassifyBuilding(values, template);

        // Resolve build blocker from .ifo file or fallback
        var (sizeX, sizeZ) = ResolveBuildBlocker(values);
        if (sizeX <= 0 || sizeZ <= 0) return null; // skip dimensionless assets

        // Resolve localization via OasisId
        var localization = ResolveLocalization(values);

        // Resolve influence
        var (influenceRadius, influenceRange) = ResolveInfluence(values);

        // Resolve blocked area (coastal buildings)
        var (blockedLength, blockedWidth, direction) = ResolveBlockedArea(values);

        var building = new BuildingInfo
        {
            Header = Header,
            Faction = faction,
            Group = group,
            Template = template,
            Identifier = localization.GetValueOrDefault("eng", name),
            IconFileName = NormalizeIconPath(iconFilename),
            InfluenceRadius = influenceRadius,
            InfluenceRange = influenceRange,
            Road = false,
            Borderless = template is "MilitaryWall" or "AqueductConnector",
            Guid = int.TryParse(guid, out var g) ? g : 0,
            BlockedAreaLength = blockedLength,
            BlockedAreaWidth = blockedWidth,
            Direction = direction,
        };

        building.BuildBlocker = new SerializableDictionary<double>();
        building.BuildBlocker["x"] = sizeX;
        building.BuildBlocker["z"] = sizeZ;

        building.Localization = new SerializableDictionary<string>();
        foreach (var (lang, text) in localization)
        {
            building.Localization[lang] = text;
        }

        return building;
    }

    private (string Faction, string Group) ClassifyBuilding(XmlNode values, string template)
    {
        var buildingNode = values.SelectSingleNode("Building");
        var region = buildingNode?.SelectSingleNode("AssociatedRegions")?.InnerText ?? "Roman";

        // Determine faction from population level or template
        var residenceNode = values.SelectSingleNode("Residence7");
        if (residenceNode != null)
        {
            var popLevel = residenceNode.SelectSingleNode("PopulationLevel")?.InnerText;
            if (popLevel != null && PopulationLevels.TryGetValue(popLevel, out var level))
            {
                return ($"({level.Tier}) {level.Name}", "Residences");
            }
        }

        // Classify by template type
        var group = template switch
        {
            "Production" or "Production Area" or "SlotFactoryBuilding7" => "Production Buildings",
            "Production Field" => "Farm Buildings",
            "Module Field" => "Farm Fields",
            "PublicServiceBuilding" => "Public Buildings",
            "CityInstitutionBuilding" => "Public Buildings",
            "Monument" => "Monuments",
            "Warehouse" => "Infrastructure",
            "MilitaryWall" or "MilitaryGate" or "MilitaryTowerUnit" => "Military",
            "AqueductProducer" or "AqueductConnector" or "AqueductDistributor" => "Aqueducts",
            "VillaUrban" or "GuestHouse" => "Special Buildings",
            _ => "Other"
        };

        // Determine faction tier from unlock conditions or default to region
        var faction = $"({RegionToNumber(region)}) {region}";
        return (faction, group);
    }

    private static int RegionToNumber(string region) => region switch
    {
        "Roman" => 1,
        "Celtic" => 2,
        "Romano-Celtic" => 3,
        _ => 9
    };

    private (double X, double Z) ResolveBuildBlocker(XmlNode values)
    {
        // Try to get from Object > Variations > Item > Filename → .ifo file
        var objectNode = values.SelectSingleNode("Object");
        var cfgPath = objectNode?.SelectSingleNode("Variations/Item/Filename")?.InnerText;

        if (!string.IsNullOrEmpty(cfgPath))
        {
            // Use BuildingBlockProvider to extract size from .ifo file
            // Create a temporary BuildingInfo to receive the extracted values
            var tempBuilding = new BuildingInfo();
            tempBuilding.BuildBlocker = new SerializableDictionary<double>();

            if (BuildingBlockProvider.GetBuildingBlocker(BasePath, tempBuilding, cfgPath, Version))
            {
                var x = tempBuilding.BuildBlocker["x"];
                var z = tempBuilding.BuildBlocker["z"];
                if (x > 0 && z > 0) return (x, z);
            }
        }

        // Fallback: try BuildBlocker node if present in XML
        var blockerNode = values.SelectSingleNode("Building/BuildBlocker");
        if (blockerNode != null)
        {
            var x = double.TryParse(blockerNode.SelectSingleNode("x")?.InnerText, out var bx) ? bx : 0;
            var z = double.TryParse(blockerNode.SelectSingleNode("z")?.InnerText, out var bz) ? bz : 0;
            if (x > 0 && z > 0) return (x, z);
        }

        return (0, 0);
    }

    private (int Radius, int Range) ResolveInfluence(XmlNode values)
    {
        // EffectSource → RadiusDistance or StreetDistance
        var effectSource = values.SelectSingleNode("EffectSource");
        if (effectSource != null)
        {
            var radius = int.TryParse(effectSource.SelectSingleNode("RadiusDistance")?.InnerText, out var r) ? r : 0;
            var street = int.TryParse(effectSource.SelectSingleNode("StreetDistance")?.InnerText, out var s) ? s : 0;
            return (radius, street);
        }

        // FreeAreaProductivity → InfluenceRadius
        var freeArea = values.SelectSingleNode("FreeAreaProductivity");
        if (freeArea != null)
        {
            var radius = int.TryParse(freeArea.SelectSingleNode("InfluenceRadius")?.InnerText, out var r) ? r : 0;
            return (radius, 0);
        }

        return (0, 0);
    }

    private (double Length, double Width, GridDirection Direction) ResolveBlockedArea(XmlNode values)
    {
        var buildingNode = values.SelectSingleNode("Building");
        var terrainType = buildingNode?.SelectSingleNode("TerrainType")?.InnerText;

        // Coastal buildings have a blocked area on their water side
        if (terrainType is "Water_Including_Coast" or "Coast")
        {
            // ponytail: Default blocked area for coastal buildings. 
            // Upgrade path: extract exact values from template definitions.
            return (3, 0, GridDirection.Down);
        }

        return (0, 0, GridDirection.Down);
    }

    private Dictionary<string, string> ResolveLocalization(XmlNode values)
    {
        var result = new Dictionary<string, string>();
        var textNode = values.SelectSingleNode("Text");
        var oasisId = textNode?.SelectSingleNode("OasisId")?.InnerText;

        if (!string.IsNullOrEmpty(oasisId) && _localizations.ContainsKey(oasisId))
        {
            return _localizations[oasisId];
        }

        // Fallback to Name from Standard
        var name = values.SelectSingleNode("Standard/Name")?.InnerText ?? "";
        foreach (var lang in Languages)
        {
            result[lang] = name;
        }

        return result;
    }

    private void LoadLocalizations()
    {
        var locPath = Path.Combine(BasePath, "data", "config", "base", "localizations");

        for (int i = 0; i < Languages.Length; i++)
        {
            var langFile = Path.Combine(locPath, $"texts_{LanguageFilesModern[i]}.xml");
            if (!FileSystem.File.Exists(langFile))
            {
                Console.WriteLine($"[{Version}] WARNING: Language file not found: {langFile}");
                continue;
            }

            var doc = new XmlDocument();
            doc.Load(langFile);

            // Parse localization entries — format: <Text><GUID>id</GUID><Text>value</Text></Text>
            // or OasisId-based: <ModOp GUID="oasisId" ...><Text>value</Text></ModOp>
            var textNodes = doc.SelectNodes("//Texts/Text");
            if (textNodes != null)
            {
                foreach (XmlNode textNode in textNodes)
                {
                    var id = textNode.SelectSingleNode("GUID")?.InnerText
                          ?? textNode.SelectSingleNode("OasisId")?.InnerText;
                    var text = textNode.SelectSingleNode("Text")?.InnerText;

                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(text)) continue;

                    if (!_localizations.ContainsKey(id))
                        _localizations[id] = new Dictionary<string, string>();

                    _localizations[id][Languages[i]] = text;
                }
            }
        }

        Console.WriteLine($"[{Version}] Loaded {_localizations.Count} localization entries.");
    }

    private static string NormalizeIconPath(string iconFilename)
    {
        if (string.IsNullOrEmpty(iconFilename)) return null;

        // Extract just the filename without path and extension for icon lookup
        var fileName = Path.GetFileNameWithoutExtension(iconFilename);

        // Add A8_ prefix for Anno 117 icons (matching the convention for other games)
        return $"A8_{fileName}";
    }
}
