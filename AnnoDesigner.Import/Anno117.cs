using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Gamedata;
using AnnoDesigner.Import.Model;
using AnnoDesigner.Import.Outlines;
using FileDBSerializing;
using RDAExplorer;

namespace AnnoDesigner.Import
{
    public static class Anno117
    {
        private const string Header = "(A8) Anno 117";
        private const int GUID_PROFILE_HUMAN = 41;

        public class SavegameReader
        {
            public LayoutFile ImportLayout(string path, BuildingPresets presets)
            {
                using RDAReader reader = new RDAReader() { FileName = path };
                IFileDBDocument gamedata = reader.File("data.a7s").GetFileDBDocumentInflated(); // interestingly the actual data file inside the .a8s is still named .a7s
                using ZipArchive outlines = OutlinesLoader.LoadArchive(nameof(Anno117));

                Tag metaGameManager = gamedata.Tag("MetaGameManager");
                Tag gameSessions = metaGameManager.Tag("GameSessions");

                LayoutFile layout = new LayoutFile
                {
                    FileVersion = 5,
                    LayoutVersion = new Version("1.0.0.0"),
                    Modified = File.GetLastWriteTime(path),
                    Sessions = new List<SessionLayout>(),
                };

#if DEBUG
                HashSet<int> missingPresets = new HashSet<int>();
#endif

                foreach (Tag session in gameSessions.Tags())
                {
                    int sessionGuid = session.Tag("SessionDesc").Attribute("SessionGUID").ToNumber<int>();
                    IFileDBDocument sessionData = session.Tag("SessionData").Attribute("BinaryData").ToFileDBDocument();
                    Tag gameSessionManager = sessionData.Tag("GameSessionManager");

                    Tag areaManagers = gameSessionManager.Tag("AreaManagers");
                    Dictionary<UInt16, Tag> areaInfos = gameSessionManager.Tag("AreaInfo").ToDictionary<UInt16>();

                    List<Tag> mapTemplates = gameSessionManager.Tag("MapTemplate").Tags("TemplateElement")
                        .SelectMany(element => element.Tags("Element"))
                        .Where(element => element.Attribute("MapFilePath") != null)
                        .ToList();

                    SessionLayout sessionLayout = new SessionLayout
                    {
                        Name = SessionNames.TryGetValue(sessionGuid, out var sessionNames) ? sessionNames["eng"] : $"Session {sessionGuid}", // TODO localization
                        Islands = new List<IslandLayout>(),
                    };

                    foreach (UInt16 areaId in areaInfos.Keys)
                    {
                        Tag areaInfo = areaInfos[areaId];
                        Tag areaManager = areaManagers.Tag("AreaManager_" + areaId);
                        int islandOwnerGuid = areaInfo.Attribute("OwnerProfile")?.ToNumber<int>() ?? 0;
                        if (islandOwnerGuid != GUID_PROFILE_HUMAN) continue;

                        string cityName = areaInfo.Attribute("CityName")?.ToUnicode();
                        if (cityName == null)
                        {
                            string cityNameGuid = areaInfo.Attribute("CityNameGuid").ToNumber<long>().ToString();
                            cityName = CityNames.TryGetValue(cityNameGuid, out var cityNames) ? cityNames["eng"] : $"Island {cityNameGuid}"; // TODO localization
                        }

                        Debug.WriteLine($"Processing island '{cityName}'...");
                        IEnumerable<Tag> polygonObjects = areaManager.Tag("AreaPolygonObjectManager").Tag("Polygons").Tags();
                        IEnumerable<Tag> gameObjects = areaManager.Tag("AreaObjectManager").Tag("GameObject").Tag("Objects").Tags();
                        Island island = CreateIsland(cityName, gameObjects, mapTemplates, outlines);

                        Tag streetGraph = areaManager.Tag("AreaStreetManager").Tag("Graph");
                        Tag aqueductGraph = areaManager.Tag("AreaAqueductManager").Tag("Graph");
                        Tag canalGraph = areaManager.Tag("AreaCanalManager").Tag("Graph");
                        Tag hedgeGraph = areaManager.Tag("AreaHedgeManager").Tag("Graph");
                        Tag wallGraph = areaManager.Tag("AreaWallManager").Tag("Graph");

                        foreach (Tag gameObject in gameObjects)
                        {
                            long id = gameObject.Attribute("ID").ToNumber<long>();
                            int guid = gameObject.Attribute("Guid").ToNumber<int>();
                            int state = gameObject.Attribute("StateBits")?.ToNumber<int>() ?? 0;
                            var template = FindBuildingByGuid(presets, guid);

                            if (template != null)
                            {
                                if (template.Template == "AqueductConnector") continue; // will be added later via aqueductGraph
                                var position = gameObject.Attribute("Position").ToPoint3D<float>();
                                float direction = gameObject.Attribute("Direction")?.ToNumber<float>() ?? 0;
                                GameObject building = new GameObject(template, direction, island.ToLocalCoordinates<float>(position));

                                // TODO find out values for different states (destroyed, blue-print, etc.) and change the color accordingly
                                if ((state & 102) != 0) building.Color = new SerializableColor(255, 224, 239, 255); // blue-print
                                else if (island.Colors.TryGetValue(id, out var color)) building.Color = color;
                                island.Objects.Add(building.CreateObject());
                            }
#if DEBUG
                            else if (missingPresets.Add(guid))
                            {
                                Debug.WriteLine($"Building {guid} not found!");
                            }
#endif
                        }

                        foreach (Tag polygonObject in polygonObjects)
                        {
                            long ownerId = polygonObject.Tag("ModuleOwner").Attribute("ObjectID").ToNumber<long>();
                            if (ownerId == 0) continue;

                            Tag tilesGrid = polygonObject.Tag("SubTilesGrid");
                            int guid = polygonObject.Attribute("GUID").ToNumber<int>();
                            var template = FindBuildingByGuid(presets, guid);
                            var color = island.Colors.TryGetValue(ownerId);

                            if (template != null)
                            {
                                ProcessTilesGrid(tilesGrid, (value, position) =>
                                {
                                    TileObject result = new TileObject(template, island.ToLocalCoordinates(position), value);
                                    if (color.HasValue) result.Color = color.Value;
                                    island.Objects.Add(result.CreateObject());
                                });
                            }
#if DEBUG
                            else if (missingPresets.Add(guid))
                            {
                                Debug.WriteLine($"Building {guid} not found!");
                            }
#endif
                        }

                        if (streetGraph != null)
                        {
                            ProcessGraph(streetGraph, tile =>
                            {
                                var template = FindBuildingByGuid(presets, tile.Guid);

#if DEBUG
                                if (template == null && missingPresets.Add(tile.Guid))
                                {
                                    Debug.WriteLine($"Street object {tile.Guid} not found!");
                                }
#endif

                                RoadObject result = new RoadObject(template, island.ToLocalCoordinates(tile.Position), tile.Rotation, tile.Quadrants);
#if DEBUG
                                if (tile.Color.HasValue) result.Color = tile.Color.Value;
#endif

                                island.Objects.Add(result.CreateObject());
                            });
                        }

                        if (aqueductGraph != null)
                        {
                            ProcessGraph(aqueductGraph, tile =>
                            {
                                var template = FindBuildingByGuid(presets, tile.Guid);

#if DEBUG
                                if (template == null && missingPresets.Add(tile.Guid))
                                {
                                    Debug.WriteLine($"Aqueduct object {tile.Guid} not found!");
                                }
#endif

                                TileObject result = new TileObject(template, island.ToLocalCoordinates(tile.Position), tile.Rotation, tile.Quadrants);
#if DEBUG
                                if (tile.Color.HasValue) result.Color = tile.Color.Value;
#endif

                                island.Objects.Add(result.CreateObject());
                            });
                        }

                        // drainage channels (AreaCanalManager) drain marshland. the sluice gate is a
                        // normal building but the channel mesh is its own graph, same as streets and
                        // aqueducts, so run it through the TileGraph too and diagonal channels come out
                        // as proper diagonal tiles instead of a staircase
                        if (canalGraph != null)
                        {
                            ProcessGraph(canalGraph, tile =>
                            {
                                var template = FindBuildingByGuid(presets, tile.Guid);

#if DEBUG
                                if (template == null && missingPresets.Add(tile.Guid))
                                {
                                    Debug.WriteLine($"Canal object {tile.Guid} not found!");
                                }
#endif

                                RoadObject channel = new RoadObject(template, island.ToLocalCoordinates(tile.Position), tile.Rotation, tile.Quadrants);
                                // channels reuse the gray Road template, tint them blue so they read as drainage
                                channel.Color = CanalColor;
                                island.Objects.Add(channel.CreateObject());
                            }, snapToGrid: true);
                        }

                        sessionLayout.Islands.Add(new IslandLayout
                        {
                            Name = cityName,
                            Objects = island.Objects,
                        });
                    }

                    layout.Sessions.Add(sessionLayout);
                }

                return layout;
            }

            #region Graph Section

            private static void ProcessGraph(Tag graph, Action<TileGraph.TileResult> action, bool snapToGrid = false)
            {
                IEnumerable<Point2D<int>> nodes = graph.Tag("Nodes").Attributes().Select(node => node.ToPoint2D<int>()); // TODO can we simply ignore the nodes?
                IEnumerable<Tag> edges = graph.Tag("Edges").Tags();
                ProcessEdges(edges, action, snapToGrid);
            }

            private static void ProcessEdges(IEnumerable<Tag> edges, Action<TileGraph.TileResult> action, bool snapToGrid)
            {
                TileGraph graph = new TileGraph();

                foreach (Tag edge in edges)
                {
                    int guid = edge.Attribute("guid").ToNumber<int>();
                    ProcessEdge(guid, graph, edge.Tag("Edge"), snapToGrid);
                }

                foreach (var tile in graph.Merge())
                {
                    action(tile);
                }
            }

            private static void ProcessEdge(int guid, TileGraph graph, Tag edge, bool snapToGrid)
            {
                // for some reason the positions in a graph in Anno 117 are scaled by a factor of 2 so we need to scale them down
                Point2D<float> start = edge.Attribute("PosMin").ToPoint2D<int>().Scale(0.5f);
                Point2D<float> end = edge.Attribute("PosMax").ToPoint2D<int>().Scale(0.5f);

                // snap canal endpoints to tile centers (see SnapToTile for why)
                if (snapToGrid)
                {
                    start = SnapToTile(start);
                    end = SnapToTile(end);
                }

                graph.AddEdge(guid, new Line2D<float>(start, end));
            }

            // steel blue, so channels stand out from road-gray
            private static readonly SerializableColor CanalColor = new SerializableColor(255, 70, 130, 180);

            // after the 0.5 scale, graph coords sit on tile centers (x.5) like streets do. canal nodes
            // mix centers (x.5) and corners (x.0), so snap everything to the nearest center. that stops
            // the integer rasterizer looping forever and keeps channels on the whole-tile grid
            private static Point2D<float> SnapToTile(Point2D<float> p)
            {
                return new Point2D<float>(ToTileCenter(p.X), ToTileCenter(p.Y));
            }

            private static float ToTileCenter(float value)
            {
                return (float)(Math.Floor(value) + 0.5);
            }

            #endregion

            #region Tiles Grid Section

            private static void ProcessTilesGrid(Tag tilesGrid, Action<byte, Point2D<float>> action)
            {
                Tag grid = tilesGrid.Tag("Grid").Tags().Single();
                Point2D<int> origin = tilesGrid.Attribute("GridOriginWS").ToPoint2D<int>();
                ProcessTilesGrid(grid, origin, action);
            }

            private static void ProcessTilesGrid(Tag grid, Point2D<int> origin, Action<byte, Point2D<float>> action)
            {
                int rows = grid.Attribute("y").ToNumber<int>();
                byte[] bits = grid.Attribute("bits").ToNibbles().ToArray();
                int columns = grid.Attribute("x").ToNumber<int>() / 4; // x is bits per row and we need nibbles per row, so divide by 4
                int stride = bits.Length / rows; // bits array is padded, get stride by dividing total length by number of rows
                ProcessTilesGrid(bits, columns, rows, stride, origin, action);
            }

            private static void ProcessTilesGrid(byte[] bits, int width, int height, int stride, Point2D<int> origin, Action<byte, Point2D<float>> action)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte value = bits[y * stride + x];
                        if (value != 0) action(value, new Point2D<float>(x + origin.X + (TileObject.Size / 2), y + origin.Y + (TileObject.Size / 2)));
                    }
                }
            }

            #endregion

            #region Presets Section

            private static BuildingInfo FindBuildingByGuid(BuildingPresets presets, int guid)
            {
                switch (guid)
                {
                    case 81354: guid = 19723; break; // Aqueduct Roman Aqueduct MaxGround
                    case 82038: guid = 29525; break; // Aqueduct Roman Celtic Aqueduct MaxGround
                }

                return presets.Buildings.FirstOrDefault(b => b.Header.Equals(Anno117.Header) && b.Guid == guid);
            }

            #endregion
        }

        #region Map Templates/Islands Section

        // TODO quick-n-dirty hard-coded localizations
        private static readonly Dictionary<int, SerializableDictionary<string>> SessionNames = new Dictionary<int, SerializableDictionary<string>>
        {
            { 3245, new SerializableDictionary<string> { ["eng"] = "Latium", ["ger"] = "Latium", ["fra"] = "Latium", ["pol"] = "Latium", ["rus"] = "Лаций", ["esp"] = "Latium" } },
            { 6627, new SerializableDictionary<string> { ["eng"] = "Albion", ["ger"] = "Albion", ["fra"] = "Albion", ["pol"] = "Albion", ["rus"] = "Альбион", ["esp"] = "Albion" } },
        };

        // TODO quick-n-dirty hard-coded localizations
        private static readonly Dictionary<string, SerializableDictionary<string>> CityNames = new Dictionary<string, SerializableDictionary<string>>
        {
            { "-6910895826578425784", new SerializableDictionary<string> { ["eng"] = "Neapolis", ["ger"] = "Neapolis", ["fra"] = "Neapolis", ["pol"] = "Neapolis", ["rus"] = "Неаполис", ["esp"] = "Neapolis" } },
            { "-6903704104905646605", new SerializableDictionary<string> { ["eng"] = "Medea", ["ger"] = "Medea", ["fra"] = "Medea", ["pol"] = "Medea", ["rus"] = "Медея", ["esp"] = "Medea" } },
            { "-6909938355726172971", new SerializableDictionary<string> { ["eng"] = "Pompus", ["ger"] = "Pompus", ["fra"] = "Pompus", ["pol"] = "Pompus", ["rus"] = "Помп", ["esp"] = "Pompus" } },
            { "-6911082973769020014", new SerializableDictionary<string> { ["eng"] = "Nubicuculia ", ["ger"] = "Nubicuculia ", ["fra"] = "Nubicuculia ", ["pol"] = "Nubicuculia ", ["rus"] = "Нубикукулия ", ["esp"] = "Nubicuculia " } },
            { "-6909643642940730022", new SerializableDictionary<string> { ["eng"] = "Kypressia", ["ger"] = "Kypressia", ["fra"] = "Kypressia", ["pol"] = "Kypressia", ["rus"] = "Кипрессия", ["esp"] = "Kypressia" } },
            { "-6912568877034745199", new SerializableDictionary<string> { ["eng"] = "Capitoline", ["ger"] = "Capitoline", ["fra"] = "Capitoline", ["pol"] = "Capitoline", ["rus"] = "Капитолина", ["esp"] = "Capitoline" } },
            { "-6899754567191428887", new SerializableDictionary<string> { ["eng"] = "Chianti", ["ger"] = "Chianti", ["fra"] = "Chianti", ["pol"] = "Chianti", ["rus"] = "Кьянти", ["esp"] = "Chianti" } },
            { "-6913724870522382933", new SerializableDictionary<string> { ["eng"] = "Confluenta", ["ger"] = "Confluenta", ["fra"] = "Confluenta", ["pol"] = "Confluenta", ["rus"] = "Конфлюэнца", ["esp"] = "Confluenta" } },
            { "-6901823300157382298", new SerializableDictionary<string> { ["eng"] = "Apropolis", ["ger"] = "Apropolis", ["fra"] = "Apropolis", ["pol"] = "Apropolis", ["rus"] = "Апрополис", ["esp"] = "Apropolis" } },
            { "-6916042865393146909", new SerializableDictionary<string> { ["eng"] = "Terra Sigillata", ["ger"] = "Terra Sigillata", ["fra"] = "Terra Sigillata", ["pol"] = "Terra Sigillata", ["rus"] = "Терра Сигиллата", ["esp"] = "Terra Sigillata" } },
            { "-6910450983200981621", new SerializableDictionary<string> { ["eng"] = "Calypsis", ["ger"] = "Calypsis", ["fra"] = "Calypsis", ["pol"] = "Calypsis", ["rus"] = "Калипсис", ["esp"] = "Calypsis" } },
            { "-6905277757697723859", new SerializableDictionary<string> { ["eng"] = "Mimosa", ["ger"] = "Mimosa", ["fra"] = "Mimosa", ["pol"] = "Mimosa", ["rus"] = "Мимоса", ["esp"] = "Mimosa" } },
            { "-6901547682945105263", new SerializableDictionary<string> { ["eng"] = "Aegis", ["ger"] = "Aegis", ["fra"] = "Aegis", ["pol"] = "Aegis", ["rus"] = "Эгида", ["esp"] = "Aegis" } },
            { "-6909780794135005627", new SerializableDictionary<string> { ["eng"] = "Tabula Raza", ["ger"] = "Tabula Raza", ["fra"] = "Tabula Raza", ["pol"] = "Tabula Raza", ["rus"] = "Табула Раса", ["esp"] = "Tabula Raza" } },
            { "-6900994153400255896", new SerializableDictionary<string> { ["eng"] = "Mogontia Sum", ["ger"] = "Mogontia Sum", ["fra"] = "Mogontia Sum", ["pol"] = "Mogontia Sum", ["rus"] = "Могонтия Сум", ["esp"] = "Mogontia Sum" } },
            { "-6917509417000990121", new SerializableDictionary<string> { ["eng"] = "Peregrinata", ["ger"] = "Peregrinata", ["fra"] = "Peregrinata", ["pol"] = "Peregrinata", ["rus"] = "Перегрината", ["esp"] = "Peregrinata" } },
            { "-6912095349257278107", new SerializableDictionary<string> { ["eng"] = "Argussus", ["ger"] = "Argussus", ["fra"] = "Argussus", ["pol"] = "Argussus", ["rus"] = "Аргусс", ["esp"] = "Argussus" } },
            { "-6916950175101446843", new SerializableDictionary<string> { ["eng"] = "Euphoria", ["ger"] = "Euphoria", ["fra"] = "Euphoria", ["pol"] = "Euphoria", ["rus"] = "Эйфория", ["esp"] = "Euphoria" } },
            { "-6900914774506571939", new SerializableDictionary<string> { ["eng"] = "Panem", ["ger"] = "Panem", ["fra"] = "Panem", ["pol"] = "Panem", ["rus"] = "Панем", ["esp"] = "Panem" } },
            { "-6902965061888440992", new SerializableDictionary<string> { ["eng"] = "Herculanea", ["ger"] = "Herculanea", ["fra"] = "Herculanea", ["pol"] = "Herculanea", ["rus"] = "Геркуланея", ["esp"] = "Herculanea" } },
            { "-6903309791480670849", new SerializableDictionary<string> { ["eng"] = "Zycada ", ["ger"] = "Zycada ", ["fra"] = "Zycada ", ["pol"] = "Zycada ", ["rus"] = "Зикада ", ["esp"] = "Zycada " } },
            { "-6901794369422116498", new SerializableDictionary<string> { ["eng"] = "Titania ", ["ger"] = "Titania ", ["fra"] = "Titania ", ["pol"] = "Titania ", ["rus"] = "Титания ", ["esp"] = "Titania " } },
            { "-6911152344235185987", new SerializableDictionary<string> { ["eng"] = "Laurarium", ["ger"] = "Laurarium", ["fra"] = "Laurarium", ["pol"] = "Laurarium", ["rus"] = "Лаурариум", ["esp"] = "Laurarium" } },
            { "-6916406503715387219", new SerializableDictionary<string> { ["eng"] = "Vineta", ["ger"] = "Vineta", ["fra"] = "Vineta", ["pol"] = "Vineta", ["rus"] = "Винета", ["esp"] = "Vineta" } },
            { "-6912197980920388720", new SerializableDictionary<string> { ["eng"] = "Hestiacum", ["ger"] = "Hestiacum", ["fra"] = "Hestiacum", ["pol"] = "Hestiacum", ["rus"] = "Гестиакум", ["esp"] = "Hestiacum" } },
            { "-6907947263626234431", new SerializableDictionary<string> { ["eng"] = "Arcadia", ["ger"] = "Arcadia", ["fra"] = "Arcadia", ["pol"] = "Arcadia", ["rus"] = "Аркадия", ["esp"] = "Arcadia" } },
            { "-6911966118823199253", new SerializableDictionary<string> { ["eng"] = "Campania", ["ger"] = "Campania", ["fra"] = "Campania", ["pol"] = "Campania", ["rus"] = "Кампания", ["esp"] = "Campania" } },
            { "-6910755084398406435", new SerializableDictionary<string> { ["eng"] = "Florentia", ["ger"] = "Florentia", ["fra"] = "Florentia", ["pol"] = "Florentia", ["rus"] = "Флорента", ["esp"] = "Florentia" } },
            { "-6913092097736949801", new SerializableDictionary<string> { ["eng"] = "Bonononia", ["ger"] = "Bonononia", ["fra"] = "Bonononia", ["pol"] = "Bonononia", ["rus"] = "Бононония", ["esp"] = "Bonononia" } },
            { "-6903871094194901759", new SerializableDictionary<string> { ["eng"] = "Fabula Togata", ["ger"] = "Fabula Togata", ["fra"] = "Fabula Togata", ["pol"] = "Fabula Togata", ["rus"] = "Фабула Тогата", ["esp"] = "Fabula Togata" } },
            { "-6908608951485934453", new SerializableDictionary<string> { ["eng"] = "Placentia", ["ger"] = "Placentia", ["fra"] = "Placentia", ["pol"] = "Placentia", ["rus"] = "Плакенция", ["esp"] = "Placentia" } },
            { "-6906788294032924924", new SerializableDictionary<string> { ["eng"] = "Aenea", ["ger"] = "Aenea", ["fra"] = "Aenea", ["pol"] = "Aenea", ["rus"] = "Энея", ["esp"] = "Aenea" } },
            { "-6916427613873767780", new SerializableDictionary<string> { ["eng"] = "Luccania", ["ger"] = "Luccania", ["fra"] = "Luccania", ["pol"] = "Luccania", ["rus"] = "Луккания", ["esp"] = "Luccania" } },
            { "-6900151477030508439", new SerializableDictionary<string> { ["eng"] = "Pons Census", ["ger"] = "Pons Census", ["fra"] = "Pons Census", ["pol"] = "Pons Census", ["rus"] = "Понт Кенсус", ["esp"] = "Pons Census" } },
            { "-6910891213205164518", new SerializableDictionary<string> { ["eng"] = "Argonautica", ["ger"] = "Argonautica", ["fra"] = "Argonautica", ["pol"] = "Argonautica", ["rus"] = "Аргонавтика", ["esp"] = "Argonautica" } },
            { "-6908205509221390333", new SerializableDictionary<string> { ["eng"] = "Modicum", ["ger"] = "Modicum", ["fra"] = "Modicum", ["pol"] = "Modicum", ["rus"] = "Модикум", ["esp"] = "Modicum" } },
            { "-6905591051823995589", new SerializableDictionary<string> { ["eng"] = "Silvanium", ["ger"] = "Silvanium", ["fra"] = "Silvanium", ["pol"] = "Silvanium", ["rus"] = "Сильваниум", ["esp"] = "Silvanium" } },
            { "-6905299749109756085", new SerializableDictionary<string> { ["eng"] = "Naissus", ["ger"] = "Naissus", ["fra"] = "Naissus", ["pol"] = "Naissus", ["rus"] = "Наисс", ["esp"] = "Naissus" } },
            { "-6912680946974747361", new SerializableDictionary<string> { ["eng"] = "Horatia", ["ger"] = "Horatia", ["fra"] = "Horatia", ["pol"] = "Horatia", ["rus"] = "Горация", ["esp"] = "Horatia" } },
            { "-6916393056039183168", new SerializableDictionary<string> { ["eng"] = "Ithaca", ["ger"] = "Ithaca", ["fra"] = "Ithaca", ["pol"] = "Ithaca", ["rus"] = "Итака", ["esp"] = "Ithaca" } },
            { "-6912637229232708910", new SerializableDictionary<string> { ["eng"] = "Annopolis", ["ger"] = "Annopolis", ["fra"] = "Annopolis", ["pol"] = "Annopolis", ["rus"] = "Аннополис", ["esp"] = "Annopolis" } },
            { "-6903133623088066288", new SerializableDictionary<string> { ["eng"] = "Anninium", ["ger"] = "Anninium", ["fra"] = "Anninium", ["pol"] = "Anninium", ["rus"] = "Анниниум", ["esp"] = "Anninium" } },
            { "-6915144683597602841", new SerializableDictionary<string> { ["eng"] = "Valerium", ["ger"] = "Valerium", ["fra"] = "Valerium", ["pol"] = "Valerium", ["rus"] = "Валериум", ["esp"] = "Valerium" } },
            { "-6915659993562907202", new SerializableDictionary<string> { ["eng"] = "Virifortis", ["ger"] = "Virifortis", ["fra"] = "Virifortis", ["pol"] = "Virifortis", ["rus"] = "Вирифортис", ["esp"] = "Virifortis" } },
            { "-6911465303939140845", new SerializableDictionary<string> { ["eng"] = "Argentum", ["ger"] = "Argentum", ["fra"] = "Argentum", ["pol"] = "Argentum", ["rus"] = "Аргентум", ["esp"] = "Argentum" } },
            { "-6904991025289109450", new SerializableDictionary<string> { ["eng"] = "Augustea", ["ger"] = "Augustea", ["fra"] = "Augustea", ["pol"] = "Augustea", ["rus"] = "Августея", ["esp"] = "Augustea" } },
            { "-6906244161047140894", new SerializableDictionary<string> { ["eng"] = "Mytholos", ["ger"] = "Mytholos", ["fra"] = "Mytholos", ["pol"] = "Mytholos", ["rus"] = "Митолос", ["esp"] = "Mytholos" } },
            { "-6904508274800972459", new SerializableDictionary<string> { ["eng"] = "Megaron", ["ger"] = "Megaron", ["fra"] = "Megaron", ["pol"] = "Megaron", ["rus"] = "Мегарон", ["esp"] = "Megaron" } },
            { "-6900544844075572245", new SerializableDictionary<string> { ["eng"] = "Ostia Nova", ["ger"] = "Ostia Nova", ["fra"] = "Ostia Nova", ["pol"] = "Ostia Nova", ["rus"] = "Новая Остия", ["esp"] = "Ostia Nova" } },
            { "-6901430857007502031", new SerializableDictionary<string> { ["eng"] = "Aurealis", ["ger"] = "Aurealis", ["fra"] = "Aurealis", ["pol"] = "Aurealis", ["rus"] = "Ауреалис", ["esp"] = "Aurealis" } },
            { "-6912774777066435111", new SerializableDictionary<string> { ["eng"] = "Alba Luna", ["ger"] = "Alba Luna", ["fra"] = "Alba Luna", ["pol"] = "Alba Luna", ["rus"] = "Альба Луна", ["esp"] = "Alba Luna" } },
            { "-6914982207830520372", new SerializableDictionary<string> { ["eng"] = "Ravenna", ["ger"] = "Ravenna", ["fra"] = "Ravenna", ["pol"] = "Ravenna", ["rus"] = "Равенна", ["esp"] = "Ravenna" } },
            { "-6904836250417018590", new SerializableDictionary<string> { ["eng"] = "Lerna", ["ger"] = "Lerna", ["fra"] = "Lerna", ["pol"] = "Lerna", ["rus"] = "Лерна", ["esp"] = "Lerna" } },
            { "-6902259912938871369", new SerializableDictionary<string> { ["eng"] = "Nemea", ["ger"] = "Nemea", ["fra"] = "Nemea", ["pol"] = "Nemea", ["rus"] = "Немея", ["esp"] = "Nemea" } },
            { "-6910373335767931169", new SerializableDictionary<string> { ["eng"] = "Erythia", ["ger"] = "Erythia", ["fra"] = "Erythia", ["pol"] = "Erythia", ["rus"] = "Эрития", ["esp"] = "Erythia" } },
            { "-6911835811697220020", new SerializableDictionary<string> { ["eng"] = "Nusquam", ["ger"] = "Nusquam", ["fra"] = "Nusquam", ["pol"] = "Nusquam", ["rus"] = "Нусквам", ["esp"] = "Nusquam" } },
            { "-6914640435945920420", new SerializableDictionary<string> { ["eng"] = "Tiberia", ["ger"] = "Tiberia", ["fra"] = "Tiberia", ["pol"] = "Tiberia", ["rus"] = "Тиберия", ["esp"] = "Tiberia" } },
            { "-6902917056256401586", new SerializableDictionary<string> { ["eng"] = "Penthesilea", ["ger"] = "Penthesilea", ["fra"] = "Penthesilea", ["pol"] = "Penthesilea", ["rus"] = "Пентесилея", ["esp"] = "Penthesilea" } },
            { "-6906565992835134627", new SerializableDictionary<string> { ["eng"] = "Hermeticum", ["ger"] = "Hermeticum", ["fra"] = "Hermeticum", ["pol"] = "Hermeticum", ["rus"] = "Герметикум", ["esp"] = "Hermeticum" } },
            { "-6915621047850187605", new SerializableDictionary<string> { ["eng"] = "Sirena", ["ger"] = "Sirena", ["fra"] = "Sirena", ["pol"] = "Sirena", ["rus"] = "Сирена", ["esp"] = "Sirena" } },
            { "-6908384330156911277", new SerializableDictionary<string> { ["eng"] = "Mithraeum", ["ger"] = "Mithraeum", ["fra"] = "Mithraeum", ["pol"] = "Mithraeum", ["rus"] = "Митреум", ["esp"] = "Mithraeum" } },
            { "-6904497080564646733", new SerializableDictionary<string> { ["eng"] = "Villa Victualia", ["ger"] = "Villa Victualia", ["fra"] = "Villa Victualia", ["pol"] = "Villa Victualia", ["rus"] = "Вилла Виктуалия", ["esp"] = "Villa Victualia" } },
            { "-6916403020618686498", new SerializableDictionary<string> { ["eng"] = "Illiadunum", ["ger"] = "Illiadunum", ["fra"] = "Illiadunum", ["pol"] = "Illiadunum", ["rus"] = "Илиадунум", ["esp"] = "Illiadunum" } },
            { "-6901915352523730755", new SerializableDictionary<string> { ["eng"] = "Aeterna", ["ger"] = "Aeterna", ["fra"] = "Aeterna", ["pol"] = "Aeterna", ["rus"] = "Этерна", ["esp"] = "Aeterna" } },
            { "-6901543950956727716", new SerializableDictionary<string> { ["eng"] = "Apricus", ["ger"] = "Apricus", ["fra"] = "Apricus", ["pol"] = "Apricus", ["rus"] = "Априкус", ["esp"] = "Apricus" } },
            { "-6904686165343754239", new SerializableDictionary<string> { ["eng"] = "Meliora", ["ger"] = "Meliora", ["fra"] = "Meliora", ["pol"] = "Meliora", ["rus"] = "Мелиора", ["esp"] = "Meliora" } },
            { "-6907806448712142778", new SerializableDictionary<string> { ["eng"] = "Occasum", ["ger"] = "Occasum", ["fra"] = "Occasum", ["pol"] = "Occasum", ["rus"] = "Оккасум", ["esp"] = "Occasum" } },
            { "-6905224314615218480", new SerializableDictionary<string> { ["eng"] = "Appolonia", ["ger"] = "Appolonia", ["fra"] = "Appolonia", ["pol"] = "Appolonia", ["rus"] = "Аполлония", ["esp"] = "Appolonia" } },
            { "-6912175705500545346", new SerializableDictionary<string> { ["eng"] = "Caelestis", ["ger"] = "Caelestis", ["fra"] = "Caelestis", ["pol"] = "Caelestis", ["rus"] = "Кэлестис", ["esp"] = "Caelestis" } },
            { "-6912639249984372334", new SerializableDictionary<string> { ["eng"] = "Proventus", ["ger"] = "Proventus", ["fra"] = "Proventus", ["pol"] = "Proventus", ["rus"] = "Провент", ["esp"] = "Proventus" } },
            { "-6917021190395939534", new SerializableDictionary<string> { ["eng"] = "Matria", ["ger"] = "Matria", ["fra"] = "Matria", ["pol"] = "Matria", ["rus"] = "Матрия", ["esp"] = "Matria" } },
            { "-6907498518992725313", new SerializableDictionary<string> { ["eng"] = "Sanctum", ["ger"] = "Sanctum", ["fra"] = "Sanctum", ["pol"] = "Sanctum", ["rus"] = "Санктум", ["esp"] = "Sanctum" } },
            { "-6915434144559915748", new SerializableDictionary<string> { ["eng"] = "Lorica", ["ger"] = "Lorica", ["fra"] = "Lorica", ["pol"] = "Lorica", ["rus"] = "Лорика", ["esp"] = "Lorica" } },
            { "-6902506024114188108", new SerializableDictionary<string> { ["eng"] = "Sartago", ["ger"] = "Sartago", ["fra"] = "Sartago", ["pol"] = "Sartago", ["rus"] = "Сартаго", ["esp"] = "Sartago" } },
            { "-6899855873568483910", new SerializableDictionary<string> { ["eng"] = "Thymia", ["ger"] = "Thymia", ["fra"] = "Thymia", ["pol"] = "Thymia", ["rus"] = "Тимия", ["esp"] = "Thymia" } },
            { "-6913227466237078616", new SerializableDictionary<string> { ["eng"] = "Pinna Nobilis", ["ger"] = "Pinna Nobilis", ["fra"] = "Pinna Nobilis", ["pol"] = "Pinna Nobilis", ["rus"] = "Пинна Нобилис", ["esp"] = "Pinna Nobilis" } },
            { "-6912604743712261289", new SerializableDictionary<string> { ["eng"] = "Agathea", ["ger"] = "Agathea", ["fra"] = "Agathea", ["pol"] = "Agathea", ["rus"] = "Агатея", ["esp"] = "Agathea" } },
            { "-6912822456482251924", new SerializableDictionary<string> { ["eng"] = "Anchora", ["ger"] = "Anchora", ["fra"] = "Anchora", ["pol"] = "Anchora", ["rus"] = "Анхора", ["esp"] = "Anchora" } },
            { "-6900598363071281532", new SerializableDictionary<string> { ["eng"] = "Taurus", ["ger"] = "Taurus", ["fra"] = "Taurus", ["pol"] = "Taurus", ["rus"] = "Таур", ["esp"] = "Taurus" } },
            { "-6915399563604389452", new SerializableDictionary<string> { ["eng"] = "Naissus", ["ger"] = "Naissus", ["fra"] = "Naissus", ["pol"] = "Naissus", ["rus"] = "Наисс", ["esp"] = "Naissus" } },
            { "-6900726770397432375", new SerializableDictionary<string> { ["eng"] = "Brixia", ["ger"] = "Brixia", ["fra"] = "Brixia", ["pol"] = "Brixia", ["rus"] = "Бриксия", ["esp"] = "Brixia" } },
            { "-6915193998894468055", new SerializableDictionary<string> { ["eng"] = "Porta Libertia", ["ger"] = "Porta Libertia", ["fra"] = "Porta Libertia", ["pol"] = "Porta Libertia", ["rus"] = "Порта Либертия", ["esp"] = "Porta Libertia" } },
            { "-6915223668743738937", new SerializableDictionary<string> { ["eng"] = "Forum Aquili", ["ger"] = "Forum Aquili", ["fra"] = "Forum Aquili", ["pol"] = "Forum Aquili", ["rus"] = "Форум Аквили", ["esp"] = "Forum Aquili" } },
            { "-6917307273510311907", new SerializableDictionary<string> { ["eng"] = "Marsonia", ["ger"] = "Marsonia", ["fra"] = "Marsonia", ["pol"] = "Marsonia", ["rus"] = "Марсония", ["esp"] = "Marsonia" } },
            { "-6913460863983572396", new SerializableDictionary<string> { ["eng"] = "Capua Nova", ["ger"] = "Capua Nova", ["fra"] = "Capua Nova", ["pol"] = "Capua Nova", ["rus"] = "Новая Капуя", ["esp"] = "Capua Nova" } },
            { "-6901449840824349890", new SerializableDictionary<string> { ["eng"] = "Barbirum", ["ger"] = "Barbirum", ["fra"] = "Barbirum", ["pol"] = "Barbirum", ["rus"] = "Барбирум", ["esp"] = "Barbirum" } },
            { "-6915880589036409573", new SerializableDictionary<string> { ["eng"] = "Opulens", ["ger"] = "Opulens", ["fra"] = "Opulens", ["pol"] = "Opulens", ["rus"] = "Опуленс", ["esp"] = "Opulens" } },
            { "-6903735017318977304", new SerializableDictionary<string> { ["eng"] = "Remula", ["ger"] = "Remula", ["fra"] = "Remula", ["pol"] = "Remula", ["rus"] = "Ремула", ["esp"] = "Remula" } },
            { "-6901497971429674642", new SerializableDictionary<string> { ["eng"] = "Pons Praetoria", ["ger"] = "Pons Praetoria", ["fra"] = "Pons Praetoria", ["pol"] = "Pons Praetoria", ["rus"] = "Понт Претория", ["esp"] = "Pons Praetoria" } },
            { "-6914764817152730048", new SerializableDictionary<string> { ["eng"] = "Beneventum", ["ger"] = "Beneventum", ["fra"] = "Beneventum", ["pol"] = "Beneventum", ["rus"] = "Беневентум", ["esp"] = "Beneventum" } },
            { "-6902867658212366839", new SerializableDictionary<string> { ["eng"] = "Nymphaeum", ["ger"] = "Nymphaeum", ["fra"] = "Nymphaeum", ["pol"] = "Nymphaeum", ["rus"] = "Нимфеум", ["esp"] = "Nymphaeum" } },
            { "-6910628361017533282", new SerializableDictionary<string> { ["eng"] = "Fortuna", ["ger"] = "Fortuna", ["fra"] = "Fortuna", ["pol"] = "Fortuna", ["rus"] = "Фортуна", ["esp"] = "Fortuna" } },
            { "-6904560589525960718", new SerializableDictionary<string> { ["eng"] = "Saliente", ["ger"] = "Saliente", ["fra"] = "Saliente", ["pol"] = "Saliente", ["rus"] = "Салиенте", ["esp"] = "Saliente" } },
            { "-6899853169552267976", new SerializableDictionary<string> { ["eng"] = "Monopolis", ["ger"] = "Monopolis", ["fra"] = "Monopolis", ["pol"] = "Monopolis", ["rus"] = "Монополис", ["esp"] = "Monopolis" } },
            { "-6907242241209457629", new SerializableDictionary<string> { ["eng"] = "Acanthus", ["ger"] = "Acanthus", ["fra"] = "Acanthus", ["pol"] = "Acanthus", ["rus"] = "Акант", ["esp"] = "Acanthus" } },
            { "-6904535642169061569", new SerializableDictionary<string> { ["eng"] = "Atlantea", ["ger"] = "Atlantea", ["fra"] = "Atlantea", ["pol"] = "Atlantea", ["rus"] = "Атлантея", ["esp"] = "Atlantea" } },
            { "-6903617984978312189", new SerializableDictionary<string> { ["eng"] = "Gargantea", ["ger"] = "Gargantea", ["fra"] = "Gargantea", ["pol"] = "Gargantea", ["rus"] = "Гаргантея", ["esp"] = "Gargantea" } },
            { "-6912987672659586989", new SerializableDictionary<string> { ["eng"] = "Colossus", ["ger"] = "Colossus", ["fra"] = "Colossus", ["pol"] = "Colossus", ["rus"] = "Колосс", ["esp"] = "Colossus" } },
            { "-6903698317728696243", new SerializableDictionary<string> { ["eng"] = "Pelagia", ["ger"] = "Pelagia", ["fra"] = "Pelagia", ["pol"] = "Pelagia", ["rus"] = "Пелагия", ["esp"] = "Pelagia" } },
            { "-6913665452232832235", new SerializableDictionary<string> { ["eng"] = "Omnia", ["ger"] = "Omnia", ["fra"] = "Omnia", ["pol"] = "Omnia", ["rus"] = "Омния", ["esp"] = "Omnia" } },
            { "-6900888703511564319", new SerializableDictionary<string> { ["eng"] = "Firmium", ["ger"] = "Firmium", ["fra"] = "Firmium", ["pol"] = "Firmium", ["rus"] = "Фирмиум", ["esp"] = "Firmium" } },
            { "-6900020035482696899", new SerializableDictionary<string> { ["eng"] = "Carradunon", ["ger"] = "Carradunon", ["fra"] = "Carradunon", ["pol"] = "Carradunon", ["rus"] = "Каррадинон", ["esp"] = "Carradunon" } },
            { "-6912501424777277993", new SerializableDictionary<string> { ["eng"] = "Hibernia", ["ger"] = "Hibernia", ["fra"] = "Hibernia", ["pol"] = "Hibernia", ["rus"] = "Гиберния", ["esp"] = "Hibernia" } },
            { "-6901606357506718862", new SerializableDictionary<string> { ["eng"] = "Glenmire", ["ger"] = "Glenmire", ["fra"] = "Glenmire", ["pol"] = "Glenmire", ["rus"] = "Гленмир", ["esp"] = "Glenmire" } },
            { "-6909435919878302760", new SerializableDictionary<string> { ["eng"] = "Arduinna", ["ger"] = "Arduinna", ["fra"] = "Arduinna", ["pol"] = "Arduinna", ["rus"] = "Ардинна", ["esp"] = "Arduinna" } },
            { "-6904342992514896050", new SerializableDictionary<string> { ["eng"] = "Nampfteuil", ["ger"] = "Nampfteuil", ["fra"] = "Nampfteuil", ["pol"] = "Nampfteuil", ["rus"] = "Нампфтэиль", ["esp"] = "Nampfteuil" } },
            { "-6915578398204458151", new SerializableDictionary<string> { ["eng"] = "Cavalon", ["ger"] = "Cavalon", ["fra"] = "Cavalon", ["pol"] = "Cavalon", ["rus"] = "Кавалон", ["esp"] = "Cavalon" } },
            { "-6909200589750889578", new SerializableDictionary<string> { ["eng"] = "Caer Lindis", ["ger"] = "Caer Lindis", ["fra"] = "Caer Lindis", ["pol"] = "Caer Lindis", ["rus"] = "Кайр-Линдис", ["esp"] = "Caer Lindis" } },
            { "-6908948415981048714", new SerializableDictionary<string> { ["eng"] = "Noviomagus", ["ger"] = "Noviomagus", ["fra"] = "Noviomagus", ["pol"] = "Noviomagus", ["rus"] = "Новиомаг", ["esp"] = "Noviomagus" } },
            { "-6912268057475510918", new SerializableDictionary<string> { ["eng"] = "Tor Dolya", ["ger"] = "Tor Dolya", ["fra"] = "Tor Dolya", ["pol"] = "Tor Dolya", ["rus"] = "Тор-Долиа", ["esp"] = "Tor Dolya" } },
            { "-6905993758753803387", new SerializableDictionary<string> { ["eng"] = "Rhydfell", ["ger"] = "Rhydfell", ["fra"] = "Rhydfell", ["pol"] = "Rhydfell", ["rus"] = "Ридфелл", ["esp"] = "Rhydfell" } },
            { "-6915815547048474485", new SerializableDictionary<string> { ["eng"] = "Riverford", ["ger"] = "Riverford", ["fra"] = "Riverford", ["pol"] = "Riverford", ["rus"] = "Риверфорд", ["esp"] = "Riverford" } },
            { "-6909145066560032069", new SerializableDictionary<string> { ["eng"] = "Arbora Mortis", ["ger"] = "Arbora Mortis", ["fra"] = "Arbora Mortis", ["pol"] = "Arbora Mortis", ["rus"] = "Арбора Мортис", ["esp"] = "Arbora Mortis" } },
            { "-6914434375280472243", new SerializableDictionary<string> { ["eng"] = "Natrixia", ["ger"] = "Natrixia", ["fra"] = "Natrixia", ["pol"] = "Natrixia", ["rus"] = "Натриксия", ["esp"] = "Natrixia" } },
            { "-6914736938391480838", new SerializableDictionary<string> { ["eng"] = "Aquae Profundis", ["ger"] = "Aquae Profundis", ["fra"] = "Aquae Profundis", ["pol"] = "Aquae Profundis", ["rus"] = "Аква Профундис", ["esp"] = "Aquae Profundis" } },
            { "-6913427805648852902", new SerializableDictionary<string> { ["eng"] = "Waeterflod", ["ger"] = "Waeterflod", ["fra"] = "Waeterflod", ["pol"] = "Waeterflod", ["rus"] = "Ватерфлод", ["esp"] = "Waeterflod" } },
            { "-6909929022424657746", new SerializableDictionary<string> { ["eng"] = "Aquarium", ["ger"] = "Aquarium", ["fra"] = "Aquarium", ["pol"] = "Aquarium", ["rus"] = "Аквариум", ["esp"] = "Aquarium" } },
            { "-6902506672986310113", new SerializableDictionary<string> { ["eng"] = "Duromagus", ["ger"] = "Duromagus", ["fra"] = "Duromagus", ["pol"] = "Duromagus", ["rus"] = "Дуромаг", ["esp"] = "Duromagus" } },
            { "-6915492812653068734", new SerializableDictionary<string> { ["eng"] = "Lughdanum", ["ger"] = "Lughdanum", ["fra"] = "Lughdanum", ["pol"] = "Lughdanum", ["rus"] = "Луданум", ["esp"] = "Lughdanum" } },
            { "-6911384189700506138", new SerializableDictionary<string> { ["eng"] = "Caperby", ["ger"] = "Caperby", ["fra"] = "Caperby", ["pol"] = "Caperby", ["rus"] = "Каперби", ["esp"] = "Caperby" } },
            { "-6917526715932550338", new SerializableDictionary<string> { ["eng"] = "Caer Maelod", ["ger"] = "Caer Maelod", ["fra"] = "Caer Maelod", ["pol"] = "Caer Maelod", ["rus"] = "Кайр-Майлод", ["esp"] = "Caer Maelod" } },
            { "-6915887343642475940", new SerializableDictionary<string> { ["eng"] = "Runlet", ["ger"] = "Runlet", ["fra"] = "Runlet", ["pol"] = "Runlet", ["rus"] = "Ранлет", ["esp"] = "Runlet" } },
            { "-6906881878559773058", new SerializableDictionary<string> { ["eng"] = "Tír na nÓg", ["ger"] = "Tír na nÓg", ["fra"] = "Tír na nÓg", ["pol"] = "Tír na nÓg", ["rus"] = "Тирь-на-Ног", ["esp"] = "Tír na nÓg" } },
            { "-6915209340950535138", new SerializableDictionary<string> { ["eng"] = "Pourdoon", ["ger"] = "Pourdoon", ["fra"] = "Pourdoon", ["pol"] = "Pourdoon", ["rus"] = "Поурдоон", ["esp"] = "Pourdoon" } },
            { "-6907159421917865055", new SerializableDictionary<string> { ["eng"] = "Cambasbury", ["ger"] = "Cambasbury", ["fra"] = "Cambasbury", ["pol"] = "Cambasbury", ["rus"] = "Камбасбури", ["esp"] = "Cambasbury" } },
            { "-6904336980164162220", new SerializableDictionary<string> { ["eng"] = "Gwaelodyke ", ["ger"] = "Gwaelodyke ", ["fra"] = "Gwaelodyke ", ["pol"] = "Gwaelodyke ", ["rus"] = "Гвайлодике ", ["esp"] = "Gwaelodyke " } },
            { "-6911732058344105594", new SerializableDictionary<string> { ["eng"] = "Corbenicca", ["ger"] = "Corbenicca", ["fra"] = "Corbenicca", ["pol"] = "Corbenicca", ["rus"] = "Корбеникка", ["esp"] = "Corbenicca" } },
            { "-6915080821832825201", new SerializableDictionary<string> { ["eng"] = "Whisperwind", ["ger"] = "Whisperwind", ["fra"] = "Whisperwind", ["pol"] = "Whisperwind", ["rus"] = "Виспервинд", ["esp"] = "Whisperwind" } },
            { "-6910875784341395847", new SerializableDictionary<string> { ["eng"] = "Vidumagus", ["ger"] = "Vidumagus", ["fra"] = "Vidumagus", ["pol"] = "Vidumagus", ["rus"] = "Видумаг", ["esp"] = "Vidumagus" } },
            { "-6900201788030023126", new SerializableDictionary<string> { ["eng"] = "Boudobriga", ["ger"] = "Boudobriga", ["fra"] = "Boudobriga", ["pol"] = "Boudobriga", ["rus"] = "Боудобрига", ["esp"] = "Boudobriga" } },
            { "-6900242408194250192", new SerializableDictionary<string> { ["eng"] = "Ravenbridge", ["ger"] = "Ravenbridge", ["fra"] = "Ravenbridge", ["pol"] = "Ravenbridge", ["rus"] = "Рэйвенбридж", ["esp"] = "Ravenbridge" } },
            { "-6912669644816551747", new SerializableDictionary<string> { ["eng"] = "Skelligton ", ["ger"] = "Skelligton ", ["fra"] = "Skelligton ", ["pol"] = "Skelligton ", ["rus"] = "Скеллигтон ", ["esp"] = "Skelligton " } },
            { "-6912884111367357042", new SerializableDictionary<string> { ["eng"] = "Liskearn", ["ger"] = "Liskearn", ["fra"] = "Liskearn", ["pol"] = "Liskearn", ["rus"] = "Лискярнь", ["esp"] = "Liskearn" } },
            { "-6917119349044127375", new SerializableDictionary<string> { ["eng"] = "Durodur", ["ger"] = "Durodur", ["fra"] = "Durodur", ["pol"] = "Durodur", ["rus"] = "Дуродур", ["esp"] = "Durodur" } },
            { "-6906268345724581547", new SerializableDictionary<string> { ["eng"] = "Aquae Limes", ["ger"] = "Aquae Limes", ["fra"] = "Aquae Limes", ["pol"] = "Aquae Limes", ["rus"] = "Аква Лимес", ["esp"] = "Aquae Limes" } },
            { "-6915334310311425526", new SerializableDictionary<string> { ["eng"] = "Faewynn", ["ger"] = "Faewynn", ["fra"] = "Faewynn", ["pol"] = "Faewynn", ["rus"] = "Вайуинн", ["esp"] = "Faewynn" } },
            { "-6903992419982858026", new SerializableDictionary<string> { ["eng"] = "Colfarne", ["ger"] = "Colfarne", ["fra"] = "Colfarne", ["pol"] = "Colfarne", ["rus"] = "Колфарне", ["esp"] = "Colfarne" } },
            { "-6907679479762831917", new SerializableDictionary<string> { ["eng"] = "Fiodhorum", ["ger"] = "Fiodhorum", ["fra"] = "Fiodhorum", ["pol"] = "Fiodhorum", ["rus"] = "Фиорум", ["esp"] = "Fiodhorum" } },
            { "-6905604145051777150", new SerializableDictionary<string> { ["eng"] = "Niamhbriva", ["ger"] = "Niamhbriva", ["fra"] = "Niamhbriva", ["pol"] = "Niamhbriva", ["rus"] = "Нивбрива", ["esp"] = "Niamhbriva" } },
            { "-6900591465712643009", new SerializableDictionary<string> { ["eng"] = "Addernest", ["ger"] = "Addernest", ["fra"] = "Addernest", ["pol"] = "Addernest", ["rus"] = "Аддернест", ["esp"] = "Addernest" } },
            { "-6907849321451923793", new SerializableDictionary<string> { ["eng"] = "Gwynford", ["ger"] = "Gwynford", ["fra"] = "Gwynford", ["pol"] = "Gwynford", ["rus"] = "Гуинворд", ["esp"] = "Gwynford" } },
            { "-6915360446986737458", new SerializableDictionary<string> { ["eng"] = "Pendwfr", ["ger"] = "Pendwfr", ["fra"] = "Pendwfr", ["pol"] = "Pendwfr", ["rus"] = "Пендувр", ["esp"] = "Pendwfr" } },
            { "-6904207302371337296", new SerializableDictionary<string> { ["eng"] = "Caverna", ["ger"] = "Caverna", ["fra"] = "Caverna", ["pol"] = "Caverna", ["rus"] = "Каверна", ["esp"] = "Caverna" } },
            { "-6913997781912211780", new SerializableDictionary<string> { ["eng"] = "Malladinas", ["ger"] = "Malladinas", ["fra"] = "Malladinas", ["pol"] = "Malladinas", ["rus"] = "Малладинас", ["esp"] = "Malladinas" } },
            { "-6902418748576762075", new SerializableDictionary<string> { ["eng"] = "Dubhain", ["ger"] = "Dubhain", ["fra"] = "Dubhain", ["pol"] = "Dubhain", ["rus"] = "Дувань", ["esp"] = "Dubhain" } },
            { "-6901341211508092998", new SerializableDictionary<string> { ["eng"] = "Ashleaf ", ["ger"] = "Ashleaf ", ["fra"] = "Ashleaf ", ["pol"] = "Ashleaf ", ["rus"] = "Эшлиф ", ["esp"] = "Ashleaf " } },
            { "-6909100533751412520", new SerializableDictionary<string> { ["eng"] = "Dol Ebrudes", ["ger"] = "Dol Ebrudes", ["fra"] = "Dol Ebrudes", ["pol"] = "Dol Ebrudes", ["rus"] = "Дол-Эбрудес", ["esp"] = "Dol Ebrudes" } },
            { "-6899700538004239064", new SerializableDictionary<string> { ["eng"] = "Morbryggan", ["ger"] = "Morbryggan", ["fra"] = "Morbryggan", ["pol"] = "Morbryggan", ["rus"] = "Морбриганн", ["esp"] = "Morbryggan" } },
            { "-6905861830534938664", new SerializableDictionary<string> { ["eng"] = "Cairnavon", ["ger"] = "Cairnavon", ["fra"] = "Cairnavon", ["pol"] = "Cairnavon", ["rus"] = "Кайрнауон", ["esp"] = "Cairnavon" } },
            { "-6903203074322888200", new SerializableDictionary<string> { ["eng"] = "Brigadur", ["ger"] = "Brigadur", ["fra"] = "Brigadur", ["pol"] = "Brigadur", ["rus"] = "Бригадур", ["esp"] = "Brigadur" } },
            { "-6900630012654674281", new SerializableDictionary<string> { ["eng"] = "Axby", ["ger"] = "Axby", ["fra"] = "Axby", ["pol"] = "Axby", ["rus"] = "Аксби", ["esp"] = "Axby" } },
            { "-6906900125807966870", new SerializableDictionary<string> { ["eng"] = "Din Filid", ["ger"] = "Din Filid", ["fra"] = "Din Filid", ["pol"] = "Din Filid", ["rus"] = "Динь-Филид", ["esp"] = "Din Filid" } },
            { "-6900755104351755208", new SerializableDictionary<string> { ["eng"] = "Breannwod", ["ger"] = "Breannwod", ["fra"] = "Breannwod", ["pol"] = "Breannwod", ["rus"] = "Бреаннуод", ["esp"] = "Breannwod" } },
            { "-6910027691632077087", new SerializableDictionary<string> { ["eng"] = "Fayrford", ["ger"] = "Fayrford", ["fra"] = "Fayrford", ["pol"] = "Fayrford", ["rus"] = "Файрфорд", ["esp"] = "Fayrford" } },
            { "-6901666726645576120", new SerializableDictionary<string> { ["eng"] = "Brackwater", ["ger"] = "Brackwater", ["fra"] = "Brackwater", ["pol"] = "Brackwater", ["rus"] = "Бракуотер", ["esp"] = "Brackwater" } },
            { "-6907493066637101874", new SerializableDictionary<string> { ["eng"] = "Llanfairgwyndllbuidhebeag", ["ger"] = "Llanfairgwyndllbuidhebeag", ["fra"] = "Llanfairgwyndllbuidhebeag", ["pol"] = "Llanfairgwyndllbuidhebeag", ["rus"] = "Лланвайргуиндллбиебягь", ["esp"] = "Llanfairgwyndllbuidhebeag" } },
            { "-6906399454716494546", new SerializableDictionary<string> { ["eng"] = "Cataracta", ["ger"] = "Cataracta", ["fra"] = "Cataracta", ["pol"] = "Cataracta", ["rus"] = "Катаракта", ["esp"] = "Cataracta" } },
            { "-6904896562297208780", new SerializableDictionary<string> { ["eng"] = "Pendlecraig", ["ger"] = "Pendlecraig", ["fra"] = "Pendlecraig", ["pol"] = "Pendlecraig", ["rus"] = "Пенделкрайг", ["esp"] = "Pendlecraig" } },
            { "-6911130874658447669", new SerializableDictionary<string> { ["eng"] = "Morringar", ["ger"] = "Morringar", ["fra"] = "Morringar", ["pol"] = "Morringar", ["rus"] = "Моррингар", ["esp"] = "Morringar" } },
            { "-6909598199131612042", new SerializableDictionary<string> { ["eng"] = "Fishcroft", ["ger"] = "Fishcroft", ["fra"] = "Fishcroft", ["pol"] = "Fishcroft", ["rus"] = "Фишкрофт", ["esp"] = "Fishcroft" } },
            { "-6911484603292727851", new SerializableDictionary<string> { ["eng"] = "Creechfyrdd", ["ger"] = "Creechfyrdd", ["fra"] = "Creechfyrdd", ["pol"] = "Creechfyrdd", ["rus"] = "Крихвирт", ["esp"] = "Creechfyrdd" } },
            { "-6903021866628355634", new SerializableDictionary<string> { ["eng"] = "Dundover", ["ger"] = "Dundover", ["fra"] = "Dundover", ["pol"] = "Dundover", ["rus"] = "Диндовер", ["esp"] = "Dundover" } },
            { "-6915143815507781936", new SerializableDictionary<string> { ["eng"] = "Rosethorn", ["ger"] = "Rosethorn", ["fra"] = "Rosethorn", ["pol"] = "Rosethorn", ["rus"] = "Роузторн", ["esp"] = "Rosethorn" } },
            { "-6915378973575606765", new SerializableDictionary<string> { ["eng"] = "Fenwick", ["ger"] = "Fenwick", ["fra"] = "Fenwick", ["pol"] = "Fenwick", ["rus"] = "Фенуик", ["esp"] = "Fenwick" } },
            { "-6913067428771638117", new SerializableDictionary<string> { ["eng"] = "Otterston", ["ger"] = "Otterston", ["fra"] = "Otterston", ["pol"] = "Otterston", ["rus"] = "Оттерстон", ["esp"] = "Otterston" } },
            { "-6916746368506876478", new SerializableDictionary<string> { ["eng"] = "Lindisfrome", ["ger"] = "Lindisfrome", ["fra"] = "Lindisfrome", ["pol"] = "Lindisfrome", ["rus"] = "Линдисфром", ["esp"] = "Lindisfrome" } },
            { "-6902441395709243747", new SerializableDictionary<string> { ["eng"] = "Allwell", ["ger"] = "Allwell", ["fra"] = "Allwell", ["pol"] = "Allwell", ["rus"] = "Оллуэлл", ["esp"] = "Allwell" } },
            { "-6913715715785112149", new SerializableDictionary<string> { ["eng"] = "Haredale", ["ger"] = "Haredale", ["fra"] = "Haredale", ["pol"] = "Haredale", ["rus"] = "Хейрдейл", ["esp"] = "Haredale" } },
            { "-6900507541041108544", new SerializableDictionary<string> { ["eng"] = "Brigantium ", ["ger"] = "Brigantium ", ["fra"] = "Brigantium ", ["pol"] = "Brigantium ", ["rus"] = "Бригантиум ", ["esp"] = "Brigantium " } },
            { "-6899820284887359025", new SerializableDictionary<string> { ["eng"] = "Forest of Wren", ["ger"] = "Forest of Wren", ["fra"] = "Bois de Wren", ["pol"] = "Forest of Wren", ["rus"] = "Лес Рена", ["esp"] = "Wrenforest" } },
            { "-6908878040335532089", new SerializableDictionary<string> { ["eng"] = "Elvyn", ["ger"] = "Elvyn", ["fra"] = "Elvyn", ["pol"] = "Elvyn", ["rus"] = "Элвин", ["esp"] = "Elvyn" } },
            { "-6911876674942903896", new SerializableDictionary<string> { ["eng"] = "Willowfax", ["ger"] = "Willowfax", ["fra"] = "Willowfax", ["pol"] = "Willowfax", ["rus"] = "Уиллоуфакс", ["esp"] = "Willowfax" } },
            { "-6907561115564480647", new SerializableDictionary<string> { ["eng"] = "Isca Laetitia", ["ger"] = "Isca Laetitia", ["fra"] = "Isca Laetitia", ["pol"] = "Isca Laetitia", ["rus"] = "Иска Летиция", ["esp"] = "Isca Laetitia" } },
            { "-6907625315967782929", new SerializableDictionary<string> { ["eng"] = "Blaecford", ["ger"] = "Blaecford", ["fra"] = "Blaecford", ["pol"] = "Blaecford", ["rus"] = "Блайкфорд", ["esp"] = "Blaecford" } },
            { "-6917041600678347420", new SerializableDictionary<string> { ["eng"] = "Benbulbin", ["ger"] = "Benbulbin", ["fra"] = "Benbulbin", ["pol"] = "Benbulbin", ["rus"] = "Бенбильбин", ["esp"] = "Benbulbin" } },
            { "-6912626687885890053", new SerializableDictionary<string> { ["eng"] = "Iskaleith", ["ger"] = "Iskaleith", ["fra"] = "Iskaleith", ["pol"] = "Iskaleith", ["rus"] = "Искалайт", ["esp"] = "Iskaleith" } },
            { "-6916617506761911576", new SerializableDictionary<string> { ["eng"] = "Pengynt", ["ger"] = "Pengynt", ["fra"] = "Pengynt", ["pol"] = "Pengynt", ["rus"] = "Пенгинт", ["esp"] = "Pengynt" } },
            { "-6916888661756724634", new SerializableDictionary<string> { ["eng"] = "Thadochasannum", ["ger"] = "Thadochasannum", ["fra"] = "Thadochasannum", ["pol"] = "Thadochasannum", ["rus"] = "Тадохасаннум", ["esp"] = "Thadochasannum" } },
            { "-6916932756352264268", new SerializableDictionary<string> { ["eng"] = "Hadrianum", ["ger"] = "Hadrianum", ["fra"] = "Hadrianum", ["pol"] = "Hadrianum", ["rus"] = "Адрианум", ["esp"] = "Hadrianum" } },
            { "-6905031042100912764", new SerializableDictionary<string> { ["eng"] = "Nevrmoor", ["ger"] = "Nevrmoor", ["fra"] = "Nevrmoor", ["pol"] = "Nevrmoor", ["rus"] = "Неврмоор", ["esp"] = "Nevrmoor" } },
            { "-6904078543127532725", new SerializableDictionary<string> { ["eng"] = "Argantum", ["ger"] = "Argantum", ["fra"] = "Argantum", ["pol"] = "Argantum", ["rus"] = "Аргантум", ["esp"] = "Argantum" } },
            { "-6913724717389365158", new SerializableDictionary<string> { ["eng"] = "Ethelrhos", ["ger"] = "Ethelrhos", ["fra"] = "Ethelrhos", ["pol"] = "Ethelrhos", ["rus"] = "Этелрос", ["esp"] = "Ethelrhos" } },
            { "-6911582677813858129", new SerializableDictionary<string> { ["eng"] = "Barrenburgh", ["ger"] = "Barrenburgh", ["fra"] = "Barrenburgh", ["pol"] = "Barrenburgh", ["rus"] = "Барренбур", ["esp"] = "Barrenburgh" } },
            { "-6902226141082502021", new SerializableDictionary<string> { ["eng"] = "Pendaestum", ["ger"] = "Pendaestum", ["fra"] = "Pendaestum", ["pol"] = "Pendaestum", ["rus"] = "Пендеструм", ["esp"] = "Pendaestum" } },
            { "-6916542042023286770", new SerializableDictionary<string> { ["eng"] = "Llancros", ["ger"] = "Llancros", ["fra"] = "Llancros", ["pol"] = "Llancros", ["rus"] = "Лланкрос", ["esp"] = "Llancros" } },
            { "-6910884123183689839", new SerializableDictionary<string> { ["eng"] = "Waystone ", ["ger"] = "Waystone ", ["fra"] = "Waystone ", ["pol"] = "Waystone ", ["rus"] = "Уэйстоун ", ["esp"] = "Waystone " } },
            { "-6902912422379203435", new SerializableDictionary<string> { ["eng"] = "Mistletown ", ["ger"] = "Mistletown ", ["fra"] = "Mistletown ", ["pol"] = "Mistletown ", ["rus"] = "Мислтаун ", ["esp"] = "Mistletown " } },
            { "-6909681294622724638", new SerializableDictionary<string> { ["eng"] = "Aquae Eponeia", ["ger"] = "Aquae Eponeia", ["fra"] = "Aquae Eponeia", ["pol"] = "Aquae Eponeia", ["rus"] = "Аква Эпонея", ["esp"] = "Aquae Eponeia" } },
            { "-6903671515238804754", new SerializableDictionary<string> { ["eng"] = "Greyhaven", ["ger"] = "Greyhaven", ["fra"] = "Havres-Gris", ["pol"] = "Greyhaven", ["rus"] = "Грейхейвен", ["esp"] = "Greyhaven" } },
            { "-6905631907619708923", new SerializableDictionary<string> { ["eng"] = "Vindocarrium", ["ger"] = "Vindocarrium", ["fra"] = "Vindocarrium", ["pol"] = "Vindocarrium", ["rus"] = "Виндокарриум", ["esp"] = "Vindocarrium" } },
            { "-6912277273766206665", new SerializableDictionary<string> { ["eng"] = "Muir Sinon", ["ger"] = "Muir Sinon", ["fra"] = "Muir Sinon", ["pol"] = "Muir Sinon", ["rus"] = "Мирь-Шинон", ["esp"] = "Muir Sinon" } },
            { "-6901143251590461575", new SerializableDictionary<string> { ["eng"] = "Margum", ["ger"] = "Margum", ["fra"] = "Margum", ["pol"] = "Margum", ["rus"] = "Маргум", ["esp"] = "Margum" } },
            { "-6900721420940109177", new SerializableDictionary<string> { ["eng"] = "Cantius", ["ger"] = "Cantius", ["fra"] = "Cantius", ["pol"] = "Cantius", ["rus"] = "Кантий", ["esp"] = "Cantius" } },
            { "-6901630553193357943", new SerializableDictionary<string> { ["eng"] = "Gavelock", ["ger"] = "Gavelock", ["fra"] = "Gavelock", ["pol"] = "Gavelock", ["rus"] = "Гавлок", ["esp"] = "Gavelock" } },
            { "-6902045416000219871", new SerializableDictionary<string> { ["eng"] = "Hogsark", ["ger"] = "Hogsark", ["fra"] = "Hogsark", ["pol"] = "Hogsark", ["rus"] = "Хогсарк", ["esp"] = "Hogsark" } },
            { "-6905523335631708229", new SerializableDictionary<string> { ["eng"] = "Brockstor", ["ger"] = "Brockstor", ["fra"] = "Brockstor", ["pol"] = "Brockstor", ["rus"] = "Брокстор", ["esp"] = "Brockstor" } },
            { "-6900205689620972484", new SerializableDictionary<string> { ["eng"] = "Aberbriar", ["ger"] = "Aberbriar", ["fra"] = "Aberbriar", ["pol"] = "Aberbriar", ["rus"] = "Абербриар", ["esp"] = "Aberbriar" } },
            { "-6916618405273274608", new SerializableDictionary<string> { ["eng"] = "Temetywyll", ["ger"] = "Temetywyll", ["fra"] = "Temetywyll", ["pol"] = "Temetywyll", ["rus"] = "Теметиуилл", ["esp"] = "Temetywyll" } },
            { "-6902025404426899585", new SerializableDictionary<string> { ["eng"] = "Barkum", ["ger"] = "Barkum", ["fra"] = "Barkum", ["pol"] = "Barkum", ["rus"] = "Баркум", ["esp"] = "Barkum" } },
            { "-6901381828065168954", new SerializableDictionary<string> { ["eng"] = "Meriscum", ["ger"] = "Meriscum", ["fra"] = "Meriscum", ["pol"] = "Meriscum", ["rus"] = "Мерискум", ["esp"] = "Meriscum" } },
            { "-6907932783767634302", new SerializableDictionary<string> { ["eng"] = "Rhiannovum", ["ger"] = "Rhiannovum", ["fra"] = "Rhiannovum", ["pol"] = "Rhiannovum", ["rus"] = "Рианновум", ["esp"] = "Rhiannovum" } },
            { "-6900742136016623019", new SerializableDictionary<string> { ["eng"] = "Skeog", ["ger"] = "Skeog", ["fra"] = "Skeog", ["pol"] = "Skeog", ["rus"] = "Шкёгь", ["esp"] = "Skeog" } },
        };

        // extracted from .a7minfo files
        private static readonly Dictionary<string, Size<int>> IslandSizes = new Dictionary<string, Size<int>>
        {
            // campaign (Marcia) player islands, not in the base pool. outlines came from the province RDAs
            { "roman_island_campaign_player_01", new Size<int>(512, 512) },
            { "celtic_island_campaign_player_01", new Size<int>(320, 320) },
            { "celtic_island_campaign_player_02", new Size<int>(512, 512) },
            { "celtic_island_campaign_player_03", new Size<int>(320, 320) },
            // dlc01 pool islands
            { "roman_dlc01_island_continental_01", new Size<int>(768, 768) },
            { "roman_dlc01_island_medium_01", new Size<int>(320, 320) },
            { "roman_dlc01_island_medium_02", new Size<int>(320, 320) },
            { "roman_dlc01_island_medium_03", new Size<int>(320, 320) },
            { "roman_dlc01_island_small_02", new Size<int>(256, 256) },
            { "roman_dlc_01_island_small_01", new Size<int>(256, 256) },
            { "roman_island_extralarge_01", new Size<int>(512, 512) },
            { "roman_island_extralarge_02", new Size<int>(448, 448) },
            { "roman_island_extralarge_03", new Size<int>(512, 512) },
            { "roman_island_extralarge_04", new Size<int>(512, 512) },
            { "roman_island_large_01", new Size<int>(512, 512) },
            { "roman_island_large_02", new Size<int>(512, 512) },
            { "roman_island_large_03", new Size<int>(512, 512) },
            { "roman_island_large_04", new Size<int>(512, 512) },
            { "roman_island_large_05", new Size<int>(512, 512) },
            { "roman_island_large_06", new Size<int>(384, 384) },
            { "roman_island_large_07", new Size<int>(512, 512) },
            { "roman_island_large_09", new Size<int>(512, 512) },
            { "roman_island_medium_01", new Size<int>(320, 320) },
            { "roman_island_medium_02", new Size<int>(256, 256) },
            { "roman_island_medium_03", new Size<int>(320, 320) },
            { "roman_island_medium_04", new Size<int>(320, 320) },
            { "roman_island_medium_05", new Size<int>(256, 256) },
            { "roman_island_medium_06", new Size<int>(320, 320) },
            { "roman_island_medium_07", new Size<int>(320, 320) },
            { "roman_island_medium_08", new Size<int>(320, 320) },
            { "roman_island_small_01", new Size<int>(256, 256) },
            { "roman_island_small_02", new Size<int>(192, 192) },
            { "roman_island_small_03", new Size<int>(256, 256) },
            { "roman_island_small_04", new Size<int>(256, 256) },
            { "roman_island_small_05", new Size<int>(256, 256) },
            { "roman_island_small_06", new Size<int>(256, 256) },
            { "roman_island_small_07", new Size<int>(256, 256) },
            { "celtic_island_large_01", new Size<int>(512, 512) },
            { "celtic_island_large_02", new Size<int>(512, 512) },
            { "celtic_island_large_03", new Size<int>(384, 384) },
            { "celtic_island_large_04", new Size<int>(512, 512) },
            { "celtic_island_large_05", new Size<int>(512, 512) },
            { "celtic_island_large_06", new Size<int>(384, 384) },
            { "celtic_island_large_07", new Size<int>(320, 320) },
            { "celtic_island_large_08", new Size<int>(384, 384) },
            { "celtic_island_medium_01", new Size<int>(256, 256) },
            { "celtic_island_medium_02", new Size<int>(256, 256) },
            { "celtic_island_medium_03", new Size<int>(256, 256) },
            { "celtic_island_medium_04", new Size<int>(256, 256) },
            { "celtic_island_medium_05", new Size<int>(320, 320) },
            { "celtic_island_medium_06", new Size<int>(320, 320) },
            { "celtic_island_medium_07", new Size<int>(256, 256) },
            { "celtic_island_small_01", new Size<int>(256, 256) },
            { "celtic_island_small_02", new Size<int>(256, 256) },
            { "celtic_island_small_03", new Size<int>(256, 256) },
            { "celtic_island_small_04", new Size<int>(256, 256) },
            { "celtic_island_small_05", new Size<int>(256, 256) },
            { "celtic_island_small_06", new Size<int>(192, 192) },
            { "celtic_island_small_07", new Size<int>(192, 192) },
        };

        /// <summary>
        /// Searchs within the <paramref name="templateElements"/> for the island that contains the <paramref name="gameObjects"/> based on their position.
        /// </summary>
        private static Island CreateIsland(string cityName, IEnumerable<Tag> gameObjects, IEnumerable<Tag> templateElements, ZipArchive outlines)
        {
            foreach (Tag element in templateElements)
            {
                string islandTemplate = Path.GetFileNameWithoutExtension(element.Attribute("MapFilePath").ToUnicode());
                Point2D<int> islandPosition = element.Attribute("Position").ToPoint2D<int>();

                if (IslandSizes.TryGetValue(islandTemplate, out Size<int> islandSize))
                {
                    Rectangle<int> islandRectangle = new Rectangle<int>(islandPosition.X, islandPosition.Y, islandSize.Width, islandSize.Height);
                    GridDirection islandRotation = (GridDirection)element.Attribute("Rotation90").ToNumber<byte>();

                    if (islandRectangle.ContainsAll(gameObjects))
                    {
                        var island = new Island(cityName, islandTemplate, islandPosition, islandRotation, islandSize, outlines.GetEntry(islandTemplate + ".ad"));
                        List<Tag> ownerObjects = gameObjects.Where(o => o.Tag("ModuleOwner") != null)
                            .DistinctBy(o => o.Attribute("ID").ToNumber<long>())
                            .ToList();

                        for (int i = 0; i < ownerObjects.Count; i++)
                        {
                            Tag moduleOwner = ownerObjects[i].Tag("ModuleOwner");
                            long ownerId = moduleOwner.Parent.Attribute("ID").ToNumber<long>();
                            // colour a farm and its fields by building guid (the crop), not list position,
                            // so every farm of the same crop shares a colour instead of cycling per instance
                            int ownerGuid = ownerObjects[i].Attribute("Guid").ToNumber<int>();
                            var color = Modules.Colors[Math.Abs(ownerGuid) % Modules.Colors.Count];
                            island.Colors[ownerId] = color;

                            if (moduleOwner.Attribute("BinArray") != null)
                            {
                                List<long> modules = moduleOwner.Attribute("BinArray").ToNumbers<long>()
                                    .SkipLast(1) // BinArray has 1 element more than owner has modules, unclear what this element is though so skip it for now
                                    .ToList();

                                long moduleId = modules[0];
                                island.Colors[moduleId] = color;

                                for (int j = 1; j < modules.Count; j++)
                                {
                                    moduleId += modules[j] + 1;
                                    island.Colors[moduleId] = color;
                                }
                            }
                        }

                        return island;
                    }
                }
            }

            // island isn't in the size database (detection is still incomplete), so fall back to a
            // bounding box off the game objects. buildings still import at the right relative positions
            var fallbackPoints = gameObjects
                .Where(o => o.Attribute("Position") != null)
                .Select(o => o.Attribute("Position").ToPoint2D<float>())
                .ToList();
            if (fallbackPoints.Count == 0) throw new Exception("No matching island found and no objects to derive one from!");
            int minX = (int)Math.Floor(fallbackPoints.Min(p => p.X)) - 2;
            int minY = (int)Math.Floor(fallbackPoints.Min(p => p.Y)) - 2;
            int maxX = (int)Math.Ceiling(fallbackPoints.Max(p => p.X)) + 2;
            int maxY = (int)Math.Ceiling(fallbackPoints.Max(p => p.Y)) + 2;
            return new Island(cityName, "fallback", new Point2D<int>(minX, minY), GridDirection.Up, new Size<int>(maxX - minX, maxY - minY), null);
        }

        #endregion
    }
}
