using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Extensions;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.ViewModels
{
    public partial class StatisticsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private string usedArea;

        [ObservableProperty]
        private double usedTiles;

        [ObservableProperty]
        private double minTiles;

        [ObservableProperty]
        private string efficiency;

        [ObservableProperty]
        private bool areStatisticsAvailable;

        [ObservableProperty]
        private bool showBuildingList;

        [ObservableProperty]
        private bool showStatisticsBuildingCount;
        //private bool _showSelectedBuildingList;
        [ObservableProperty]
        private ObservableCollection<StatisticsBuilding> buildings = new ObservableCollection<StatisticsBuilding>();

        [ObservableProperty]
        private ObservableCollection<StatisticsBuilding> selectedBuildings = new ObservableCollection<StatisticsBuilding>();
        private readonly StatisticsCalculationHelper _statisticsCalculationHelper;
        private readonly ConcurrentDictionary<string, BuildingInfo> _cachedPresetsBuilding;
        private readonly ILocalizationHelper _localizationHelper;
        private readonly ICommons _commons;
        private readonly IAppSettings _appSettings;

        public StatisticsViewModel(ILocalizationHelper localizationHelperToUse, ICommons commonsToUse, IAppSettings appSettingsToUse)
        {
            _localizationHelper = localizationHelperToUse;
            _commons = commonsToUse;
            _appSettings = appSettingsToUse;

            UsedArea = "12x4";
            UsedTiles = 308;
            MinTiles = 48;
            Efficiency = "16%";
            AreStatisticsAvailable = true;

            ShowBuildingList = true;
            Buildings = new ObservableCollection<StatisticsBuilding>();
            SelectedBuildings = new ObservableCollection<StatisticsBuilding>();
            _statisticsCalculationHelper = new StatisticsCalculationHelper();
            _cachedPresetsBuilding = new ConcurrentDictionary<string, BuildingInfo>(Environment.ProcessorCount, 50);
        }



        public bool ShowSelectedBuildingList
        {
            get { return ShowBuildingList && SelectedBuildings.Any(); }
        }



        public void ToggleBuildingList(bool showBuildingList, IList<LayoutObject> placedObjects, ICollection<LayoutObject> selectedObjects, BuildingPresets buildingPresets)
        {
            ShowBuildingList = showBuildingList;
            if (showBuildingList)
            {
                _ = UpdateStatisticsAsync(UpdateMode.All, placedObjects, selectedObjects, buildingPresets);
            }
        }

        public async Task UpdateStatisticsAsync(UpdateMode mode,
            IList<LayoutObject> placedObjects,
            ICollection<LayoutObject> selectedObjects,
            BuildingPresets buildingPresets)
        {
            if (placedObjects.Count == 0)
            {
                AreStatisticsAvailable = false;
                return;
            }

            AreStatisticsAvailable = true;

            var calculateStatisticsTask = Task.Run(() => _statisticsCalculationHelper.CalculateStatistics(placedObjects.Select(_ => _.WrappedAnnoObject), includeRoads: _appSettings.IncludeRoadsInStatisticCalculation));

            if (mode != UpdateMode.NoBuildingList && ShowBuildingList)
            {
                var groupedPlacedBuildings = placedObjects.GroupBy(_ => _.Identifier).ToList();

                IEnumerable<IGrouping<string, LayoutObject>> groupedSelectedBuildings = null;
                if (selectedObjects != null && selectedObjects.Count > 0)
                {
                    groupedSelectedBuildings = selectedObjects.Where(_ => _ != null).GroupBy(_ => _.Identifier).ToList();
                }

                var buildingsTask = Task.Run(() => GetStatisticBuildings(groupedPlacedBuildings, buildingPresets));
                var selectedBuildingsTask = Task.Run(() => GetStatisticBuildings(groupedSelectedBuildings, buildingPresets));
                SelectedBuildings = await selectedBuildingsTask;
                Buildings = await buildingsTask;
            }

            var calculatedStatistics = await calculateStatisticsTask;

            UsedArea = $"{calculatedStatistics.UsedAreaWidth}x{calculatedStatistics.UsedAreaHeight}";
            UsedTiles = calculatedStatistics.UsedTiles;
            MinTiles = calculatedStatistics.MinTiles;
            Efficiency = $"{calculatedStatistics.Efficiency}%";
        }

        private ObservableCollection<StatisticsBuilding> GetStatisticBuildings(IEnumerable<IGrouping<string, LayoutObject>> groupedBuildingsByIdentifier, BuildingPresets buildingPresets)
        {
            if (groupedBuildingsByIdentifier is null || !groupedBuildingsByIdentifier.Any())
            {
                return new ObservableCollection<StatisticsBuilding>();
            }

            var tempList = new List<StatisticsBuilding>();

            var validBuildingsGrouped = groupedBuildingsByIdentifier
                        .Where(_ => (!_.ElementAt(0).WrappedAnnoObject.Road || _appSettings.IncludeRoadsInStatisticCalculation) && _.ElementAt(0).Identifier != null)
                        .Where(x => x.AsEnumerable().WithoutIgnoredObjects().Count() > 0)
                        .OrderByDescending(_ => _.Count());
            foreach (var item in validBuildingsGrouped)
            {
                var statisticBuilding = new StatisticsBuilding();

                var identifierToCheck = item.ElementAt(0).Identifier;
                if (!string.IsNullOrWhiteSpace(identifierToCheck))
                {
                    //try to find building in presets by identifier
                    if (!_cachedPresetsBuilding.TryGetValue(identifierToCheck, out var building))
                    {
                        building = buildingPresets.Buildings.Find(_ => string.Equals(_.Identifier, identifierToCheck, StringComparison.OrdinalIgnoreCase));
                        _ = _cachedPresetsBuilding.TryAdd(identifierToCheck, building);
                    }

                    var isUnknownObject = string.Equals(identifierToCheck, "Unknown Object", StringComparison.OrdinalIgnoreCase);
                    if (building != null || isUnknownObject)
                    {
                        statisticBuilding.Count = item.Count();
                        statisticBuilding.Name = isUnknownObject ? _localizationHelper.GetLocalization("UnknownObject") : building.Localization[_commons.CurrentLanguageCode];
                    }
                    else
                    {
                        // Ruled those 2 out to keep Building Name Changes done through the Labeling of the building
                        // and when the building is not in the preset. Those statisticBuildings.name will not translated to
                        // other luangages anymore, as users can give there own names.
                        // However i made it so, that if localizations get those translations, it will translated.
                        // 06-02-2021, on request of user(s) on Discord read this on
                        // https://discord.com/channels/571011757317947406/571011757317947410/800118895855665203
                        //item.ElementAt(0).Identifier = "";
                        //statisticBuilding.Name = _localizationHelper.GetLocalization("StatNameNotFound");

                        statisticBuilding.Count = item.Count();
                        statisticBuilding.Name = _localizationHelper.GetLocalization(item.ElementAt(0).Identifier);
                    }
                }
                else
                {
                    statisticBuilding.Count = item.Count();
                    statisticBuilding.Name = _localizationHelper.GetLocalization("StatNameNotFound");
                }

                tempList.Add(statisticBuilding);
            }

            return new ObservableCollection<StatisticsBuilding>(tempList.OrderByDescending(x => x.Count).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
        }

        // Generated partial change handlers from [ObservableProperty]
        partial void OnShowBuildingListChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowSelectedBuildingList));
        }

        partial void OnSelectedBuildingsChanged(ObservableCollection<StatisticsBuilding> value)
        {
            OnPropertyChanged(nameof(ShowSelectedBuildingList));
        }
    }
}
