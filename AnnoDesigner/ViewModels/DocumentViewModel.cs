
using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services.Undo;
using AnnoDesigner.Core.Presets.Models;
using System.Collections.Generic;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Controls.Canvas;
using AnnoDesigner.Controls.Canvas.Services;
using AnnoDesigner.Services;
using AnnoDesigner.CustomEventArgs;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AnnoDesigner.ViewModels
{
    /// <summary>
    /// Represents a single document (layout) in the multi-document interface.
    /// Each instance manages its own canvas, undo stack, and document-specific state.
    /// </summary>
    public partial class DocumentViewModel : ObservableObject
    {
        #region Fields

        private readonly IDocumentServices _services;

        [ObservableProperty]
        private string _documentTitle;

        [ObservableProperty]
        private string _filePath;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private Guid _documentId;

        [ObservableProperty]
        private IAnnoCanvas _canvas;

        [ObservableProperty]
        private StatisticsViewModel _statistics;

        [ObservableProperty]
        private BuildingSettingsViewModel _buildingSettings;

        [ObservableProperty]
        private LayoutSettingsViewModel _layoutSettings;

        #endregion

        #region Constructor

        public DocumentViewModel(
            IDocumentServices services,
            BuildingPresets buildingPresets,
            Dictionary<string, IconImage> icons)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _documentId = Guid.NewGuid();
            _documentTitle = "Untitled";

            InitializeCanvas(buildingPresets, icons);
            InitializeViewModels();
            RegisterEventHandlers();
        }

        #endregion

        #region Initialization

        private void InitializeCanvas(
            BuildingPresets buildingPresets,
            Dictionary<string, IconImage> icons)
        {
            // Create new canvas instance with scoped services
            Canvas = new AnnoCanvas(
                buildingPresets,
                icons,
                _services.AppSettings,
                _services.CoordinateHelper,
                _services.BrushCache,
                _services.PenCache,
                _services.MessageBoxService,
                _services.LocalizationHelper,
                _services.CreateUndoManager(), // Scoped per document
                layoutFileServiceFactory => new LayoutFileService(
                    layoutFileServiceFactory,
                    _services.MessageBoxService,
                    _services.LocalizationHelper),
                _services.ClipboardService
            );

            // Apply default rendering settings
            Canvas.RenderGrid = _services.AppSettings.ShowGrid;
            Canvas.RenderIcon = _services.AppSettings.ShowIcons;
            Canvas.RenderLabel = _services.AppSettings.ShowLabels;
            Canvas.RenderInfluences = _services.AppSettings.ShowInfluences;
            Canvas.RenderTrueInfluenceRange = _services.AppSettings.ShowTrueInfluenceRange;
            Canvas.RenderHarborBlockedArea = _services.AppSettings.ShowHarborBlockedArea;
            Canvas.RenderPanorama = _services.AppSettings.ShowPanorama;
        }

        private void InitializeViewModels()
        {
            // Create document-specific view models
            Statistics = new StatisticsViewModel(
                _services.LocalizationHelper,
                _services.Commons,
                _services.AppSettings
            );

            BuildingSettings = new BuildingSettingsViewModel(
                _services.AppSettings,
                _services.MessageBoxService,
                _services.LocalizationHelper
            );
            BuildingSettings.AnnoCanvasToUse = Canvas;

            LayoutSettings = new LayoutSettingsViewModel();
        }

        private void RegisterEventHandlers()
        {
            Canvas.StatisticsUpdated += OnCanvasStatisticsUpdated;
            Canvas.OnLoadedFileChanged += OnCanvasLoadedFileChanged;
            Canvas.OnStatusMessageChanged += OnCanvasStatusMessageChanged;
            Canvas.UndoManager.PropertyChanged += OnUndoManagerPropertyChanged;
        }

        #endregion

        #region Event Handlers

        private void OnCanvasStatisticsUpdated(object sender, UpdateStatisticsEventArgs e)
        {
            _ = Statistics.UpdateStatisticsAsync(
                e.Mode,
                Canvas.PlacedObjects.ToList(),
                Canvas.SelectedObjects,
                Canvas.BuildingPresets
            );
        }

        private void OnCanvasLoadedFileChanged(object sender, FileLoadedEventArgs e)
        {
            FilePath = e.FilePath;
            var fileName = string.IsNullOrWhiteSpace(e.FilePath)
                ? "Untitled"
                : System.IO.Path.GetFileName(e.FilePath);

            DocumentTitle = e.Layout?.LayoutVersion != null
                ? $"{fileName} ({e.Layout.LayoutVersion})"
                : fileName;

            IsDirty = false;
        }

        private void OnCanvasStatusMessageChanged(string message)
        {
            StatusMessageChanged?.Invoke(this, message);
        }

        private void OnUndoManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IUndoManager.IsDirty))
            {
                IsDirty = Canvas.UndoManager.IsDirty;
            }
        }

        #endregion

        #region Events

        public event EventHandler<string> StatusMessageChanged;
        public event EventHandler CloseRequested;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                await SaveAsAsync();
            }
            else
            {
                await _services.LayoutService.SaveLayoutAsync(Canvas, FilePath);
                IsDirty = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsAsync()
        {
            var filePath = _services.FileDialogService.ShowSaveFile(
                Constants.SavedLayoutExtension,
                Constants.SaveOpenDialogFilter
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                await _services.LayoutService.SaveLayoutAsync(Canvas, filePath);
                FilePath = filePath;
                IsDirty = false;
            }
        }

        [RelayCommand]
        private async Task<bool> CheckUnsavedChangesAsync()
        {
            if (!IsDirty)
            {
                return true;
            }

            var result = await _services.MessageBoxService.ShowQuestionWithCancel(
                null,
                _services.LocalizationHelper.GetLocalization("SaveUnsavedChanges"),
                _services.LocalizationHelper.GetLocalization("UnsavedChanged")
            ).ConfigureAwait(false);

            if (result == null)
            {
                return false; // Cancel
            }

            if (result.Value)
            {
                await SaveAsync();
            }

            return true;
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            if (await CheckUnsavedChangesAsync())
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            // Unregister event handlers
            if (Canvas != null)
            {
                Canvas.StatisticsUpdated -= OnCanvasStatisticsUpdated;
                Canvas.OnLoadedFileChanged -= OnCanvasLoadedFileChanged;
                Canvas.OnStatusMessageChanged -= OnCanvasStatusMessageChanged;
            }

            // Dispose of scoped services
            _services.Dispose();
        }

        #endregion
    }

}
