namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface ICommandHistoryService
    {
        void Push(object command);
        void Undo();
        void Redo();
    }
}
