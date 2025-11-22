using System;
using AnnoDesigner.Services.Undo.Operations;

namespace AnnoDesigner.Services.Undo
{
    public interface IUndoManager
    {
        bool IsDirty { get; set; }

        void Undo();

        void Redo();

        void Clear();

        void RegisterOperation(IOperation operation);

        void AsSingleUndoableOperation(Action action);
    }
}
