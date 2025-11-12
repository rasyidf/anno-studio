using AnnoDesigner.CanvasV2.FeatureFlags;
using AnnoDesigner.CanvasV2.Integration;
using AnnoDesigner.CommandLine;
using AnnoDesigner.CommandLine.Arguments;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Extensions;
using AnnoDesigner.Models;
using AnnoDesigner.ViewModels;
using NLog;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace AnnoDesigner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow, ICloseable
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private MainViewModel _mainViewModel;
    private readonly IAppSettings _appSettings;
    private readonly IFeatureFlags _featureFlags;
    private readonly bool _useCanvasV2;

    public new MainViewModel DataContext { get => base.DataContext as MainViewModel; set => base.DataContext = value; }

    #region Initialization

    public MainWindow(IAppSettings appSettingsToUse, IFeatureFlags featureFlags)
    {
        _appSettings = appSettingsToUse;
        _featureFlags = featureFlags;
        _useCanvasV2 = _featureFlags.IsEnabled(CanvasFeatureFlagNames.UseCanvasV2);

        InitializeComponent();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        _mainViewModel = DataContext;

        // Feature flag: Use CanvasV2 if enabled, otherwise use v1 AnnoCanvas
        if (_useCanvasV2)
        {
            // Replace v1 canvas with v2 canvas programmatically using the self-contained adapter
            logger.Info("Using AnnoCanvasV2 (feature flag enabled)");

            // Remove v1 canvas from scroll viewer
            canvasScrollViewer.Content = null;

            // Create a self-contained adapter which constructs the V2 viewmodel and view internally.
            CanvasV2Adapter canvasAdapter = new(
                featureFlags: _featureFlags,
                undoManager: annoCanvas.UndoManager,
                clipboardService: annoCanvas.ClipboardService,
                appSettings: _appSettings,
                coordinateHelper: new Helper.CoordinateHelper(),
                brushCache: new Helper.BrushCache(),
                penCache: new Helper.PenCache(),
                layoutLoader: new Core.Layout.LayoutLoader(),
                localizationHelper: _mainViewModel.LocalizationHelper,
                messageBoxService: new Services.MessageBoxService(),
                hotkeyManager: _mainViewModel.HotkeyCommandManager,
                buildingPresets: annoCanvas.BuildingPresets,
                icons: annoCanvas.Icons
            );

            // Set the adapter's control as the content of the scroll viewer
            canvasScrollViewer.Content = canvasAdapter.Control;

            // Expose to MainViewModel
            _mainViewModel.AnnoCanvas = canvasAdapter;

            // Ensure hotkeys are registered with the main hotkey manager (no-op if already wired)
            _mainViewModel.AnnoCanvas.RegisterHotkeys(_mainViewModel.HotkeyCommandManager);

            // Wire up feature flag changes when MainViewModel changes canvas settings
            _mainViewModel.PropertyChanged += (s, args) =>
            {
                if (_featureFlags is CanvasV2.FeatureFlags.SimpleFeatureFlags simpleFlags)
                {
                    switch (args.PropertyName)
                    {
                        case nameof(_mainViewModel.CanvasShowGrid):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderGrid, _mainViewModel.CanvasShowGrid);
                            break;
                        case nameof(_mainViewModel.CanvasShowLabels):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderLabels, _mainViewModel.CanvasShowLabels);
                            break;
                        case nameof(_mainViewModel.CanvasShowIcons):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderIcons, _mainViewModel.CanvasShowIcons);
                            break;
                        case nameof(_mainViewModel.CanvasShowInfluences):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderInfluences, _mainViewModel.CanvasShowInfluences);
                            break;
                        case nameof(_mainViewModel.CanvasShowTrueInfluenceRange):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderTrueInfluenceRange, _mainViewModel.CanvasShowTrueInfluenceRange);
                            break;
                        case nameof(_mainViewModel.CanvasShowHarborBlockedArea):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderHarborBlockedArea, _mainViewModel.CanvasShowHarborBlockedArea);
                            break;
                        case nameof(_mainViewModel.CanvasShowPanorama):
                            simpleFlags.Set(CanvasFeatureFlagNames.RenderPanorama, _mainViewModel.CanvasShowPanorama);
                            break;
                    }
                }
            };

            logger.Info("CanvasV2 successfully initialized with AnnoCanvasViewModel");
        }
        else
        {
            // Wire up V1 canvas (default)
            logger.Info("Using AnnoCanvas v1 (default)");
            _mainViewModel.AnnoCanvas = annoCanvas;
            _mainViewModel.AnnoCanvas.RegisterHotkeys(_mainViewModel.HotkeyCommandManager);
        }

        _mainViewModel.ShowStatisticsChanged += MainViewModel_ShowStatisticsChanged;

        App.DpiScale = VisualTreeHelper.GetDpi(this);

        DpiChanged += MainWindow_DpiChanged;

        ToggleStatisticsView(_mainViewModel.StatisticsViewModel.IsVisible);

        _mainViewModel.LoadSettings();

        _mainViewModel.LoadAvailableIcons();

        //load presets before checking for updates
        _mainViewModel.LoadPresets();

        // check for updates on startup
        if (_appSettings.EnableAutomaticUpdateCheck)
        {
            //just fire and forget
            _ = _mainViewModel.PreferencesUpdateViewModel.CheckForUpdates(isAutomaticUpdateCheck: true);
        }

        // load color presets
        //colorPicker.StandardColors.Clear();
        //This is currently disabled
        //colorPicker.ShowStandardColors = false;
        //try
        //{
        //    ColorPresetsLoader loader = new ColorPresetsLoader();
        //    var defaultScheme = loader.LoadDefaultScheme();
        //    foreach (var curPredefinedColor in defaultScheme.Colors.GroupBy(x => x.Color).Select(x => x.Key))
        //    {
        //        //colorPicker.StandardColors.Add(new Xceed.Wpf.Toolkit.ColorItem(curPredefinedColor.Color, $"{curPredefinedColor.TargetTemplate}"));
        //        colorPicker.StandardColors.Add(new Xceed.Wpf.Toolkit.ColorItem(curPredefinedColor, curPredefinedColor.ToHex()));
        //    }
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(ex.Message, "Loading of the color presets failed");
        //}            

        // load file given by argument
        if (App.StartupArguments is OpenArgs startupArgs && !string.IsNullOrEmpty(startupArgs.FilePath))
        {
            _ = _mainViewModel.OpenFileAsync(startupArgs.FilePath);
        }
        // export layout to image
        else if (App.StartupArguments is ExportArgs exportArgs && !string.IsNullOrEmpty(exportArgs.LayoutFilePath) && !string.IsNullOrEmpty(exportArgs.ExportedImageFilePath))
        {
            Core.Layout.Models.LayoutFile layout = new LayoutLoader().LoadLayout(exportArgs.LayoutFilePath);
            CanvasRenderSetting settings = new()
            {
                GridSize = exportArgs.GridSize,
                RenderGrid = exportArgs.RenderGrid ?? (!exportArgs.UseUserSettings || _appSettings.ShowGrid),
                RenderIcon = exportArgs.RenderIcon ?? (!exportArgs.UseUserSettings || _appSettings.ShowIcons),
                RenderLabel = exportArgs.RenderLabel ?? (!exportArgs.UseUserSettings || _appSettings.ShowLabels),
                RenderStatistics = exportArgs.RenderStatistics ?? (!exportArgs.UseUserSettings || _appSettings.StatsShowStats),
                RenderVersion = exportArgs.RenderVersion ?? true,
                RenderHarborBlockedArea = exportArgs.RenderHarborBlockedArea ?? (exportArgs.UseUserSettings && _appSettings.ShowHarborBlockedArea),
                RenderInfluences = exportArgs.RenderInfluences ?? (exportArgs.UseUserSettings && _appSettings.ShowInfluences),
                RenderPanorama = exportArgs.RenderPanorama ?? (exportArgs.UseUserSettings && _appSettings.ShowPanorama),
                RenderTrueInfluenceRange = exportArgs.RenderTrueInfluenceRange ?? (exportArgs.UseUserSettings && _appSettings.ShowTrueInfluenceRange)
            };

            if (_useCanvasV2 && _mainViewModel.AnnoCanvas is AnnoDesigner.CanvasV2.Integration.CanvasV2Adapter v2)
            {
                // Use V2 renderer directly for headless export
                v2.RenderToFile(exportArgs.ExportedImageFilePath, layout.Objects, Array.Empty<AnnoDesigner.Core.Models.AnnoObject>(), Math.Max(exportArgs.Border, 0), settings);
            }
            else
            {
                // Fall back to v1 export pipeline
                _mainViewModel.PrepareCanvasForRender(layout.Objects, [], Math.Max(exportArgs.Border, 0), settings)
                    .RenderToFile(exportArgs.ExportedImageFilePath);
            }

            ConsoleManager.Show();
            Console.WriteLine($"Export completed: \"{exportArgs.LayoutFilePath}\"");
            ConsoleManager.Hide();

            Close();
        }
    }

    private void MainViewModel_ShowStatisticsChanged(object sender, EventArgs e)
    {
        ToggleStatisticsView(_mainViewModel.StatisticsViewModel.IsVisible);
    }

    #endregion

    #region UI events

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        App.DpiScale = e.NewDpi;
    }

    private void ToggleStatisticsView(bool showStatisticsView)
    {
        //colStatisticsView.MinWidth = showStatisticsView ? 100 : 0;
        //colStatisticsView.Width = showStatisticsView ? GridLength.Auto : new GridLength(0);

        statisticsView.Visibility = showStatisticsView ? Visibility.Visible : Visibility.Collapsed;
        statisticsView.MinWidth = showStatisticsView ? 100 : 0;

        //splitterStatisticsView.Visibility = showStatisticsView ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TextBoxSearchPresetsKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            //used to fix issue with misplaced caret in TextBox
            TextBoxSearchPresets.UpdateLayout();
            _ = TextBoxSearchPresets.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            //TextBoxSearchPresets.InvalidateVisual();                
        }
    }

    #endregion

    private async void WindowClosing(object sender, CancelEventArgs e)
    {
        bool canClose = _mainViewModel?.AnnoCanvas != null
            ? await _mainViewModel.AnnoCanvas.CheckUnsavedChanges()
            : await annoCanvas.CheckUnsavedChanges();
        // Prefer the canvas exposed on the MainViewModel (adapter-aware). Fall back to the v1 control if necessary.

        if (!canClose)
        {
            e.Cancel = true;
            return;
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (_mainViewModel == null)
        {
            return;
        }

        _mainViewModel.MainWindowWindowState = WindowState;

        _mainViewModel?.SaveSettings();

#if DEBUG
        string userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
        logger.Trace($"saving settings: \"{userConfig}\"");
#endif
    }

}