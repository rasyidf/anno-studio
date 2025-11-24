using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AnnoDesigner.Controls.Canvas.Services;
using AnnoDesigner.Core;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Loader;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.CustomEventArgs;
using AnnoDesigner.Extensions;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services;
using AnnoDesigner.Services.Undo;
using AnnoDesigner.Services.Undo.Operations;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace AnnoDesigner.Controls.Canvas
{
    /// <summary>
    /// Interaction logic for AnnoCanvas.xaml
    /// </summary>
    public partial class AnnoCanvas2 : UserControl, IAnnoCanvas, IHotkeySource, IScrollInfo
    {
        internal static readonly Logger logger = LogManager.GetCurrentClassLogger();

        //Important: These match values in the translations dictionary (e.g "Rotate" matches "Rotate" in the localization dictionary)
        public const string ROTATE_LOCALIZATION_KEY = "Rotate";
        public const string ROTATE_ALL_LOCALIZATION_KEY = "RotateAll";
        public const string COPY_LOCALIZATION_KEY = "Copy";
        public const string PASTE_LOCALIZATION_KEY = "Paste";
        public const string DELETE_LOCALIZATION_KEY = "Delete";
        public const string DUPLICATE_LOCALIZATION_KEY = "Duplicate";
        public const string DELETE_OBJECT_UNDER_CURSOR_LOCALIZATION_KEY = "DeleteObjectUnderCursor";
        public const string UNDO_LOCALIZATION_KEY = "Undo";
        public const string REDO_LOCALIZATION_KEY = "Redo";
        public const string ENABLE_DEBUG_MODE_LOCALIZATION_KEY = "EnableDebugMode";
        public const string SELECT_ALL_SAME_IDENTIFIER_LOCALIZATION_KEY = "SelectAllSameIdentifier";

        public event EventHandler<UpdateStatisticsEventArgs> StatisticsUpdated;
        public event EventHandler<EventArgs> ColorsInLayoutUpdated;
        /// <summary>
        /// Event which is fired when the status message should be changed.
        /// </summary>
        public event EventHandler<FileLoadedEventArgs> OnLoadedFileChanged;
        public event EventHandler<OpenFileEventArgs> OpenFileRequested;
        public event EventHandler<SaveFileEventArgs> SaveFileRequested;

        #region Properties

        public IUndoManager UndoManager { get; internal set; }

        public IClipboardService ClipboardService { get; set; }

        /// <summary>
        /// Contains all loaded icons as a mapping of name (the filename without extension) to loaded BitmapImage.
        /// </summary>
        public Dictionary<string, IconImage> Icons { get; private set; }

        public BuildingPresets BuildingPresets { get; private set; }

        /// <summary>
        /// Backing field of the GridSize property.
        /// </summary>
        internal int _gridSize = Constants.GridStepDefault;

        /// <summary>
        /// Gets or sets the width of the grid cells.
        /// Increasing the grid size results in zooming in and vice versa.
        /// </summary>
        public int GridSize
        {
            get { return _gridSize; }
            set
            {
                var tmp = value;

                if (tmp < Constants.GridStepMin)
                {
                    tmp = Constants.GridStepMin;
                }
                else if (tmp > Constants.GridStepMax)
                {
                    tmp = Constants.GridStepMax;
                }

                if (_gridSize != tmp)
                {
                    _gridSize = tmp;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderGrid property.
        /// </summary>
        internal bool _renderGrid;

        /// <summary>
        /// Gets or sets a value indicating whether the grid should be rendered.
        /// </summary>
        public bool RenderGrid
        {
            get { return _renderGrid; }
            set
            {
                if (_renderGrid != value)
                {
                    _renderGrid = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderInfluences property.
        /// </summary>
        internal bool _renderInfluences;

        /// <summary>
        /// Gets or sets a value indicating whether the influences should be rendered.
        /// </summary>
        public bool RenderInfluences
        {
            get { return _renderInfluences; }
            set
            {
                if (_renderInfluences != value)
                {
                    _renderInfluences = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderLabel property.
        /// </summary>
        internal bool _renderLabel;

        /// <summary>
        /// Gets or sets a value indicating whether the labels of objects should be rendered.
        /// </summary>
        public bool RenderLabel
        {
            get { return _renderLabel; }
            set
            {
                if (_renderLabel != value)
                {
                    _renderLabel = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderIcon property.
        /// </summary>
        internal bool _renderIcon;

        /// <summary>
        /// Gets or sets a value indicating whether the icons of objects should be rendered.
        /// </summary>
        public bool RenderIcon
        {
            get { return _renderIcon; }
            set
            {
                if (_renderIcon != value)
                {
                    _renderIcon = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderTrueInfluenceRange property.
        /// </summary>
        internal bool _renderTrueInfluenceRange;

        /// <summary>
        /// Gets or sets a value indicating whether the influence range should be calculated from roads present in the grid.
        /// </summary>
        public bool RenderTrueInfluenceRange
        {
            get { return _renderTrueInfluenceRange; }
            set
            {
                if (_renderTrueInfluenceRange != value)
                {
                    _renderTrueInfluenceRange = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderHarborBlockedArea property.
        /// </summary>
        internal bool _renderHarborBlockedArea;

        /// <summary>
        /// Gets or sets value indication whether the blocked harbor aread should be rendered.
        /// </summary>
        public bool RenderHarborBlockedArea
        {
            get { return _renderHarborBlockedArea; }
            set
            {
                if (_renderHarborBlockedArea != value)
                {
                    _renderHarborBlockedArea = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the RenderPanorama property.
        /// </summary>
        internal bool _renderPanorama;

        /// <summary>
        /// Gets or sets a value indicating whether the skyscraper panorama should be visible.
        /// </summary>
        public bool RenderPanorama
        {
            get { return _renderPanorama; }
            set
            {
                if (_renderPanorama != value)
                {
                    _renderPanorama = value;
                    _isRenderingForced = true;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Backing field of the CurrentObject property
        /// </summary>
        internal List<LayoutObject> _currentObjects = [];

        /// <summary>
        /// Current object to be placed. Fires an event when changed.
        /// </summary>
        public List<LayoutObject> CurrentObjects
        {
            get { return _currentObjects; }
            internal set
            {
                if (_currentObjects != value)
                {
                    _currentObjects = value;
                    if (value.Count != 0)
                    {
                        OnCurrentObjectChanged?.Invoke(value[0]);
                    }
                }
            }
        }

        /// <summary>
        /// List of all currently placed objects.
        /// </summary>
        public QuadTree<LayoutObject> PlacedObjects { get; set; }

        /// <summary>
        /// List of all currently selected objects.
        /// All of them must also be contained in the _placedObjects list.
        /// </summary>
        public HashSet<LayoutObject> SelectedObjects { get; set; }

        /// <summary>
        /// Event which is fired when the current object is changed
        /// </summary>
        public event Action<LayoutObject> OnCurrentObjectChanged;

        /// <summary>
        /// Backing field of the StatusMessage property.
        /// </summary>
        internal string _statusMessage;

        /// <summary>
        /// Current status message.
        /// </summary>
        public string StatusMessage
        {
            get { return _statusMessage; }
            internal set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnStatusMessageChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Event which is fired when the status message has been changed.
        /// </summary>
        public event Action<string> OnStatusMessageChanged;

        /// <summary>
        /// Backing field of the LoadedFile property.
        /// </summary>
        internal string _loadedFile;

        /// <summary>
        /// Last loaded file, i.e. the currently active file. Fire an event when changed.
        /// </summary>
        public string LoadedFile
        {
            get { return _loadedFile; }
            set
            {
                if (_loadedFile != value)
                {
                    _loadedFile = value;
                    OnLoadedFileChanged?.Invoke(this, new FileLoadedEventArgs(value));
                }
            }
        }

        #endregion

        #region internals and constructor






        #region internal members
        internal const int DPI_FACTOR = 1;

        internal ILayoutLoader _layoutLoader;
        internal ILayoutFileService _layoutFileService;
        internal ICoordinateHelper _coordinateHelper;
        internal readonly IInputInteractionService _inputInteractionService = new InputInteractionService();
        internal IAppSettings _appSettings;
        internal IBrushCache _brushCache;
        internal IPenCache _penCache;
        internal IMessageBoxService _messageBoxService;
        internal ILocalizationHelper _localizationHelper;

        private TransformService _transformService;
        private LayoutModelService _layoutModelService;
        private SelectionService _selectionService;

        internal const string IDENTIFIER_SKYSCRAPER = "A7_residence_SkyScraper_";
        internal readonly Regex _regex_panorama = SkyScraperRegex();//RegexOptions.IgnoreCase -> slow in < .NET 5 (triggers several calls to ToLower)



        /// <summary>
        /// Backing field of the CurrentMode property.
        /// </summary>
        internal MouseMode _currentMode;

        /// <summary>
        /// Indicates the current mouse mode.
        /// </summary>
        internal MouseMode CurrentMode
        {
            get { return _currentMode; }
            set
            {
                _currentMode = value;
                StatusMessage = "Mode: " + _currentMode;
            }
        }

        /// <summary>
        /// Indicates whether the mouse is within this control.
        /// </summary>
        internal bool _mouseWithinControl;

        /// <summary>
        /// The current mouse position.
        /// </summary>
        internal Point _mousePosition = new(double.NaN, double.NaN);

        /// <summary>
        /// The position where the mouse button was pressed.
        /// </summary>
        internal Point _mouseDragStart;

        /// <summary>
        /// The rectangle used for selection.
        /// </summary>
        internal Rect _selectionRect;

        /// <summary>
        /// A list of object position <see cref="Rect"/>s. Used when dragging selected objects (when MouseMode is <see cref="MouseMode.DragSelection"/>).
        /// Holds a Rect that represents the object's previous position prior to dragging.
        /// </summary>
        internal List<(LayoutObject Item, Rect OldGridRect)> _oldObjectPositions;

        /// <summary>
        /// The collision rect derived from the current selection.
        /// </summary>
        internal Rect _collisionRect;

        /// <summary>
        /// Calculation helper used when computing the <see cref="_collisionRect"/>.
        /// </summary>
        internal StatisticsCalculationHelper _statisticsCalculationHelper;

        /// <summary>
        /// The current viewport.
        /// </summary>
        internal Viewport _viewport;

        /// <summary>
        /// A transform used to translate items within the viewport.
        /// </summary>
        internal TranslateTransform _viewportTransform;

        /// <summary>
        /// A guideline set used for pixel-aligned drawing.
        /// </summary>
        internal GuidelineSet _guidelineSet;

        /// <summary>
        /// A flag representing if <see cref="ScrollViewer.InvalidateScrollInfo"/> needs to be called on the next render.
        /// </summary>
        internal bool _invalidateScrollInfo;

        /// <summary>
        /// A Rect representing the true space the current layout takes up.
        /// </summary>
        internal Rect _layoutBounds;

        /// <summary>
        /// A Rect representing the scrollable area of the canvas.
        /// </summary>
        internal Rect _scrollableBounds;

        /// <summary>
        /// A Size representing the area the AnnoCanvas control is currently allowed to take up.
        /// </summary>
        internal Size _oldArrangeBounds;

        /// <summary>
        /// The typeface used when rendering text on the canvas.
        /// </summary>
        internal readonly Typeface TYPEFACE = new("Verdana");

        /// <summary>
        /// Does currently selected objects contain object which is not ignored from rendering?
        /// </summary>
        internal bool selectionContainsNotIgnoredObject;

        #endregion

        #region Pens and Brushes

        /// <summary>
        /// Used for object borders.
        /// </summary>
        internal Pen _linePen;

        /// <summary>
        /// Used for grid lines.
        /// </summary>
        internal Pen _gridLinePen;

        public double LinePenThickness
        {
            get { return _linePen.Thickness; }
        }

        /// <summary>
        /// Used for selection and hover highlights and selection rect.
        /// </summary>
        internal Pen _highlightPen;

        /// <summary>
        /// Used for the radius circle.
        /// </summary>
        internal Pen _radiusPen;

        /// <summary>
        /// Used to highlight objects within influence.
        /// </summary>
        internal Pen _influencedPen;

        /// <summary>
        /// Used to fill the selection rect and influence circle.
        /// </summary>
        internal Brush _lightBrush;

        /// <summary>
        /// Used to fill objects within influence.
        /// </summary>
        internal Brush _influencedBrush;

        #endregion

        #region Debug options

        /// <summary>
        /// Brush used for filling and drawing debug-related information.
        /// </summary>
        internal SolidColorBrush _debugBrushDark;
        /// <summary>
        /// Brush used for filling and drawing debug-related information.
        /// </summary>
        internal SolidColorBrush _debugBrushLight;

        internal bool _debugModeIsEnabled = false;
        internal readonly bool _debugShowObjectPositions = true;
        internal readonly bool _debugShowQuadTreeViz = true;
        internal readonly bool _debugShowSelectionRectCoordinates = true;
        internal readonly bool _debugShowSelectionCollisionRect = true;
        internal readonly bool _debugShowViewportRectCoordinates = true;
        internal readonly bool _debugShowScrollableRectCoordinates = true;
        internal readonly bool _debugShowLayoutRectCoordinates = true;
        internal readonly bool _debugShowMouseGridCoordinates = true;
        internal readonly bool _debugShowObjectCount = true;
        #endregion
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public AnnoCanvas2() : this(null, null)
        {
        }

        public AnnoCanvas2(BuildingPresets presetsToUse,
            Dictionary<string, IconImage> iconsToUse,
            IAppSettings appSettingsToUse = null,
            ICoordinateHelper coordinateHelperToUse = null,
            IBrushCache brushCacheToUse = null,
            IPenCache penCacheToUse = null,
            IMessageBoxService messageBoxServiceToUse = null,
            ILocalizationHelper localizationHelperToUse = null,
            IUndoManager undoManager = null,
            Func<IUndoManager, ILayoutFileService> layoutFileServiceFactoryToUse = null,
            IClipboardService clipboardService = null)
        {
            InitializeComponentAndRenderer();
            InitializeServices(appSettingsToUse, coordinateHelperToUse, brushCacheToUse, penCacheToUse, messageBoxServiceToUse, localizationHelperToUse, undoManager, layoutFileServiceFactoryToUse, clipboardService);
            InitializeLayoutServices();
            InitializeModeAndObjects();
            SetupHotkeys();
            LoadColorsAndPens();
            LoadPresetsAndIcons(presetsToUse, iconsToUse);
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
        }

        private void InitializeComponentAndRenderer()
        {
            InitializeComponent();
            _canvasRenderer = new CanvasRenderer(this);
        }

        private void InitializeServices(IAppSettings appSettingsToUse,
            ICoordinateHelper coordinateHelperToUse,
            IBrushCache brushCacheToUse,
            IPenCache penCacheToUse,
            IMessageBoxService messageBoxServiceToUse,
            ILocalizationHelper localizationHelperToUse,
            IUndoManager undoManager,
            Func<IUndoManager, ILayoutFileService> layoutFileServiceFactoryToUse,
            IClipboardService clipboardService)
        {
            _appSettings = appSettingsToUse ?? AppSettings.Instance;
            _appSettings.SettingsChanged += AppSettings_SettingsChanged;
            _coordinateHelper = coordinateHelperToUse ?? new CoordinateHelper();
            _brushCache = brushCacheToUse ?? new BrushCache();
            _penCache = penCacheToUse ?? new PenCache();
            _messageBoxService = messageBoxServiceToUse ?? new MessageBoxService();
            _localizationHelper = localizationHelperToUse ?? Localization.Localization.Instance;
            _layoutLoader = new LayoutLoader();
            UndoManager = undoManager ?? new UndoManager();
            IClipboard clipboard = new WindowsClipboard();
            ClipboardService = clipboardService ?? new ClipboardService(_layoutLoader, clipboard);

            // prefer factory passed in explicitly, otherwise try resolving one from the app's service provider
            var factory = layoutFileServiceFactoryToUse;
            if (factory == null && App.Current?.Services != null)
            {
                factory = App.Current.Services.GetService<Func<IUndoManager, ILayoutFileService>>();
            }

            _layoutFileService = factory?.Invoke(UndoManager) ?? new LayoutFileService(UndoManager, _messageBoxService, _localizationHelper);

            _showScrollBars = _appSettings.ShowScrollbars;
            _hideInfluenceOnSelection = _appSettings.HideInfluenceOnSelection;

            UpdateScrollBarVisibility();
        }

        private void InitializeLayoutServices()
        {
            _statisticsCalculationHelper = new StatisticsCalculationHelper();
            _layoutModelService = new LayoutModelService(_coordinateHelper, _statisticsCalculationHelper);
            _selectionService = new SelectionService(_layoutModelService);
            _transformService = new TransformService(_coordinateHelper);
        }

        private void InitializeModeAndObjects()
        {
            var sw = new Stopwatch();
            sw.Start();

            //initialize
            CurrentMode = MouseMode.Standard;
            PlacedObjects = _layoutModelService.PlacedObjects;
            SelectedObjects = _selectionService.SelectedObjects;
            _oldObjectPositions = [];
            _viewport = new Viewport();
            _viewportTransform = new TranslateTransform(0d, 0d);

            sw.Stop();
            logger.Trace($"init variables took: {sw.ElapsedMilliseconds}ms");
        }

        private void SetupHotkeys()
        {
            //Commands
            RotateCommand = new RelayCommand(ExecuteRotate);
            rotateAllCommand = new RelayCommand(ExecuteRotateAll);
            copyCommand = new RelayCommand(ExecuteCopy);
            pasteCommand = new RelayCommand(ExecutePaste);
            deleteCommand = new RelayCommand(ExecuteDelete);
            duplicateCommand = new RelayCommand(ExecuteDuplicate);
            deleteObjectUnderCursorCommand = new RelayCommand(ExecuteDeleteObjectUnderCursor);
            undoCommand = new RelayCommand(ExecuteUndo);
            redoCommand = new RelayCommand(ExecuteRedo);
            enableDebugModeCommand = new RelayCommand(ExecuteEnableDebugMode);
            selectAllSameIdentifierCommand = new RelayCommand(ExecuteSelectAllSameIdentifier);

            //Set up default keybindings

            //for rotation with the r key.
            var rotateBinding1 = new InputBinding(RotateCommand, new PolyGesture(Key.R, ModifierKeys.None));
            rotateHotkey1 = new Hotkey("Rotate_1", rotateBinding1, ROTATE_LOCALIZATION_KEY);

            //for rotation with middle click
            var rotateBinding2 = new InputBinding(RotateCommand, new PolyGesture(ExtendedMouseAction.MiddleClick));
            rotateHotkey2 = new Hotkey("Rotate_2", rotateBinding2, ROTATE_LOCALIZATION_KEY);

            var rotateAllBinding = new InputBinding(rotateAllCommand, new PolyGesture(Key.R, ModifierKeys.Shift));
            rotateAllHotkey = new Hotkey(ROTATE_ALL_LOCALIZATION_KEY, rotateAllBinding, ROTATE_ALL_LOCALIZATION_KEY);

            var copyBinding = new InputBinding(copyCommand, new PolyGesture(Key.C, ModifierKeys.Control));
            copyHotkey = new Hotkey(COPY_LOCALIZATION_KEY, copyBinding, COPY_LOCALIZATION_KEY);

            var pasteBinding = new InputBinding(pasteCommand, new PolyGesture(Key.V, ModifierKeys.Control));
            pasteHotkey = new Hotkey(PASTE_LOCALIZATION_KEY, pasteBinding, PASTE_LOCALIZATION_KEY);

            var deleteBinding = new InputBinding(deleteCommand, new PolyGesture(Key.Delete, ModifierKeys.None));
            deleteHotkey = new Hotkey(DELETE_LOCALIZATION_KEY, deleteBinding, DELETE_LOCALIZATION_KEY);

            var duplicateBinding = new InputBinding(duplicateCommand, new PolyGesture(ExtendedMouseAction.LeftDoubleClick, ModifierKeys.None));
            duplicateHotkey = new Hotkey(DUPLICATE_LOCALIZATION_KEY, duplicateBinding, DUPLICATE_LOCALIZATION_KEY);

            var deleteHoveredOjectBinding = new InputBinding(deleteObjectUnderCursorCommand, new PolyGesture(ExtendedMouseAction.RightClick, ModifierKeys.None));
            deleteObjectUnderCursorHotkey = new Hotkey(DELETE_OBJECT_UNDER_CURSOR_LOCALIZATION_KEY, deleteHoveredOjectBinding, DELETE_OBJECT_UNDER_CURSOR_LOCALIZATION_KEY);

            var undoBinding = new InputBinding(undoCommand, new PolyGesture(Key.Z, ModifierKeys.Control));
            undoHotkey = new Hotkey(UNDO_LOCALIZATION_KEY, undoBinding, UNDO_LOCALIZATION_KEY);

            var redoBinding = new InputBinding(redoCommand, new PolyGesture(Key.Y, ModifierKeys.Control));
            redoHotkey = new Hotkey(REDO_LOCALIZATION_KEY, redoBinding, REDO_LOCALIZATION_KEY);

            var enableDebugModeBinding = new InputBinding(enableDebugModeCommand, new PolyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift));
            enableDebugModeHotkey = new Hotkey(ENABLE_DEBUG_MODE_LOCALIZATION_KEY, enableDebugModeBinding, ENABLE_DEBUG_MODE_LOCALIZATION_KEY);

            var selectAllSameIdentifierBinding = new InputBinding(selectAllSameIdentifierCommand, new PolyGesture(ExtendedMouseAction.LeftClick, ModifierKeys.Control | ModifierKeys.Shift));
            selectAllSameIdentifierHotkey = new Hotkey(SELECT_ALL_SAME_IDENTIFIER_LOCALIZATION_KEY, selectAllSameIdentifierBinding, SELECT_ALL_SAME_IDENTIFIER_LOCALIZATION_KEY);

            //We specifically do not add the `InputBinding`s to the `InputBindingCollection` of `AnnoCanvas`, as if we did that,
            //`InputBinding.Gesture.Matches()` would be fired for *every* event - MouseWheel, MouseDown, KeyUp, KeyDown, MouseMove etc
            //which we don't want, as it produces a noticeable performance impact.
        }

        private void LoadColorsAndPens()
        {
            LoadGridLineColor();
            LoadObjectBorderLineColor();

            _highlightPen = _penCache.GetPen(Brushes.Yellow, DPI_FACTOR * 2);
            _radiusPen = _penCache.GetPen(Brushes.Black, DPI_FACTOR * 2);
            _influencedPen = _penCache.GetPen(Brushes.LawnGreen, DPI_FACTOR * 2);

            var color = Colors.LightYellow;
            color.A = 32;
            _lightBrush = _brushCache.GetSolidBrush(color);
            color = Colors.LawnGreen;
            color.A = 32;
            _influencedBrush = _brushCache.GetSolidBrush(color);
            _debugBrushLight = Brushes.Blue;
            _debugBrushDark = Brushes.DarkBlue;
        }

        private void LoadPresetsAndIcons(BuildingPresets presetsToUse, Dictionary<string, IconImage> iconsToUse)
        {
            // load presets and icons if not in design time
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                var sw = new Stopwatch();
                sw.Start();
                // load presets
                try
                {
                    if (presetsToUse == null)
                    {
                        var loader = new BuildingPresetsLoader();
                        BuildingPresets = loader.Load(Path.Combine(App.ApplicationPath, CoreConstants.PresetsFiles.BuildingPresetsFile));
                    }
                    else
                    {
                        BuildingPresets = presetsToUse;
                    }
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowError(ex.Message,
                          _localizationHelper.GetLocalization("LoadingPresetsFailed"));
                }

                sw.Stop();
                logger.Trace($"loading presets took: {sw.ElapsedMilliseconds}ms");

                if (iconsToUse == null)
                {
                    sw.Start();

                    // load icon name mapping
                    IconMappingPresets iconNameMapping = null;
                    try
                    {
                        var loader = new IconMappingPresetsLoader();
                        iconNameMapping = loader.LoadFromFile(Path.Combine(App.ApplicationPath, CoreConstants.PresetsFiles.IconNameFile));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Loading of the icon names failed.");

                        _messageBoxService.ShowError(_localizationHelper.GetLocalization("LoadingIconNamesFailed"),
                            _localizationHelper.GetLocalization("Error"));
                    }

                    sw.Stop();
                    logger.Trace($"loading icon mapping took: {sw.ElapsedMilliseconds}ms");

                    sw.Start();

                    // load icons
                    var iconLoader = new IconLoader();
                    Icons = iconLoader.Load(Path.Combine(App.ApplicationPath, Constants.IconFolder), iconNameMapping);

                    sw.Stop();
                    logger.Trace($"loading icons took: {sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    Icons = iconsToUse;
                }
            }
        }

        #endregion

        internal bool _showScrollBars;
        internal bool _hideInfluenceOnSelection;

        internal void AppSettings_SettingsChanged(object sender, EventArgs e)
        {
            LoadGridLineColor();
            LoadObjectBorderLineColor();
            _needsRefreshAfterSettingsChanged = true;

            _showScrollBars = _appSettings.ShowScrollbars;
            _hideInfluenceOnSelection = _appSettings.HideInfluenceOnSelection;

            UpdateScrollBarVisibility();
        }

        #endregion

        #region Rendering

        internal bool _isRenderingForced;
        internal bool _needsRefreshAfterSettingsChanged;
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            //force scroll bars to update when we resize the window
            if (_oldArrangeBounds != arrangeBounds)
            {
                _oldArrangeBounds = arrangeBounds;
                InvalidateScroll();
            }
            return base.ArrangeOverride(arrangeBounds);
        }


        internal ICanvasRenderer _canvasRenderer;

        /// <summary>
        /// Renders the whole scene including grid, placed objects, current object, selection highlights, influence radii and selection rectangle.
        /// </summary>
        /// <param name="drawingContext">context used for rendering</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Delegate rendering to the renderer service (first step toward extraction)
            _canvasRenderer.Render(drawingContext);
        }



        internal void RecalculateSelectionContainsNotIgnoredObject()
        {
            selectionContainsNotIgnoredObject = SelectedObjects.Any(x => !x.IsIgnoredObject());
        }

        /// <summary>
        /// Used to load current color for grid lines from settings.
        /// </summary>
        /// <remarks>As this method can be called when AppSettings are updated, we make sure to not call anything that relies on the UI thread from here.</remarks>
        internal void LoadGridLineColor()
        {
            var colorFromJson = SerializationHelper.LoadFromJsonString<UserDefinedColor>(_appSettings.ColorGridLines);//explicit variable to make debugging easier
            _gridLinePen = _penCache.GetPen(_brushCache.GetSolidBrush(colorFromJson.Color), DPI_FACTOR * 1);
            var halfPenWidth = _gridLinePen.Thickness / 2;
            var guidelines = new GuidelineSet();
            guidelines.GuidelinesX.Add(halfPenWidth);
            guidelines.GuidelinesY.Add(halfPenWidth);
            guidelines.Freeze();
            _guidelineSet = guidelines;
        }

        /// <summary>
        /// Used to load current color for object border lines from settings.
        /// </summary>
        /// <remarks>As this method can be called when AppSettings are updated, we make sure to not call anything that relies on the UI thread from here.</remarks>
        internal void LoadObjectBorderLineColor()
        {
            var colorFromJson = SerializationHelper.LoadFromJsonString<UserDefinedColor>(_appSettings.ColorObjectBorderLines);//explicit variable to make debugging easier
            _linePen = _penCache.GetPen(_brushCache.GetSolidBrush(colorFromJson.Color), DPI_FACTOR * 1);
        }

        /// <summary>
        /// Reindexes given objects in the <see cref="PlacedObjects"/>. This is potentially a very expensive operation.
        /// Calling this method when the LayoutObjects in <see name="newPositions"/> and <see name="oldPositions"/> do not
        /// match can cause object duplication.
        /// </summary>
        /// <remarks>
        /// When the parameter types were IEnumerable, sequences passed in sometimes got GC'd between calls when using MouseMode.DragAll, 
        /// as the objects were not referenced anywhere between the end of the foreach loop and the AddRange call (the variables
        /// themselves did not count as references due to IEnumerable lazy evaluation).
        /// By making sure the parameters are lists, we avoid this issues.
        /// </remarks>
        internal void ReindexMovedObjects()
        {
            foreach (var (item, oldBounds) in _oldObjectPositions)
            {
                PlacedObjects.ReIndex(item, oldBounds);
            }
            _oldObjectPositions.Clear();
        }

        /// <summary>
        /// Forces an update to the parent <see cref="ScrollViewer"/> on the next render (if necessary), and
        /// recomputes <see cref="_scrollableBounds"/>, which represents the currently scrollable area.
        /// This computation relies on <see cref="_layoutBounds"/> being up to date.
        /// </summary>
        internal void InvalidateScroll()
        {
            //make sure the scrollable area encompasses the current viewport plus the bounding rect of the current layout
            var r = _viewport.Absolute;
            r.Union(_layoutBounds);
            _scrollableBounds = r;

            //update scroll viewer on next render
            _invalidateScrollInfo = true;
            _isRenderingForced = true;
        }

        /// <summary>
        /// Computes the bounds of the current layout
        /// </summary>
        internal void InvalidateBounds()
        {
            _layoutBounds = ComputeBoundingRect(PlacedObjects);

        }
        #endregion


        #region Event handling

        #region Mouse

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            _mouseWithinControl = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _mouseWithinControl = false;

            //clear selection rectangle
            CurrentMode = MouseMode.Standard;
            _selectionRect = Rect.Empty;

            //update object positions if dragging
            ReindexMovedObjects();

            InvalidateVisual();
        }

        /// <summary>
        /// Handles the zoom level
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Delegate most of the zoom computation to the InputInteractionService so this control stays small.
            var mousePosition = e.GetPosition(this);
            var result = _inputInteractionService.HandleMouseWheel(e.Delta, mousePosition, GridSize, _appSettings.UseZoomToPoint, _appSettings.ZoomSensitivityPercentage, _viewport, PlacedObjects.Count == 0, _coordinateHelper);

            GridSize = result.NewGridSize;
            _viewport.Left = result.NewViewportLeft;
            _viewport.Top = result.NewViewportTop;

            InvalidateScroll();
        }

        internal void HandleMouse(MouseEventArgs e)
        {
            _ = Focus();
            // refresh retrieved mouse position
            _mousePosition = e.GetPosition(this);
            _canvasRenderer.MoveCurrentObjectsToMouse();
        }

        /// <summary>
        /// Handles pressing of mouse buttons
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!IsFocused)
            {
                _ = Focus();
            }

            HandleMouse(e);
            HotkeyCommandManager.HandleCommand(e);
            _mouseDragStart = _mousePosition;

            // Let the InputInteractionService make a high-level decision; the control will apply the actual changes
            var decision = _inputInteractionService.DecideOnMouseDown(
                leftPressed: e.LeftButton == MouseButtonState.Pressed,
                rightPressed: e.RightButton == MouseButtonState.Pressed,
                mousePosition: _mousePosition,
                currentModeWasDragSelection: CurrentMode == MouseMode.DragSelection,
                currentObjectsCount: CurrentObjects.Count,
                getObjectAt: GetObjectAt,
                selectedContains: SelectedObjects.Contains,
                isControlPressed: IsControlPressed(),
                isShiftPressed: IsShiftPressed());

            // Apply the action suggested by the decision
            switch (decision.Action)
            {
                case MouseDownAction.DragAllStartAndRegisterMove:
                    if (CurrentMode == MouseMode.DragSelection)
                    {
                        UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
                        {
                            ObjectPropertyValues = _oldObjectPositions.Select(pair => (pair.Item, pair.OldGridRect, pair.Item.Bounds)).ToList(),
                            QuadTree = PlacedObjects
                        });

                        ReindexMovedObjects();
                    }

                    CurrentMode = MouseMode.DragAllStart;
                    break;
                case MouseDownAction.PlaceCurrentObjects:
                    _ = TryPlaceCurrentObjects(isContinuousDrawing: false);
                    break;
                case MouseDownAction.SelectionRectStart:
                    CurrentMode = MouseMode.SelectionRectStart;
                    _unselectedObjects = null;
                    break;
                case MouseDownAction.DragSelectionStart:
                    CurrentMode = MouseMode.DragSelectionStart;
                    _unselectedObjects = null;
                    break;
                case MouseDownAction.DragSingleStart:
                    CurrentMode = MouseMode.DragSingleStart;
                    _unselectedObjects = null;
                    break;
                default:
                    break;
            }

            InvalidateVisual();
        }

        internal List<LayoutObject> _unselectedObjects = null;

        /// <summary>
        /// Here be dragons.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            HandleMouse(e);

            // check if user begins to drag
            if (Math.Abs(_mouseDragStart.X - _mousePosition.X) >= 1 || Math.Abs(_mouseDragStart.Y - _mousePosition.Y) >= 1)
            {
                switch (CurrentMode)
                {
                    case MouseMode.SelectionRectStart:
                        CurrentMode = MouseMode.SelectionRect;
                        _selectionRect = new Rect();
                        break;
                    case MouseMode.DragSelectionStart:
                        CurrentMode = MouseMode.DragSelection;
                        break;
                    case MouseMode.DragSingleStart:
                        SelectedObjects.Clear();
                        var obj = GetObjectAt(_mouseDragStart);
                        _selectionService.AddSelectedObject(obj, ShouldAffectObjectsWithIdentifier());
                        RecalculateSelectionContainsNotIgnoredObject();
                        //after adding the object, compute the collision rect
                        _collisionRect = obj.GridRect;
                        CurrentMode = MouseMode.DragSelection;
                        break;
                    case MouseMode.DragAllStart:
                        CurrentMode = MouseMode.DragAll;
                        break;
                }
            }

            if (CurrentMode == MouseMode.DragAll)
            {
                // move all selected objects
                var dx = (int)_coordinateHelper.ScreenToGrid(_mousePosition.X - _mouseDragStart.X, GridSize);
                var dy = (int)_coordinateHelper.ScreenToGrid(_mousePosition.Y - _mouseDragStart.Y, GridSize);

                //shift the viewport;
                if (_appSettings.InvertPanningDirection)
                {
                    _viewport.Left -= dx;
                    _viewport.Top -= dy;
                }
                else
                {
                    _viewport.Left += dx;
                    _viewport.Top += dy;
                }

                // adjust the drag start to compensate the amount we already moved
                _mouseDragStart.X += _coordinateHelper.GridToScreen(dx, GridSize);
                _mouseDragStart.Y += _coordinateHelper.GridToScreen(dy, GridSize);

                //invalidate scroll info on next render;
                InvalidateScroll();
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (CurrentObjects.Count != 0)
                {
                    CurrentMode = MouseMode.PlaceObjects;
                    // place new object
                    _ = TryPlaceCurrentObjects(isContinuousDrawing: true);
                }
                else
                {
                    // selection of multiple objects
                    switch (CurrentMode)
                    {
                        case MouseMode.SelectionRect:
                            {
                                if (IsControlPressed() || IsShiftPressed())
                                {
                                    // remove previously selected by the selection rect
                                    if (ShouldAffectObjectsWithIdentifier())
                                    {
                                        _selectionService.RemoveSelectedObjects(
                                            [.. SelectedObjects.Where(_ => _.CalculateScreenRect(GridSize).IntersectsWith(_selectionRect))],
                                            true
                                        );
                                    }
                                    else
                                    {
                                        _selectionService.RemoveSelectedObjects(x => x.CalculateScreenRect(GridSize).IntersectsWith(_selectionRect));
                                    }
                                }
                                else
                                {
                                    SelectedObjects.Clear();
                                }

                                // adjust rect
                                _selectionRect = new Rect(_mouseDragStart, _mousePosition);
                                // select intersecting objects
                                var selectionRectGrid = _coordinateHelper.ScreenToGrid(_selectionRect, GridSize);
                                selectionRectGrid = _viewport.OriginToViewport(selectionRectGrid);
                                _selectionService.AddSelectedObjects(PlacedObjects.GetItemsIntersecting(selectionRectGrid),
                                                   ShouldAffectObjectsWithIdentifier());
                                RecalculateSelectionContainsNotIgnoredObject();

                                StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
                                break;
                            }
                        case MouseMode.DragSelection:
                            {
                                _inputInteractionService.HandleDragSelection(
                                    _mousePosition,
                                    ref _mouseDragStart,
                                    GridSize,
                                    ref _oldObjectPositions,
                                    ref _collisionRect,
                                    SelectedObjects,
                                    PlacedObjects,
                                    _coordinateHelper,
                                    out var invalidateScroll,
                                    out var statisticsUpdated,
                                    out var forceRendering);

                                if (statisticsUpdated)
                                {
                                    StatisticsUpdated?.Invoke(this, new UpdateStatisticsEventArgs(UpdateMode.NoBuildingList));
                                }

                                if (invalidateScroll)
                                {
                                    var oldLayoutBounds = _layoutBounds;
                                    InvalidateBounds();
                                    if (oldLayoutBounds != _layoutBounds)
                                    {
                                        InvalidateScroll();
                                    }
                                }

                                if (forceRendering)
                                {
                                    ForceRendering();
                                    return;
                                }

                                break;
                            }
                    }
                }
            }

            InvalidateVisual();
        }

        /// <summary>
        /// Handles the release of mouse buttons.
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            HandleMouse(e);

            if (CurrentMode == MouseMode.DragAll)
            {
                if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
                {
                    CurrentMode = MouseMode.Standard;
                }

                return;
            }

            if (e.ChangedButton == MouseButton.Left && CurrentObjects.Count == 0)
            {
                switch (CurrentMode)
                {
                    default:
                        {
                            // clear selection if no key is pressed
                            if (!(IsControlPressed() || IsShiftPressed()))
                            {
                                SelectedObjects.Clear();
                            }

                            var obj = GetObjectAt(_mousePosition);
                            if (obj != null)
                            {
                                // user clicked an object: select or deselect it
                                if (SelectedObjects.Contains(obj))
                                {
                                    _selectionService.RemoveSelectedObject(obj);
                                }
                                else
                                {
                                    _selectionService.AddSelectedObject(obj);
                                }
                                RecalculateSelectionContainsNotIgnoredObject();
                            }

                            _collisionRect = ComputeBoundingRect(SelectedObjects);
                            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
                            // return to standard mode, i.e. clear any drag-start modes
                            CurrentMode = MouseMode.Standard;
                            if (selectionContainsNotIgnoredObject)
                            {
                                _selectionService.RemoveSelectedObjects(Extensions.IEnumerableExtensions.IsIgnoredObject);
                            }
                            break;
                        }
                    case MouseMode.SelectSameIdentifier:
                        {
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                    case MouseMode.SelectionRect:
                        _collisionRect = ComputeBoundingRect(SelectedObjects);
                        // cancel dragging of selection rect
                        CurrentMode = MouseMode.Standard;
                        if (selectionContainsNotIgnoredObject)
                        {
                            _selectionService.RemoveSelectedObjects(Extensions.IEnumerableExtensions.IsIgnoredObject);
                        }
                        break;
                    case MouseMode.DragSelection:
                        _inputInteractionService.HandleMouseUpDragSelection(_oldObjectPositions, SelectedObjects, false, out var registerUndo, out var reindex, out var clearSelection);
                        if (registerUndo)
                        {
                            UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
                            {
                                ObjectPropertyValues = _oldObjectPositions.Select(pair => (pair.Item, pair.OldGridRect, pair.Item.Bounds)).ToList(),
                                QuadTree = PlacedObjects
                            });
                        }
                        if (reindex)
                        {
                            ReindexMovedObjects();
                        }
                        if (clearSelection)
                        {
                            SelectedObjects.Clear();
                        }
                        CurrentMode = MouseMode.Standard;
                        break;
                }
            }
            else if (e.ChangedButton == MouseButton.Left && CurrentObjects.Count != 0)
            {
                CurrentMode = MouseMode.PlaceObjects;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                switch (CurrentMode)
                {
                    case MouseMode.PlaceObjects:
                    case MouseMode.DeleteObject:
                    case MouseMode.Standard:
                        {
                            if (CurrentObjects.Count != 0)
                            {
                                // cancel placement of object
                                CurrentObjects.Clear();
                            }

                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                    case MouseMode.DragSelection:
                        {
                            _inputInteractionService.HandleMouseUpDragSelection(_oldObjectPositions, SelectedObjects, true, out var registerUndo, out var reindex, out var clearSelection);
                            if (registerUndo)
                            {
                                UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
                                {
                                    ObjectPropertyValues = _oldObjectPositions.Select(pair => (pair.Item, pair.OldGridRect, pair.Item.Bounds)).ToList(),
                                    QuadTree = PlacedObjects
                                });
                            }
                            if (reindex)
                            {
                                ReindexMovedObjects();
                            }
                            if (clearSelection)
                            {
                                SelectedObjects.Clear();
                            }
                            if (CurrentObjects.Count != 0)
                            {
                                // cancel placement of object
                                CurrentObjects.Clear();
                            }
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                    case MouseMode.SelectSameIdentifier:
                        {
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                switch (CurrentMode)
                {
                    case MouseMode.SelectSameIdentifier:
                        {
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                }
            }
            else if (e.ChangedButton == MouseButton.XButton1)
            {
                switch (CurrentMode)
                {
                    case MouseMode.SelectSameIdentifier:
                        {
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                }
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                switch (CurrentMode)
                {
                    case MouseMode.SelectSameIdentifier:
                        {
                            CurrentMode = MouseMode.Standard;
                            break;
                        }
                }
            }

            InvalidateVisual();
        }

        #endregion

        #region Keyboard

        /// <summary>
        /// Handles key presses
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            //Used here instead of adding to the InputBindingsCollection as we don't want run `Binding.Matches` on *every* event.
            //When an InputBinding is added to the InputBindingsCollection, the  `Matches` method is fired for every event - KeyUp,
            //KeyDown, MouseUp, MouseMove, MouseWheel etc.
            HotkeyCommandManager.HandleCommand(e);

            if (e.Handled)
            {
                InvalidateVisual();
            }

        }

        /// <summary>
        /// Checks whether the user is pressing the control key.
        /// </summary>
        /// <returns><see langword="true"/> if the control key is pressed, otherwise <see langword="false"/>.</returns>
        internal static bool IsControlPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        }

        /// <summary>
        /// Checks whether the user is pressing the shift key.
        /// </summary>
        /// <returns><see langword="true"/> if the shift key is pressed, otherwise <see langword="false"/>.</returns>
        internal static bool IsShiftPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        }

        /// <summary>
        /// Checks whether actions should affect all objects with the same identifier.
        /// </summary>
        /// <returns><see langword="true"/> if all objects with same identifier should be affected, otherwise <see langword="false"/>.</returns>
        internal static bool ShouldAffectObjectsWithIdentifier()
        {
            return IsShiftPressed() && IsControlPressed();
        }

        #endregion

        #endregion

        #region Collision handling

        /// <summary>
        /// Checks if there is a collision between given objects a and b.
        /// </summary>
        /// <param name="a">first object</param>
        /// <param name="b">second object</param>
        /// <returns>true if there is a collision, otherwise false</returns>
        internal static bool ObjectIntersectionExists(LayoutObject a, LayoutObject b)
        {
            return a.CollisionRect.IntersectsWith(b.CollisionRect);
        }

        /// <summary>
        /// Checks if there is a collision between a list of AnnoObjects a and object b.
        /// </summary>
        /// <param name="a">List of objects</param>
        /// <param name="b">second object</param>
        /// <returns>true if there is a collision, otherwise false</returns>
        internal static bool ObjectIntersectionExists(IEnumerable<LayoutObject> a, LayoutObject b)
        {
            return a.Any(_ => _.CollisionRect.IntersectsWith(b.CollisionRect));
        }

        /// <summary>
        /// Tries to place current objects on the grid.
        /// Fails if there are any collisions.
        /// </summary>
        /// <param name="isContinuousDrawing"><c>true</c> if drawing the same object(s) over and over</param>
        /// <returns>true if placement succeeded, otherwise false</returns>
        internal bool TryPlaceCurrentObjects(bool isContinuousDrawing)
        {
            if (CurrentObjects.Count == 0)
            {
                return true;
            }

            var forcePlacement = IsShiftPressed();
            var newObjects = _layoutModelService.GetObjectsToPlace(CurrentObjects, forcePlacement);

            if (newObjects.Count > 0)
            {
                UndoManager.RegisterOperation(new AddObjectsOperation<LayoutObject>()
                {
                    Objects = newObjects,
                    Collection = PlacedObjects
                });

                PlacedObjects.AddRange(newObjects);
                StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);

                //no need to update colors if drawing the same object(s)
                if (!isContinuousDrawing)
                {
                    ColorsInLayoutUpdated?.Invoke(this, EventArgs.Empty);
                }

                var boundingRect = ComputeBoundingRect(newObjects);
                if (!_layoutBounds.Contains(boundingRect))
                {
                    InvalidateBounds();
                }

                if (!_scrollableBounds.Contains(boundingRect))
                {
                    InvalidateScroll();
                }

                return true;
            }

            return false;
        }

        internal Size _intersectingRectSize = new(1, 1);

        /// <summary>
        /// Retrieves the object at the given position given in screen coordinates.
        /// </summary>
        /// <param name="position">position given in screen coordinates</param>
        /// <returns>object at the position, <see langword="null"/> if no object could be found</returns>
        internal LayoutObject GetObjectAt(Point position)
        {
            return _layoutModelService.GetObjectAt(position, GridSize, _viewport);
        }

        /// <summary>
        /// Computes a <see cref="Rect"/> that encompasses the given objects.
        /// </summary>
        /// <param name="objects">The collection of <see cref="LayoutObject"/> to compute the bounding <see cref="Rect"/> for.</param>
        /// <returns>The <see cref="Rect"/> that encompasses all <paramref name="objects"/>.</returns>
        public Rect ComputeBoundingRect(IEnumerable<LayoutObject> objects)
        {
            return _layoutModelService.ComputeBoundingRect(objects);
        }

        #endregion

        #region API

        /// <summary>
        /// Sets the current object, i.e. the object which the user can place.
        /// </summary>
        /// <param name="obj">object to apply</param>
        public void SetCurrentObject(LayoutObject obj)
        {
            obj.Position = _mousePosition;
            // note: setting of the backing field doesn't fire the changed event
            _currentObjects.Clear();
            _currentObjects.Add(obj);
            InvalidateVisual();
        }

        /// <summary>
        /// Resets the zoom to the default level.
        /// </summary>
        public void ResetZoom()
        {
            GridSize = Constants.GridStepDefault;
        }

        /// <summary>
        /// Normalizes the layout with border parameter set to zero.
        /// </summary>
        public void Normalize()
        {
            Normalize(0);
        }

        /// <summary>
        /// Normalizes the layout, i.e. moves all objects so that the top-most and left-most objects are exactly at the top and left coordinate zero if border is zero.
        /// Otherwise moves all objects further to the bottom-right by border in grid-units.
        /// </summary>
        /// <param name="border"></param>
        public void Normalize(int border)
        {
            if (PlacedObjects.Count == 0)
            {
                return;
            }

            var dx = PlacedObjects.Min(_ => _.Position.X) - border;
            var dy = PlacedObjects.Min(_ => _.Position.Y) - border;
            var diff = new Vector(dx, dy);

            if (diff.LengthSquared > 0)
            {
                UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
                {
                    ObjectPropertyValues = PlacedObjects.Select(obj => (obj, obj.Bounds, new Rect(obj.Position - diff, obj.Size))).ToList(),
                    QuadTree = PlacedObjects
                });

                // make a copy of a list to avoid altering collection during iteration
                var placedObjects = PlacedObjects.ToList();

                foreach (var item in placedObjects)
                {
                    PlacedObjects.Move(item, -diff);
                }

                InvalidateVisual();
                InvalidateBounds();
                InvalidateScroll();
            }
        }

        /// <summary>
        /// Resets viewport of the canvas to top left corner.
        /// </summary>
        public void ResetViewport()
        {
            _viewport.Top = 0;
            _viewport.Left = 0;
        }

        #endregion

        #region New/Save/Load/Export methods

        public async Task CheckUnsavedChangesBeforeCrash()
        {
            await _layoutFileService.CheckUnsavedChangesBeforeCrashAsync(
                () => LoadedFile,
                file =>
                {
                    LoadedFile = file;
                    SaveFileRequested?.Invoke(this, new SaveFileEventArgs(file));
                });
        }

        /// <summary>
        /// Checks for unsaved changes. Shows Yes/No/Cancel dialog to let user decide what to do.
        /// </summary>
        /// <returns>True if changes were saved or discarded. False if operation should be cancelled.</returns>
        public async Task<bool> CheckUnsavedChanges()
        {
            return await _layoutFileService.CheckUnsavedChangesAsync(
                () => LoadedFile,
                file =>
                {
                    LoadedFile = file;
                    SaveFileRequested?.Invoke(this, new SaveFileEventArgs(file));
                    // Fire OnLoadedFileChanged event to trigger recalculation of MainWindowTitle
                    OnLoadedFileChanged?.Invoke(this, new FileLoadedEventArgs(file));
                });
        }

        /// <summary>
        /// Removes all objects from the grid.
        /// </summary>
        public async Task NewFile()
        {
            if (!await CheckUnsavedChanges())
            {
                return;
            }

            ResetViewport();
            PlacedObjects.Clear();
            SelectedObjects.Clear();
            UndoManager.Clear();
            LoadedFile = "";
            InvalidateBounds();
            InvalidateScroll();
            InvalidateVisual();

            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
            ColorsInLayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Saves the current layout to file.
        /// </summary>
        public bool Save()
        {
            if (string.IsNullOrEmpty(LoadedFile))
            {
                return SaveAs();
            }
            else
            {
                SaveFileRequested?.Invoke(this, new SaveFileEventArgs(LoadedFile));
                // Fire OnLoadedFileChanged event to trigger recalculation of MainWindowTitle
                OnLoadedFileChanged?.Invoke(this, new FileLoadedEventArgs(LoadedFile));
                return true;
            }
        }

        /// <summary>
        /// Opens a dialog and saves the current layout to file.
        /// </summary>
        public bool SaveAs()
        {
            var file = _layoutFileService.SaveAsAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(file))
            {
                LoadedFile = file;
                SaveFileRequested?.Invoke(this, new SaveFileEventArgs(LoadedFile));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens a dialog and loads the given file.
        /// </summary>
        public async Task OpenFile()
        {
            // Let the layout file service handle unsaved-changes prompt. If user elects to save, the service will call the
            // provided onSavedFile callback which will update LoadedFile and raise save events.
            var selectedFile = await _layoutFileService.OpenFileAsync(
                () => LoadedFile,
                file => { LoadedFile = file; });

            if (!string.IsNullOrEmpty(selectedFile))
            {
                OpenFileRequested?.Invoke(this, new OpenFileEventArgs(selectedFile));
                InvalidateBounds();
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Holds event handlers for command executions.
        /// </summary>
        internal static readonly Dictionary<ICommand, Action<AnnoCanvas>> CommandExecuteMappings;

        public HotkeyCommandManager HotkeyCommandManager { get; set; }
        /// <summary>
        /// Creates event handlers for command executions and registers them at the CommandManager.
        /// </summary>
        static AnnoCanvas2()
        {
            // create event handler mapping
            CommandExecuteMappings = new Dictionary<ICommand, Action<AnnoCanvas>>
            {
                { ApplicationCommands.New, async _ => await _.NewFile() },
                { ApplicationCommands.Open, async _ => await _.OpenFile() },
                { ApplicationCommands.Save, _ => _.Save() },
                { ApplicationCommands.SaveAs, _ => _.SaveAs() }
            };

            // register event handlers for the specified commands
            foreach (var action in CommandExecuteMappings)
            {
                CommandManager.RegisterClassCommandBinding(typeof(AnnoCanvas), new CommandBinding(action.Key, ExecuteCommand));
            }
        }


        /// <summary>
        /// Registers hotkeys with the <see cref="HotkeyCommandManager"/>.
        /// </summary>
        /// <param name="manager"></param>
        public void RegisterHotkeys(HotkeyCommandManager manager)
        {
            HotkeyCommandManager = manager;
            manager.AddHotkey(rotateHotkey1);
            manager.AddHotkey(rotateHotkey2);
            manager.AddHotkey(rotateAllHotkey);
            manager.AddHotkey(copyHotkey);
            manager.AddHotkey(pasteHotkey);
            manager.AddHotkey(deleteHotkey);
            manager.AddHotkey(duplicateHotkey);
            manager.AddHotkey(deleteObjectUnderCursorHotkey);
            manager.AddHotkey(undoHotkey);
            manager.AddHotkey(redoHotkey);
            manager.AddHotkey(enableDebugModeHotkey);
            manager.AddHotkey(selectAllSameIdentifierHotkey);
        }


        /// <summary>
        /// Handler for all executed command events.
        ///  </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void ExecuteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is AnnoCanvas canvas && CommandExecuteMappings.TryGetValue(e.Command, out var value))
            {
                value.Invoke(canvas);
                e.Handled = true;
            }
        }

        /// <summary>
        /// R key rotate
        /// </summary>
        internal Hotkey rotateHotkey1;
        /// <summary>
        /// MiddleClick rotate
        /// </summary>
        internal Hotkey rotateHotkey2;
        public ICommand RotateCommand { get; internal set; }
        internal void ExecuteRotate(object param)
        {
            if (CurrentObjects.Count == 1)
            {
                CurrentObjects[0].Size = _coordinateHelper.Rotate(CurrentObjects[0].Size);
                CurrentObjects[0].Direction = _coordinateHelper.Rotate(CurrentObjects[0].Direction);
            }
            else if (CurrentObjects.Count > 1)
            {
                _transformService.Rotate(CurrentObjects).Consume();
            }
            else
            {
                //Count == 0;
                //Rotate from selected objects
                CurrentObjects = CloneLayoutObjects(SelectedObjects);
                _transformService.Rotate(CurrentObjects).Consume();
            }
        }

        internal Hotkey rotateAllHotkey;
        internal ICommand rotateAllCommand;
        internal void ExecuteRotateAll(object param)
        {
            UndoManager.AsSingleUndoableOperation(() =>
            {
                var placedObjects = PlacedObjects.ToList();
                UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
                {
                    QuadTree = PlacedObjects,
                    ObjectPropertyValues = PlacedObjects.Select(obj => (obj, obj.Bounds, _coordinateHelper.Rotate(obj.Bounds))).ToList()
                });

                foreach (var (item, oldRect) in _transformService.Rotate(placedObjects))
                {
                    PlacedObjects.ReIndex(item, oldRect);
                }
                Normalize(1);
            });
        }

        internal Hotkey copyHotkey;
        internal ICommand copyCommand;
        internal void ExecuteCopy(object param)
        {
            if (SelectedObjects.Count != 0)
            {
                ClipboardService.Copy(SelectedObjects.Select(x => x.WrappedAnnoObject));

                var localizedMessage = SelectedObjects.Count == 1 ? _localizationHelper.GetLocalization("ItemCopied") : _localizationHelper.GetLocalization("ItemsCopied");
                StatusMessage = $"{SelectedObjects.Count} {localizedMessage}";
            }
        }

        internal Hotkey pasteHotkey;
        internal ICommand pasteCommand;
        internal void ExecutePaste(object param)
        {
            var objects = ClipboardService.Paste();
            if (objects.Count > 0)
            {
                CurrentObjects = [.. objects.Select(x => new LayoutObject(x, _coordinateHelper, _brushCache, _penCache))];
            }
        }

        internal Hotkey deleteHotkey;
        internal ICommand deleteCommand;
        internal void ExecuteDelete(object param)
        {
            UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
            {
                Objects = [.. SelectedObjects],
                Collection = PlacedObjects
            });

            // remove all currently selected objects from the grid and clear selection    
            foreach (var item in SelectedObjects)
            {
                _ = PlacedObjects.Remove(item);
            }
            SelectedObjects.Clear();
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
            CurrentMode = MouseMode.DeleteObject;
        }

        internal Hotkey duplicateHotkey;
        internal ICommand duplicateCommand;
        internal void ExecuteDuplicate(object param)
        {
            var obj = GetObjectAt(_mousePosition);
            if (obj != null)
            {
                CurrentObjects.Clear();
                CurrentObjects.Add(new LayoutObject(new AnnoObject(obj.WrappedAnnoObject), _coordinateHelper, _brushCache, _penCache));
                OnCurrentObjectChanged(obj);
            }
        }

        internal Hotkey deleteObjectUnderCursorHotkey;
        internal ICommand deleteObjectUnderCursorCommand;
        internal void ExecuteDeleteObjectUnderCursor(object param)
        {
            if (CurrentObjects.Count == 0)
            {
                var obj = GetObjectAt(_mousePosition);
                if (obj != null)
                {
                    // Remove object, only ever remove a single object this way.
                    UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
                    {
                        Objects =
                        [
                            obj
                        ],
                        Collection = PlacedObjects
                    });

                    _ = PlacedObjects.Remove(obj);
                    _selectionService.RemoveSelectedObject(obj);
                    RecalculateSelectionContainsNotIgnoredObject();
                    StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
                    CurrentMode = MouseMode.DeleteObject;

                    InvalidateVisual();
                }
            }
        }

        internal Hotkey undoHotkey;
        internal ICommand undoCommand;
        internal void ExecuteUndo(object param)
        {
            UndoManager.Undo();
            ForceRendering();
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
        }

        internal Hotkey redoHotkey;
        internal ICommand redoCommand;
        internal void ExecuteRedo(object param)
        {
            UndoManager.Redo();
            ForceRendering();
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
        }

        internal Hotkey selectAllSameIdentifierHotkey;
        internal ICommand selectAllSameIdentifierCommand;
        internal void ExecuteSelectAllSameIdentifier(object param)
        {
            //select all objects with same identifier as object under mouse cursor
            var objectToCheck = GetObjectAt(_mousePosition);
            if (objectToCheck != null)
            {
                CurrentMode = MouseMode.SelectSameIdentifier;

                if (SelectedObjects.Contains(objectToCheck))
                {
                    _selectionService.RemoveSelectedObject(objectToCheck, includeSameObjects: true);
                }
                else
                {
                    _selectionService.AddSelectedObject(objectToCheck, includeSameObjects: true);
                }

                RecalculateSelectionContainsNotIgnoredObject();
                ForceRendering();
                StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
            }
        }

        internal Hotkey enableDebugModeHotkey;
        internal ICommand enableDebugModeCommand;
        internal void ExecuteEnableDebugMode(object param)
        {
            _debugModeIsEnabled = !_debugModeIsEnabled;
            ForceRendering();
        }

        #endregion

        #region Helper methods

        internal List<LayoutObject> CloneLayoutObjects(HashSet<LayoutObject> list)
        {
            return list.Select(x => new LayoutObject(new AnnoObject(x.WrappedAnnoObject), _coordinateHelper, _brushCache, _penCache)).ToListWithCapacity(list.Count);
        }

        internal List<LayoutObject> CloneLayoutObjects(IEnumerable<LayoutObject> list, int capacity)
        {
            return list.Select(x => new LayoutObject(new AnnoObject(x.WrappedAnnoObject), _coordinateHelper, _brushCache, _penCache)).ToListWithCapacity(capacity);
        }

        internal void UpdateScrollBarVisibility()
        {
            if (ScrollOwner != null)
            {
                if (_showScrollBars)
                {
                    ScrollOwner.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    ScrollOwner.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                }
                else
                {
                    ScrollOwner.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    ScrollOwner.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                }
            }
        }

        #endregion

        public void RaiseStatisticsUpdated(UpdateStatisticsEventArgs args)
        {
            StatisticsUpdated?.Invoke(this, args);
        }

        public void RaiseColorsInLayoutUpdated()
        {
            ColorsInLayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void ForceRendering()
        {
            _isRenderingForced = true;
            InvalidateVisual();
        }

        #region IScrollInfo

        public double ExtentWidth => _scrollableBounds.Width;
        public double ExtentHeight => _scrollableBounds.Height;
        public double ViewportWidth => _viewport.Width;
        public double ViewportHeight => _viewport.Height;

        public double HorizontalOffset
        {
            get
            {
                return _appSettings.InvertScrollingDirection
                    ? _scrollableBounds.Left - _viewport.Left + (_scrollableBounds.Width - _viewport.Width)
                    : _viewport.Left - _scrollableBounds.Left;
            }
        }

        public double VerticalOffset
        {
            get
            {
                return _appSettings.InvertScrollingDirection
                    ? _scrollableBounds.Top - _viewport.Top + (_scrollableBounds.Height - _viewport.Height)
                    : _viewport.Top - _scrollableBounds.Top;
            }
        }

        public ScrollViewer ScrollOwner { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }

        public void LineUp()
        {
            _viewport.Top -= 1;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void LineDown()
        {
            _viewport.Top += 1;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void LineLeft()
        {
            _viewport.Left -= 1;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void LineRight()
        {
            _viewport.Left += 1;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void PageUp()
        {
            _viewport.Top -= _viewport.Height;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void PageDown()
        {
            _viewport.Top += _viewport.Height;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void PageLeft()
        {
            _viewport.Left -= _viewport.Width;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void PageRight()
        {
            _viewport.Left += _viewport.Width;
            if (!_scrollableBounds.Contains(_viewport.Absolute))
            {
                InvalidateScroll();
            }
            InvalidateVisual();
        }

        public void MouseWheelUp()
        {
            //Will zoom the canvas, rather than scroll the canvas
            //throw new NotImplementedException();
        }

        public void MouseWheelDown()
        {
            //Will zoom the canvas, rather than scroll the canvas
            //throw new NotImplementedException();
        }

        public void MouseWheelLeft()
        {
            //throw new NotImplementedException();
        }

        public void MouseWheelRight()
        {
            //throw new NotImplementedException();
        }

        public void SetHorizontalOffset(double offset)
        {
            //handle when offset is +/- infinity (when scrolling to top/bottom using the end and home keys)
            offset = Math.Max(offset, 0d);
            offset = Math.Min(offset, _scrollableBounds.Width);
            _viewport.Left = _appSettings.InvertScrollingDirection
                ? _scrollableBounds.Left - offset + (_scrollableBounds.Width - _viewport.Width)
                : _scrollableBounds.Left + offset;
            _viewport.Left = _scrollableBounds.Left + offset;
            InvalidateScroll();
            InvalidateVisual();
        }

        public void SetVerticalOffset(double offset)
        {
            //handle when offset is +/- infinity (when scrolling to top/bottom using the end and home keys)
            offset = Math.Max(offset, 0d);
            offset = Math.Min(offset, _scrollableBounds.Height);
            _viewport.Top = _appSettings.InvertScrollingDirection
                ? _scrollableBounds.Top - offset + (_scrollableBounds.Height - _viewport.Height)
                : _scrollableBounds.Top + offset;
            InvalidateScroll();
            InvalidateVisual();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return _viewport.Absolute;
        }

        [GeneratedRegex("A7_residence_SkyScraper_(?<tier>[45])lvl(?<level>[1-5])", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        internal static partial Regex SkyScraperRegex();

        #endregion
    }
}
