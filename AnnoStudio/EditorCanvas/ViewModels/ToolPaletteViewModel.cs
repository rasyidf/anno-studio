using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.ViewModels;

/// <summary>
/// ViewModel for the tool palette.
/// </summary>
public partial class ToolPaletteViewModel : ObservableObject
{
    [ObservableProperty]
    private IEditorTool? _selectedTool;

    public ObservableCollection<IEditorTool> Tools { get; } = new();

    public ToolPaletteViewModel()
    {
    }

    public void SetTools(IEnumerable<IEditorTool> tools)
    {
        Tools.Clear();
        foreach (var tool in tools)
        {
            Tools.Add(tool);
        }
    }

    [RelayCommand]
    private void SelectTool(IEditorTool tool)
    {
        SelectedTool = tool;
    }
}
