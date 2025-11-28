using System.Collections.Generic;
using System.Windows.Input;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of command history for undo/redo functionality.
/// </summary>
public class CommandHistory : ICommandHistory
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private readonly int _maxStackSize;

    public CommandHistory(int maxStackSize = 100)
    {
        _maxStackSize = maxStackSize;
    }

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public int UndoStackSize => _undoStack.Count;

    public int RedoStackSize => _redoStack.Count;

    public void Execute(ICommand command)
    {
        if (command == null || !command.CanExecute(null))
            return;

        command.Execute(null);
        _undoStack.Push(command);

        // Clear redo stack when new command is executed
        _redoStack.Clear();

        // Limit stack size
        if (_undoStack.Count > _maxStackSize)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = items.Length - _maxStackSize; i < items.Length; i++)
            {
                _undoStack.Push(items[i]);
            }
        }
    }

    public void Undo()
    {
        if (!CanUndo)
            return;

        var command = _undoStack.Pop();
        
        // Undo the command (assuming ITransformOperation pattern)
        if (command is ITransformOperation transform)
        {
            transform.Undo();
        }

        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (!CanRedo)
            return;

        var command = _redoStack.Pop();
        
        // Redo the command
        if (command is ITransformOperation transform)
        {
            transform.Redo();
        }

        _undoStack.Push(command);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
