using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnnoDesigner.Controls.Canvas;
using AnnoDesigner.Core;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Layout.Exceptions;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Helper;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.CustomEventArgs;
using AnnoDesigner.Extensions;
using AnnoDesigner.Helper;
using AnnoDesigner.Localization;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.PreferencesPages;
using AnnoDesigner.Services.Undo.Operations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NLog;

namespace AnnoDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ICommons _commons;
        private readonly IAppSettings _appSettings;
        private readonly ILayoutLoader _layoutLoader;
        private readonly ICoordinateHelper _coordinateHelper;
        private readonly IBrushCache _brushCache;
        private readonly IPenCache _penCache;
        private readonly IAdjacentCellGrouper _adjacentCellGrouper;
        private readonly IRecentFilesHelper _recentFilesHelper;
        private readonly IMessageBoxService _messageBoxService;
        private readonly ILocalizationHelper _localizationHelper;
        private readonly IFileSystem _fileSystem;

        public event EventHandler<EventArgs> ShowStatisticsChanged;

        private Dictionary<int, bool> _treeViewState;
        private readonly TreeLocalizationContainer _treeLocalizationContainer;

        //for identifier checking process
        private static readonly List<string> IconFieldNamesCheck =
            ["icon_116_22", "icon_27_6", "field", "general_module"];

        private readonly IconImage _noIconItem;

        public MainViewModel(ICommons commonsToUse,
            IAppSettings appSettingsToUse,
            IRecentFilesHelper recentFilesHelperToUse,
            IMessageBoxService messageBoxServiceToUse,
            IUpdateHelper updateHelperToUse,
            ILocalizationHelper localizationHelperToUse,
            IFileSystem fileSystemToUse,
            ILayoutLoader layoutLoaderToUse = null,
            ICoordinateHelper coordinateHelperToUse = null,
            IBrushCache brushCacheToUse = null,
            IPenCache penCacheToUse = null,
            IAdjacentCellGrouper adjacentCellGrouper = null,
            ITreeLocalizationLoader treeLocalizationLoader = null)
        {
            _commons = commonsToUse;
            _commons.SelectedLanguageChanged += Commons_SelectedLanguageChanged;

            _appSettings = appSettingsToUse;
            _appSettings.SettingsChanged += AppSettings_SettingsChanged;
            _recentFilesHelper = recentFilesHelperToUse;
            _messageBoxService = messageBoxServiceToUse;
            _localizationHelper = localizationHelperToUse;
            _fileSystem = fileSystemToUse;

            _layoutLoader = layoutLoaderToUse ?? new LayoutLoader();
            _coordinateHelper = coordinateHelperToUse ?? new CoordinateHelper();
            _brushCache = brushCacheToUse ?? new BrushCache();
            _penCache = penCacheToUse ?? new PenCache();
            _adjacentCellGrouper = adjacentCellGrouper ?? new AdjacentCellGrouper();

            HotkeyCommandManager = new HotkeyCommandManager(_localizationHelper);

            StatisticsViewModel = new StatisticsViewModel(_localizationHelper, _commons, appSettingsToUse)
            {
                IsVisible = _appSettings.StatsShowStats,
                ShowStatisticsBuildingCount = _appSettings.StatsShowBuildingCount
            };

            BuildingSettingsViewModel =
                new BuildingSettingsViewModel(_appSettings, _messageBoxService, _localizationHelper);

            // load tree localization            
            try
            {
                _treeLocalizationContainer = treeLocalizationLoader?.LoadFromFile(
                    _fileSystem.Path.Combine(App.ApplicationPath, CoreConstants.PresetsFiles.TreeLocalizationFile));
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError(ex.Message,
                    _localizationHelper.GetLocalization("LoadingTreeLocalizationFailed"));
            }

            PresetsTreeViewModel =
                new PresetsTreeViewModel(new TreeLocalization(_commons, _treeLocalizationContainer), _commons);
            PresetsTreeViewModel.ApplySelectedItem += PresetTreeViewModel_ApplySelectedItem;

            PresetsTreeSearchViewModel = new PresetsTreeSearchViewModel();
            PresetsTreeSearchViewModel.PropertyChanged += PresetsTreeSearchViewModel_PropertyChanged;

            WelcomeViewModel = new WelcomeViewModel(_commons, _appSettings);

            AboutViewModel = new AboutViewModel();

            PreferencesUpdateViewModel = new UpdateSettingsViewModel(_commons, _appSettings, _messageBoxService,
                updateHelperToUse, _localizationHelper);
            PreferencesKeyBindingsViewModel = new ManageKeybindingsViewModel(HotkeyCommandManager, _commons,
                _messageBoxService, _localizationHelper);
            PreferencesGeneralViewModel = new GeneralSettingsViewModel(_appSettings, _commons, _recentFilesHelper);

            LayoutSettingsViewModel = new LayoutSettingsViewModel();
            LayoutSettingsViewModel.PropertyChangedWithValues += LayoutSettingsViewModel_PropertyChangedWithValues;


            AvailableIcons = [];
            _noIconItem = GenerateNoIconItem();
            AvailableIcons.Add(_noIconItem);
            SelectedIcon = _noIconItem;

            RecentFiles = [];
            _recentFilesHelper.Updated += RecentFilesHelper_Updated;

            Languages =
            [
                new SupportedLanguage("English") { FlagPath = "Assets/Flags/United Kingdom.png" },
                new SupportedLanguage("Deutsch") { FlagPath = "Assets/Flags/Germany.png" },
                new SupportedLanguage("Français") { FlagPath = "Assets/Flags/France.png" },
                new SupportedLanguage("Polski") { FlagPath = "Assets/Flags/Poland.png" },
                new SupportedLanguage("Русский") { FlagPath = "Assets/Flags/Russia.png" },
                new SupportedLanguage("Español") { FlagPath = "Assets/Flags/Spain.png" },
            ];
            //Languages.Add(new SupportedLanguage("Italiano"));
            //Languages.Add(new SupportedLanguage("český"));

            MainWindowTitle = "Anno Designer";
            PresetsSectionHeader = "Building presets - not loaded";

            PreferencesUpdateViewModel.VersionValue = Constants.Version.ToString();
            PreferencesUpdateViewModel.FileVersionValue =
                CoreConstants.LayoutFileVersion.ToString("0.#", CultureInfo.InvariantCulture);

            RecentFilesHelper_Updated(this, EventArgs.Empty);
        }

        private void LayoutSettingsViewModel_PropertyChangedWithValues(object sender,
            PropertyChangedWithValuesEventArgs<object> e)
        {
            if (string.Equals(e.PropertyName, nameof(LayoutSettingsViewModel.LayoutVersion),
                    StringComparison.OrdinalIgnoreCase))
            {
                AnnoCanvas.UndoManager.RegisterOperation(new ModifyLayoutVersionOperation()
                {
                    LayoutSettingsViewModel = sender as LayoutSettingsViewModel,
                    OldValue = e.OldValue as Version,
                    NewValue = e.NewValue as Version,
                });
            }
        }

        private IconImage GenerateNoIconItem()
        {
            var localizations = new Dictionary<string, string>();

            foreach (var curLanguageCode in _commons.LanguageCodeMap.Values)
            {
                var curTranslationOfNone = _localizationHelper.GetLocalization("NoIcon", curLanguageCode);
                localizations.Add(curLanguageCode, curTranslationOfNone);
            }

            return new IconImage("NoIcon") { Localizations = localizations };
        }

        private void Commons_SelectedLanguageChanged(object sender, EventArgs e)
        {
            try
            {
                InitLanguageMenu(_commons.CurrentLanguage);

                if (AnnoCanvas == null)
                {
                    return;
                }

                RepopulateTreeView();

                PopulateSuggestions();

                BuildingSettingsViewModel.UpdateLanguageBuildingInfluenceType();

                //update settings
                _appSettings.SelectedLanguage = _commons.CurrentLanguage;

                _ = UpdateStatisticsAsync(UpdateMode.All);

                PresetsTreeSearchViewModel.SearchText = string.Empty;
                HotkeyCommandManager.UpdateLanguage();

                AvailableIcons.Clear();
                AvailableIcons.Add(_noIconItem);
                LoadAvailableIcons();
                SelectedIcon = _noIconItem;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error when changing the language.");
            }
            finally
            {
                IsLanguageChange = false;
            }
        }

        private void AppSettings_SettingsChanged(object sender, EventArgs e)
        {
            _ = UpdateStatisticsAsync(UpdateMode.All);
        }

        private void RecentFilesHelper_Updated(object sender, EventArgs e)
        {
            RecentFiles.Clear();

            foreach (var curRecentFile in _recentFilesHelper.RecentFiles)
            {
                RecentFiles.Add(new RecentFileItem(curRecentFile.Path));
            }

            OnPropertyChanged(nameof(HasRecentFiles));
        }

        private void PresetsTreeSearchViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: PresetsTreeSearchViewModel PropertyChanged: {e.PropertyName}");
            System.Console.WriteLine($"DEBUG: PresetsTreeSearchViewModel PropertyChanged: {e.PropertyName}");
            
            if (string.Equals(e.PropertyName, nameof(PresetsTreeSearchViewModel.DebouncedSearchText),
                    StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Setting FilterText to: '{PresetsTreeSearchViewModel.DebouncedSearchText}'");
                System.Console.WriteLine($"DEBUG: Setting FilterText to: '{PresetsTreeSearchViewModel.DebouncedSearchText}'");
                PresetsTreeViewModel.FilterText = PresetsTreeSearchViewModel.DebouncedSearchText;

                if (!IsLanguageChange && string.IsNullOrWhiteSpace(PresetsTreeSearchViewModel.DebouncedSearchText))
                {
                    PresetsTreeViewModel.SetCondensedTreeState(_treeViewState, AnnoCanvas.BuildingPresets.Version);
                }
            }
            else if (string.Equals(e.PropertyName, nameof(PresetsTreeSearchViewModel.HasFocus),
                         StringComparison.OrdinalIgnoreCase) &&
                     PresetsTreeSearchViewModel.HasFocus &&
                     string.IsNullOrWhiteSpace(PresetsTreeSearchViewModel.SearchText))
            {
                _treeViewState = PresetsTreeViewModel.GetCondensedTreeState();
            }
            else if (string.Equals(e.PropertyName, nameof(PresetsTreeSearchViewModel.SelectedGameVersionFilters),
                         StringComparison.OrdinalIgnoreCase))
            {
                var filterGameVersion = CoreConstants.GameVersion.Unknown;

                foreach (var curSelectedFilter in PresetsTreeSearchViewModel.SelectedGameVersionFilters)
                {
                    filterGameVersion |= curSelectedFilter.Type;
                }

                PresetsTreeViewModel.FilterGameVersion = filterGameVersion;
            }
        }

        private void PresetTreeViewModel_ApplySelectedItem(object sender, EventArgs e)
        {
            ApplyPreset(PresetsTreeViewModel.SelectedItem.AnnoObject);
        }

        private void ApplyPreset(AnnoObject selectedItem)
        {
            try
            {
                if (selectedItem == null) return;
                var copySelectedItem = new AnnoObject(selectedItem);
                copySelectedItem.Color = ColorPresetsHelper.Instance.GetPredefinedColor(copySelectedItem) ??
                                         BuildingSettingsViewModel.SelectedColor ?? Colors.Red;
                UpdateUiFromObject(new LayoutObject(copySelectedItem, _coordinateHelper, _brushCache, _penCache));

                ApplyCurrentObject();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error applying preset.");
                _messageBoxService.ShowError(
                    _localizationHelper.GetLocalization("LayoutLoadingError"),
                    _localizationHelper.GetLocalization("Error"));
            }
        }

        private void ApplyCurrentObject()
        {
            // parse user inputs and create new object
            var renameBuildingIdentifier = BuildingSettingsViewModel.BuildingName;
            var textBoxText = "UnknownObject";
            var obj = new AnnoObject
            {
                Size = new Size(BuildingSettingsViewModel.BuildingWidth, BuildingSettingsViewModel.BuildingHeight),
                Color = BuildingSettingsViewModel.SelectedColor ?? Colors.Red,
                Label =
                    BuildingSettingsViewModel.IsEnableLabelChecked
                        ? BuildingSettingsViewModel.BuildingName
                        : string.Empty,
                Icon = SelectedIcon == _noIconItem ? null : SelectedIcon.Name,
                Radius = BuildingSettingsViewModel.BuildingRadius,
                InfluenceRange = BuildingSettingsViewModel.BuildingInfluenceRange,
                PavedStreet = BuildingSettingsViewModel.IsPavedStreet,
                Borderless = BuildingSettingsViewModel.IsBorderlessChecked,
                Road = BuildingSettingsViewModel.IsRoadChecked,
                Identifier = BuildingSettingsViewModel.BuildingIdentifier,
                Template = BuildingSettingsViewModel.BuildingTemplate,
                BlockedAreaLength = BuildingSettingsViewModel.BuildingBlockedAreaLength,
                BlockedAreaWidth = BuildingSettingsViewModel.BuildingBlockedAreaWidth,
                Direction = BuildingSettingsViewModel.BuildingDirection
            };

            var objIconFileName = "";
            //Parse the Icon path into something we can check.
            if (!string.IsNullOrWhiteSpace(obj.Icon))
            {
                if (obj.Icon.StartsWith("A5_"))
                {
                    objIconFileName = obj.Icon[3..] + ".png"; //when Anno 2070, it use not A5_ in the original naming.
                }
                else
                {
                    objIconFileName = obj.Icon + ".png";
                }
            }

            // do some sanity checks
            if (obj.Size is { Width: > 0, Height: > 0 } && obj.Radius >= 0)
            {
                //gets icons and origin building info
                var buildingInfo = AnnoCanvas.BuildingPresets.Buildings.FirstOrDefault(_ =>
                    _.IconFileName?.Equals(objIconFileName, StringComparison.OrdinalIgnoreCase) ?? false);
                if (buildingInfo != null)
                {
                    //Put in the Blocked Area if there is one
                    if (buildingInfo.BlockedAreaLength > 0)
                    {
                        obj.BlockedAreaLength = buildingInfo.BlockedAreaLength;
                        obj.BlockedAreaWidth = buildingInfo.BlockedAreaWidth;
                        obj.Direction = buildingInfo.Direction;
                    }

                    //if user entered a new Label Name (as in renaming existing building or naming own building) then name and identifier will be renamed
                    if (BuildingSettingsViewModel.BuildingRealName != BuildingSettingsViewModel.BuildingName)
                    {
                        obj.Identifier = renameBuildingIdentifier;
                        obj.Template = renameBuildingIdentifier;
                    }
                }
                else
                {
                    //if no Identifier is found or if user entered a new Label Name (as in renaming existing building or naming own building) then name and identifier will be renamed
                    if (string.IsNullOrWhiteSpace(BuildingSettingsViewModel.BuildingIdentifier) ||
                        BuildingSettingsViewModel.BuildingRealName != BuildingSettingsViewModel.BuildingName)
                    {
                        if (!string.IsNullOrWhiteSpace(renameBuildingIdentifier))
                        {
                            obj.Identifier = renameBuildingIdentifier;
                            obj.Template = renameBuildingIdentifier;
                        }
                        else
                        {
                            obj.Identifier = textBoxText;
                        }
                    }
                }

                AnnoCanvas.SetCurrentObject(new LayoutObject(obj, _coordinateHelper, _brushCache, _penCache));
            }
            else
            {
                throw new Exception("Invalid building configuration.");
            }
        }

        /// <summary>
        /// Fired on the OnCurrentObjectChanged event
        /// </summary>
        private void UpdateUiFromObject(LayoutObject layoutObject)
        {
            var obj = layoutObject?.WrappedAnnoObject;
            if (obj == null)
            {
                return;
            }

            // size
            BuildingSettingsViewModel.BuildingWidth = (int)layoutObject.Size.Width;
            BuildingSettingsViewModel.BuildingHeight = (int)layoutObject.Size.Height;
            // color
            BuildingSettingsViewModel.SelectedColor = layoutObject.Color;
            // label
            BuildingSettingsViewModel.BuildingName = obj.Label;
            BuildingSettingsViewModel.BuildingRealName = obj.Label;
            // Identifier
            BuildingSettingsViewModel.BuildingIdentifier = layoutObject.Identifier;
            // Template
            BuildingSettingsViewModel.BuildingTemplate = obj.Template;
            // icon
            try
            {
                if (string.IsNullOrWhiteSpace(obj.Icon))
                {
                    SelectedIcon = _noIconItem;
                }
                else
                {
                    var foundIconImage = AvailableIcons.SingleOrDefault(x =>
                        x.Name.Equals(_fileSystem.Path.GetFileNameWithoutExtension(obj.Icon),
                            StringComparison.OrdinalIgnoreCase));
                    SelectedIcon = foundIconImage ?? _noIconItem;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error finding {nameof(IconImage)} for value \"{obj.Icon}\".{Environment.NewLine}{ex}");

                SelectedIcon = _noIconItem;
            }

            // radius
            BuildingSettingsViewModel.BuildingRadius = obj.Radius;
            //InfluenceRange
            if (!BuildingSettingsViewModel.IsPavedStreet)
            {
                BuildingSettingsViewModel.BuildingInfluenceRange = obj.InfluenceRange;
            }
            else
            {
                _ = BuildingSettingsViewModel.GetDistanceRange(true,
                    AnnoCanvas.BuildingPresets.Buildings.FirstOrDefault(_ =>
                        _.Identifier == BuildingSettingsViewModel.BuildingIdentifier));
            }

            //Set Influence Type
            if (obj.Radius > 0 && obj.InfluenceRange > 0)
            {
                //Building uses both a radius and an influence
                //Has to be set manually 
                BuildingSettingsViewModel.SelectedBuildingInfluence =
                    BuildingSettingsViewModel.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Both);
            }
            else if (obj.Radius > 0)
            {
                BuildingSettingsViewModel.SelectedBuildingInfluence =
                    BuildingSettingsViewModel.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Radius);
            }
            else if (obj.InfluenceRange > 0)
            {
                BuildingSettingsViewModel.SelectedBuildingInfluence =
                    BuildingSettingsViewModel.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Distance);

                if (obj.PavedStreet)
                {
                    BuildingSettingsViewModel.IsPavedStreet = obj.PavedStreet;
                }
            }
            else
            {
                BuildingSettingsViewModel.SelectedBuildingInfluence =
                    BuildingSettingsViewModel.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.None);
            }

            // flags            
            //BuildingSettingsViewModel.IsEnableLabelChecked = !string.IsNullOrEmpty(obj.Label);
            BuildingSettingsViewModel.IsBorderlessChecked = obj.Borderless;
            BuildingSettingsViewModel.IsRoadChecked = obj.Road;
        }

        private void AnnoCanvas_StatisticsUpdated(object sender, UpdateStatisticsEventArgs e)
        {
            _ = UpdateStatisticsAsync(e.Mode);
        }

        private void AnnoCanvas_StatusMessageChanged(string message)
        {
            StatusMessage = message;
        }

        private void AnnoCanvas_LoadedFileChanged(object sender, FileLoadedEventArgs args)
        {
            var fileName = string.Empty;
            var layoutVersion = args.Layout?.LayoutVersion ?? LayoutSettingsViewModel.LayoutVersion;
            if (!string.IsNullOrWhiteSpace(args.FilePath) && layoutVersion != null)
            {
                fileName = $"{_fileSystem.Path.GetFileName(args.FilePath)} ({layoutVersion})";
                LayoutSettingsViewModel.LayoutVersion = layoutVersion;
            }
            else if (!string.IsNullOrWhiteSpace(args.FilePath))
            {
                fileName = _fileSystem.Path.GetFileName(args.FilePath);
            }

            MainWindowTitle = string.IsNullOrEmpty(fileName)
                ? "Anno Designer"
                : $"{fileName} - Anno Designer";

            if (!string.IsNullOrWhiteSpace(args.FilePath))
            {
                Logger.Info($"Loaded file: {(string.IsNullOrEmpty(args.FilePath) ? "(none)" : args.FilePath)}");

                _recentFilesHelper.AddFile(new RecentFile(args.FilePath, DateTime.UtcNow));
            }
        }

        private void AnnoCanvas_OpenFileRequested(object sender, OpenFileEventArgs e)
        {
            using var _ = OpenFile(e.FilePath);
        }

        private void AnnoCanvas_SaveFileRequested(object sender, SaveFileEventArgs e)
        {
            SaveFile(e.FilePath);
        }

        private Task UpdateStatisticsAsync(UpdateMode mode)
        {
            return StatisticsViewModel is null || AnnoCanvas is null
                ? Task.CompletedTask
                : StatisticsViewModel.UpdateStatisticsAsync(mode,
                    [.. AnnoCanvas.PlacedObjects],
                    AnnoCanvas.SelectedObjects,
                    AnnoCanvas.BuildingPresets);
        }

        /// <summary>
        /// Called when localisation is changed, to repopulate the tree view
        /// </summary>
        private void RepopulateTreeView()
        {
            if (AnnoCanvas.BuildingPresets == null) return;
            var treeState = PresetsTreeViewModel.GetCondensedTreeState();

            PresetsTreeViewModel.LoadItems(AnnoCanvas.BuildingPresets);

            PresetsTreeViewModel.SetCondensedTreeState(treeState, AnnoCanvas.BuildingPresets.Version);
        }

        public void LoadAvailableIcons()
        {
            foreach (var icon in AnnoCanvas.Icons.OrderBy(x => x.Value.NameForLanguage(_commons.CurrentLanguageCode)))
            {
                AvailableIcons.Add(icon.Value);
            }
        }

        public void LoadSettings()
        {
            StatisticsViewModel.ToggleBuildingList(_appSettings.StatsShowBuildingCount, [.. AnnoCanvas.PlacedObjects],
                AnnoCanvas.SelectedObjects, AnnoCanvas.BuildingPresets);

            PreferencesUpdateViewModel.AutomaticUpdateCheck = _appSettings.EnableAutomaticUpdateCheck;
            PreferencesUpdateViewModel.UpdateSupportsPrerelease = _appSettings.UpdateSupportsPrerelease;
            PreferencesUpdateViewModel.ShowMultipleInstanceWarning = _appSettings.ShowMultipleInstanceWarning;

            UseCurrentZoomOnExportedImageValue = _appSettings.UseCurrentZoomOnExportedImageValue;
            RenderSelectionHighlightsOnExportedImageValue = _appSettings.RenderSelectionHighlightsOnExportedImageValue;
            RenderVersionOnExportedImageValue = _appSettings.RenderVersionOnExportedImageValue;

            CanvasShowGrid = _appSettings.ShowGrid;
            CanvasShowIcons = _appSettings.ShowIcons;
            CanvasShowLabels = _appSettings.ShowLabels;
            CanvasShowTrueInfluenceRange = _appSettings.ShowTrueInfluenceRange;
            CanvasShowInfluences = _appSettings.ShowInfluences;
            CanvasShowHarborBlockedArea = _appSettings.ShowHarborBlockedArea;
            CanvasShowPanorama = _appSettings.ShowPanorama;

            BuildingSettingsViewModel.IsPavedStreet = _appSettings.IsPavedStreet;

            MainWindowHeight = _appSettings.MainWindowHeight;
            MainWindowWidth = _appSettings.MainWindowWidth;
            MainWindowLeft = _appSettings.MainWindowLeft;
            MainWindowTop = _appSettings.MainWindowTop;
            MainWindowWindowState = _appSettings.MainWindowWindowState;
            HotkeyCommandManager.LoadHotkeyMappings(
                SerializationHelper.LoadFromJsonString<Dictionary<string, HotkeyInformation>>(_appSettings
                    .HotkeyMappings));
        }

        public void SaveSettings()
        {
            _appSettings.IsPavedStreet = BuildingSettingsViewModel.IsPavedStreet;

            _appSettings.ShowGrid = CanvasShowGrid;
            _appSettings.ShowIcons = CanvasShowIcons;
            _appSettings.ShowLabels = CanvasShowLabels;
            _appSettings.ShowTrueInfluenceRange = CanvasShowTrueInfluenceRange;
            _appSettings.ShowInfluences = CanvasShowInfluences;
            _appSettings.ShowHarborBlockedArea = CanvasShowHarborBlockedArea;
            _appSettings.ShowPanorama = CanvasShowPanorama;

            _appSettings.StatsShowStats = StatisticsViewModel.IsVisible;
            _appSettings.StatsShowBuildingCount = StatisticsViewModel.ShowStatisticsBuildingCount;

            _appSettings.EnableAutomaticUpdateCheck = PreferencesUpdateViewModel.AutomaticUpdateCheck;
            _appSettings.UpdateSupportsPrerelease = PreferencesUpdateViewModel.UpdateSupportsPrerelease;
            _appSettings.ShowMultipleInstanceWarning = PreferencesUpdateViewModel.ShowMultipleInstanceWarning;

            _appSettings.UseCurrentZoomOnExportedImageValue = UseCurrentZoomOnExportedImageValue;
            _appSettings.RenderSelectionHighlightsOnExportedImageValue = RenderSelectionHighlightsOnExportedImageValue;
            _appSettings.RenderVersionOnExportedImageValue = RenderVersionOnExportedImageValue;

            var savedTreeState = SerializationHelper.SaveToJsonString(PresetsTreeViewModel.GetCondensedTreeState());

            _appSettings.PresetsTreeExpandedState = savedTreeState;
            _appSettings.PresetsTreeLastVersion = PresetsTreeViewModel.BuildingPresetsVersion;

            _appSettings.TreeViewSearchText = PresetsTreeSearchViewModel.SearchText;
            _appSettings.PresetsTreeGameVersionFilter = PresetsTreeViewModel.FilterGameVersion.ToString();

            _appSettings.MainWindowHeight = MainWindowHeight;
            _appSettings.MainWindowWidth = MainWindowWidth;
            _appSettings.MainWindowLeft = MainWindowLeft;
            _appSettings.MainWindowTop = MainWindowTop;
            _appSettings.MainWindowWindowState = MainWindowWindowState;

            var remappedHotkeys = HotkeyCommandManager.GetRemappedHotkeys();
            _appSettings.HotkeyMappings = SerializationHelper.SaveToJsonString(remappedHotkeys);

            _appSettings.Save();
        }

        public void LoadPresets()
        {
            var presets = AnnoCanvas.BuildingPresets;
            if (presets == null)
            {
                PresetsSectionHeader = "Building presets - load failed";
                return;
            }

            PresetsSectionHeader = $"Building presets - loaded v{presets.Version}";

            PreferencesUpdateViewModel.PresetsVersionValue = presets.Version;
            PreferencesUpdateViewModel.ColorPresetsVersionValue = ColorPresetsHelper.Instance.PresetsVersion;
            PreferencesUpdateViewModel.TreeLocalizationVersionValue = _treeLocalizationContainer.Version;

            PresetsTreeViewModel.LoadItems(presets);

            PopulateSuggestions();

            RestoreSearchAndFilter();
        }

        private void RestoreSearchAndFilter()
        {
            var isFiltered = false;

            //apply saved search before restoring state
            if (!string.IsNullOrWhiteSpace(_appSettings.TreeViewSearchText))
            {
                PresetsTreeSearchViewModel.SearchText = _appSettings.TreeViewSearchText;
                isFiltered = true;
            }

            if (Enum.TryParse<CoreConstants.GameVersion>(_appSettings.PresetsTreeGameVersionFilter, ignoreCase: true,
                    out var parsedValue))
            {
                //if all games were deselected on last app run, now select all
                if (parsedValue == CoreConstants.GameVersion.Unknown)
                {
                    foreach (var curGameVersion in Enum.GetValues<CoreConstants.GameVersion>())
                    {
                        if (curGameVersion is CoreConstants.GameVersion.Unknown or CoreConstants.GameVersion.All)
                        {
                            continue;
                        }

                        parsedValue |= curGameVersion;
                    }
                }

                PresetsTreeSearchViewModel.SelectedGameVersions = parsedValue;
                isFiltered = true;
            }
            else
            {
                //if saved value is not known, now select all
                parsedValue = CoreConstants.GameVersion.Unknown;

                foreach (var curGameVersion in Enum.GetValues<CoreConstants.GameVersion>())
                {
                    if (curGameVersion is CoreConstants.GameVersion.Unknown or CoreConstants.GameVersion.All)
                    {
                        continue;
                    }

                    parsedValue |= curGameVersion;
                }

                PresetsTreeSearchViewModel.SelectedGameVersions = parsedValue;
            }

            //if not filtered, then restore tree state
            if (isFiltered || string.IsNullOrWhiteSpace(_appSettings.PresetsTreeExpandedState)) return;
            var savedTreeState = SerializationHelper.LoadFromJsonString<Dictionary<int, bool>>(_appSettings
                .PresetsTreeExpandedState);
            PresetsTreeViewModel.SetCondensedTreeState(savedTreeState, _appSettings.PresetsTreeLastVersion);
        }

        /// <summary>
        /// Loads a new layout from file.
        /// </summary>
        public async Task OpenFile(string filePath, bool forceLoad = false)
        {
            try
            {
                var layout = _layoutLoader.LoadLayout(filePath, forceLoad);
                if (layout != null)
                {
                    OpenLayout(layout);
                }

                AnnoCanvas.LoadedFile = filePath;

                AnnoCanvas.ForceRendering();

                AnnoCanvas_LoadedFileChanged(this, new FileLoadedEventArgs(filePath, layout));
            }
            catch (LayoutFileUnsupportedFormatException layoutEx)
            {
                Logger.Warn(layoutEx, "Version of layout file is not supported.");

                if (await _messageBoxService.ShowQuestion(
                        _localizationHelper.GetLocalization("FileVersionUnsupportedMessage"),
                        _localizationHelper.GetLocalization("FileVersionUnsupportedTitle")))
                {
                    await OpenFile(filePath, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading layout from JSON.");

                IoErrorMessageBox(ex);
            }
        }

        /// <summary>
        /// Opens new layout from memory.
        /// </summary>
        private void OpenLayout(LayoutFile layout)
        {
            AnnoCanvas.SelectedObjects.Clear();
            AnnoCanvas.PlacedObjects.Clear();
            AnnoCanvas.UndoManager.Clear();

            var layoutObjects = new List<LayoutObject>(layout.Objects.Count);
            layoutObjects.AddRange(layout.Objects.Select(curObj =>
                new LayoutObject(curObj, _coordinateHelper, _brushCache, _penCache)));

            LayoutSettingsViewModel.LayoutVersion = layout.LayoutVersion;

            _ = AnnoCanvas.ComputeBoundingRect(layoutObjects);
            AnnoCanvas.PlacedObjects.AddRange(layoutObjects);

            AnnoCanvas.Normalize(1);
            AnnoCanvas.ResetViewport();

            AnnoCanvas.RaiseStatisticsUpdated(UpdateStatisticsEventArgs.All);
            AnnoCanvas.RaiseColorsInLayoutUpdated();
            AnnoCanvas.UndoManager.Clear();
        }

        /// <summary>
        /// Writes layout to file.
        /// </summary>
        public void SaveFile(string filePath)
        {
            try
            {
                AnnoCanvas.Normalize(1);
                var layoutToSave = new LayoutFile(AnnoCanvas.PlacedObjects.Select(x => x.WrappedAnnoObject))
                {
                    LayoutVersion = LayoutSettingsViewModel.LayoutVersion
                };
                _layoutLoader.SaveLayout(layoutToSave, filePath);
                AnnoCanvas.UndoManager.IsDirty = false;
            }
            catch (Exception e)
            {
                IoErrorMessageBox(e);
            }
        }

        /// <summary>
        /// Displays a message box containing some error information.
        /// </summary>
        /// <param name="e">exception containing error information</param>
        private void IoErrorMessageBox(Exception e)
        {
            _messageBoxService.ShowError(e.Message, _localizationHelper.GetLocalization("IOErrorMessage"));
        }

        #region properties

        public IAnnoCanvas AnnoCanvas
        {
            get;
            set
            {
                if (field != null)
                {
                    field.StatisticsUpdated -= AnnoCanvas_StatisticsUpdated;
                }

                field = value;
                field.StatisticsUpdated += AnnoCanvas_StatisticsUpdated;
                field.OnCurrentObjectChanged += UpdateUiFromObject;
                field.OnStatusMessageChanged += AnnoCanvas_StatusMessageChanged;
                field.OnLoadedFileChanged += AnnoCanvas_LoadedFileChanged;
                field.OpenFileRequested += AnnoCanvas_OpenFileRequested;
                field.SaveFileRequested += AnnoCanvas_SaveFileRequested;
                BuildingSettingsViewModel.AnnoCanvasToUse = field;

                field.RenderGrid = CanvasShowGrid;
                field.RenderIcon = CanvasShowIcons;
                field.RenderLabel = CanvasShowLabels;
                field.RenderTrueInfluenceRange = CanvasShowTrueInfluenceRange;
                field.RenderInfluences = CanvasShowInfluences;
                field.RenderHarborBlockedArea = CanvasShowHarborBlockedArea;
                field.RenderPanorama = CanvasShowPanorama;
            }
        }

        public bool CanvasShowGrid
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderGrid = field;
            }
        }

        public bool CanvasShowIcons
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderIcon = field;
            }
        }

        public bool CanvasShowLabels
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderLabel = field;
            }
        }

        public bool CanvasShowTrueInfluenceRange
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderTrueInfluenceRange = field;
            }
        }

        public bool CanvasShowInfluences
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderInfluences = field;
            }
        }

        private bool CanvasShowHarborBlockedArea
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderHarborBlockedArea = field;
            }
        }

        private bool CanvasShowPanorama
        {
            get;
            set
            {
                _ = SetProperty(ref field, value);
                AnnoCanvas?.RenderPanorama = field;
            }
        }

        public bool UseCurrentZoomOnExportedImageValue
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public bool RenderSelectionHighlightsOnExportedImageValue
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public bool RenderVersionOnExportedImageValue
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public bool IsLanguageChange
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public bool IsBusy
        {
            get;
            init { _ = SetProperty(ref field, value); }
        }

        public string StatusMessage
        {
            get;
            private set { _ = SetProperty(ref field, value); }
        }

        public ObservableCollection<SupportedLanguage> Languages
        {
            get;
            private init { _ = SetProperty(ref field, value); }
        }

        public SupportedLanguage SelectedLanguage => Languages?.FirstOrDefault(l => l.IsSelected);

        private void InitLanguageMenu(string selectedLanguage)
        {
            //unselect all other languages
            foreach (var curLanguage in Languages)
            {
                curLanguage.IsSelected =
                    string.Equals(curLanguage.Name, selectedLanguage, StringComparison.OrdinalIgnoreCase);
            }

            OnPropertyChanged(nameof(SelectedLanguage));
        }

        public ObservableCollection<IconImage> AvailableIcons
        {
            get;
            private init { _ = SetProperty(ref field, value); }
        }

        public IconImage SelectedIcon
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public string MainWindowTitle
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public string PresetsSectionHeader
        {
            get;
            private set { _ = SetProperty(ref field, value); }
        }

        public double MainWindowHeight
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public double MainWindowWidth
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public double MainWindowLeft
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public double MainWindowTop
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public WindowState MainWindowWindowState
        {
            get;
            set { _ = SetProperty(ref field, value); }
        }

        public HotkeyCommandManager HotkeyCommandManager
        {
            get;
            private init { _ = SetProperty(ref field, value); }
        }

        public ObservableCollection<RecentFileItem> RecentFiles
        {
            get;
            init
            {
                if (SetProperty(ref field, value))
                {
                    OnPropertyChanged(nameof(HasRecentFiles));
                }
            }
        }

        public bool HasRecentFiles
        {
            get { return RecentFiles.Count > 0; }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void OpenProjectHomepage(object param)
        {
            try
            {
                var url = "https://github.com/AnnoDesigner/anno-designer/";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _ = Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _ = Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error opening project homepage.");
            }
        }

        [RelayCommand]
        private void CloseWindow(ICloseable window)
        {
            window?.Close();
        }

        [RelayCommand]
        private void CanvasResetZoom(object param)
        {
            AnnoCanvas.ResetZoom();
        }

        [RelayCommand]
        private void CanvasZoomIn(object param)
        {
            if (AnnoCanvas == null)
            {
                return;
            }

            var newSize = AnnoCanvas.GridSize * 2;
            if (newSize > Constants.GridStepMax)
            {
                newSize = Constants.GridStepMax;
            }

            AnnoCanvas.GridSize = newSize;
        }

        [RelayCommand]
        private void CanvasZoomOut(object param)
        {
            if (AnnoCanvas == null)
            {
                return;
            }

            var newSize = Math.Max(Constants.GridStepMin, AnnoCanvas.GridSize / 2);
            AnnoCanvas.GridSize = newSize;
        }

        [RelayCommand]
        private void CanvasZoomFit(object param)
        {
            try
            {
                if (AnnoCanvas == null || AnnoCanvas.PlacedObjects == null || AnnoCanvas.PlacedObjects.Count == 0)
                {
                    return;
                }

                var bounds = AnnoCanvas.ComputeBoundingRect(AnnoCanvas.PlacedObjects);

                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return;
                }

                var host = Application.Current?.MainWindow?.FindName("annoCanvas") as FrameworkElement;
                var availableWidth = host?.ActualWidth ?? (Application.Current?.MainWindow?.ActualWidth ?? 800);
                var availableHeight = host?.ActualHeight ?? (Application.Current?.MainWindow?.ActualHeight ?? 600);

                // leave some padding
                var padding = 20;
                availableWidth = Math.Max(100, availableWidth - padding);
                availableHeight = Math.Max(100, availableHeight - padding);

                var gridSizeForWidth = (int)Math.Floor(availableWidth / bounds.Width);
                var gridSizeForHeight = (int)Math.Floor(availableHeight / bounds.Height);

                var targetGridSize = Math.Min(gridSizeForWidth, gridSizeForHeight);

                if (targetGridSize < Constants.GridStepMin)
                {
                    targetGridSize = Constants.GridStepMin;
                }
                else if (targetGridSize > Constants.GridStepMax)
                {
                    targetGridSize = Constants.GridStepMax;
                }

                AnnoCanvas.GridSize = targetGridSize; 
                AnnoCanvas.CenterViewportOnRect(bounds);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during ZoomFit.");
            }
        }

        [RelayCommand]
        private void CanvasZoomToSelection(object param)
        {
            try
            {
                if (AnnoCanvas == null || AnnoCanvas.SelectedObjects == null || AnnoCanvas.SelectedObjects.Count == 0)
                {
                    return;
                }

                var bounds = AnnoCanvas.ComputeBoundingRect(AnnoCanvas.SelectedObjects);

                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return;
                }

                var host = Application.Current?.MainWindow?.FindName("annoCanvas") as FrameworkElement;
                var availableWidth = host?.ActualWidth ?? (Application.Current?.MainWindow?.ActualWidth ?? 800);
                var availableHeight = host?.ActualHeight ?? (Application.Current?.MainWindow?.ActualHeight ?? 600);

                var padding = 20;
                availableWidth = Math.Max(100, availableWidth - padding);
                availableHeight = Math.Max(100, availableHeight - padding);

                var gridSizeForWidth = (int)Math.Floor(availableWidth / bounds.Width);
                var gridSizeForHeight = (int)Math.Floor(availableHeight / bounds.Height);

                var targetGridSize = Math.Min(gridSizeForWidth, gridSizeForHeight);

                if (targetGridSize < Constants.GridStepMin)
                {
                    targetGridSize = Constants.GridStepMin;
                }
                else if (targetGridSize > Constants.GridStepMax)
                {
                    targetGridSize = Constants.GridStepMax;
                }

                AnnoCanvas.GridSize = targetGridSize;
                // center the viewport on the selected objects
                AnnoCanvas.CenterViewportOnRect(bounds);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during ZoomToSelection.");
            }
        }


        [RelayCommand]
        private void CanvasNormalize(object param)
        {
            AnnoCanvas.Normalize(1);
            AnnoCanvas.ResetViewport();
        }


        /// <summary>
        /// Filters all roads in current layout, finds largest groups of them and replaces them with merged variants.
        /// Respects road color during merging.
        /// </summary> 
        [RelayCommand]
        public void MergeRoads(object param)
        {
            var roadColorGroups = AnnoCanvas.PlacedObjects.Where(p => p.WrappedAnnoObject.Road)
                .GroupBy(p => (p.WrappedAnnoObject.Borderless, p.Color));
            foreach (var roadColorGroup in roadColorGroups)
            {
                if (roadColorGroup.Count() <= 1)
                {
                    continue;
                }

                var bounds =
                    (Rect)new StatisticsCalculationHelper().CalculateStatistics(
                        roadColorGroup.Select(p => p.WrappedAnnoObject));

                var cells = Enumerable.Range(0, (int)bounds.Width).Select(_ => new LayoutObject[(int)bounds.Height])
                    .ToArray();
                foreach (var item in roadColorGroup)
                {
                    for (var i = 0; i < item.Size.Width; i++)
                    {
                        for (var j = 0; j < item.Size.Height; j++)
                        {
                            cells[(int)(item.Position.X + i - bounds.Left)][(int)(item.Position.Y + j - bounds.Top)] =
                                item;
                        }
                    }
                }

                var groups = _adjacentCellGrouper.GroupAdjacentCells(cells).ToList();
                AnnoCanvas.UndoManager.AsSingleUndoableOperation(() =>
                {
                    var oldObjects = groups.SelectMany(g => g.Items).ToList();
                    foreach (var item in oldObjects)
                    {
                        _ = AnnoCanvas.PlacedObjects.Remove(item);
                    }

                    var newObjects = groups
                        .Select(g => new LayoutObject(
                            new AnnoObject(g.Items.First().WrappedAnnoObject)
                            {
                                Position = g.Bounds.TopLeft + (Vector)bounds.TopLeft,
                                Size = g.Bounds.Size
                            },
                            _coordinateHelper,
                            _brushCache,
                            _penCache
                        ))
                        .ToList();
                    AnnoCanvas.PlacedObjects.AddRange(newObjects);

                    AnnoCanvas.UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
                    {
                        Objects = oldObjects,
                        Collection = AnnoCanvas.PlacedObjects
                    });
                    AnnoCanvas.UndoManager.RegisterOperation(new AddObjectsOperation<LayoutObject>()
                    {
                        Objects = newObjects,
                        Collection = AnnoCanvas.PlacedObjects
                    });
                });
            }
        }


        [RelayCommand]
        private void LoadLayoutFromJson(object param)
        {
            if (!AnnoCanvas.CheckUnsavedChanges().GetAwaiter().GetResult())
            {
                return;
            }

            var input = InputWindow.Prompt(this, _localizationHelper.GetLocalization("LoadLayoutMessage"),
                _localizationHelper.GetLocalization("LoadLayoutHeader"));

            LoadLayoutFromJsonSub(input, false).GetAwaiter().GetResult();
        }

        private async Task LoadLayoutFromJsonSub(string jsonString, bool forceLoad = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    var jsonArray = Encoding.UTF8.GetBytes(jsonString);
                    using var ms = new MemoryStream(jsonArray);
                    var loadedLayout = _layoutLoader.LoadLayout(ms, forceLoad);
                    if (loadedLayout != null)
                    {
                        AnnoCanvas.SelectedObjects.Clear();
                        AnnoCanvas.PlacedObjects.Clear();
                        AnnoCanvas.PlacedObjects.AddRange(loadedLayout.Objects.Select(x =>
                            new LayoutObject(x, _coordinateHelper, _brushCache, _penCache)));

                        AnnoCanvas.UndoManager.Clear();

                        AnnoCanvas.LoadedFile = string.Empty;
                        AnnoCanvas.Normalize(1);
                        AnnoCanvas.ResetViewport();

                        _ = UpdateStatisticsAsync(UpdateMode.All);
                    }
                }
            }
            catch (LayoutFileUnsupportedFormatException layoutEx)
            {
                Logger.Warn(layoutEx, "Version of layout does not match.");

                if (await _messageBoxService.ShowQuestion(
                        _localizationHelper.GetLocalization("FileVersionMismatchMessage"),
                        _localizationHelper.GetLocalization("FileVersionMismatchTitle")))
                {
                    await LoadLayoutFromJsonSub(jsonString, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading layout from JSON.");
                _messageBoxService.ShowError(_localizationHelper.GetLocalization("LayoutLoadingError"),
                    _localizationHelper.GetLocalization("Error"));
            }
        }


        [RelayCommand]
        private void UnregisterExtension(object param)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var regCheckAdFileExtension = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                    .OpenSubKey(@"Software\Classes\anno_designer", false);
                if (regCheckAdFileExtension != null)
                {
                    // removes the registry entries when exists          
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\anno_designer");
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.ad");

                    ShowRegistrationMessageBox(isDeregistration: true);
                }
            }
        }


        [RelayCommand]
        private void RegisterExtension(object param)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // registers the anno_designer class type and adds the correct command string to pass a file argument to the application
                Registry.SetValue(Constants.FileAssociationRegistryKey, null,
                    $"\"{App.ExecutablePath}\" open \"%1\"");
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\anno_designer\DefaultIcon", null,
                    $"\"{App.ExecutablePath}\",0");
                // registers the .ad file extension to the anno_designer class
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\.ad", null, "anno_designer");

                ShowRegistrationMessageBox(isDeregistration: false);
            }
        }

        public static void UpdateRegisteredExtension()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            if ($"\"{App.ExecutablePath}\" \"%1\""
                .Equals(Registry.GetValue(Constants.FileAssociationRegistryKey, null, null)))
            {
                Registry.SetValue(Constants.FileAssociationRegistryKey, null,
                    $"\"{App.ExecutablePath}\" open \"%1\"");
            }
        }

        private void ShowRegistrationMessageBox(bool isDeregistration)
        {
            var message = isDeregistration
                ? _localizationHelper.GetLocalization("UnregisterFileExtensionSuccessful")
                : _localizationHelper.GetLocalization("RegisterFileExtensionSuccessful");

            _messageBoxService.ShowMessage(message, _localizationHelper.GetLocalization("Successful"));
        }


        [RelayCommand]
        private void ExportImage(object param)
        {
            ExportImageSub(UseCurrentZoomOnExportedImageValue, RenderSelectionHighlightsOnExportedImageValue,
                RenderVersionOnExportedImageValue);
        }

        /// <summary>
        /// Renders the current layout to file.
        /// </summary>
        /// <param name="exportZoom">indicates whether the current zoom level should be applied, if false the default zoom is used</param>
        /// <param name="exportSelection">indicates whether selection and influence highlights should be rendered</param>
        private void ExportImageSub(bool exportZoom, bool exportSelection, bool exportVersion)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = Constants.ExportedImageExtension,
                Filter = Constants.ExportDialogFilter
            };

            if (!string.IsNullOrEmpty(AnnoCanvas.LoadedFile))
            {
                // default the filename to the same name as the saved layout
                dialog.FileName = _fileSystem.Path.GetFileNameWithoutExtension(AnnoCanvas.LoadedFile);
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    RenderToFile(dialog.FileName, 1, exportZoom, exportSelection, StatisticsViewModel.IsVisible,
                        exportVersion);

                    _messageBoxService.ShowMessage(Application.Current.MainWindow,
                        _localizationHelper.GetLocalization("ExportImageSuccessful"),
                        _localizationHelper.GetLocalization("Successful"));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error exporting image.");
                    _messageBoxService.ShowError(Application.Current.MainWindow,
                        _localizationHelper.GetLocalization("ExportImageError"),
                        _localizationHelper.GetLocalization("Error"));
                }
            }
        }

        /// <summary>
        /// Asynchronously renders the current layout to file.
        /// </summary>
        /// <param name="filename">filename of the output image</param>
        /// <param name="border">normalization value used prior to exporting</param>
        /// <param name="exportZoom">indicates whether the current zoom level should be applied, if false the default zoom is used</param>
        /// <param name="exportSelection">indicates whether selection and influence highlights should be rendered</param>
        private void RenderToFile(string filename, int border, bool exportZoom, bool exportSelection,
            bool renderStatistics, bool renderVersion)
        {
            if (AnnoCanvas.PlacedObjects.Count == 0)
            {
                return;
            }

            Logger.Trace($"UI thread: {Environment.CurrentManagedThreadId} ({Thread.CurrentThread.Name})");

            void RenderThread()
            {
                var target = PrepareCanvasForRender(
                    AnnoCanvas.PlacedObjects.Select(o => o.WrappedAnnoObject),
                    exportSelection ? AnnoCanvas.SelectedObjects.Select(o => o.WrappedAnnoObject) : [],
                    border,
                    new CanvasRenderSetting()
                    {
                        GridSize = exportZoom ? AnnoCanvas.GridSize : null,
                        RenderGrid = AnnoCanvas.RenderGrid,
                        RenderHarborBlockedArea = AnnoCanvas.RenderHarborBlockedArea,
                        RenderIcon = AnnoCanvas.RenderIcon,
                        RenderInfluences = AnnoCanvas.RenderInfluences,
                        RenderLabel = AnnoCanvas.RenderLabel,
                        RenderPanorama = AnnoCanvas.RenderPanorama,
                        RenderTrueInfluenceRange = AnnoCanvas.RenderTrueInfluenceRange,
                        RenderStatistics = renderStatistics,
                        RenderVersion = renderVersion
                    }
                );

                // render canvas to file
                target.RenderToFile(filename);
            }

            var thread = new Thread(RenderThread) { IsBackground = true, Name = "exportImage" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                thread.SetApartmentState(ApartmentState.STA);
            }

            thread.Start();
            _ = thread.Join(TimeSpan.FromSeconds(10));
        }

        public FrameworkElement PrepareCanvasForRender(
            IEnumerable<AnnoObject> placedObjects,
            IEnumerable<AnnoObject> selectedObjects,
            int border,
            CanvasRenderSetting renderSettings = null)
        {
            renderSettings ??= new CanvasRenderSetting() { RenderGrid = true, RenderIcon = true };

            var sw = new Stopwatch();
            sw.Start();

            var icons = new Dictionary<string, IconImage>(StringComparer.OrdinalIgnoreCase);
            foreach (var curIcon in AnnoCanvas.Icons)
            {
                icons.Add(curIcon.Key,
                    new IconImage(curIcon.Value.Name, curIcon.Value.Localizations, curIcon.Value.IconPath));
            }

            var annoObjects = placedObjects.ToList();
            var statistics = new StatisticsCalculationHelper().CalculateStatistics(annoObjects, true, true);

            var quadTree = new QuadTree<LayoutObject>((Rect)statistics);
            quadTree.AddRange(annoObjects.Select(o =>
                new LayoutObject(o, _coordinateHelper, _brushCache, _penCache)));
            // initialize output canvas
            var target =
                new AnnoCanvas(AnnoCanvas.BuildingPresets, icons, _appSettings, _coordinateHelper, _brushCache,
                    _penCache, _messageBoxService)
                {
                    PlacedObjects = quadTree,
                    RenderGrid = renderSettings.RenderGrid,
                    RenderIcon = renderSettings.RenderIcon,
                    RenderLabel = renderSettings.RenderLabel,
                    RenderHarborBlockedArea = renderSettings.RenderHarborBlockedArea,
                    RenderPanorama = renderSettings.RenderPanorama,
                    RenderTrueInfluenceRange = renderSettings.RenderTrueInfluenceRange,
                    RenderInfluences = renderSettings.RenderInfluences,
                };

            sw.Stop();
            Logger.Trace($"creating canvas took: {sw.ElapsedMilliseconds}ms");

            // normalize layout
            target.Normalize(border);

            // set zoom level
            if (renderSettings.GridSize.HasValue)
            {
                target.GridSize = renderSettings.GridSize.Value;
            }

            // set selection
            target.SelectedObjects.UnionWith(selectedObjects.Select(o =>
                new LayoutObject(o, _coordinateHelper, _brushCache, _penCache)));

            // calculate output size
            var width = _coordinateHelper.GridToScreen(
                target.PlacedObjects.Max(_ => _.Position.X + _.Size.Width) + border,
                target.GridSize); //if +1 then there are weird black lines next to the statistics view
            var height =
                _coordinateHelper.GridToScreen(target.PlacedObjects.Max(_ => _.Position.Y + _.Size.Height) + border,
                    target.GridSize) + 1; //+1 for black grid line at bottom

            if (renderSettings.RenderVersion)
            {
                var versionView = new VersionView() { Context = LayoutSettingsViewModel };

                target.DockPanel.Children.Insert(0, versionView);
                DockPanel.SetDock(versionView, Dock.Bottom);

                versionView.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                height += versionView.DesiredSize.Height;
            }

            if (renderSettings.RenderStatistics)
            {
                var exportStatisticsViewModel = new StatisticsViewModel(_localizationHelper, _commons, _appSettings);
                _ = exportStatisticsViewModel.UpdateStatisticsAsync(UpdateMode.All, [.. target.PlacedObjects],
                    target.SelectedObjects, target.BuildingPresets);
                exportStatisticsViewModel.ShowBuildingList = StatisticsViewModel.ShowBuildingList;

                var exportStatisticsView = new StatisticsView() { Context = exportStatisticsViewModel };

                target.DockPanel.Children.Insert(0, exportStatisticsView);
                DockPanel.SetDock(exportStatisticsView, Dock.Right);

                //fix wrong for wrong width: https://stackoverflow.com/q/27894477
                exportStatisticsView.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                //according to https://stackoverflow.com/a/25507450
                //and https://stackoverflow.com/a/1320666
                exportStatisticsView.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                //exportStatisticsView.Arrange(new Rect(new Point(0, 0), exportStatisticsView.DesiredSize));

                if (exportStatisticsView.DesiredSize.Height > height)
                {
                    height = exportStatisticsView.DesiredSize.Height + target.LinePenThickness + border;
                }

                width += exportStatisticsView.DesiredSize.Width + target.LinePenThickness;
            }

            target.Width = width;
            target.Height = height;
            target.UpdateLayout();

            // apply size
            var outputSize = new Size(width, height);
            target.Measure(outputSize);
            target.Arrange(new Rect(outputSize));

            return target;
        }


        [RelayCommand]
        private void CopyLayoutToClipboard(object param)
        {
            CopyLayoutToClipboardSub();
        }

        private void CopyLayoutToClipboardSub()
        {
            try
            {
                using var ms = new MemoryStream();
                AnnoCanvas.Normalize(1);
                var layoutToSave = new LayoutFile(AnnoCanvas.PlacedObjects.Select(x => x.WrappedAnnoObject));
                _layoutLoader.SaveLayout(layoutToSave, ms);

                var jsonString = Encoding.UTF8.GetString(ms.ToArray());

                Clipboard.SetText(jsonString, TextDataFormat.UnicodeText);

                _messageBoxService.ShowMessage(_localizationHelper.GetLocalization("ClipboardContainsLayoutAsJson"),
                    _localizationHelper.GetLocalization("Successful"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving layout to JSON.");
                _messageBoxService.ShowError(ex.Message, _localizationHelper.GetLocalization("LayoutSavingError"));
            }
        }


        [RelayCommand]
        private void LanguageSelected(object param)
        {
            if (IsLanguageChange)
            {
                return;
            }

            try
            {
                IsLanguageChange = true;

                if (param is not SupportedLanguage selectedLanguage)
                {
                    return;
                }

                InitLanguageMenu(selectedLanguage.Name);

                _commons.CurrentLanguage = selectedLanguage.Name;
            }
            finally
            {
                IsLanguageChange = false;
            }
        }


        [RelayCommand]
        private void ShowAboutWindow(object param)
        {
            var aboutWindow = new About { Owner = Application.Current.MainWindow, DataContext = AboutViewModel };

            _ = aboutWindow.ShowDialog();
        }


        [RelayCommand]
        private void ShowWelcomeWindow(object param)
        {
            var welcomeWindow = new Welcome { Owner = Application.Current.MainWindow, DataContext = WelcomeViewModel };
            welcomeWindow.Show();
        }


        [RelayCommand]
        private void ShowStatistics(object param)
        {
            ShowStatisticsChanged?.Invoke(this, EventArgs.Empty);
        }


        [RelayCommand]
        private void ShowStatisticsBuildingCount(object param)
        {
            StatisticsViewModel.ToggleBuildingList(StatisticsViewModel.ShowStatisticsBuildingCount,
                [.. AnnoCanvas.PlacedObjects], AnnoCanvas.SelectedObjects, AnnoCanvas.BuildingPresets);
        }


        [RelayCommand]
        private void PlaceBuilding(object param)
        {
            try
            {
                ApplyCurrentObject();
            }
            catch (Exception)
            {
                _messageBoxService.ShowError(
                    _localizationHelper.GetLocalization("InvalidBuildingConfiguration"),
                    _localizationHelper.GetLocalization("Error"));
            }
        }


        [RelayCommand]
        private void ShowPreferencesWindow(object param)
        {
            var preferencesWindow = new PreferencesWindow()
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var vm = new PreferencesViewModel();
            preferencesWindow.DataContext = vm;

            vm.Pages.Add(new PreferencePage
            {
                Name = nameof(GeneralSettingsPage),
                ViewModel = PreferencesGeneralViewModel,
                HeaderKeyForTranslation = "GeneralSettings"
            });
            vm.Pages.Add(new PreferencePage
            {
                Name = nameof(ManageKeybindingsPage),
                ViewModel = PreferencesKeyBindingsViewModel,
                HeaderKeyForTranslation = "ManageKeybindings"
            });
            vm.Pages.Add(new PreferencePage
            {
                Name = nameof(UpdateSettingsPage),
                ViewModel = PreferencesUpdateViewModel,
                HeaderKeyForTranslation = "UpdateSettings"
            });

            preferencesWindow.ShowDialog();
        }


        [RelayCommand]
        private void ShowLicensesWindow(object param)
        {
            var licensesWindow = new LicensesWindow() { Owner = Application.Current.MainWindow };
            _ = licensesWindow.ShowDialog();
        }


        [RelayCommand]
        private void OpenRecentFile(object param)
        {
            if (param is not RecentFileItem recentFile)
            {
                return;
            }

            if (_fileSystem.File.Exists(recentFile.Path))
            {
                if (!AnnoCanvas.CheckUnsavedChanges().ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    return;
                }

                _ = OpenFile(recentFile.Path);

                _recentFilesHelper.AddFile(new RecentFile(recentFile.Path, DateTime.UtcNow));
            }
            else
            {
                _recentFilesHelper.RemoveFile(new RecentFile(recentFile.Path, recentFile.LastUsed));
                _messageBoxService.ShowWarning(Application.Current.MainWindow,
                    _localizationHelper.GetLocalization("FileNotFound"),
                    _localizationHelper.GetLocalization("Warning"));
            }
        }

        private void PopulateSuggestions()
        {
            PresetsTreeSearchViewModel.SuggestionsList.Clear();

            var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in PresetsTreeViewModel.Items)
            {
                CollectBuildingNames(item, suggestions);
            }

            foreach (var suggestion in suggestions.OrderBy(s => s))
            {
                PresetsTreeSearchViewModel.SuggestionsList.Add(suggestion);
            }
        }

        private void CollectBuildingNames(Models.PresetsTree.GenericTreeItem item, HashSet<string> suggestions)
        {
            if (item.AnnoObject != null)
            {
                suggestions.Add(item.Header);
            }

            foreach (var child in item.Children)
            {
                CollectBuildingNames(child, suggestions);
            }
        }

        #endregion

        #region view models

        public StatisticsViewModel StatisticsViewModel { get; init; }

        public BuildingSettingsViewModel BuildingSettingsViewModel { get; init; }

        public PresetsTreeViewModel PresetsTreeViewModel { get; init; }

        public PresetsTreeSearchViewModel PresetsTreeSearchViewModel { get; init; }

        public WelcomeViewModel WelcomeViewModel { get; init; }

        public AboutViewModel AboutViewModel { get; init; }

        public UpdateSettingsViewModel PreferencesUpdateViewModel { get; init; }

        private ManageKeybindingsViewModel PreferencesKeyBindingsViewModel { get; init; }

        private GeneralSettingsViewModel PreferencesGeneralViewModel { get; init; }

        public LayoutSettingsViewModel LayoutSettingsViewModel { get; init; }

        #endregion
    }
}