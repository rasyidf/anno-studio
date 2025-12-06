using System.Collections.Generic;
using AnnoDesigner.Services.Undo;
using AnnoDesigner.Services.Undo.Operations;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class CommandHistoryService : ICommandHistoryService
    {
        private readonly Stack<object> _undo = new();
        private readonly Stack<object> _redo = new();
        private readonly IUndoManager _undoManager;

        public CommandHistoryService(IUndoManager undoManager = null)
        {
            _undoManager = undoManager;
        }

        public void Push(object command)
        {
            // If we have an IUndoManager and the pushed command is an IOperation, register it there.
            if (_undoManager != null && command is IOperation operation)
            {
                _undoManager.RegisterOperation(operation);
                return;
            }

            _undo.Push(command);
            _redo.Clear();
        }

        public void Undo()
        {
            // Prefer the UndoManager if available
            if (_undoManager != null)
            {
                _undoManager.Undo();
                return;
            }

            if (_undo.Count == 0) return;
            var c = _undo.Pop();
            _redo.Push(c);
            // actual command application will be implemented later
        }

        public void Redo()
        {
            // Prefer the UndoManager if available
            if (_undoManager != null)
            {
                _undoManager.Redo();
                return;
            }

            if (_redo.Count == 0) return;
            var c = _redo.Pop();
            _undo.Push(c);
            // actual command application will be implemented later
        }
    }
}
