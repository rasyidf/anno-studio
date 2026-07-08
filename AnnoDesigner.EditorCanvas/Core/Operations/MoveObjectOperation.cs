using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Operations;

public class MoveObjectOperation : IUndoOperation
{
    private readonly CanvasObject _obj;
    private readonly Rect _oldBounds;
    private readonly Rect _newBounds;
    public string Description => "Move object";

    public MoveObjectOperation(CanvasObject obj, Rect oldBounds, Rect newBounds)
    {
        _obj = obj;
        _oldBounds = oldBounds;
        _newBounds = newBounds;
    }

    public void Execute() => _obj.Bounds = _newBounds;
    public void Undo() => _obj.Bounds = _oldBounds;
}
