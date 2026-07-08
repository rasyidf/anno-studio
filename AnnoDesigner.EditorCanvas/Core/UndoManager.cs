using System;
using System.Collections.Generic;

namespace AnnoDesigner.Controls.EditorCanvas.Core;

/// <summary>
/// Stack-based undo/redo manager. Redo stack is cleared on new operation.
/// </summary>
public class UndoManager : IUndoManager
{
    private readonly Stack<IUndoOperation> _undoStack = new();
    private readonly Stack<IUndoOperation> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void Execute(IUndoOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        operation.Execute();
        _undoStack.Push(operation);
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var op = _undoStack.Pop();
        op.Undo();
        _redoStack.Push(op);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var op = _redoStack.Pop();
        op.Execute();
        _undoStack.Push(op);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
