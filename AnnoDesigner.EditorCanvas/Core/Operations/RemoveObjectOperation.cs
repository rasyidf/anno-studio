using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Controls.EditorCanvas.Content;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Operations;

public class RemoveObjectOperation : IUndoOperation
{
    private readonly IObjectManager<CanvasObject> _manager;
    private readonly CanvasObject _obj;
    public string Description => "Remove object";

    public RemoveObjectOperation(IObjectManager<CanvasObject> manager, CanvasObject obj)
    {
        _manager = manager;
        _obj = obj;
    }

    public void Execute() => _manager.Remove(_obj);
    public void Undo() => _manager.Add(_obj);
}
