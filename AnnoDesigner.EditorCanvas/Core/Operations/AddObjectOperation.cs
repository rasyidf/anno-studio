using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Controls.EditorCanvas.Content;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Operations;

public class AddObjectOperation : IUndoOperation
{
    private readonly IObjectManager<CanvasObject> _manager;
    private readonly CanvasObject _obj;
    public string Description => "Add object";

    public AddObjectOperation(IObjectManager<CanvasObject> manager, CanvasObject obj)
    {
        _manager = manager;
        _obj = obj;
    }

    public void Execute() => _manager.Add(_obj);
    public void Undo() => _manager.Remove(_obj);
}
