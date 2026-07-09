using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using PresetParser.Anno1800;
using PresetParser.Anno1800.Models;
using PresetParser.Extensions;
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

    // ponytail: instance tracking lists (were Program.annoBuildingLists / anno1800IconNameLists)
    private readonly List<string> _annoBuildingLists = [];
    private readonly List<string> _anno1800IconNameLists = [];
    private int _annoBuildingsListCount = 0;

    // ponytail: DVDataList is a debug/export artifact for DuxVitae tool. Kept as instance state
    // to avoid breaking the classification logic that depends on it. Upgrade path: extract to a separate export service.
    private const string DVDataSeperator = ",";
    private readonly string[] _dvDataList = new string[100000000];

    // ponytail: farm field tracking list (was Program.farmFieldList1800)
    private readonly IList<FarmField> _farmFieldList1800 = [];

    // ponytail: PPTN tracking list (was Program.PPTNList written to file). We just track in-memory to skip duplicates.
    private readonly List<string> _pptnList = ["Text", "TrackingValue", "LandAnimal", "Bird", "Painter", "IncidentResolverUnit", "Inhabitant", "VisualObjectEditor", "VisualObject", "Prop", "SimpleVehicle", "StarterObject Enter/Leave Point",
        "RemovableWaterBlocker", "StaticEventBlockingObject", "StaticBlockingObject", "PositionMarker", "AudioSpots", "Projectile", "Collectable", "VisualQuestObject", "ScannerObject", "TradeShip", "QuestObject", "ChannelTarget", "QuestItem", "PaMSy_Base",
        "Product", "Transporter", "FeedbackParametersGlobal", "AnimalSessionDesc", "AnimalGlobalDesc", "DifficultySetup", "FireIncident", "ResolveActionCost", "IncidentCommunication", "RiotIncident", "IllnessIncident", "ExplosionIncident", "Festival",
        "StandaloneIncidentEffectConfiguration", "Trigger", "ProgressLevel", "FeatureUnlock", "MapTemplate", "NewsArticleList", "NewspaperArticle", "SimpleAsset", "Profile_Virtual_NeverOwnsObjects", "Profile_2ndParty", "Profile_3rdParty_NoDiplomacy_NoTrader",
        "QuestPool", "Quest", "Matcher", "SessionModerate", "SessionSouthAmerica", "ExpeditionEventPool", "Expedition", "ItemEffectTargetPool", "InfluenceTitleBuff", "ItemWithUI", "ActiveItem", "Audio", "GameParameter", "SwitchGroup", "StateGroup", "Video",
        "RewardPool", "MonumentEventReward", "MonumentEvent", "Notification", "InfoTip", "InfoLayerIcon", "ConstructionMenu", "IncidentOverlayConfig", "ObjectmenuResidenceScene", "ObjectmenuKontor", "ObjectmenuShipScene", "ObjectmenuVisitorHarborScene",
        "ObjectmenuCityInstitutionScene", "ObjectmenuCommuterHarbourScene", "ObjectmenuMilitary", "MaintenanceBarConfig", "ObjectMenuScenarioRuinScene", "IslandBarScene", "WorkforceMenu", "GenericPopup", "FilteredSelectionPopup", "NegotiationPopup",
        "NewspaperScene", "ValueAssetMap", "RightClickMenu", "ItemFilter", "KeywordFilter", "ItemKeywords", "StaticHelpConfig", "PlayerLogo", "Icon", "TargetGroup", "Portrait", "Seamine", "RewardItemPool", "UplayReward", "Island", "CraftingPopup",
        "TreasureMapScene", "Fertility", "Profile_3rdParty", "WorldMap", "MinimapDot", "NewspaperSpecialEditionArticle", "NewspaperImage", "Street", "IrrigationFeature", "ResearchFeature", "RiverslotFeature", "Region", "ResearchCentreScene",
        "TradeContractFeature", "ConstructionCategory", "Skin", "LandSpy", "TownhallItem", "ScenarioInformation", "SeasonFeature", "TownhallBuff", "CameraSequence", "EffectContainer", "HarbourOfficeBuff", "EcoSystemFeature", "EcoSystemBuff", "AssetPool",
        "ThirdpartyFeedback", "Fish", "IceFloe", "Herd", "Flock", "FeedbackVehicle", "FleetDummy", "CampaignUncleMansion", "ItemSpecialAction", "FeedbackBuildingGroup", "UnlockNewsTracker", "ObjectBuildNewsTracker", "OverallSatisfactionNewsTracker",
        "NeedSatisfactionNewsTracker", "IncomeBalanceNewsTracker", "WorkforceNewsTracker", "WorkforceSliderNewsTracker", "IncidentNewsTracker", "ShipBuiltNewsTracker", "MilitaryNewsTracker", "DiplomacyNewsTracker", "CityAttractivenessNewsTracker",
        "HostileTakeoverNewsTracker", "PlacementScore", "ScenarioSelectionMarker", "VehicleBuff", "ShipSpecialist", "VehicleItem", "FluffItem", "ItemSet", "SwitchChoice", "StateChoice", "StaticHelpTopic", "DivingBellObject", "VisualBuilding_NoLogic",
        "FeedbackObject", "UnlockableAsset", "ResearchSubcategory", "ProgressBalancing", "NeedsSatisfactionNews", "ObjectmenuPierScene", "AirShip", "RecipeList", "Recipe", "MovingMobPicturePuzzle", "FeedbackUnitClass", "TrafficFeedbackUnit", "SinglePlayerGame",
        "Season", "Profile_1stParty_Scenario_Narrator", "Resource", "VisualSoundEmitter", "WalkableObject", "ThreeHeadedAnimal", "ScenarioRuinEco", "ScenarioLoadingScene", "ScenarioGameOverScene", "WorkArea", "DivingBellShip", "FeedbackDescription",
        "FeedbackSessionDescriptionOverwritable", "WarShip", "ForwardBuff", "CultureBuff", "ItemCategory", "CultureItem", "ItemCrafterHarbor", "ReplenishPermit", "PopulationLevel7", "PopulationGroup7", "BuildPermitGroup", "BridgeBuilding", "Godlike", "Tree", "RFX",
        "ItemCrafterBuilding", "FertilizerBaseModule", "FertilizerBaseBuilding", "102664", "102665", "140492", "174", "121", "152", "119", "167", "169", "168", "170", "77", "1010524", "122", "123", "124", "120", "126", "145", "127", "128", "1010567", "100446", "101008",
        "117108", "100439", "100440", "1010361", "102229", "102383", "102892", "102483", "102450", "102666", "102448", "100442", "1010062", "100441", "118718", "100437", "100443", "101432", "102428", "102425", "101965", "1010158", "102631", "102635", "102638", "102641",
        "102644", "102371", "102588", "142613", "2001096", "142615", "142873", "141027", "141079", "142792", "141013", "141010", "141076", "141082", "141084", "141189", "803895", "501757", "501941", "112551", "113695", "113964", "113965", "113784", "113785", "113786",
        "113787", "113788", "113789", "1010035", "1000178", "2001019", "142467", "102344", "667", "25000035", "501516", "15000005", "15000006", "15000000", "130097", "130101", "130103", "130096", "130100", "190865", "190872", "21389", "118745", "668", "137943", "689",
        "764", "963", "138793", "139107", "140037", "140043", "101293", "101294", "101295", "101290", "101291", "101292", "101254", "101255", "130237", "130236", "130238", "130239", "130291", "130240", "130241", "130242", "130243", "130244", "130246", "130248", "22395", "22374",
        "270008", "269865", "24187", "24024", "24027", "24028", "24029", "24056", "24057", "24058", "24059", "949", "680", "685", "686", "24030", "24350", "139859", "24053", "24048", "24034", "24012", "24014", "24016", "24018", "24033", "24036", "24019", "24023", "24043",
        "24044", "24054", "24164", "24114", "24151", "24153", "24154", "24155", "24159", "24160", "24162", "24163", "24087", "24086", "24061", "24064", "24065", "24068", "24078", "24079", "24081", "24082", "24100", "24107", "24108", "24109", "24113", "24195", "24196",
        "24197", "24201", "24199", "24198", "24179", "24191", "24192", "24248", "24286", "141486", "142412", "142413", "140786", "140787", "140789", "140790", "140794", "500005", "500017", "25000193", "25000194", "501007", "500904", "500908", "500910", "500913", "500912",
        "500906", "500911", "501254", "500905", "500946", "500950", "25000195", "500951", "502008", "502009", "502022", "502023", "502000", "502001", "1010278", "1010280", "1010298", "1010297", "101061", "102498", "101332", "1010547", "100418", "101415", "1010333", "1010329",
        "101251", "1010342", "101252", "1010340", "1010334", "101253", "1010338", "101062", "101258", "101257", "101259", "101323", "101324", "101325", "1010348", "133004", "269848", "269849", "269958", "269835", "25056", "138761", "139917", "139935", "24250", "101309",
        "101308", "1010257", "1010193", "1010207", "1010202", "1010208", "1010200", "133095", "140500", "140503", "140504", "140505", "140506", "142932", "140595", "140596", "140597", "141414", "141893", "500107", "25000087", "500481", "1010017", "130098", "140041",
        "140039", "114327", "114759", "114328", "24792", "24793", "25224", "24768", "25743", "25546", "25506", "24807", "101344", "101329", "101405", "101406", "101330", "118219", "118216", "118215", "118220", "118221", "118222", "118223", "118224", "118225", "118226",
        "118227", "118228", "118952", "118953", "80022", "80027", "102443", "101327", "1003240", "1003250", "1000071", "1003231", "1001799", "1001792", "1001789", "80110", "502085", "502083", "502084", "502082", "502081", "502080", "502078", "130245", "2320", "19534",
        "118236", "117659", "117660", "117661", "117662", "114331", "24794", "24798", "25003", "25019", "25020", "24795", "24800", "24801", "25064", "25350", "25508", "24802", "24806", "25330", "101303", "1010318", "1010317", "101296", "1010330", "1010331", "101311",
        "101339", "102460", "102459", "1010335", "1010336", "1010337", "100524", "1010549", "1010507", "1010270", "1010273", "501008", "502075", "502038", "502044", "125295", "24828", "24829", "100009", "100722", "1010233", "1010192", "1010195", "1010250", "1010196",
        "1010216", "1010214", "1010258", "1010251", "1010252", "1010255", "1010256", "1010239", "1010259", "114452", "114448", "114441", "114495", "114490", "24825", "24836", "24820", "24844", "24845", "24856", "24857", "24860", "24861", "25547", "25548", "25549",
        "EffectExclusiveTag", "DropGoodPopup", "ItemSearchConfig", "180023", "1049", "849", "140985", "1060", "720", "102430", "102429", "962", "ProductList", "501996", "501995", "502017", "502021", "502050", "501422", "501423", "501424", "2006", "2005", "2013", "2014",
        "1379", "1797", "1798", "130260", "130247", "130261", "502034", "502027", "502067", "1000029", "192484", "192483", "192482", "191788", "191789", "191790", "191750", "191751", "191752", "191753", "191754", "191755", "190675", "190676", "191006", "190269", "191008",
        "191007", "191009", "191010", "191572", "192468", "192450", "190693", "190724", "190722", "190723", "190410", "190725", "190653", "190656", "191312", "191313", "190760", "190757", "190759", "191463", "191581", "191582", "191387", "191466", "190818", "190819",
        "190820", "192305", "190826", "190824", "190891", "190892", "190893", "190913", "190861", "1945", "4602", "4603", "4616", "4617", "4618", "100817", "1988", "535", "536", "4267", "2280", "1361", "2279", "2047", "2048", "102931", "103608", "103406", "103414",
        "103415", "103416", "103417", "103419", "103423", "103425", "103429", "103430", "103610", "103612", "103613", "103614", "103615", "103619", "103620", "103621", "1384", "2226", "2232", "2281", "117302", "117303", "116175", "116173", "116186", "116189", "140788",
        "140790", "140792", "140795", "142344", "142345", "142346", "142347", "142348", "142349", "24525", "24526", "24527", "24528", "141531", "141532", "141533", "141530", "500907", "502072", "501957", "2287", "2284", "102319", "2038", "4619", "4620", "4621", "4622",
        "4623", "3761", "3661", "692", "693", "695", "635", "835", "1418", "1308", "1353", "906", "538", "966", "4254", "103643", "103645", "103646", "103647", "103648", "103649", "103650", "103651", "103652", "103653", "114166", "110942", "110943", "110944",
        "110950", "110948", "110938", "110936", "110937", "111179", "111040", "111039", "111038", "111034", "111033", "111032", "111028", "111027", "111026", "111020", "111019", "111018", "1010218", "1010210", "1654", "1655", "1058", "1059", "112518", "2397", "2400",
        "ItemTransferFeature", "ScenarioWorkshopItem", "ScenarioWorkshopPackage"
    ];

    #region Static Exclude/Include Lists

    private static readonly List<string> IncludeBuildingsTemplateNames1800 = ["ResidenceBuilding", "ResidenceBuilding7", "FarmBuilding", "FreeAreaBuilding", "FactoryBuilding7", "HeavyFactoryBuilding",
        "SlotFactoryBuilding7", "Farmfield", "OilPumpBuilding", "PublicServiceBuilding", "CityInstitutionBuilding", "CultureBuilding", "Market", "Warehouse", "PowerplantBuilding", "HarborOffice", "HarborWarehouse7",
        "HarborDepot", "Shipyard", "HarborBuildingAttacker", "RepairCrane", "HarborLandingStage7", "VisitorPier", "WorkforceConnector", "Guildhouse", "OrnamentalBuilding", "CultureModule", "Palace", "BuffFactory",
        "BuildPermitBuilding", "BuildPermitModules", "OrnamentalModule", "IrrigationPropagationSource", "ResearchCenter", "Dockland", "HarborOrnament", "Restaurant", "Busstop", "Multifactory", "FreeAreaRecipeBuilding",
        "Mall", "CultureModule", "Hacienda", "Heater_Arctic", "Monument", "HarborWarehouseStrategic", "WorkAreaRiverBuilding", "Slot", "WorkAreaSlot", "AdditionalModule", "RecipeFarm", "ItemWithUICrafting",
        "PostBoxBuildingWithDepot", "PostBoxBuildingWithPublicService", "AirshipPlatform", "AirshipPlatformModuleItemTransfer", "AirshipPlatformPostModule", "AirshipPlatformModuleWorkforceTransfer",
        "AirshipPostFreeModule"
    ];

    private static readonly List<string> IncludeBuildingsTemplateGUID1800 = ["100451", "1010266", "1010343", "1010288", "101331", "1010320", "1010263", "1010372", "1010359", "1010358", "1010462",
        "1010463", "1010464", "1010275", "1010271", "1010516", "1010517", "1010519", "1000155", "101623", "1003272", "118218", "100849", "1010186", "100438", "114435", "1010371", "100516", "100517", "102449", "100783",
        "100519", "100429", "100510", "100511", "119259", "101404", "1010311", "100415", "100586", "1010540", "100515", "100784", "1010525", "101403", "100416", "1010283", "1010520", "1010310", "1010522", "1010523",
        "101263", "24657", "24658", "24652", "101280", "1010321", "1010304", "1010309", "1010308", "1010305", "1010500", "1010501", "1010504", "1010505", "1010277", "1010542", "1010546", "1010543", "101272", "100514",
        "742", "962"
    ];

    private static readonly List<string> ExcludeBuildingsGUID1800 = ["269850", "269851", "25175", "25176"];

    private static readonly List<string> ExcludeNameList1800 = ["TreePlanter_GGJ_TEST", "(Wood Field)", "(Hunting Grounds)", "(Wash House)", "Fake Ornament [test 2nd party]", "Third_party_", "CQO_",
        "CO_Tunnel_", "- Pirates", "Ai_", "AarhantLighthouseFake", "CO_Tunnel_Entrance01_Fake", "AI Version No Unlock", "Active fertility", "- Decree", "fertility", "Arctic Cook", "Arctic Builder", "Arctic Hunter",
        "Arctic Sewer", " Buff", " Seeds", "Harbour Slot (Ghost) Arctic", "Tractor_module_01 (GASOLINE TEST)", "Fuel_station_01 (GASOLINE TEST)", "StoryIsland01 Monastery Kontor", "StoryIsland02 Military Kontor",
        "StoryIsland03 Economy Kontor", "CourtOfJustice_", "Basin_Base", "- Paragon_", "setBuff", "Buff_", "Harbor_13 (Coal Storage)", "Harbor_12 (Coal Harbor)"
    ];

    #endregion

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
            ParseAssetsFile1800(BasePath + p.Path, p.XPath, buildings);
        }

        AddExtraPresets(buildings);
        AddExtraRoads(buildings);
        AddBlockingTiles(buildings);

        Console.WriteLine($"[{Version}] Parsed {buildings.Count} buildings.");
        return buildings;
    }

    /// <summary>
    /// Parses a single 1800 asset XML file and extracts building nodes.
    /// </summary>
    private void ParseAssetsFile1800(string filename, string xPathToBuildingsNode, List<IBuildingInfo> buildings)
    {
        var assetsDocument = new XmlDocument();
        assetsDocument.Load(filename);
        var buildingNodes = assetsDocument.SelectNodes(xPathToBuildingsNode).Cast<XmlNode>().ToList();

        foreach (var buildingNode in buildingNodes)
        {
            ParseBuilding1800(buildings, buildingNode, Constants.ANNO_VERSION_1800);
        }
    }

    // ponytail: This is the largest method in the codebase (~1200 LOC), migrated verbatim from Program.cs.
    // Ceiling: monolithic classification logic. Upgrade path: decompose into pipeline stages
    // (validate → classify faction/group → resolve icon → resolve localization → emit).
    private void ParseBuilding1800(List<IBuildingInfo> buildings, XmlNode buildingNode, string annoVersion)
    {
        var templateName = "";
        var factionName = "";
        var identifierName = "";
        var groupName = "";
        var headerName = "(A7) Anno " + Constants.ANNO_VERSION_1800;
        var guidNumber = 0;
        bool isExcludedName, isExcludedTemplate, isExcludedGUID, isExcludeIconName;
        var oldColor = Console.ForegroundColor;

        #region Get valid Building Information

        var values = buildingNode["Values"];
        if (!buildingNode.HasChildNodes)
        {
            return;
        }
        else
        {
            for (var i = 0; i < buildingNode.ChildNodes.Count; i++)
            {
                var firstChildName = buildingNode.ChildNodes[i].Name;
                switch (firstChildName)
                {
                    case "BaseAssetGUID": templateName = buildingNode["BaseAssetGUID"].InnerText; break;
                    case "Template": templateName = buildingNode["Template"].InnerText; break;
                    case "ScenarioBaseAssetGUID": templateName = buildingNode["ScenarioBaseAssetGUID"].InnerText; break;
                }

                if (templateName == null)
                {
                    oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--> No Template found, Building is skipped");
                    Console.ForegroundColor = oldColor;
                    return;
                }

                if (templateName.IsMatch(_pptnList))
                {
                    return;
                }

                if (!templateName.Contains(IncludeBuildingsTemplateNames1800) && !templateName.Contains(IncludeBuildingsTemplateGUID1800))
                {
                    if (!templateName.Contains(_pptnList) && !string.IsNullOrEmpty(templateName))
                    {
                        // ponytail: was writing to PPTNFile. Skipped (debug artifact). Just track.
                        _pptnList.Add(templateName);
                    }
                    return;
                }

                if (!values.HasChildNodes)
                {
                    return;
                }
            }
        }

        var guidName = values["Standard"]?["GUID"]?.InnerText;
        if (!string.IsNullOrEmpty(guidName))
        {
            isExcludedGUID = guidName.Contains(ExcludeBuildingsGUID1800);
            guidNumber = Convert.ToInt32(values["Standard"]["GUID"].InnerText);
        }
        else
        {
            isExcludedGUID = false;
        }

        if (guidNumber == 0) { return; }
        if (guidNumber is 1308 or 1353) { return; }

        isExcludedTemplate = identifierName.Contains(_pptnList);

        if (string.IsNullOrEmpty(values["Standard"]?["Name"]?.InnerText))
        {
            oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("--> Error in Identifier Name : " + guidName + " >> " + templateName + ".");
            Console.ForegroundColor = oldColor;
            return;
        }

        identifierName = values["Standard"]["Name"].InnerText.FirstCharToUpper();
        isExcludedName = identifierName.Contains(ExcludeNameList1800);

        if (isExcludedName || isExcludedTemplate || isExcludedGUID) { return; }
        if (identifierName.Contains("SA_Docklands_Orna_")) { return; }

        if (identifierName.Contains("DEPRECATED_"))
        {
            oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            identifierName = identifierName.Replace("DEPRECATED_", "");
            Console.WriteLine("--> Removed 'DEPRECATED_' to get object still in AD: ");
            Console.ForegroundColor = oldColor;
        }

        #endregion

        #region Classify faction and group

        var associatedRegion = values?["Building"]?["AssociatedRegions"]?.InnerText ?? "";
        factionName = associatedRegion switch
        {
            "Moderate;Colony01" => "All Worlds",
            _ => associatedRegion.FirstCharToUpper(),
        };
        if (values?["Building"]?["BuildingType"]?.InnerText != null)
        {
            groupName = values["Building"]["BuildingType"].InnerText;
        }
        if (groupName == "") { groupName = "Not Placed Yet"; }

        switch (templateName)
        {
            case "BuildPermitBuilding": if (!identifierName.StartsWith("GG_OldNate")) { factionName = "Ornaments"; groupName = "13 World's Fair Rewards"; } break;
            case "Farmfield": { groupName = "Farm Fields"; break; }
            case "SlotFactoryBuilding7": { factionName = "All Worlds"; groupName = "Mining Buildings"; break; }
            case "Warehouse": { factionName = "(01) Farmers"; groupName = null; break; }
            case "HarborWarehouse7": { factionName = "Harbor"; groupName = null; break; }
            case "HarborWarehouseStrategic": { factionName = "Harbor"; groupName = "Logistics"; break; }
            case "HarborDepot": { factionName = "Harbor"; groupName = "Depots"; break; }
            case "HarborLandingStage7": { factionName = "Harbor"; groupName = null; break; }
            case "HarborBuildingAttacker": { factionName = "Harbor"; groupName = "Military"; break; }
            case "Shipyard": { factionName = "Harbor"; groupName = "Shipyards"; break; }
            case "VisitorPier": { factionName = "Harbor"; groupName = "Special Buildings"; break; }
            case "WorkforceConnector": { factionName = "Harbor"; groupName = "Special Buildings"; break; }
            case "RepairCrane": { factionName = "Harbor"; groupName = "Special Buildings"; break; }
            case "HarborOffice": { factionName = "Harbor"; groupName = "Special Buildings"; break; }
            case "PowerplantBuilding": { factionName = "Electricity"; groupName = null; break; }
            case "1010462": { templateName = "CityInstitutionBuilding"; break; }
            case "1010463": { templateName = "CityInstitutionBuilding"; break; }
            case "1010464": { templateName = "CityInstitutionBuilding"; break; }
            case "1010358": { templateName = "PublicServiceBuilding"; break; }
            case "1010359": { templateName = "PublicServiceBuilding"; break; }
            case "1010372": { templateName = "Market"; break; }
            case "1010275": { templateName = "Farmfield"; groupName = "Farm Fields"; break; }
            case "1010263": { templateName = "FarmBuilding"; break; }
            case "1010271": { templateName = "Farmfield"; groupName = "Farm Fields"; break; }
            case "1010266": { templateName = "FreeAreaBuilding"; break; }
            case "100451": { templateName = "FactoryBuilding7"; break; }
            case "1010288": { templateName = "FactoryBuilding7"; break; }
            case "1010320": { templateName = "FactoryBuilding7"; break; }
            case "101331": { templateName = "HeavyFactoryBuilding"; break; }
            case "FarmBuilding_Arctic": { templateName = "FarmBuilding"; break; }
            case "PalaceModule": { templateName = "PalaceBuilding"; factionName = "(05) Investors"; groupName = "Palace Buildings"; break; }
            case "PalaceMinistry": { templateName = "PalaceBuilding"; factionName = "All Worlds"; groupName = "Special Buildings"; break; }
            case "1010517": { templateName = "SkyTradingPost"; factionName = "(11) Technicians"; groupName = "Public Buildings"; break; }
            case "FactoryBuilding7_BuildPermit": { factionName = "(13) Scholars"; groupName = "Permitted Buildings"; break; }
            case "HarborOrnament": { factionName = "Ornaments"; groupName = "22 Docklands Ornaments"; break; }
            default: { groupName = templateName.FirstCharToUpper(); break; }
        }

        if (groupName == "Farm Fields")
        {
            if (factionName == "Moderate") { factionName = "(06) Old World Fields"; groupName = null; }
            if (factionName == "All Worlds") { factionName = "(06) Old World Fields"; groupName = null; }
            if (factionName == "Colony01") { factionName = "(09) New World Fields"; groupName = null; }
            if (factionName == "Arctic") { factionName = "(12) Arctic Farm Fields"; groupName = null; }
            if (factionName == "Africa") { factionName = "(16) Enbesa Farm Fields"; groupName = null; }
            if (factionName == "Africa;Colony01") { factionName = "(16) Enbesa Farm Fields"; groupName = null; }
        }

        if (factionName == "Moderate" && identifierName == "Fuel_station_01 (FuelStation)") { identifierName = "Moderate_fuel_station_01 (FuelStation)"; }

        // ponytail: large identifier-based classification switch, copied verbatim from Program.cs
        switch (identifierName)
        {
            case "Silo (Grain)": { factionName = "(06) Old World Fields"; groupName = null; break; }
            case "Tractor_module_01 (Tractor)": { factionName = "(06) Old World Fields"; groupName = null; break; }
            case "Farm Fertilizer Module Moderate": { factionName = "(06) Old World Fields"; groupName = null; break; }
            case "Silo (Corn)": { factionName = "(09) New World Fields"; groupName = null; break; }
            case "Colony01_tractor_module_01 (Tractor)": { factionName = "(09) New World Fields"; groupName = null; break; }
            case "Farm Fertilizer Module Colony01": { factionName = "(09) New World Fields"; groupName = null; break; }
            case "Africa_silo (Teff)": { factionName = "(16) Enbesa Farm Fields"; groupName = null; break; }
            case "Africa_tractor_module_01 (Tractor)": { factionName = "(16) Enbesa Farm Fields"; groupName = null; break; }
            case "Farm Fertilizer Module Africa": { factionName = "(16) Enbesa Farm Fields"; groupName = null; break; }
            case "Entertainment_musicpavillion_empty": { factionName = "Attractiveness"; groupName = null; break; }
            case "Culture_01 (Zoo)": { factionName = "Attractiveness"; groupName = null; break; }
            case "Culture_02 (Museum)": { factionName = "Attractiveness"; groupName = null; break; }
            case "Culture_03 (BotanicalGarden)": { factionName = "Attractiveness"; groupName = null; break; }
            case "Monument_01_00": { factionName = "Attractiveness"; groupName = null; break; }
            case "Culture_1x1_plaza": { factionName = "Attractiveness"; groupName = "Modules"; break; }
            case "Residence_tier01": { factionName = "(01) Farmers"; identifierName = "Residence_Old_World"; groupName = "Residence"; break; }
            case "Residence_colony01_tier01": { factionName = "(07) Jornaleros"; identifierName = "Residence_New_World"; groupName = "Residence"; templateName = "ResidenceBuilding7"; break; }
            case "Residence_arctic_tier01": { factionName = "(10) Explorers"; identifierName = "Residence_Arctic_World"; groupName = "Residence"; break; }
            case "Residence_colony02_tier01": { factionName = "(14) Shepherds"; identifierName = "Residence_Africa_World"; groupName = "Residence"; templateName = "ResidenceBuilding7"; break; }
            case "Coastal_03 (Quartz Sand Coast Building)": { factionName = "All Worlds"; groupName = "Mining Buildings"; break; }
            case "Mining_arctic_02 (Gold Mine)": { factionName = "All Worlds"; groupName = "Mining Buildings"; break; }
            case "Electricity_03 (Gas Power Plant)": { factionName = "(05) Investors"; groupName = "Public Buildings"; break; }
            case "Event_ornament_historyedition": { factionName = "Ornaments"; groupName = "11 Special Ornaments"; break; }
            case "Hotel": { factionName = "(17) Tourists"; groupName = null; break; }
            case "Tourist_monument_00": { factionName = "(17) Tourists"; groupName = null; break; }
            case "Multifactory_Chemical_Blank": { factionName = "(17) Tourists"; groupName = null; break; }
            case "Bus Stop": { factionName = "(17) Tourists"; groupName = null; break; }
            case "HighLife_monument_00": { factionName = "(18) High Life"; groupName = null; break; }
            case "Random slot mining": { factionName = "All Worlds"; groupName = "Empty Slots"; break; }
            case "Mining_03_slot (Clay Pit Slot)": { factionName = "All Worlds"; groupName = "Empty Slots"; break; }
            case "Random slot oil pump": { factionName = "All Worlds"; groupName = "Empty Slots"; break; }
            case "Mining_arctic_01_slot (Gas Mine Slot)": { factionName = "All Worlds"; groupName = "Empty Slots"; break; }
            case "Oasis_Riverslot": { factionName = "All Worlds"; groupName = "Empty Slots"; break; }
            case "Agriculture_colony01_13 (Forestation)": { factionName = "(30) Scenario 1: Eden Burning"; groupName = "Farm Buildings"; templateName = "Scenario1"; break; }
            case "Coastal_02 (Water Purifier)": { factionName = "(30) Scenario 1: Eden Burning"; groupName = "Harbor Buildings"; templateName = "Scenario1"; break; }
        }

        if (identifierName.Contains("TouristSeason Ornament") || identifierName.Contains("TouristSeason FlowerBed"))
        { factionName = "Ornaments"; groupName = "23 Tourist Ornaments"; }

        if (groupName == "Restaurant") { factionName = "(17) Tourists"; }

        if (identifierName.Contains("HighLife Ornament") || identifierName.Contains("Fountain_system"))
        { factionName = "Ornaments"; groupName = "24 High Life Ornaments"; }

        if (identifierName.Contains("Multifactory_SA_") || identifierName.Contains("Multifactory_Manufacturer_") || identifierName.Contains("Multifactory_Assembly_") || identifierName.Contains("Multifactory_Chemical_LaqcuerColor"))
        { factionName = "(18) High Life"; groupName = "Production Buildings"; }

        if (groupName == "Mall") { factionName = "(18) High Life"; }

        if (identifierName.StartsWith("Hacienda"))
        {
            factionName = "(19) Seeds Of Change";
            switch (templateName)
            {
                case "HarborDepot": groupName = "Harbor Buildings"; break;
                case "ResidenceBuilding7_Colony": groupName = "Residences"; break;
                case "Multifactory": groupName = "Production Buildings"; break;
                case "BuffFactoryCulture": groupName = "Modules: Production"; break;
                case "OrnamentalBuilding": groupName = "Modules: Ornaments"; break;
                case "Hacienda": groupName = null; break;
            }
            if (templateName == "RecipeFarm")
            {
                if (guidNumber != 24794)
                {
                    _dvDataList[24794] = _dvDataList[24794] + "," + guidNumber;
                    return;
                }
                if (guidNumber == 24794) { groupName = "Production Buildings"; }
            }
        }
        switch (guidNumber)
        {
            case 24770: factionName = "(19) Seeds Of Change"; groupName = "Modules: Ornaments"; break;
            case 25224: factionName = "(19) Seeds Of Change"; groupName = "Modules: Ornaments"; break;
        }

        if (identifierName.Contains("TreePlanter_")) { factionName = "Orchards"; groupName = null; }

        if (identifierName.Contains("PedestrianZone") || identifierName == "Groundplane System")
        { factionName = "Ornaments"; groupName = "25 Pedestrian Zone"; }

        // Empire of the Skies
        if (guidNumber is 835 or 648 or 1345 or 1418)
        {
            factionName = "(20) Empire of the Skies"; groupName = "Production Buildings";
            if (guidNumber == 835) { templateName = "FactoryBuilding7"; }
        }
        if (guidNumber is 1372 or 1375 or 2399) { factionName = "(20) Empire of the Skies"; groupName = "Mining Buildings"; }
        if (identifierName.StartsWith("DLC11 ")) { factionName = "(20) Empire of the Skies"; groupName = "Ornaments"; }

        if (guidNumber == 112684)
        {
            _dvDataList[4260] = "4260,A7_post_office.png,Service_arctic_02 (Post Office),112684";
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("---> Building added to Replacement List (DLC11 Replacement): 3327 << " + guidNumber);
            Console.ResetColor();
            return;
        }
        if (guidNumber == 2654)
        {
            _dvDataList[2654] = "2654,A7_airship_platform_southamerica.png,airship landing platform colony01,963";
            identifierName = "airship landing platform colony01";
            templateName = "AirshipPlatform";
        }
        if (guidNumber == 4513)
        {
            _dvDataList[4259] = "4259,A7_airship_platform_post.png,Platform module post Passage,4513";
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("---> Building added to Replacement List (DLC11 Replacement): 4259 << " + guidNumber);
            Console.ResetColor();
            return;
        }

        if (guidNumber == 4260) { factionName = "(11) Technicians"; groupName = "Public Buildings"; identifierName = "Service_arctic_02 (Post Office)"; }
        if (guidNumber is 538 or 962 or 3741) { factionName = "(03) Artisans"; groupName = "Public Buildings"; }
        if (guidNumber is 3661 or 3761 or 2654) { factionName = "(08) Obreros"; groupName = "Public Buildings"; }
        if (guidNumber == 4259) { factionName = "(11) Technicians"; groupName = "Airship Platform Module"; }
        if (guidNumber is 966 or 964) { factionName = "(03) Artisans"; groupName = "Airship Platform Module"; }
        if (guidNumber is 967 or 2274 or 2276) { factionName = "(08) Obreros"; groupName = "Airship Platform Module"; }

        if (identifierName.StartsWith("Multifactory_Magazin(DropGoods)_Moderate_")) { factionName = "(03) Artisans"; }
        if (identifierName.StartsWith("Multifactory_Magazin(DropGoods)_SA")) { factionName = "(08) Obreros"; }

        if (identifierName.StartsWith("GGJ_2x2_") || identifierName.StartsWith("Eoy21_Charity"))
        { factionName = "(30) Scenario 1: Eden Burning"; groupName = "Ornaments"; templateName = "Scenario1"; }

        if ((guidNumber > 769 && guidNumber < 951 && guidNumber != 835 && !identifierName.StartsWith("Multifactory_Magazin(DropGoods)_")) || guidNumber == 686)
        {
            factionName = "(30) Scenario 1: Eden Burning";
            switch (templateName)
            {
                case "HeavyFreeAreaBuilding": groupName = "Production Buildings"; break;
                case "HeavyFactoryBuilding": groupName = "Production Buildings"; break;
                case "FactoryBuilding7": groupName = "Production Buildings"; break;
                case "101272": groupName = "Production Buildings"; break;
                case "101280": groupName = "Farm Fields"; break;
                case "101263": groupName = "Farm Buildings"; break;
            }
            templateName = "Scenario1";
        }
        if (guidNumber == 24134) { factionName = "(30) Scenario 1: Eden Burning"; groupName = "Farm Buildings"; templateName = "Scenario1"; }
        if (guidNumber == 24136) { factionName = "(30) Scenario 1: Eden Burning"; groupName = "Public Buildings"; templateName = "Scenario1"; }

        if (identifierName.Contains("Scenario02") || identifierName == "Amoniac Factory" || identifierName == "Cyanide Pool Module" || identifierName == "Cyanide Leacher" || identifierName == "SilverMint" || identifierName == "SilverSmelter")
        { factionName = "(31) Scenario 2: Seasons of Silver"; groupName = null; }

        if (identifierName.Contains("scenario03") || identifierName.Contains("Scenario03"))
        { factionName = "(32) Scenario 3: Clash of the Curiers"; groupName = null; }

        if (identifierName.StartsWith("CDLC08")) { factionName = "Ornaments"; groupName = "27 Industrial Zone"; }
        if (identifierName.StartsWith("GG_OldNate_")) { factionName = "Ornaments"; groupName = "28 Grand Gallery"; }

        var groupInfo = NewFactionAndGroup1800.GetNewFactionAndGroup1800(identifierName, factionName, groupName, templateName);
        factionName = groupInfo.Faction;
        groupName = groupInfo.Group;
        templateName = groupInfo.Template;

        if (guidNumber == 24119) { factionName = ""; groupName = "FactoryBuilding7"; }
        if (guidNumber == 24121) { factionName = ""; groupName = "FactoryBuilding7"; }
        if (guidNumber == 24124) { factionName = ""; groupName = "FactoryBuilding7"; }
        if (guidNumber == 24110) { factionName = ""; groupName = "FactoryBuilding7"; }
        if (guidNumber == 24116) { factionName = ""; groupName = "FactoryBuilding7"; }
        if (guidNumber == 24055) { factionName = ""; groupName = "FactoryBuilding7"; }

        if (factionName?.Length == 0 || factionName == "Moderate" || factionName == "Colony01" || factionName == "Arctic" || factionName == "Africa")
        { factionName = "Not Placed Yet -" + factionName; }
        if (factionName == "Meta;Moderate;Colony01;Arctic;Africa") { factionName = "Not Placed Yet -All Worlds"; }

        #endregion

        #region Sorting the Ornaments

        groupInfo = NewOrnamentsGroup1800.GetNewOrnamentsGroup1800(identifierName, factionName, groupName, templateName);
        factionName = groupInfo.Faction;
        groupName = groupInfo.Group;
        templateName = groupInfo.Template;

        if (identifierName == "Palace") { templateName = "PalaceBuilding"; factionName = "(05) Investors"; groupName = "Palace Buildings"; }

        if (templateName.Contains("Dockland"))
        {
            if (templateName != "DocklandMain") { factionName = "Harbor"; groupName = "Docklands Modules"; templateName = "DocklandsHarbor"; }
            else { factionName = "Harbor"; groupName = null; templateName = "DocklandsHarbor"; }
        }

        if (templateName == "OrnamentalBuilding")
        {
            if (identifierName.Contains("CityOrnament ")) { factionName = "Ornaments"; groupName = "20 City Lights"; }
        }
        if (templateName == "OrnamentalBuilding" && factionName == "Not Placed Yet -Africa") { factionName = "Ornaments"; groupName = "21 Enbesa Ornaments"; }

        groupInfo = MapToTemplateName1800.GetNewOrnamentsGroup1800(identifierName, factionName, groupName, templateName);
        factionName = groupInfo.Faction;
        groupName = groupInfo.Group;
        templateName = groupInfo.Template;

        #endregion

        #region Manual DVDataList inserts

        if (identifierName == "Africa_tractor_module_02 (Harvester)") { _dvDataList[119026] = _dvDataList[119026] + DVDataSeperator + Convert.ToString(guidNumber); return; }
        if (identifierName == "Scenario02_tractor_module_02 (Harvester)") { _dvDataList[25547] = _dvDataList[25547] + DVDataSeperator + Convert.ToString(guidNumber); return; }

        #endregion



        #region Build the preset data

        IBuildingInfo b = new BuildingInfo
        {
            Header = headerName,
            Faction = factionName,
            Group = groupName,
            Template = templateName,
            Identifier = identifierName,
            Guid = guidNumber,
        };

        // DVDataList module deduplication for Zoo/Museum/Botanical/Music Pavilion
        var DVreplaceName = "A7_";
        string DVicon = null;
        if (values["Standard"]?["IconFilename"]?.InnerText != null)
        {
            DVicon = values["Standard"]["IconFilename"].InnerText;
        }
        if (DVicon != null)
        {
            var sDVIcons = DVicon.Split('/');
            DVicon = sDVIcons.LastOrDefault().StartsWith("icon_")
                ? sDVIcons.LastOrDefault().Replace("icon_", DVreplaceName)
                : DVreplaceName + sDVIcons.LastOrDefault();
            if (DVicon == "A7_Zoo module.png" && b.Guid != 100455)
            { _dvDataList[100455] = _dvDataList[100455] + DVDataSeperator + Convert.ToString(b.Guid); return; }
            if (DVicon == "A7_music_pavillion.png" && b.Guid != 113452)
            { _dvDataList[113452] = _dvDataList[113452] + DVDataSeperator + Convert.ToString(b.Guid); return; }
        }
        if ((b.Identifier.StartsWith("Culture_01_module_") || (b.Group == "CultureModule" && b.Faction == "All Worlds" && DVicon == "A7_general_module_01.png")) && b.Guid != 100455)
        { _dvDataList[100455] = _dvDataList[100455] + DVDataSeperator + Convert.ToString(b.Guid); return; }
        if (b.Identifier.StartsWith("Culture_02_module_") && b.Guid != 100454)
        { _dvDataList[100454] = _dvDataList[100454] + DVDataSeperator + Convert.ToString(b.Guid); return; }
        if (b.Identifier.StartsWith("C03_") && b.Guid != 111104)
        { _dvDataList[111104] = _dvDataList[111104] + DVDataSeperator + Convert.ToString(b.Guid); return; }

        switch (b.Guid)
        {
            case 24828: return;
            case 101267: return;
            case 100417: return;
            case 113750: return;
            case 129025: return;
            case 101516: return;
            case 102093: return;
            case 102112: return;
        }

        Console.WriteLine(b.Identifier + " - " + b.Guid);

        // Residence tier placement for DuxVitae
        switch (b.Guid)
        {
            case 1010343: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 1010344: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 1010345: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 1010346: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 1010347: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 114445: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(1) Old World"; break;
            case 101254: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(2) New World"; break;
            case 101255: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(2) New World"; break;
            case 112091: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(3) Arctic"; break;
            case 112652: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(3) Arctic"; break;
            case 114436: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(4) Enbesa"; break;
            case 114437: b.Faction = "Residences"; b.Template = "DefColDef"; b.Group = "(4) Enbesa"; break;
        }

        #endregion

        #region Get BuildBlockers

        if (values["Object"] != null)
        {
            if (values["Object"]?["Variations"]?.FirstChild["Filename"]?.InnerText != null)
            {
                if (!BuildingBlockProvider.GetBuildingBlocker(BasePath, b, values["Object"]["Variations"].FirstChild["Filename"].InnerText, annoVersion))
                { return; }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("- BuildBlocker not found, skipping: Missing Object File (B)");
                Console.ResetColor();
                return;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("- BuildBlocker not found, skipping: Object Information not found (A)");
            Console.ResetColor();
            return;
        }

        #endregion

        #region Set BlockedArea and Direction for coastal buildings

        switch (b.Identifier)
        {
            case "Coastal_01 (Fish Coast Building)": b.BlockedAreaLength = 5; b.Direction = GridDirection.Right; break;
            case "Coastal_colony01_02 (Fish Coast Building)": b.BlockedAreaLength = 5; b.Direction = GridDirection.Right; break;
            case "Coastal_arctic_02 (Seal Hunter)": b.BlockedAreaLength = 6; b.Direction = GridDirection.Right; break;
            case "Coastal_colony02_01 (Salt Coast Building)": b.BlockedAreaLength = 6; b.Direction = GridDirection.Right; break;
            case "Coastal_colony02_02 (Seafood Fisher)": b.BlockedAreaLength = 5; b.Direction = GridDirection.Right; break;
            case "Coastal_02 (Niter Coast Building)": b.BlockedAreaLength = 7; b.Direction = GridDirection.Right; break;
            case "Coastal_arctic_01 (Whale Coast Building)": b.BlockedAreaLength = 13; b.Direction = GridDirection.Right; break;
            case "Harbor_16 (Commuter Pier)": b.BlockedAreaLength = 7; b.Direction = GridDirection.Right; break;
            case "Dockland_Module_Storage": b.BlockedAreaLength = 3; b.Direction = GridDirection.Right; break;
            case "Dockland_Module_RepairCrane": b.BlockedAreaLength = 3; b.Direction = GridDirection.Right; break;
            case "Dockland_Module_SpeedUp": b.BlockedAreaLength = 3; b.Direction = GridDirection.Right; break;
            case "Harbor_02 (Sailing Shipyard)": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Harbor_03 (Steam Shipyard)": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Harbor_08 (Pier)": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Harbor_09 (tourism_pier_01)": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Kontor_imperial_01": b.BlockedAreaLength = 25; b.BlockedAreaWidth = 4.5; b.Direction = GridDirection.Right; break;
            case "Harbor_14a (Oil Harbor I)": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Dockland - Main": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Dockland_Module_Pier": b.BlockedAreaLength = 25; b.Direction = GridDirection.Right; break;
            case "Coastal_03 (Quartz Sand Coast Building)": b.BlockedAreaLength = 6; b.Direction = GridDirection.Right; break;
            case "Coastal_02 (Water Purifier)": b.BlockedAreaLength = 6; b.Direction = GridDirection.Right; break;
        }

        #endregion

        #region Icon resolution

        var replaceName = "A7_";
        string icon = null;
        if (values["Standard"]?["IconFilename"]?.InnerText != null)
        {
            icon = values["Standard"]["IconFilename"].InnerText;
        }

        if (icon != null)
        {
            var sIcons = icon.Split('/');
            icon = sIcons.LastOrDefault().StartsWith("icon_")
                ? sIcons.LastOrDefault().Replace("icon_", replaceName)
                : replaceName + sIcons.LastOrDefault();

            switch (guidName)
            {
                case "102133": icon = replaceName + "park_props_1x1_21.png"; break;
                case "102139": icon = replaceName + "park_props_1x1_27.png"; break;
                case "102140": icon = replaceName + "park_props_1x1_28.png"; break;
                case "102141": icon = replaceName + "park_props_1x1_29.png"; break;
                case "102142": icon = replaceName + "park_props_1x1_30.png"; break;
                case "102143": icon = replaceName + "park_props_1x1_31.png"; break;
                case "102131": icon = replaceName + "park_props_1x1_17.png"; break;
                case "101284": icon = replaceName + "community_lodge.png"; break;
            }
            switch (b.Identifier)
            {
                case "AmusementPark CottonCandy": icon = replaceName + "cotton_candy.png"; break;
                case "Coastal_colony02_01 (Salt Coast Building)": icon = replaceName + "salt_africa.png"; break;
                case "Random slot mining": icon = replaceName + "mineral_desposits.png"; break;
                case "Random slot oil pump": icon = replaceName + "oil.png"; break;
                case "Season4 random mining slot colony01": icon = replaceName + "mineral_desposits.png"; break;
            }
            switch (b.Guid)
            {
                case 4258: icon = replaceName + "airship_landing_plattform.png"; break;
            }

            if (icon.StartsWith("A7_spring_") || icon.StartsWith("A7_summer_") || icon.StartsWith("A7_autumn_") || icon.StartsWith("A7_winter_"))
            { b.Faction = "Ornaments"; b.Group = "26 Seasonal Decorations"; }

            b.IconFileName = icon;
        }
        else
        {
            b.IconFileName = null;
            switch (identifierName)
            {
                case "Residence_New_World": b.IconFileName = replaceName + "resident.png"; break;
                case "Agriculture_colony01_06 (Timber Yard)": b.IconFileName = replaceName + "wood_log.png"; break;
                case "Factory_colony01_01 (Timber Factory)": b.IconFileName = replaceName + "wooden_planks.png"; break;
                case "Heavy_colony01_01 (Oil Heavy Industry)": b.IconFileName = replaceName + "oil.png"; break;
                case "Processing_colony01_03 (Inlay Processing)": b.IconFileName = replaceName + "inlay.png"; break;
                case "Factory_colony01_02 (Sailcloth Factory)": b.IconFileName = replaceName + "sail.png"; break;
                case "Agriculture_colony01_09 (Cattle Farm)": b.IconFileName = replaceName + "meat_raw.png"; break;
                case "Service_colony01_01 (Marketplace)": b.IconFileName = replaceName + "market.png"; break;
                case "Service_colony01_02 (Chapel)": b.IconFileName = replaceName + "church.png"; break;
                case "Kontor_main_01": b.IconFileName = replaceName + "harbour_buildings.png"; break;
                case "Institution_colony01_01 (Police)": b.IconFileName = replaceName + "police.png"; break;
                case "Institution_colony01_02 (Fire Department)": b.IconFileName = replaceName + "fire_house.png"; break;
                case "Institution_colony01_03 (Hospital)": b.IconFileName = replaceName + "hospital.png"; break;
                case "Agriculture_colony01_11_field (Alpaca Pasture)": b.IconFileName = replaceName + "general_module_01.png"; break;
                case "Agriculture_colony01_09_field (Cattle Pasture)": b.IconFileName = replaceName + "general_module_01.png"; break;
                case "Residence_Africa_World": b.IconFileName = replaceName + "resident.png"; break;
                case "Harbor_arctic_01 (Depot)": b.IconFileName = replaceName + "depot.png"; break;
                case "Institution_colony02_02 (Police)": b.IconFileName = replaceName + "police.png"; break;
                case "Institution_colony02_03 (Hospital)": b.IconFileName = replaceName + "hospital.png"; break;
                case "Factory_colony01_05 (Brick Factory)": b.IconFileName = replaceName + "bricks.png"; break;
                case "Agriculture_colony01_12_field (Palm Tree Field)": b.IconFileName = replaceName + "coconut_palm_trees.png"; break;
            }
            switch (b.Template)
            {
                case "WorkAreaSlot": b.IconFileName = replaceName + "mineral_desposits.png"; break;
            }
            switch (b.Guid)
            {
                case 101290: b.IconFileName = replaceName + "kontor_main.png"; break;
                case 112659: b.IconFileName = replaceName + "kontor_main.png"; break;
                case 112865: b.IconFileName = replaceName + "kontor_main.png"; break;
                case 114626: b.IconFileName = replaceName + "kontor_main.png"; break;
                case 114629: b.IconFileName = replaceName + "kontor_main.png"; break;
                case 24134: b.IconFileName = replaceName + "fish_ggj_1.png"; break;
                case 24658: b.IconFileName = replaceName + "pigs.png"; break;
                case 24136: b.IconFileName = replaceName + "aqua_well.png"; break;
                case 4260: b.IconFileName = replaceName + "post_office.png"; break;
            }
        }

        // Icon fixups for Not Placed Yet buildings
        if (b.Faction.StartsWith("Not Placed Yet -"))
        {
            if (b.Identifier.Contains("(Warehouse ")) { b.IconFileName = replaceName + "warehouse.png"; }
            if (b.Identifier.Contains("(Depot)") || b.Group?.StartsWith("1010519") == true) { b.IconFileName = replaceName + "depot.png"; }
            if (b.Group is "100783" or "101403") { b.IconFileName = replaceName + "oil_habour_01.png"; }
            if (b.Group == "100519") { b.IconFileName = "A7_pier.png"; }
            if (b.Group == "100429") { b.IconFileName = "A7_visitor_harbour.png"; }
            if (b.Identifier.StartsWith("Kontor_airship_arctic_")) { b.IconFileName = replaceName + "airship_landing_plattform.png"; }
            if (b.Identifier.StartsWith("Kontor_imperial_") || b.Identifier.StartsWith("Kontor_main_")) { b.IconFileName = replaceName + "kontor_main.png"; }
            if (b.Group is "101404" or "119259") { b.IconFileName = replaceName + "oil_habour_01.png"; }
            if (b.Group == "1010311") { b.IconFileName = replaceName + "gold_ore.png"; }
            if (b.Group == "100415") { b.IconFileName = replaceName + "townhall.png"; }
            if (b.Group == "100586") { b.IconFileName = replaceName + "harbour_kontor.png"; }
            if (b.Group == "100784") { b.IconFileName = replaceName + "oil_storage.png"; }
            if (b.Group == "1010525") { b.IconFileName = replaceName + "repair_crane.png"; }
            if (b.Group == "1010516") { b.IconFileName = replaceName + "guildhouse.png"; }
            if (b.Group == "Slot" && b.Faction == "Not Placed Yet -Moderate" && b.Identifier != "Heavy_01_01_slot (Oil Pump Slot)") { b.IconFileName = replaceName + "mineral_desposits.png"; }
            if (b.Identifier == "Random slot mining arctic") { b.IconFileName = replaceName + "oil.png"; }
            if (b.Group == "1010522") { b.IconFileName = replaceName + "defense_tower_pucklegun.png"; }
            if (b.Group == "1010523") { b.IconFileName = replaceName + "defense_tower_cannon.png"; }
            if (b.Group == "1010520") { b.IconFileName = replaceName + "sail_shipyard.png"; }
        }

        if (b.Guid != 681 && ((guidNumber > 679 && guidNumber < 687) || guidNumber == 949))
        {
            factionName = "(30) Scenario 1: Eden Burning";
            b.IconFileName = replaceName + "dam_a.png";
        }

        // DLC11 ornament dedup
        var DoDLC11_OrnamentRemove = false;
        if (!string.IsNullOrEmpty(b.IconFileName))
        {
            if (b.IconFileName.Contains("_nw.png") && b.Faction == "(20) Empire of the Skies")
            {
                switch (guidNumber)
                {
                    case 3356: _dvDataList[3327] = "3327,A7_airport_cafe_ow.png,DLC11 Cafe Moderate,3356"; DoDLC11_OrnamentRemove = true; break;
                    case 3357: _dvDataList[3330] = "3330,A7_airport_cafetables_ow.png,DLC11 Cafe Tables Moderate,3357"; DoDLC11_OrnamentRemove = true; break;
                    case 3358: _dvDataList[3331] = "3331,A7_airport_clock_ow.png,DLC11 Clock Moderate,3358"; DoDLC11_OrnamentRemove = true; break;
                    case 3360: _dvDataList[3337] = "3337,A7_airport_flag_02_ow.png,DLC11 Flagpole Moderate,3360"; DoDLC11_OrnamentRemove = true; break;
                    case 3361: _dvDataList[3338] = "3338,A7_airport_seats_ow.png,DLC11 Benches Small Moderate,3361"; DoDLC11_OrnamentRemove = true; break;
                    case 3362: _dvDataList[3339] = "3339,A7_airport_seats_large_ow.png,DLC11 Benches Large Moderate,3362"; DoDLC11_OrnamentRemove = true; break;
                    case 3363: _dvDataList[3340] = "3340,A7_airport_sign_ow.png,DLC11 Sign Moderate,3363"; DoDLC11_OrnamentRemove = true; break;
                    case 3368: _dvDataList[3341] = "3341,A7_airport_arrivals_ow.png,DLC11 Gate Moderate,3368"; DoDLC11_OrnamentRemove = true; break;
                }
            }
        }
        if (DoDLC11_OrnamentRemove) { return; }

        // Icon-based identifier corrections
        var isExcludedGuidStr = Convert.ToString(b.Guid);
        switch (b.IconFileName)
        {
            case "A7_park_props_1x1_14.png": b.Identifier = "Park_1x1_statue_grass"; b.Faction = "Ornaments"; b.Group = "05 Park Statues"; break;
            case "A7_city_2x2_03.png": b.IconFileName = replaceName + "city_2x2_02.png"; break;
            case "A7_city_2x2_02.png": b.IconFileName = replaceName + "city_2x2_03.png"; break;
        }

        isExcludedName = identifierName.IsPartOf(_annoBuildingLists);
        isExcludeIconName = b.IconFileName.IsPartOf(_anno1800IconNameLists);

        if (isExcludedName || isExcludeIconName)
        {
            foreach (var DVData in _dvDataList)
            {
                if (!string.IsNullOrEmpty(DVData))
                {
                    var DVDataCheck = DVData.Split(',');
                    if (DVDataCheck.Length > 2)
                    {
                        if (b.Identifier == DVDataCheck[2] && !string.IsNullOrEmpty(DVDataCheck[2]))
                        {
                            if (b.Identifier.IsMatch(buildings))
                            {
                                var DVDataGUID = Convert.ToInt32(DVDataCheck[0]);
                                _dvDataList[DVDataGUID] = _dvDataList[DVDataGUID] + DVDataSeperator + isExcludedGuidStr;
                                return;
                            }
                        }
                        if (b.IconFileName == DVDataCheck[1] && !string.IsNullOrEmpty(DVDataCheck[1]) && isExcludedName && isExcludeIconName)
                        {
                            var DVDataGUID = Convert.ToInt32(DVDataCheck[0]);
                            _dvDataList[DVDataGUID] = _dvDataList[DVDataGUID] + DVDataSeperator + isExcludedGuidStr;
                            return;
                        }
                    }
                }
            }
        }

        #endregion



        #region Influence Radius

        b.InfluenceRadius = Convert.ToInt32(values?["FreeAreaProductivity"]?["InfluenceRadius"]?.InnerText);
        if (string.IsNullOrEmpty(Convert.ToString(b.InfluenceRadius)) || b.InfluenceRadius == 0)
        { b.InfluenceRadius = Convert.ToInt32(values?["ModuleOwner"]?["ModuleBuildRadius"]?.InnerText); }
        if (string.IsNullOrEmpty(Convert.ToString(b.InfluenceRadius)) || b.InfluenceRadius == 0)
        { b.InfluenceRadius = Convert.ToInt32(values?["ItemContainer"]?["SocketScopeRadius"]?.InnerText); }
        if (string.IsNullOrEmpty(Convert.ToString(b.InfluenceRadius)) || b.InfluenceRadius == 0)
        { b.InfluenceRadius = Convert.ToInt32(values?["BuffFactory"]?["ProductionBuffDistance"]?.InnerText); }

        switch (b.Identifier)
        {
            case "Agriculture_colony01_06 (Timber Yard)": b.InfluenceRadius = 9; break;
            case "Heavy_colony01_01 (Oil Heavy Industry)": b.InfluenceRadius = 12; break;
            case "Town hall": b.InfluenceRadius = 20; break;
            case "Guild_house_arctic": b.InfluenceRadius = 15; break;
            case "Mining_arctic_01 (Gas Mine)": b.InfluenceRadius = 10; break;
            case "DepartmentStore_Blank": b.InfluenceRadius = 45; break;
            case "Pharmacy_Blank": b.InfluenceRadius = 45; break;
            case "FurnitureStore_Blank": b.InfluenceRadius = 45; break;
            case "Harbor_07 (Repair Crane)": b.InfluenceRadius = 20; break;
            case "Dockland_Module_RepairCrane": b.InfluenceRadius = 20; break;
            case "Harbor_office": b.InfluenceRadius = 20; break;
            case "Tourist_monument_00": b.InfluenceRadius = 107; break;
        }

        #endregion

        #region Influence Range

        b.InfluenceRange = 0;
        if (b.Template == "CityInstitutionBuilding")
        {
            b.InfluenceRange = 26;
            if (b.Identifier == "Institution_arctic_01 (Ranger Station)") { b.InfluenceRange = 50; }
        }
        else if (!string.IsNullOrEmpty(values?["PublicService"]?["FullSatisfactionDistance"]?.InnerText))
        { b.InfluenceRange = Convert.ToInt32(values["PublicService"]["FullSatisfactionDistance"].InnerText); }
        else if (!string.IsNullOrEmpty(values?["BuffFactory"]?["PublicServiceData"]?["FullSatisfactionDistance"]?.InnerText))
        { b.InfluenceRange = Convert.ToInt32(values["BuffFactory"]["PublicServiceData"]["FullSatisfactionDistance"].InnerText); }
        else
        {
            switch (identifierName)
            {
                case "Service_colony01_03 (Boxing Arena)": b.InfluenceRange = 30; break;
                case "Service_colony01_01 (Marketplace)": b.InfluenceRange = 35; break;
                case "Electricity_02 (Oil Power Plant)": b.InfluenceRange = 35; break;
            }
        }
        if (templateName == "Mall" && groupName == "Mall" && identifierName.Contains("_Blank")) { b.InfluenceRange = 44; }
        if (b.Guid == 24136) { b.InfluenceRange = 18; }
        if (b.Template == "Busstop")
        {
            b.InfluenceRadius = Convert.ToInt32(values?["BusStop"]?["ActivationRadius"]?.InnerText);
            b.InfluenceRange = Convert.ToInt32(values["BusStop"]["StreetConnectionRange"].InnerText);
        }

        #endregion

        #region Localization

        string buildingGuid = guidNumber != 0 ? Convert.ToString(guidNumber) : null;
        if (!string.IsNullOrEmpty(values?["Text"]?["TextOverride"]?.InnerText))
        { buildingGuid = values["Text"]["TextOverride"].InnerText; }

        if (b.Guid == 24134) { buildingGuid = "972"; }
        if (b.Guid == 24136) { buildingGuid = "993"; }
        if (b.Guid == 112726) { buildingGuid = "4258"; }

        var langNodeStartPath = "/TextExport/Texts/Text";
        var langNodeDepth = "Text";
        var languageCount = 0;
        b.Localization = new SerializableDictionary<string>();

        foreach (var Language in Languages)
        {
            var langDocument = languageCount switch
            {
                0 => _langDocuments.GetValueOrDefault("eng"),
                1 => _langDocuments.GetValueOrDefault("ger"),
                2 => _langDocuments.GetValueOrDefault("fra"),
                3 => _langDocuments.GetValueOrDefault("pol"),
                4 => _langDocuments.GetValueOrDefault("rus"),
                5 => _langDocuments.GetValueOrDefault("esp"),
                _ => null
            };

            var translation = "";
            if (!string.IsNullOrEmpty(buildingGuid) && langDocument != null)
            {
                var translationNodes = langDocument.SelectNodes(langNodeStartPath)
                    .Cast<XmlNode>().SingleOrDefault(_ => _["GUID"].InnerText == buildingGuid);
                if (translationNodes != null)
                {
                    translation = translationNodes?.SelectNodes(langNodeDepth)?.Item(0).InnerText;
                    if (translation == null)
                    { throw new InvalidOperationException("Cannot get translation, text node not found"); }

                    while (translation.Contains("AssetData"))
                    {
                        var nextGuid = translation.Split('(', ')');
                        translationNodes = langDocument.SelectNodes(langNodeStartPath)
                            .Cast<XmlNode>().SingleOrDefault(_ => _["GUID"].InnerText == nextGuid[1]);
                        translation = translationNodes?.SelectNodes(langNodeDepth)?.Item(0).InnerText;
                    }

                    // ponytail: manual translation overrides copied verbatim. Upgrade path: data-driven translation table.
                    translation = ApplyTranslationOverrides(buildingGuid, languageCount, translation);

                    // Tier numbers for residences
                    if (b.Guid is 1010343 or 101254 or 112091 or 114436) { translation = "(1) " + translation; }
                    if (b.Guid is 1010344 or 101255 or 112652 or 114437) { translation = "(2) " + translation; }
                    if (b.Guid == 1010345) { translation = "(3) " + translation; }
                    if (b.Guid == 1010346) { translation = "(4) " + translation; }
                    if (b.Guid == 1010347) { translation = "(5) " + translation; }
                    if (b.Guid == 114445) { translation = "(6) " + translation; }
                }
                else
                {
                    if (languageCount < 1)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("---> No Translation found, it will set to Identifier.");
                        Console.ResetColor();
                    }
                    translation = values["Standard"]["Name"].InnerText;
                }

                if (translation.StartsWith("river_colony02_"))
                {
                    translation = translation.Remove(6, 12);
                    translation = translation.FirstCharToUpper();
                }

                // Farm field count appending
                if (templateName is "FarmBuilding" or "Farmfield")
                {
                    string fieldAmountValue = null;
                    string fieldGuidValue = null;
                    switch (templateName)
                    {
                        case "FarmBuilding":
                            fieldGuidValue = values["ModuleOwner"]["ConstructionOptions"]["Item"]["ModuleGUID"].InnerText;
                            fieldAmountValue = values?["ModuleOwner"]?["ModuleLimits"]?["Main"]?["Limit"]?.InnerText;
                            break;
                        case "Farmfield":
                            fieldGuidValue = values["Standard"]["GUID"].InnerText;
                            fieldAmountValue = "0";
                            break;
                    }

                    if (fieldAmountValue != null)
                    {
                        var isFieldInfoFound = false;
                        foreach (var curFieldInfo in _farmFieldList1800)
                        {
                            if (string.Equals(curFieldInfo.FieldGuid, fieldGuidValue, StringComparison.OrdinalIgnoreCase))
                            {
                                isFieldInfoFound = true;
                                fieldAmountValue = curFieldInfo.FieldAmount;
                                if (Convert.ToInt32(fieldAmountValue) <= 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine("-- > Farm field Skipped, Zero Field counter");
                                    Console.ResetColor();
                                    return;
                                }
                                break;
                            }
                        }
                        if (!isFieldInfoFound)
                        { _farmFieldList1800.Add(new FarmField() { FieldGuid = fieldGuidValue, FieldAmount = fieldAmountValue }); }
                        translation = translation + " - (" + fieldAmountValue + ")";
                    }
                }
            }
            else
            {
                translation = values["Standard"]["Name"].InnerText;
            }
            b.Localization.Dict.Add(Languages[languageCount], translation);
            languageCount++;
        }

        #endregion

        #region Final identifier renaming

        switch (b.IconFileName)
        {
            case "A7_col_park_props_system_1x1_24_back.png": b.Identifier = "Park_1x1_bush_02"; break;
            case "A7_park_props_1x1_26.png": b.Identifier = "Park_1x1_bush_03"; break;
            case "A7_col_park_props_system_2x2_03_back.png": b.Identifier = "Park_2x2_garden_02"; break;
            case "A7_col_park_props_system_3x3_02_front.png": b.Identifier = "Park_3x3_fountain_02"; break;
            case "A7_col_park_props_system_3x3_03_back.png": b.Identifier = "Park_3x3_gazebo_02"; break;
        }

        #endregion

        #region DVDataList final population and duplicate icon-based dedup

        // ponytail: DVDataList icon-based replacement logic. Copied verbatim.
        // Upgrade path: replace with a lookup dictionary keyed by icon filename.
        var DVDatacounted2 = false;
        var DVDataGUID2 = 0;
        if (b.IconFileName != null)
        {
            switch (b.IconFileName)
            {
                case "A7_airship_hangar.png": if (b.Guid != 112685) { DVDataGUID2 = 112685; DVDatacounted2 = true; } break;
                case "A7_research_center.png": if (b.Guid != 118938) { DVDataGUID2 = 118938; DVDatacounted2 = true; } break;
                case "A7_warehouse.png": if (b.Guid != 1010371) { DVDataGUID2 = 1010371; DVDatacounted2 = true; } break;
                case "A7_oil_habour_01.png": if (b.Guid != 100783) { DVDataGUID2 = 100783; DVDatacounted2 = true; } break;
                case "A7_kontor_main.png": if (b.Guid != 1010540) { DVDataGUID2 = 1010540; DVDatacounted2 = true; } break;
                case "A7_visitor_harbour.png": if (b.Guid != 100429) { DVDataGUID2 = 100429; DVDatacounted2 = true; } break;
                case "A7_dam_a.png": if (b.Guid != 686) { DVDataGUID2 = 686; DVDatacounted2 = true; } break;
                case "A7_highlife_skyliner_monument.png": if (b.Guid != 403) { DVDataGUID2 = 403; DVDatacounted2 = true; } break;
                case "A7_depot.png": if (b.Guid != 1010519) { DVDataGUID2 = 1010519; DVDatacounted2 = true; } break;
                case "A7_pier.png": if (b.Guid != 100519) { DVDataGUID2 = 100519; DVDatacounted2 = true; } break;
                case "A7_world_fair_2.png": if (b.Guid != 1010489) { DVDataGUID2 = 1010489; DVDatacounted2 = true; } break;
                case "A7_botanic_garden.png": if (b.Guid != 110935) { DVDataGUID2 = 110935; DVDatacounted2 = true; } break;
                case "A7_museum.png": if (b.Guid != 1010471) { DVDataGUID2 = 1010471; DVDatacounted2 = true; } break;
                case "A7_zoo.png": if (b.Guid != 1010470) { DVDataGUID2 = 1010470; DVDatacounted2 = true; } break;
                case "A7_airship_landing_plattform.png": if (b.Guid != 112726) { DVDataGUID2 = 112726; DVDatacounted2 = true; } break;
                case "A7_gold_ore.png": if (b.Guid != 1010311) { DVDataGUID2 = 1010311; DVDatacounted2 = true; } break;
                case "A7_townhall.png": if (b.Guid != 100415) { DVDataGUID2 = 100415; DVDatacounted2 = true; } break;
                case "A7_harbour_kontor.png": if (b.Guid != 100586) { DVDataGUID2 = 100586; DVDatacounted2 = true; } break;
                case "A7_oil_storage.png": if (b.Guid != 100784) { DVDataGUID2 = 100784; DVDatacounted2 = true; } break;
                case "A7_repair_crane.png": if (b.Guid != 1010525) { DVDataGUID2 = 1010525; DVDatacounted2 = true; } break;
                case "A7_guildhouse.png": if (b.Guid != 1010516) { DVDataGUID2 = 1010516; DVDatacounted2 = true; } break;
                case "A7_mineral_desposits.png": if (b.Guid != 1000029) { DVDataGUID2 = 1000029; DVDatacounted2 = true; } break;
                case "A7_oil.png": if (b.Guid is not 100849 and not 101331 and not 1010561) { DVDataGUID2 = 100849; DVDatacounted2 = true; } else if (b.Guid is not 101331 and not 100849 and not 101062 and not 116037) { DVDataGUID2 = 101331; DVDatacounted2 = true; } break;
                case "A7_defense_tower_pucklegun.png": if (b.Guid != 1010522) { DVDataGUID2 = 1010522; DVDatacounted2 = true; } break;
                case "A7_defense_tower_cannon.png": if (b.Guid != 1010523) { DVDataGUID2 = 1010523; DVDatacounted2 = true; } break;
                case "A7_sail_shipyard.png": if (b.Guid != 1010520) { DVDataGUID2 = 1010520; DVDatacounted2 = true; } break;
                case "A7_airship_hangar_southamerica.png": if (b.Guid != 648) { DVDataGUID2 = 648; DVDatacounted2 = true; } break;
            }
        }

        if (b.Identifier.StartsWith("Tourist_monument_0") && b.Guid != 132765) { DVDataGUID2 = 132765; DVDatacounted2 = true; }
        if (b.Faction == "Not Placed Yet -Moderate" && b.Identifier == "Forester" && b.IconFileName == "A7_wood_log.png" && b.Guid != 1010266) { DVDataGUID2 = 1010266; DVDatacounted2 = true; }
        if (b.Faction?.StartsWith("Not Placed Yet -") == true && b.IconFileName == "A7_tractor.png" && b.Guid != 269837) { DVDataGUID2 = 269837; DVDatacounted2 = true; }
        if (b.Identifier.Contains("(Clay Harvester)") && b.Guid != 117743) { DVDataGUID2 = 117743; DVDatacounted2 = true; }
        if (b.Identifier.Contains("(Paper Mill)") && b.Guid != 117744) { DVDataGUID2 = 117744; DVDatacounted2 = true; }
        if (b.Identifier.Contains("(Water Pump)") && b.Guid != 114544) { DVDataGUID2 = 114544; DVDatacounted2 = true; }

        if (DVDatacounted2)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("---> Building added to Replacement List: " + DVDataGUID2 + " << " + isExcludedGuidStr + " || " + b.IconFileName);
            _dvDataList[DVDataGUID2] = _dvDataList[DVDataGUID2] + DVDataSeperator + Convert.ToString(b.Guid);
            Console.ResetColor();
            return;
        }

        if (b.Guid is not 100455 and not 100454 and not 111104 and not 113452 and
            not 112685 and not 132765 and not 118938 and not 1010371 and
            not 100783 and not 1010540 and not 100429 and not 686 and
            not 4260 and not 4258 and not 2654 and not 1372 and not 1375)
        {
            if (string.IsNullOrEmpty(_dvDataList[b.Guid]))
            {
                var DVIdent = b.Identifier;
                if (DVIdent.Contains(',')) { DVIdent = DVIdent.Replace(",", ""); }
                _dvDataList[b.Guid] = Convert.ToString(b.Guid) + DVDataSeperator + b.IconFileName + DVDataSeperator + DVIdent;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("---> ERROR GUID WAS ALREADY IN DATALIST: GUID " + Convert.ToString(b.Guid));
                Console.ResetColor();
            }
        }

        #endregion

        // Filter out unwanted categories and "Not Placed" buildings
        if (b.Header == "(A7) Anno 1800" && b.Faction == "All Worlds" && (b.Group == "CultureModule" || b.Group == "OrnamentalBuilding")) { return; }
        if (b.Faction is "Not Placed Yet -Moderate" or "Not Placed Yet -Arctic" or "Not Placed Yet -Africa" or "Not Placed Yet -Colony01" or "Not Placed Yet -All Worlds") { return; }
        if (b.Faction == "Not Placed Yet -") { return; }

        // ponytail: ValidateIconFile skipped (writes to debug file). Upgrade path: add icon validation pass.
        _annoBuildingsListCount++;
        _annoBuildingLists.Add(values["Standard"]["Name"].InnerText);
        _anno1800IconNameLists.Add(b.IconFileName);
        buildings.Add(b);
    }


    // ponytail: translation overrides extracted to reduce ParseBuilding1800 size.
    // These are hardcoded corrections for game data errors. Upgrade path: external override file.
    private static string ApplyTranslationOverrides(string buildingGuid, int languageCount, string translation)
    {
        switch (buildingGuid)
        {
            case "102165": return languageCount switch { 0 => "Sidewalk Hedge", 1 => "Gehweg Hecke", 2 => "Haies de trottoirs", 3 => "Żywopłot Chodnikowy", 4 => "Боковая изгородь", 5 => "Seto de la acera", _ => translation };
            case "102166": return languageCount switch { 0 => "Sidewalk Hedge Corner", 1 => "Gehweg Heckenecke Ecke", 2 => "Coin de haies de trottoirs", 3 => "Żywopłot Chodnikowy narożnik", 4 => "Боковая изгородь (угол)", 5 => "Esquina del seto de la acera", _ => translation };
            case "102167": return languageCount switch { 0 => "Sidewalk Hedge End", 1 => "Gehweg Heckenende", 2 => "Extrémité de haie de trottoir", 3 => "Żywopłot Chodnikowy Koniec", 4 => "Боковая изгородь (край)", 5 => "Acera Final de seto", _ => translation };
            case "102169": return languageCount switch { 0 => "Sidewalk Hedge Junction", 1 => "Gehweg Hecken Verbindungsstelle", 2 => "Jonction de haie de trottoir", 3 => "Żywopłot Chodnikowy Złącze", 4 => "Боковая изгородь (Перекресток)", 5 => "Acera Seto Empalme", _ => translation };
            case "102171": return languageCount switch { 0 => "Sidewalk Hedge Crossing", 1 => "Gehweg Hecken Kreuzung", 2 => "Traversée de haie de trottoir", 3 => "Żywopłot Chodnikowy Skrzyżowanie", 4 => "Боковая изгородь (образного)", 5 => "Cruce de setos en la acera", _ => translation };
            case "102161": return languageCount switch { 0 => "Railings", 1 => "Zaune", 2 => "Garde-corps", 3 => "Poręcze", 4 => "Ограда", 5 => "Barandillas", _ => translation };
            case "102170": return languageCount switch { 0 => "Railings Junction", 1 => "Zaune Verbindungsstelle", 2 => "Garde-corps Jonction", 3 => "Poręcze Złącze", 4 => "Ограда (Перекресток)", 5 => "Empalme de barandillas", _ => translation };
            case "102134": return languageCount switch { 0 => "Hedge", 1 => "Hecke", 2 => "Haie (droite)", 3 => "żywopłot", 4 => "изгородь", 5 => "Cobertura", _ => translation };
            case "102139": return languageCount switch { 0 => "Path", 1 => "Pfad", 2 => "Allée (droite)", 3 => "ścieżka", 4 => "Тропинка", 5 => "Ruta", _ => translation };
            case "118938": return languageCount switch { 0 => "Research Institute", 1 => "Forschungsinstitut", 2 => "Institut de recherche", 3 => "Instytut Badawczy", 4 => "Исследовательский институт", 5 => "Instituto de Investigación", _ => translation };
            case "112670": return languageCount switch { 0 => "Arctic Depot", 1 => "Arktisches Depot", 2 => "Dépôt de l'Arctique", 3 => "Skład Arktyczny", 4 => "арктическая депо", 5 => "Depósito Ártico", _ => translation };
            case "1000029": return languageCount switch { 0 => "Empty Mining Slot", 1 => "Leerer Bergbau-Slot", 2 => "Emplacement minier vide", 3 => "Pusta szczelina wydobywcza", 4 => "Пустой горный отсек", 5 => "Ranura minera vacía", _ => translation };
            case "100849": return languageCount switch { 0 => "Oil Spring", 1 => "Ölquelle", 2 => "Puits de pétrole", 3 => "Pole naftowe", 4 => "Нефтяной источник", 5 => "Fuente de petróleo", _ => translation };
            case "972": return translation + " - (5)";
            default: return translation;
        }
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
            // Upgrade path: remove once Program.cs no longer calls ParseBuilding1800 directly
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
