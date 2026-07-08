namespace AnnoDesigner.Controls.EditorCanvas.Core;

public interface IUndoManager
{
    void Execute(IUndoOperation operation);
    void Undo();
    void Redo();
    void Clear();
    bool CanUndo { get; }
    bool CanRedo { get; }
    event System.EventHandler? StateChanged;
}
