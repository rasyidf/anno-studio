using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AnnoDesigner.Controls.Canvas;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Extensions;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.ViewModels;
using Microsoft.Win32;
using NLog;

namespace AnnoDesigner.Services;

/// <summary>
/// Handles layout export to image and clipboard operations.
/// Extracted from MainViewModel to isolate rendering/IO concerns.
/// </summary>
public class ExportService : IExportService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ILayoutLoader _layoutLoader;
    private readonly ICoordinateHelper _coordinateHelper;
    private readonly IBrushCache _brushCache;
    private readonly IPenCache _penCache;
    private readonly IAppSettings _appSettings;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILocalizationHelper _localizationHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ICommons _commons;
    private readonly Func<BuildingPresets> _getBuildingPresets;
    private readonly Func<Dictionary<string, IconImage>> _getIcons;

    // ponytail: these are passed via a Func so the service doesn't own ViewModel state.
    private readonly Func<StatisticsViewModel> _getStatisticsViewModel;
    private readonly Func<LayoutSettingsViewModel> _getLayoutSettingsViewModel;

    public ExportService(
        ILayoutLoader layoutLoader,
        ICoordinateHelper coordinateHelper,
        IBrushCache brushCache,
        IPenCache penCache,
        IAppSettings appSettings,
        IMessageBoxService messageBoxService,
        ILocalizationHelper localizationHelper,
        IFileSystem fileSystem,
        ICommons commons,
        Func<BuildingPresets> getBuildingPresets,
        Func<Dictionary<string, IconImage>> getIcons,
        Func<StatisticsViewModel> getStatisticsViewModel,
        Func<LayoutSettingsViewModel> getLayoutSettingsViewModel)
    {
        _layoutLoader = layoutLoader;
        _coordinateHelper = coordinateHelper;
        _brushCache = brushCache;
        _penCache = penCache;
        _appSettings = appSettings;
        _messageBoxService = messageBoxService;
        _localizationHelper = localizationHelper;
        _fileSystem = fileSystem;
        _commons = commons;
        _getBuildingPresets = getBuildingPresets;
        _getIcons = getIcons;
        _getStatisticsViewModel = getStatisticsViewModel;
        _getLayoutSettingsViewModel = getLayoutSettingsViewModel;
    }

    /// <inheritdoc/>
    public void ExportImage(IAnnoCanvas canvas, ExportSettings settings)
    {
        var dialog = new SaveFileDialog
        {
            DefaultExt = Constants.ExportedImageExtension,
            Filter = Constants.ExportDialogFilter
        };

        if (!string.IsNullOrEmpty(canvas.LoadedFile))
        {
            dialog.FileName = _fileSystem.Path.GetFileNameWithoutExtension(canvas.LoadedFile);
        }

        if (dialog.ShowDialog() != true) return;

        try
        {
            RenderToFile(canvas, dialog.FileName, 1, settings);

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

    /// <inheritdoc/>
    public void CopyLayoutToClipboard(IAnnoCanvas canvas)
    {
        try
        {
            using var ms = new MemoryStream();
            canvas.Normalize(1);
            var layoutToSave = new LayoutFile(canvas.PlacedObjects.Select(x => x.WrappedAnnoObject));
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

    /// <inheritdoc/>
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
        foreach (var curIcon in _getIcons() ?? new Dictionary<string, IconImage>())
        {
            icons.Add(curIcon.Key,
                new IconImage(curIcon.Value.Name, curIcon.Value.Localizations, curIcon.Value.IconPath));
        }

        var annoObjects = placedObjects.ToList();
        var statistics = new StatisticsCalculationHelper().CalculateStatistics(annoObjects, true, true);

        var quadTree = new QuadTree<LayoutObject>((Rect)statistics);
        quadTree.AddRange(annoObjects.Select(o =>
            new LayoutObject(o, _coordinateHelper, _brushCache, _penCache)));

        var target = new AnnoCanvas(_getBuildingPresets(), icons, _appSettings, _coordinateHelper, _brushCache,
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

        target.Normalize(border);

        if (renderSettings.GridSize.HasValue)
        {
            target.GridSize = renderSettings.GridSize.Value;
        }

        target.SelectedObjects.UnionWith(selectedObjects.Select(o =>
            new LayoutObject(o, _coordinateHelper, _brushCache, _penCache)));

        var width = _coordinateHelper.GridToScreen(
            target.PlacedObjects.Max(_ => _.Position.X + _.Size.Width) + border,
            target.GridSize);
        var height =
            _coordinateHelper.GridToScreen(target.PlacedObjects.Max(_ => _.Position.Y + _.Size.Height) + border,
                target.GridSize) + 1;

        if (renderSettings.RenderVersion)
        {
            var versionView = new VersionView() { Context = _getLayoutSettingsViewModel() };
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
            exportStatisticsViewModel.ShowBuildingList = _getStatisticsViewModel().ShowBuildingList;

            var exportStatisticsView = new StatisticsView() { Context = exportStatisticsViewModel };
            target.DockPanel.Children.Insert(0, exportStatisticsView);
            DockPanel.SetDock(exportStatisticsView, Dock.Right);

            exportStatisticsView.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            exportStatisticsView.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (exportStatisticsView.DesiredSize.Height > height)
            {
                height = exportStatisticsView.DesiredSize.Height + target.LinePenThickness + border;
            }

            width += exportStatisticsView.DesiredSize.Width + target.LinePenThickness;
        }

        target.Width = width;
        target.Height = height;
        target.UpdateLayout();

        var outputSize = new Size(width, height);
        target.Measure(outputSize);
        target.Arrange(new Rect(outputSize));

        return target;
    }

    private void RenderToFile(IAnnoCanvas canvas, string filename, int border, ExportSettings settings)
    {
        if (canvas.PlacedObjects.Count == 0)
        {
            return;
        }

        Logger.Trace($"UI thread: {Environment.CurrentManagedThreadId} ({Thread.CurrentThread.Name})");

        void RenderThread()
        {
            var target = PrepareCanvasForRender(
                canvas.PlacedObjects.Select(o => o.WrappedAnnoObject),
                settings.RenderSelectionHighlights ? canvas.SelectedObjects.Select(o => o.WrappedAnnoObject) : [],
                border,
                new CanvasRenderSetting()
                {
                    GridSize = settings.UseCurrentZoom ? canvas.GridSize : null,
                    RenderGrid = canvas.RenderGrid,
                    RenderHarborBlockedArea = canvas.RenderHarborBlockedArea,
                    RenderIcon = canvas.RenderIcon,
                    RenderInfluences = canvas.RenderInfluences,
                    RenderLabel = canvas.RenderLabel,
                    RenderPanorama = canvas.RenderPanorama,
                    RenderTrueInfluenceRange = canvas.RenderTrueInfluenceRange,
                    RenderStatistics = settings.RenderStatistics,
                    RenderVersion = settings.RenderVersion
                }
            );

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
}
