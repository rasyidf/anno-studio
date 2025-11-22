namespace AnnoDesigner.Services.Undo.Operations
{
    public interface IOperation
    {
        void Undo();

        void Redo();
    }
}
