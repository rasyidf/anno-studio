using System.Windows.Input;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Manages command history for undo/redo functionality.
/// </summary>
public interface ICommandHistory
{
    /// <summary>
    /// Execute a command and add it to history.
    /// </summary>
    void Execute(ICommand command);

    /// <summary>
    /// Undo the last command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redo the last undone command.
    /// </summary>
    void Redo();

    /// <summary>
    /// Check if undo is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Check if redo is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Clear all command history.
    /// </summary>
    void Clear();

    /// <summary>
    /// Get undo stack depth.
    /// </summary>
    int UndoStackSize { get; }

    /// <summary>
    /// Get redo stack depth.
    /// </summary>
    int RedoStackSize { get; }
}
