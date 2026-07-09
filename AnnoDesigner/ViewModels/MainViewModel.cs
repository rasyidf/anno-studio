
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
using AnnoDesigner.Services;
using AnnoDesigner.Services.Undo.Operations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Microsoft.Win32;
using NLog;


namespace AnnoDesigner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ICommons _commons;
    private readonly IAppSettings _appSettings;
    private readonly ILayoutLoader _layoutLoader;
    private readonly ICoordinateHelper _coordinateHelper;
    private readonly IBrushCache _brushCache;
    private readonly IPenCache _penCache;
    private readonly IDocumentServicesFactory _documentServicesFactory;
    private readonly ISharedResourceManager _sharedResourceManager;
    private readonly ILayoutService _layoutService;
    private readonly IAdjacentCellGrouper _adjacentCellGrouper;
    private readonly IRecentFilesHelper _recentFilesHelper;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILocalizationHelper _localizationHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ITransformationService _transformationService;
    private readonly IExportService _exportService;
    private readonly IPresetApplicationService _presetApplicationService;


    public event EventHandler<EventArgs> ShowStatisticsChanged;

    private Dictionary<int, bool> _treeViewState;
    private readonly TreeLocalizationContainer _treeLocalizationContainer;

    // Prefer shared resources when available
    private BuildingPresets BuildingPresets => _sharedResourceManager?.BuildingPresets ?? AnnoCanvas?.BuildingPresets;
    private Dictionary<string, IconImage> Icons => _sharedResourceManager?.Icons ?? AnnoCanvas?.Icons;

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
        ITreeLocalizationLoader treeLocalizationLoader = null,
        IDocumentServicesFactory documentServicesFactoryToUse = null,
        ISharedResourceManager sharedResourceManagerToUse = null,
        ILayoutService layoutServiceToUse = null,
        ITransformationService transformationServiceToUse = null,
        IExportService exportServiceToUse = null)
    {
        _commons = commonsToUse;
        _commons.SelectedLanguageChanged += Commons_SelectedLanguageChanged;

        _appSettings = appSettingsToUse;
        _appSettings.SettingsChanged += AppSettings_SettingsChanged;
        _recentFilesHelper = recentFilesHelperToUse;
        _messageBoxService = messageBoxServiceToUse;
        _localizationHelper = localizationHelperToUse;
        _fileSystem = fileSystemToUse;

        // Prefer shared resources when available, fall back to provided or defaults
        _sharedResourceManager = sharedResourceManagerToUse;
        _layoutLoader = sharedResourceManagerToUse?.LayoutLoader ?? layoutLoaderToUse ?? new LayoutLoader();
        _coordinateHelper = sharedResourceManagerToUse?.CoordinateHelper ?? coordinateHelperToUse ?? new CoordinateHelper();
        _brushCache = sharedResourceManagerToUse?.BrushCache ?? brushCacheToUse ?? new BrushCache();
        _penCache = sharedResourceManagerToUse?.PenCache ?? penCacheToUse ?? new PenCache();
        _adjacentCellGrouper = adjacentCellGrouper ?? new AdjacentCellGrouper();

        _documentServicesFactory = documentServicesFactoryToUse;
        _layoutService = layoutServiceToUse;
        _transformationService = transformationServiceToUse ?? new TransformationService();
        _exportService = exportServiceToUse ?? new ExportService(
            _layoutLoader,
            _coordinateHelper,
            _brushCache,
            _penCache,
            _appSettings,
            _messageBoxService,
            _localizationHelper,
            _fileSystem,
            _commons,
            () => BuildingPresets,
            () => Icons,
            () => StatisticsViewModel,
            () => LayoutSettingsViewModel);

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

        _presetApplicationService = new PresetApplicationService(
            () => BuildingSettingsViewModel,
            () => BuildingPresets,
            () => AnnoCanvas,
            _coordinateHelper,
            _brushCache,
            _penCache,
            _fileSystem,
            _messageBoxService,
            _localizationHelper,
            () => SelectedIcon,
            icon => SelectedIcon = icon,
            () => AvailableIcons,
            () => _noIconItem);

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
        // ensure at least one document exists on startup
        CreateNewDocument();
    }

    // Grouping and Z-ordering are unused; methods removed

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
                PresetsTreeViewModel.SetCondensedTreeState(_treeViewState, BuildingPresets?.Version);
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
            var filterGameVersion = PresetsTreeSearchViewModel.SelectedGameVersionFilters.Aggregate(CoreConstants.GameVersion.Unknown, (current, curSelectedFilter) => current | curSelectedFilter.Type);

            PresetsTreeViewModel.FilterGameVersion = filterGameVersion;
        }
    }

    private void PresetTreeViewModel_ApplySelectedItem(object sender, EventArgs e)
    {
        ApplyPreset(PresetsTreeViewModel.SelectedItem.AnnoObject);
    }

    private void ApplyPreset(AnnoObject selectedItem)
    {
        _presetApplicationService.ApplyPreset(selectedItem);
    }

    private void ApplyCurrentObject()
    {
        _presetApplicationService.ApplyCurrentObject();
    }

    /// <summary>
    /// Fired on the OnCurrentObjectChanged event
    /// </summary>
    private void UpdateUiFromObject(LayoutObject layoutObject)
    {
        _presetApplicationService.UpdateUiFromObject(layoutObject);
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

    private async void AnnoCanvas_OpenFileRequested(object sender, OpenFileEventArgs e)
    {
        await OpenFile(e.FilePath).ConfigureAwait(false);
    }

    private async void AnnoCanvas_SaveFileRequested(object sender, SaveFileEventArgs e)
    {
        await SaveFileAsync(e.FilePath).ConfigureAwait(false);
    }

    private Task UpdateStatisticsAsync(UpdateMode mode)
    {
        return StatisticsViewModel is null || AnnoCanvas is null
            ? Task.CompletedTask
            : StatisticsViewModel.UpdateStatisticsAsync(mode,
                [.. AnnoCanvas.PlacedObjects],
                AnnoCanvas.SelectedObjects,
                BuildingPresets);
    }

    /// <summary>
    /// Called when localisation is changed, to repopulate the tree view
    /// </summary>
    private void RepopulateTreeView()
    {
        if (BuildingPresets == null) return; // prefer shared resources when available
        var treeState = PresetsTreeViewModel.GetCondensedTreeState();

        PresetsTreeViewModel.LoadItems(BuildingPresets);

        PresetsTreeViewModel.SetCondensedTreeState(treeState, BuildingPresets?.Version);
    }

    public void LoadAvailableIcons()
    {
        // Clear existing icons (except _noIconItem) to prevent duplicates on re-load
        AvailableIcons.Clear();
        AvailableIcons.Add(_noIconItem);

        foreach (var icon in (Icons ?? []).OrderBy(x => x.Value.NameForLanguage(_commons.CurrentLanguageCode)))
        {
            AvailableIcons.Add(icon.Value);
        }
    }

    public void LoadSettings()
    {
        // AnnoCanvas may not be created yet (e.g. on startup). Guard against null to avoid NREs.
        var placedObjects = (AnnoCanvas?.PlacedObjects) is { } p ? new List<LayoutObject>(p) : new List<LayoutObject>();
        var selectedObjects = (AnnoCanvas?.SelectedObjects) is { } s ? new List<LayoutObject>(s) : new List<LayoutObject>();
        StatisticsViewModel.ToggleBuildingList(_appSettings.StatsShowBuildingCount, [.. placedObjects],
            selectedObjects, BuildingPresets);

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
        var presets = BuildingPresets;
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
                // create a new document and populate its canvas (open into new tab)
                var document = CreateNewDocument();

                document.Canvas.SelectedObjects.Clear();
                document.Canvas.PlacedObjects.Clear();
                document.Canvas.UndoManager.Clear();

                var layoutObjects = new List<LayoutObject>(layout.Objects.Count);
                layoutObjects.AddRange(layout.Objects.Select(curObj =>
                    new LayoutObject(curObj, _coordinateHelper, _brushCache, _penCache)));

                document.LayoutSettings.LayoutVersion = layout.LayoutVersion;
                _ = document.Canvas.ComputeBoundingRect(layoutObjects);
                document.Canvas.PlacedObjects.AddRange(layoutObjects);
                document.Canvas.Normalize(1);
                document.Canvas.ResetViewport();
                document.Canvas.RaiseStatisticsUpdated(UpdateStatisticsEventArgs.All);
                document.Canvas.RaiseColorsInLayoutUpdated();
                document.Canvas.UndoManager.Clear();
                document.FilePath = filePath;
                document.IsDirty = false;
            }

            // ensure focused/visible canvas shows correct loaded file info
            if (ActiveDocument?.Canvas != null)
            {
                ActiveDocument.Canvas.LoadedFile = filePath;
                ActiveDocument.Canvas.ForceRendering();
                AnnoCanvas_LoadedFileChanged(this, new FileLoadedEventArgs(filePath, layout));
            }
        }
        catch (LayoutFileUnsupportedFormatException layoutEx)
        {
            Logger.Warn(layoutEx, "Version of layout file is not supported.");

            if (await _messageBoxService.ShowQuestion(
                    _localizationHelper.GetLocalization("FileVersionUnsupportedMessage"),
                    _localizationHelper.GetLocalization("FileVersionUnsupportedTitle")).ConfigureAwait(false))
            {
                await OpenFile(filePath, true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading layout from JSON.");

            IoErrorMessageBox(ex);
        }
    }


    [RelayCommand]
    private void Align(object parameter)
    {
        if (!(AnnoCanvas is AnnoCanvas canvas)) return;
        canvas.ExecuteAlign(parameter);
    }

    [RelayCommand]
    private void Distribute(object parameter)
    {
        if (!(AnnoCanvas is AnnoCanvas canvas)) return;
        canvas.ExecuteDistribute(parameter);
    }

    [RelayCommand]
    private void Rotate(object parameter)
    {
        if (!(AnnoCanvas is AnnoCanvas canvas)) return;
        canvas.ExecuteRotate(parameter);
    }

    [RelayCommand]
    private void Flip(object parameter)
    {
        if (!(AnnoCanvas is AnnoCanvas canvas)) return;
        canvas.ExecuteFlip(parameter);
    }


    /// <summary>
    /// Writes layout to file.
    /// </summary>
    public async Task SaveFileAsync(string filePath)
    {
        try
        {
            AnnoCanvas.Normalize(1);
            var layoutToSave = new LayoutFile(AnnoCanvas.PlacedObjects.Select(x => x.WrappedAnnoObject))
            {
                LayoutVersion = LayoutSettingsViewModel.LayoutVersion
            };

            if (_layoutService != null)
            {
                await _layoutService.SaveLayoutAsync(AnnoCanvas, filePath).ConfigureAwait(false);
            }
            else
            {
                // fall back to synchronous save wrapped on a background thread
                await Task.Run(() => _layoutLoader.SaveLayout(layoutToSave, filePath)).ConfigureAwait(false);
            }

            AnnoCanvas.UndoManager.IsDirty = false;
        }
        catch (Exception e)
        {
            IoErrorMessageBox(e);
        }
    }

    /// <summary>
    /// Synchronous wrapper for callers/tests that still use the old API.
    /// </summary>
    public void SaveFile(string filePath) => SaveFileAsync(filePath).GetAwaiter().GetResult();

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
            // Detach handlers from old instance
            var old = field;
            if (old != null)
            {
                try
                {
                    old.StatisticsUpdated -= AnnoCanvas_StatisticsUpdated;
                    old.OnCurrentObjectChanged -= UpdateUiFromObject;
                    old.OnStatusMessageChanged -= AnnoCanvas_StatusMessageChanged;
                    old.OnLoadedFileChanged -= AnnoCanvas_LoadedFileChanged;
                    old.OpenFileRequested -= AnnoCanvas_OpenFileRequested;
                    old.SaveFileRequested -= AnnoCanvas_SaveFileRequested;
                }
                catch
                {
                    // ignore detach errors
                }
            }

            // assign new value
            field = value;

            if (field != null)
            {
                // attach handlers to new instance
                field.StatisticsUpdated += AnnoCanvas_StatisticsUpdated;
                field.OnCurrentObjectChanged += UpdateUiFromObject;
                field.OnStatusMessageChanged += AnnoCanvas_StatusMessageChanged;
                field.OnLoadedFileChanged += AnnoCanvas_LoadedFileChanged;
                field.OpenFileRequested += AnnoCanvas_OpenFileRequested;
                field.SaveFileRequested += AnnoCanvas_SaveFileRequested;

                // Document-scoped services (UndoManager, etc.) are created and owned by each DocumentViewModel.
                // Do not create/override them here to avoid replacing per-document undo stacks when switching documents.

                BuildingSettingsViewModel.AnnoCanvasToUse = field;

                field.RenderGrid = CanvasShowGrid;
                field.RenderIcon = CanvasShowIcons;
                field.RenderLabel = CanvasShowLabels;
                field.RenderTrueInfluenceRange = CanvasShowTrueInfluenceRange;
                field.RenderInfluences = CanvasShowInfluences;
                field.RenderHarborBlockedArea = CanvasShowHarborBlockedArea;
                field.RenderPanorama = CanvasShowPanorama;
            }
            else
            {
                // ensure dependent viewmodels know no canvas is available
                BuildingSettingsViewModel.AnnoCanvasToUse = null;
            }
        }
    }

    // Collection of open documents (document per tab/document pane)
    public ObservableCollection<DocumentViewModel> Documents { get; } = new ObservableCollection<DocumentViewModel>();

    // The currently active document (bound to AvalonDock's active content)
    public DocumentViewModel ActiveDocument
    {
        get;
        set
        {
            var old = field;
            if (SetProperty(ref field, value))
            {
                old?.IsActive = false;

                if (field != null)
                {
                    field.IsActive = true;
                    // Update the main canvas reference so existing code bound to `AnnoCanvas` keeps working
                    AnnoCanvas = field.Canvas;
                    // Clear previously registered hotkeys to avoid duplicate IDs across documents
                    HotkeyCommandManager.Clear();
                    // Register hotkeys for the new active document's canvas
                    if (field.Canvas is IHotkeySource newHotkeySource)
                    {
                        newHotkeySource.RegisterHotkeys(HotkeyCommandManager);
                    }
                }
                else
                {
                    AnnoCanvas = null;
                }
            }
        }
    }

    #region Document Commands

    [RelayCommand]
    private void NewDocument()
    {
        CreateNewDocument();
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = Constants.SavedLayoutExtension,
            Filter = Constants.SaveOpenDialogFilter
        };

        if (dialog.ShowDialog() == true)
        {
            await OpenFile(dialog.FileName).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (ActiveDocument != null)
        {
            await ActiveDocument.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        if (ActiveDocument != null)
        {
            await ActiveDocument.SaveAsCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task SaveAll()
    {
        foreach (var doc in Documents.ToList())
        {
            if (doc.IsDirty)
            {
                await doc.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);
            }
        }
    }

    [RelayCommand]
    private async Task CloseAll()
    {
        foreach (var doc in Documents.ToList())
        {
            await doc.CloseCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }

    #endregion
    /// <summary>
    /// Creates a new document and sets it active.
    /// </summary>
    public DocumentViewModel CreateNewDocument()
    {
        var services = _documentServicesFactory?.CreateDocumentServices();
        var document = new DocumentViewModel(services, BuildingPresets, Icons);

        document.CloseRequested += OnDocumentCloseRequested;
        document.StatusMessageChanged += (s, m) => StatusMessage = m;
        document.PropertyChanged += Document_PropertyChanged;

        // Add document to the collection so it appears in tabs
        Documents.Add(document);

        ActiveDocument = document;

        return document;
    }

    private void OnDocumentCloseRequested(object sender, EventArgs e)
    {
        if (sender is DocumentViewModel document)
        {
            Documents.Remove(document);

            document.PropertyChanged -= Document_PropertyChanged;
            document.Dispose();

            ActiveDocument = Documents.Count > 0 ? Documents.Last() : null;
        }
    }

    private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is DocumentViewModel doc && e.PropertyName == nameof(DocumentViewModel.IsActive))
        {
            if (doc.IsActive)
            {
                ActiveDocument = doc;
            }
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

    /// <summary>
    /// Exposes the export service for CLI/external callers that need <see cref="IExportService.PrepareCanvasForRender"/>.
    /// </summary>
    public IExportService ExportService => _exportService;

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
        if (!(AnnoCanvas is AnnoDesigner.Controls.Canvas.AnnoCanvas canvas)) return;
        canvas.ExecuteZoomIn(param);
    }

    [RelayCommand]
    private void CanvasZoomOut(object param)
    {
        if (!(AnnoCanvas is AnnoDesigner.Controls.Canvas.AnnoCanvas canvas)) return;
        canvas.ExecuteZoomOut(param);
    }

    [RelayCommand]
    private void CanvasZoomFit(object param)
    {
        if (!(AnnoCanvas is AnnoDesigner.Controls.Canvas.AnnoCanvas canvas)) return;
        canvas.ExecuteZoomFit(param);
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

            var availableWidth = Application.Current?.MainWindow?.ActualWidth ?? 800;
            var availableHeight = Application.Current?.MainWindow?.ActualHeight ?? 600;

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
        RoadMergeHelper.MergeRoads(AnnoCanvas, _adjacentCellGrouper, _coordinateHelper, _brushCache, _penCache);
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
                    _localizationHelper.GetLocalization("FileVersionMismatchTitle")).ConfigureAwait(false))
            {
                await LoadLayoutFromJsonSub(jsonString, true).ConfigureAwait(false);
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
    private void ImportAnno117Savegame(object param)
    {
        var importWindow = new Views.ImportSavegameWindow(BuildingPresets);
        importWindow.Owner = System.Windows.Application.Current.MainWindow;

        if (importWindow.ShowDialog() == true && importWindow.ImportedObjects is { Count: > 0 })
        {
            // ponytail: Load imported objects directly into canvas. 
            // Full multi-tab integration is a follow-up.
            if (!AnnoCanvas.CheckUnsavedChanges().GetAwaiter().GetResult())
            {
                return;
            }

            AnnoCanvas.SelectedObjects.Clear();
            AnnoCanvas.PlacedObjects.Clear();
            foreach (var obj in importWindow.ImportedObjects)
            {
                AnnoCanvas.PlacedObjects.Add(new LayoutObject(obj, _coordinateHelper, _brushCache, _penCache));
            }

            AnnoCanvas.Normalize(1);
            AnnoCanvas.ForceRendering();
        }
    }


    [RelayCommand]
    private void UnregisterExtension(object param)
    {
        FileAssociationHelper.UnregisterExtension(_messageBoxService, _localizationHelper);
    }


    [RelayCommand]
    private void RegisterExtension(object param)
    {
        FileAssociationHelper.RegisterExtension(App.ExecutablePath, _messageBoxService, _localizationHelper);
    }

    public static void UpdateRegisteredExtension()
    {
        FileAssociationHelper.UpdateRegisteredExtension(App.ExecutablePath);
    }


    [RelayCommand]
    private void ExportImage(object param)
    {
        _exportService?.ExportImage(AnnoCanvas, new ExportSettings
        {
            UseCurrentZoom = UseCurrentZoomOnExportedImageValue,
            RenderSelectionHighlights = RenderSelectionHighlightsOnExportedImageValue,
            RenderVersion = RenderVersionOnExportedImageValue,
            RenderStatistics = StatisticsViewModel.IsVisible
        });
    }

    [RelayCommand]
    private void CopyLayoutToClipboard(object param)
    {
        _exportService?.CopyLayoutToClipboard(AnnoCanvas);
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
            [.. AnnoCanvas.PlacedObjects], AnnoCanvas.SelectedObjects, BuildingPresets);
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
