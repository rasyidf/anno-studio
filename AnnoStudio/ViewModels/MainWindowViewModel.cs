using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using AnnoStudio.Services;

namespace AnnoStudio.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IProjectService _projectService;
        private readonly IDocumentManager _documentManager;
        private readonly IStampService _stampService;
        private readonly IPresetsService _presetsService;
        private readonly CanvasIntegrationService _canvasIntegration;

        private string _mainWindowTitle = "Anno Studio";
        private string _statusMessage = "Ready";
        private string _selectedTool = "None";
        private string _cursorPosition = "X: 0, Y: 0";
        private string _canvasSize = "0 x 0";
        private bool _canvasShowGrid = true;
        private bool _canvasShowIcons = true;
        private bool _canvasShowLabels = true;
        private IDock? _layout;
        private LayoutDocument? _activeDocument;

        public ObservableCollection<LayoutDocument> Documents => _documentManager.Documents;

        public MainWindowViewModel(Window? mainWindow = null)
        {
            // Initialize services
            _projectService = new ProjectService(mainWindow);
            _documentManager = new DocumentManager(_projectService);
            _stampService = new StampService();
            _presetsService = new PresetsService();
            _canvasIntegration = new CanvasIntegrationService();

            // Subscribe to document manager events
            _documentManager.ActiveDocumentChanged += OnActiveDocumentChanged;
            _documentManager.DocumentAdded += OnDocumentAdded;
            _documentManager.DocumentRemoved += OnDocumentRemoved;

            // Subscribe to canvas integration events
            _canvasIntegration.ActiveCanvasChanged += OnCanvasIntegrationChanged;

            // Initialize Dock layout
            var factory = new DockFactory();
            Layout = factory.CreateLayout();
            factory.InitLayout(Layout);

            // Initialize commands
            NewCanvasCommand = new AsyncRelayCommand(NewCanvas);
            OpenFileCommand = new AsyncRelayCommand(OpenFile);
            SaveFileCommand = new AsyncRelayCommand(SaveFile);
            SaveFileAsCommand = new AsyncRelayCommand(SaveFileAs);
            ExportImageCommand = new AsyncRelayCommand(ExportImage);
            ExitCommand = new AsyncRelayCommand(Exit);
            
            UndoCommand = new RelayCommand(ExecuteUndo, CanUndoImpl);
            RedoCommand = new RelayCommand(ExecuteRedo, CanRedoImpl);
            
            OpenPlaygroundCommand = new RelayCommand(ExecuteOpenPlayground);
            ShowPreferencesCommand = new RelayCommand(ExecuteShowPreferences);
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);

            // Load presets asynchronously
            _ = _presetsService.LoadPresetsAsync();
        }

        #region Properties

        public IDock? Layout
        {
            get => _layout;
            set => SetProperty(ref _layout, value);
        }

        public string MainWindowTitle
        {
            get => _mainWindowTitle;
            set => SetProperty(ref _mainWindowTitle, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public string CursorPosition
        {
            get => _cursorPosition;
            set => SetProperty(ref _cursorPosition, value);
        }

        public string CanvasSize
        {
            get => _canvasSize;
            set => SetProperty(ref _canvasSize, value);
        }

        public bool CanvasShowGrid
        {
            get => _canvasShowGrid;
            set => SetProperty(ref _canvasShowGrid, value);
        }

        public bool CanvasShowIcons
        {
            get => _canvasShowIcons;
            set => SetProperty(ref _canvasShowIcons, value);
        }

        public bool CanvasShowLabels
        {
            get => _canvasShowLabels;
            set => SetProperty(ref _canvasShowLabels, value);
        }

        public LayoutDocument? ActiveDocument
        {
            get => _activeDocument;
            private set => SetProperty(ref _activeDocument, value);
        }

        #endregion

        #region Commands

        public ICommand NewCanvasCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveFileAsCommand { get; }
        public ICommand ExportImageCommand { get; }
        public ICommand ExitCommand { get; }
        
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        
        public ICommand OpenPlaygroundCommand { get; }
        public ICommand ShowPreferencesCommand { get; }
        public ICommand ShowAboutCommand { get; }

        #endregion

        #region Command Implementations

        private async Task NewCanvas()
        {
            var document = await _projectService.CreateNewDocumentAsync();
            _documentManager.AddDocument(document);
            StatusMessage = "New canvas created";
        }

        private async Task OpenFile()
        {
            var document = await _projectService.OpenDocumentAsync();
            if (document != null)
            {
                var existing = _documentManager.GetDocumentByPath(document.FilePath!);
                if (existing != null)
                {
                    _documentManager.ActiveDocument = existing;
                    StatusMessage = $"Document already open: {existing.Title}";
                }
                else
                {
                    _documentManager.AddDocument(document);
                    StatusMessage = $"Opened: {document.Title}";
                }
            }
        }

        private async Task SaveFile()
        {
            if (ActiveDocument == null)
                return;

            var success = await _projectService.SaveDocumentAsync(ActiveDocument);
            StatusMessage = success ? $"Saved: {ActiveDocument.Title}" : "Save failed";
        }

        private async Task SaveFileAs()
        {
            if (ActiveDocument == null)
                return;

            var success = await _projectService.SaveDocumentAsAsync(ActiveDocument);
            StatusMessage = success ? $"Saved as: {ActiveDocument.Title}" : "Save as failed";
        }

        private async Task ExportImage()
        {
            if (ActiveDocument == null)
                return;

            var success = await _projectService.ExportDocumentAsync(ActiveDocument);
            StatusMessage = success ? "Export successful" : "Export failed";
        }

        private async Task Exit()
        {
            var canExit = await _documentManager.CloseAllDocumentsAsync();
            if (canExit)
            {
                // Application will exit
            }
        }

        private void ExecuteUndo()
        {
            _canvasIntegration.Undo();
            StatusMessage = "Undo";
        }

        private bool CanUndoImpl() => _canvasIntegration.CanUndo();

        private void ExecuteRedo()
        {
            _canvasIntegration.Redo();
            StatusMessage = "Redo";
        }

        private bool CanRedoImpl() => _canvasIntegration.CanRedo();

        private void ExecuteOpenPlayground()
        {
            var playground = new Views.PlaygroundWindow();
            playground.Show();
            StatusMessage = "Opened Playground window";
        }

        private void ExecuteShowPreferences()
        {
            StatusMessage = "Show preferences...";
        }

        private void ExecuteShowAbout()
        {
            StatusMessage = "Show about...";
        }

        #endregion

        #region Helper Methods

        private bool HasActiveDocument() => ActiveDocument != null;

        private void OnActiveDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            ActiveDocument = e.Document;
            
            if (ActiveDocument != null)
            {
                MainWindowTitle = $"Anno Studio - {ActiveDocument.Title}";
                
                // Update canvas integration to point to new active document's canvas
                _canvasIntegration.ActiveCanvasViewModel = ActiveDocument.EditorViewModel;
                
                // Subscribe to canvas events for status bar updates
                SubscribeToCanvasEvents(ActiveDocument.EditorViewModel);
            }
            else
            {
                MainWindowTitle = "Anno Studio";
                _canvasIntegration.ActiveCanvasViewModel = null;
                SelectedTool = "None";
                CursorPosition = "X: 0, Y: 0";
                CanvasSize = "0 x 0";
            }

            // Notify command can execute changed
            ((AsyncRelayCommand)SaveFileCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SaveFileAsCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ExportImageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)UndoCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RedoCommand).NotifyCanExecuteChanged();
        }

        private void OnCanvasIntegrationChanged(object? sender, EventArgs e)
        {
            // Notify commands that depend on canvas state
            ((RelayCommand)UndoCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RedoCommand).NotifyCanExecuteChanged();
        }

        private void SubscribeToCanvasEvents(EditorCanvas.ViewModels.EditorCanvasViewModel? viewModel)
        {
            if (viewModel?.Canvas == null)
                return;

            var canvas = viewModel.Canvas;
            
            // Track tool changes
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.SelectedTool))
                {
                    SelectedTool = viewModel.SelectedTool?.Name ?? "None";
                }
            };
            
            // Track cursor position
            canvas.EventBus.CursorPositionChanged += (s, pos) =>
            {
                CursorPosition = $"X: {pos.X:F1}, Y: {pos.Y:F1}";
            };
            
            // Track zoom changes to update canvas effective size
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.ZoomLevel))
                {
                    var width = canvas.Bounds.Width;
                    var height = canvas.Bounds.Height;
                    CanvasSize = $"{width:F0} x {height:F0} (Zoom: {viewModel.ZoomLevel:P0})";
                }
            };
            
            // Set initial values
            SelectedTool = viewModel.SelectedTool?.Name ?? "None";
            var initialWidth = canvas.Bounds.Width;
            var initialHeight = canvas.Bounds.Height;
            CanvasSize = $"{initialWidth:F0} x {initialHeight:F0}";
        }

        private void OnDocumentAdded(object? sender, DocumentChangedEventArgs e)
        {
            StatusMessage = $"Document added: {e.Document?.Title}";
        }

        private void OnDocumentRemoved(object? sender, DocumentChangedEventArgs e)
        {
            StatusMessage = $"Document closed: {e.Document?.Title}";
        }

        #endregion

        #region Public Services Access

        public IProjectService ProjectService => _projectService;
        public IDocumentManager DocumentManager => _documentManager;
        public IStampService StampService => _stampService;
        public IPresetsService PresetsService => _presetsService;
        public CanvasIntegrationService CanvasIntegration => _canvasIntegration;

        #endregion
    }
}
