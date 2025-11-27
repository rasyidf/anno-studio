using System;
using System.Windows.Input;
using Avalonia.Input;
using AnnoStudio.EditorCanvas.ViewModels;

namespace AnnoStudio.Services;

/// <summary>
/// Service that provides integration between MainWindowViewModel and EditorCanvas.
/// Manages command routing and event coordination.
/// </summary>
public class CanvasIntegrationService
{
    private EditorCanvasViewModel? _activeCanvasViewModel;
    
    /// <summary>
    /// Gets or sets the currently active canvas view model.
    /// </summary>
    public EditorCanvasViewModel? ActiveCanvasViewModel
    {
        get => _activeCanvasViewModel;
        set
        {
            if (_activeCanvasViewModel != value)
            {
                _activeCanvasViewModel = value;
                ActiveCanvasChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Event raised when the active canvas changes.
    /// </summary>
    public event EventHandler? ActiveCanvasChanged;

    /// <summary>
    /// Routes undo command to active canvas.
    /// </summary>
    public void Undo()
    {
        _activeCanvasViewModel?.UndoCommand?.Execute(null);
    }

    /// <summary>
    /// Routes redo command to active canvas.
    /// </summary>
    public void Redo()
    {
        _activeCanvasViewModel?.RedoCommand?.Execute(null);
    }

    /// <summary>
    /// Routes delete command to active canvas.
    /// </summary>
    public void Delete()
    {
        _activeCanvasViewModel?.DeleteSelectedCommand?.Execute(null);
    }

    /// <summary>
    /// Routes select all command to active canvas.
    /// </summary>
    public void SelectAll()
    {
        _activeCanvasViewModel?.SelectAllCommand?.Execute(null);
    }

    /// <summary>
    /// Routes duplicate command to active canvas.
    /// </summary>
    public void Duplicate()
    {
        _activeCanvasViewModel?.Canvas?.Shortcuts.GetShortcut("Duplicate")?.Action?.Invoke();
    }

    /// <summary>
    /// Activates a tool by name on the active canvas.
    /// </summary>
    public void ActivateTool(string toolName)
    {
        _activeCanvasViewModel?.ActivateTool(toolName);
    }

    /// <summary>
    /// Activates a tool by keyboard shortcut.
    /// </summary>
    public bool HandleToolShortcut(Key key)
    {
        if (_activeCanvasViewModel == null)
            return false;

        var toolName = key switch
        {
            Key.V => "Select",
            Key.S => "Stamp",
            Key.R => "Rectangle",
            Key.L => "Line",
            Key.P => "Draw",
            _ => null
        };

        if (toolName != null)
        {
            ActivateTool(toolName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if undo is available on the active canvas.
    /// </summary>
    public bool CanUndo()
    {
        return _activeCanvasViewModel?.Canvas?.History.CanUndo ?? false;
    }

    /// <summary>
    /// Checks if redo is available on the active canvas.
    /// </summary>
    public bool CanRedo()
    {
        return _activeCanvasViewModel?.Canvas?.History.CanRedo ?? false;
    }

    /// <summary>
    /// Checks if there is a selection on the active canvas.
    /// </summary>
    public bool HasSelection()
    {
        return _activeCanvasViewModel?.SelectedObjects.Count > 0;
    }

    /// <summary>
    /// Zooms in on the active canvas.
    /// </summary>
    public void ZoomIn()
    {
        _activeCanvasViewModel?.ZoomInCommand?.Execute(null);
    }

    /// <summary>
    /// Zooms out on the active canvas.
    /// </summary>
    public void ZoomOut()
    {
        _activeCanvasViewModel?.ZoomOutCommand?.Execute(null);
    }

    /// <summary>
    /// Resets zoom to 100% on the active canvas.
    /// </summary>
    public void ZoomReset()
    {
        _activeCanvasViewModel?.ZoomResetCommand?.Execute(null);
    }

    /// <summary>
    /// Zooms to fit all objects on the active canvas.
    /// </summary>
    public void ZoomToFit()
    {
        _activeCanvasViewModel?.ZoomToFitCommand?.Execute(null);
    }

    /// <summary>
    /// Toggles grid visibility on the active canvas.
    /// </summary>
    public void ToggleGrid()
    {
        if (_activeCanvasViewModel != null)
        {
            _activeCanvasViewModel.GridVisible = !_activeCanvasViewModel.GridVisible;
        }
    }
}
