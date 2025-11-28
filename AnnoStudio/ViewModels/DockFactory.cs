using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace AnnoStudio.ViewModels
{
    public class DockFactory : Factory
    {
        private CanvasViewModel? _canvasViewModel;
        private ToolbarViewModel? _toolbarViewModel;
        private BuildingPresetsViewModel? _buildingPresetsViewModel;
        private PropertiesViewModel? _propertiesViewModel;

        public override IRootDock CreateLayout()
        {
            // Create the canvas view model
            _canvasViewModel = new CanvasViewModel();
            
            // Create the toolbar view model
            _toolbarViewModel = new ToolbarViewModel();
            
            // Create the building presets view model with stamp service
            var stampService = new Services.StampService();
            _buildingPresetsViewModel = new BuildingPresetsViewModel(stampService);
            
            // Create the properties view model
            _propertiesViewModel = new PropertiesViewModel();
            
            // Wire up stamp selection to canvas
            _buildingPresetsViewModel.StampSelectedForPlacement += OnStampSelectedForPlacement;
            // Create tools (can be floated, docked, hidden)
            var buildingPresetsTool = new Tool
            {
                Id = "BuildingPresets",
                Title = "Building Presets",
                CanClose = true,
                CanFloat = true,
                CanPin = true
            };

            var statisticsTool = new Tool
            {
                Id = "Statistics",
                Title = "Statistics",
                CanClose = true,
                CanFloat = true,
                CanPin = true
            };

            var propertiesTool = new Tool
            {
                Id = "Properties",
                Title = "Properties",
                CanClose = true,
                CanFloat = true,
                CanPin = true
            };

            // Create document for canvas (main content area)
            var canvasDocument = new Document
            {
                Id = "Canvas",
                Title = "Designer Canvas",
                CanClose = false,
                CanFloat = false
            };

            // Create toolbar tool
            var toolbarTool = new Tool
            {
                Id = "Toolbar",
                Title = "Toolbar",
                CanClose = false,
                CanFloat = false,
                CanPin = false
            };

            // Left tool dock for building presets
            var leftToolDock = new ToolDock
            {
                Id = "LeftTools",
                Title = "LeftTools",
                Proportion = 0.25,
                ActiveDockable = buildingPresetsTool,
                VisibleDockables = CreateList<IDockable>(buildingPresetsTool),
                Alignment = Alignment.Left,
                GripMode = GripMode.Visible
            };

            // Right tool dock for properties and statistics (stacked vertically)
            var rightTopToolDock = new ToolDock
            {
                Id = "RightTopTools",
                Title = "Statistics",
                Proportion = 0.33,
                ActiveDockable = statisticsTool,
                VisibleDockables = CreateList<IDockable>(statisticsTool),
                Alignment = Alignment.Right,
                GripMode = GripMode.Visible
            };

            var rightBottomToolDock = new ToolDock
            {
                Id = "RightBottomTools",
                Title = "Properties",
                Proportion = 0.67,
                ActiveDockable = propertiesTool,
                VisibleDockables = CreateList<IDockable>(propertiesTool),
                Alignment = Alignment.Right,
                GripMode = GripMode.Visible
            };

            // Vertical stack for right tools
            var rightPane = new ProportionalDock
            {
                Id = "RightPane",
                Title = "RightPane",
                Proportion = 0.25,
                Orientation = Orientation.Vertical,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    rightTopToolDock,
                    new ProportionalDockSplitter()
                    {
                        Id = "RightPaneSplitter",
                        Title = "RightPaneSplitter"
                    },
                    rightBottomToolDock
                )
            };

            // Document dock for canvas
            var documentDock = new DocumentDock
            {
                Id = "DocumentsPane",
                Title = "Documents",
                Proportion = double.NaN,
                ActiveDockable = canvasDocument,
                VisibleDockables = CreateList<IDockable>(canvasDocument),
                CanCreateDocument = false
            };

            // Main layout - horizontal arrangement
            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Title = "MainLayout",
                Proportion = double.NaN,
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    leftToolDock,
                    new ProportionalDockSplitter()
                    {
                        Id = "LeftSplitter",
                        Title = "LeftSplitter"
                    },
                    documentDock,
                    new ProportionalDockSplitter()
                    {
                        Id = "RightSplitter",
                        Title = "RightSplitter"
                    },
                    rightPane
                )
            };

            // Top toolbar dock
            var topToolDock = new ToolDock
            {
                Id = "TopToolbar",
                Title = "TopToolbar",
                Proportion = double.NaN,
                MaxHeight = 40,
                ActiveDockable = toolbarTool,
                VisibleDockables = CreateList<IDockable>(toolbarTool),
                Alignment = Alignment.Top,
                GripMode = GripMode.Hidden
            };

            // Root layout with toolbar at top
            var rootLayout = new ProportionalDock
            {
                Id = "RootLayout",
                Title = "RootLayout",
                Proportion = double.NaN,
                Orientation = Orientation.Vertical,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    topToolDock,
                    mainLayout
                )
            };

            // Root dock
            var root = CreateRootDock();

            root.Id = "Root";
            root.Title = "Root";
            root.ActiveDockable = rootLayout;
            root.DefaultDockable = rootLayout;
            root.VisibleDockables = CreateList<IDockable>(rootLayout);

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            ContextLocator = new Dictionary<string, Func<object?>>
            {
                ["BuildingPresets"] = () => _buildingPresetsViewModel,
                ["Canvas"] = () => _canvasViewModel,
                ["Statistics"] = () => _canvasViewModel,
                ["Properties"] = () => _propertiesViewModel,
                ["Toolbar"] = () => _toolbarViewModel,
            };

            DockableLocator = new Dictionary<string, Func<IDockable?>>
            {
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
            {
                [nameof(IDockWindow)] = () =>
                {
                    var hostWindow = new HostWindow()
                    {
                        [!HostWindow.TitleProperty] = new Avalonia.Data.Binding("ActiveDockable.Title")
                    };
                    return hostWindow;
                }
            };

            base.InitLayout(layout);
        }

        /// <summary>
        /// Gets the canvas view model for toolbar initialization
        /// </summary>
        public CanvasViewModel? GetCanvasViewModel() => _canvasViewModel;

        /// <summary>
        /// Gets the toolbar view model
        /// </summary>
        public ToolbarViewModel? GetToolbarViewModel() => _toolbarViewModel;
        
        /// <summary>
        /// Handles stamp selection from the building presets panel
        /// </summary>
        private void OnStampSelectedForPlacement(object? sender, Services.BuildingStamp stamp)
        {
            if (_canvasViewModel != null)
            {
                // Set the selected stamp - CanvasViewModel will handle tool activation
                _canvasViewModel.SelectedStamp = stamp;
            }
        }
        
        /// <summary>
        /// Handles selection changes from the canvas
        /// </summary>
        private void OnCanvasSelectionChanged(object? sender, EditorCanvas.Core.Interfaces.ICanvasObject? selectedObject)
        {
            if (_propertiesViewModel != null)
            {
                _propertiesViewModel.UpdateSelection(selectedObject);
            }
        }
    }
}
