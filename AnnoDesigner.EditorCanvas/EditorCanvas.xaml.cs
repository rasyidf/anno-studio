using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Controls.EditorCanvas.Interaction;
using AnnoDesigner.Controls.EditorCanvas.Tooling;

namespace AnnoDesigner.Controls.EditorCanvas
{
    /// <summary>
    /// Code-behind for the EditorCanvas control (scaffold).
    /// This is an initial placeholder — implementation to follow.
    /// </summary>
    public partial class EditorCanvas : UserControl
    {
        private Core.IRenderer _renderer;
        /// <summary>
        /// If the current renderer supports layering this exposes it to UI or external components.
        /// </summary>
        public Core.ILayeredRenderer? LayeredRenderer => _renderer as Core.ILayeredRenderer;
        private IInputHandler _inputHandler;
        private bool _isPanning = false;
        private System.Windows.Point _panStartScreen;
        public ToolManager ToolManager { get; private set; }
        public HotkeyManager Hotkeys { get; private set; }
        // services
        public Core.IPreferencesService Preferences { get; private set; }
        public Core.ITransformService TransformService { get; private set; }
        public Content.IObjectManager<Content.Models.CanvasObject> ObjectManager { get; private set; }
        private readonly List<CanvasObject> _selectedObjects = new();
        public IReadOnlyList<CanvasObject> SelectedObjects => _selectedObjects;
        public CanvasObject? SelectedObject => _selectedObjects.FirstOrDefault();
        public event Action<IReadOnlyList<CanvasObject>>? SelectionChanged;
        public event Action<Tooling.ITool?>? ToolChanged;
        // External toggle request for Layer Manager UI
        public event Action? LayerManagerToggleRequested;
        public bool ShowGrid { get; set; } = true;
        public bool ShowGuides { get; set; } = true;
        public bool ShowToolOverlays { get; set; } = true;
        public bool AutoReturnToSelection { get; set; } = true;

        public Brush GridLineBrush
        {
            get => (Brush)GetValue(GridLineBrushProperty);
            set => SetValue(GridLineBrushProperty, value);
        }

        public Brush GuideLineBrush
        {
            get => (Brush)GetValue(GuideLineBrushProperty);
            set => SetValue(GuideLineBrushProperty, value);
        }

        public Brush ObjectStrokeBrush
        {
            get => (Brush)GetValue(ObjectStrokeBrushProperty);
            set => SetValue(ObjectStrokeBrushProperty, value);
        }

        public Brush ObjectFillBrush
        {
            get => (Brush)GetValue(ObjectFillBrushProperty);
            set => SetValue(ObjectFillBrushProperty, value);
        }

        public Brush SelectionStrokeBrush
        {
            get => (Brush)GetValue(SelectionStrokeBrushProperty);
            set => SetValue(SelectionStrokeBrushProperty, value);
        }

        public Brush OverlayTextBrush
        {
            get => (Brush)GetValue(OverlayTextBrushProperty);
            set => SetValue(OverlayTextBrushProperty, value);
        }

        public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.Register(
            nameof(GridLineBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GuideLineBrushProperty = DependencyProperty.Register(
            nameof(GuideLineBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ObjectStrokeBrushProperty = DependencyProperty.Register(
            nameof(ObjectStrokeBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.Blue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ObjectFillBrushProperty = DependencyProperty.Register(
            nameof(ObjectFillBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectionStrokeBrushProperty = DependencyProperty.Register(
            nameof(SelectionStrokeBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OverlayTextBrushProperty = DependencyProperty.Register(
            nameof(OverlayTextBrush), typeof(Brush), typeof(EditorCanvas),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        public EditorCanvas()
        {
            InitializeComponent();

            // instantiate default scaffolds
            _renderer = new Core.RendererWpf(this);

            // core services used by tools, layers and renderer
            Preferences = new Core.PreferencesService();
            TransformService = new Core.TransformService(Preferences);
            ToolManager = new Tooling.ToolManager();
            Hotkeys = new Interaction.HotkeyManager(ToolManager, HandleCommand);
            _inputHandler = new Interaction.InputInteractionService(ToolManager, Hotkeys);
            ObjectManager = new Content.ObjectManagerQuadTree();

            // register default render layers when the renderer supports layering
            if (_renderer is Core.ILayeredRenderer layered)
            {
                // Primary grid and fine-grain grid layers
                layered.AddLayer(new Core.Layers.GridLayer(order: 100) { CellSize = 128 });
                layered.AddLayer(new Core.Layers.SubGridLayer(order: 110) { CellSize = 32 });
                layered.AddLayer(new Core.Layers.DotGridLayer(order: 120) { CellSize = 64, DotRadius = 1.25 });
                layered.AddLayer(new Core.Layers.CrossGridLayer(order: 130) { CellSize = 64, CrossHalfSize = 3 });

                // Visual guidelines, objects and overlays
                layered.AddLayer(new Core.Layers.GuidelinesLayer(order: 200));
                layered.AddLayer(new Core.Layers.ObjectsLayer(order: 300));
                layered.AddLayer(new Core.Layers.ToolingOverlayLayer(order: 400));
                layered.AddLayer(new Core.Layers.OverlayTextLayer(order: 500));
            }

            // register default tools
            var selection = new Tooling.SelectionTool(ObjectManager, this);
            selection.ObjectSelected += obj => SetSelection(obj == null ? Array.Empty<CanvasObject>() : new[] { obj });
            ToolManager.RegisterTool(selection);

            ToolManager.RegisterTool(new RectSelectTool(ObjectManager, this, SetSelection, () => _renderer.Invalidate()));
            ToolManager.RegisterTool(new LassoSelectTool(ObjectManager, this, SetSelection, () => _renderer.Invalidate()));
            ToolManager.RegisterTool(new RectDrawTool(ObjectManager, this, SetSelection, () => _renderer.Invalidate(), MaybeReturnToSelection));
            ToolManager.RegisterTool(new LineDrawTool(ObjectManager, this, SetSelection, () => _renderer.Invalidate(), MaybeReturnToSelection));
            ToolManager.RegisterTool(new PencilDrawTool(ObjectManager, this, SetSelection, () => _renderer.Invalidate(), MaybeReturnToSelection));
            ToolManager.RegisterTool(new TransformTool(this, () => SelectedObjects, () => _renderer.Invalidate()));
            ToolManager.RegisterTool(new DuplicateTool(ObjectManager, () => SelectedObjects, SetSelection, () => _renderer.Invalidate(), MaybeReturnToSelection));

            ToolManager.ActiveToolChanged += tool => ToolChanged?.Invoke(tool);
            ToolManager.Activate(selection.Name);
            RegisterDefaultHotkeys();

            // wire UI events to the input handler
            this.MouseDown += (s, e) => _inputHandler.OnMouseDown(e);
            this.MouseMove += (s, e) => _inputHandler.OnMouseMove(e);
            this.MouseUp += (s, e) => _inputHandler.OnMouseUp(e);
            // Transform controls: wheel zoom, middle-button pan
            this.MouseWheel += EditorCanvas_MouseWheel;
            this.MouseDown += EditorCanvas_MouseDown;
            this.MouseMove += EditorCanvas_MouseMove;
            this.MouseUp += EditorCanvas_MouseUp;
            this.KeyDown += (s, e) => _inputHandler.OnKeyDown(e);
            this.KeyUp += (s, e) => _inputHandler.OnKeyUp(e);
            // keep focus when clicked so keyboard events are received
            this.MouseDown += (s, e) => this.Focus();
        }

        public void SetSelection(IEnumerable<CanvasObject> objects)
        {
            _selectedObjects.Clear();
            if (objects != null)
            {
                _selectedObjects.AddRange(objects.Where(o => o != null));
            }
            SelectionChanged?.Invoke(_selectedObjects);
            _renderer.Invalidate();
        }

        public void ClearSelection()
        {
            SetSelection(Array.Empty<CanvasObject>());
        }

        private void MaybeReturnToSelection()
        {
            if (AutoReturnToSelection)
            {
                ToolManager.Activate("Selection");
            }
        }

        private void HandleCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            switch (command)
            {
                case "Cancel":
                    ToolManager.CancelActive();
                    _renderer.Invalidate();
                    break;
                case "ClearSelection":
                    ClearSelection();
                    break;
                case "ZoomIn":
                    if (TransformService != null)
                    {
                        var center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2);
                        TransformService.ZoomAt(center, 1.1);
                        _renderer.Invalidate();
                    }
                    break;
                case "ZoomOut":
                    if (TransformService != null)
                    {
                        var center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2);
                        TransformService.ZoomAt(center, 1.0 / 1.1);
                        _renderer.Invalidate();
                    }
                    break;
                case "ZoomReset":
                    TransformService?.Reset();
                    _renderer.Invalidate();
                    break;
                case "PanLeft":
                    TransformService?.PanBy(new System.Windows.Vector(-50, 0));
                    _renderer.Invalidate();
                    break;
                case "PanRight":
                    TransformService?.PanBy(new System.Windows.Vector(50, 0));
                    _renderer.Invalidate();
                    break;
                case "PanUp":
                    TransformService?.PanBy(new System.Windows.Vector(0, -50));
                    _renderer.Invalidate();
                    break;
                case "PanDown":
                    TransformService?.PanBy(new System.Windows.Vector(0, 50));
                    _renderer.Invalidate();
                    break;
                case "ToggleLayerManager":
                    LayerManagerToggleRequested?.Invoke();
                    break;
                default:
                    break;
            }
        }

        private void RegisterDefaultHotkeys()
        {
            Hotkeys.ReplaceBindings(new[]
            {
                new HotkeyBinding { Id = "Select", DisplayName = "Selection Tool", Key = Key.V, ActionType = HotkeyActionType.ActivateTool, Target = "Selection" },
                new HotkeyBinding { Id = "RectSelect", DisplayName = "Rectangle Select", Key = Key.R, ActionType = HotkeyActionType.ActivateTool, Target = "RectSelect" },
                new HotkeyBinding { Id = "LassoSelect", DisplayName = "Lasso Select", Key = Key.L, ActionType = HotkeyActionType.ActivateTool, Target = "LassoSelect" },
                new HotkeyBinding { Id = "RectDraw", DisplayName = "Rectangle Draw", Key = Key.D, ActionType = HotkeyActionType.ActivateTool, Target = "RectDraw" },
                new HotkeyBinding { Id = "LineDraw", DisplayName = "Line Draw", Key = Key.N, ActionType = HotkeyActionType.ActivateTool, Target = "LineDraw" },
                new HotkeyBinding { Id = "PencilDraw", DisplayName = "Pencil Draw", Key = Key.P, ActionType = HotkeyActionType.ActivateTool, Target = "PencilDraw" },
                new HotkeyBinding { Id = "Transform", DisplayName = "Transform", Key = Key.M, ActionType = HotkeyActionType.ActivateTool, Target = "Transform" },
                new HotkeyBinding { Id = "Duplicate", DisplayName = "Duplicate Selection", Key = Key.D, Modifiers = ModifierKeys.Control, ActionType = HotkeyActionType.ActivateTool, Target = "Duplicate" },
                new HotkeyBinding { Id = "Cancel", DisplayName = "Cancel", Key = Key.Escape, ActionType = HotkeyActionType.Command, Target = "Cancel" },
                // zoom and pan commands
                new HotkeyBinding { Id = "ZoomIn", DisplayName = "Zoom In", Key = Key.OemPlus, Modifiers = ModifierKeys.Control, ActionType = HotkeyActionType.Command, Target = "ZoomIn" },
                new HotkeyBinding { Id = "ZoomOut", DisplayName = "Zoom Out", Key = Key.OemMinus, Modifiers = ModifierKeys.Control, ActionType = HotkeyActionType.Command, Target = "ZoomOut" },
                new HotkeyBinding { Id = "ZoomReset", DisplayName = "Reset Zoom", Key = Key.D0, Modifiers = ModifierKeys.Control, ActionType = HotkeyActionType.Command, Target = "ZoomReset" },
                new HotkeyBinding { Id = "PanLeft", DisplayName = "Pan Left", Key = Key.Left, ActionType = HotkeyActionType.Command, Target = "PanLeft" },
                new HotkeyBinding { Id = "PanRight", DisplayName = "Pan Right", Key = Key.Right, ActionType = HotkeyActionType.Command, Target = "PanRight" },
                new HotkeyBinding { Id = "PanUp", DisplayName = "Pan Up", Key = Key.Up, ActionType = HotkeyActionType.Command, Target = "PanUp" },
                new HotkeyBinding { Id = "PanDown", DisplayName = "Pan Down", Key = Key.Down, ActionType = HotkeyActionType.Command, Target = "PanDown" },
                // toggle a layout/layer manager UI
                new HotkeyBinding { Id = "ToggleLayerManager", DisplayName = "Toggle Layer Manager", Key = Key.L, Modifiers = ModifierKeys.Control | ModifierKeys.Shift, ActionType = HotkeyActionType.Command, Target = "ToggleLayerManager" }
            });
        }

        private void EditorCanvas_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (TransformService == null) return;

            // When control is held, we interpret wheel as zoom. Otherwise tools may handle it.
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var mouse = e.GetPosition(this);
                var scale = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
                TransformService.ZoomAt(mouse, scale);
                _renderer.Invalidate();
                e.Handled = true;
            }
        }

        private void EditorCanvas_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _panStartScreen = e.GetPosition(this);
                this.CaptureMouse();
                e.Handled = true;
            }
        }

        private void EditorCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isPanning && e != null)
            {
                var curr = e.GetPosition(this);
                var delta = curr - _panStartScreen;
                TransformService?.PanBy(new System.Windows.Vector(delta.X, delta.Y));
                _panStartScreen = curr;
                _renderer.Invalidate();
                e.Handled = true;
            }
        }

        private void EditorCanvas_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (_isPanning && e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;
                try { this.ReleaseMouseCapture(); } catch { }
                e.Handled = true;
            }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _renderer.Render(drawingContext);
        }
    }
}
