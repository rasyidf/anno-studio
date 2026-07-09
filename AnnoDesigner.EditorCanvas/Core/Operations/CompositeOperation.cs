using System.Collections.Generic;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Operations;

/// <summary>
/// Groups multiple undo operations into a single atomic undo/redo step.
/// </summary>
public class CompositeOperation : IUndoOperation
{
    private readonly List<IUndoOperation> _operations;
    public string Description { get; }

    public CompositeOperation(string description, IEnumerable<IUndoOperation> operations)
    {
        Description = description;
        _operations = new List<IUndoOperation>(operations);
    }

    public void Execute()
    {
        foreach (var op in _operations) op.Execute();
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _operations.Count - 1; i >= 0; i--)
            _operations[i].Undo();
    }
}
