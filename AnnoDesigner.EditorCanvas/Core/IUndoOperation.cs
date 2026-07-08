namespace AnnoDesigner.Controls.EditorCanvas.Core;

public interface IUndoOperation
{
    void Execute();
    void Undo();
    string Description { get; }
}
