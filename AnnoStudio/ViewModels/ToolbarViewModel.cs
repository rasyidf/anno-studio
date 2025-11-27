using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.ViewModels
{
    /// <summary>
    /// ViewModel for the toolbar that manages tool selection
    /// </summary>
    public partial class ToolbarViewModel : ObservableObject
    {
        [ObservableProperty]
        private IEditorTool? _selectedTool;

        [ObservableProperty]
        private CanvasViewModel? _canvasViewModel;

        public ObservableCollection<ToolItemViewModel> AvailableTools { get; } = new();

        public ToolbarViewModel()
        {
        }

        public void Initialize(CanvasViewModel canvasViewModel)
        {
            CanvasViewModel = canvasViewModel;
            
            // Try to populate tools immediately
            UpdateToolsFromCanvas();
            
            // Subscribe to property changes to keep tools updated
            canvasViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CanvasViewModel.Document))
                {
                    UpdateToolsFromCanvas();
                }
            };

            // Also subscribe to Document property changes for when EditorViewModel is set
            if (canvasViewModel.Document != null)
            {
                canvasViewModel.Document.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LayoutDocument.EditorViewModel))
                    {
                        UpdateToolsFromCanvas();
                    }
                };
            }
        }

        private void UpdateToolsFromCanvas()
        {
            AvailableTools.Clear();
            
            var editorViewModel = CanvasViewModel?.Document?.EditorViewModel;
            if (editorViewModel?.Tools != null)
            {
                foreach (var tool in editorViewModel.Tools)
                {
                    var toolItem = new ToolItemViewModel(tool, this);
                    AvailableTools.Add(toolItem);
                }

                // Set default tool (first one, usually Select)
                if (AvailableTools.Count > 0 && SelectedTool == null)
                {
                    SelectTool(AvailableTools[0].Tool);
                }
            }
        }

        partial void OnSelectedToolChanged(IEditorTool? value)
        {
            // Update the canvas's selected tool
            var editorViewModel = CanvasViewModel?.Document?.EditorViewModel;
            if (editorViewModel != null)
            {
                editorViewModel.SelectedTool = value;
            }

            // Update IsSelected state on all tool items
            foreach (var toolItem in AvailableTools)
            {
                toolItem.IsSelected = toolItem.Tool == value;
            }
        }

        [RelayCommand]
        private void SelectTool(IEditorTool tool)
        {
            SelectedTool = tool;
        }
    }

    /// <summary>
    /// Wrapper for tool items to support selection state
    /// </summary>
    public partial class ToolItemViewModel : ObservableObject
    {
        private readonly ToolbarViewModel _parent;

        [ObservableProperty]
        private bool _isSelected;

        public IEditorTool Tool { get; }
        public string Name => Tool.Name;

        public ToolItemViewModel(IEditorTool tool, ToolbarViewModel parent)
        {
            Tool = tool;
            _parent = parent;
        }
    }
}
