using System.Collections.Generic;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class CommandHistoryService : ICommandHistoryService
    {
        private readonly Stack<object> _undo = new();
        private readonly Stack<object> _redo = new();

        public void Push(object command)
        {
            _undo.Push(command);
            _redo.Clear();
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            var c = _undo.Pop();
            _redo.Push(c);
            // actual command application will be implemented later
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            var c = _redo.Pop();
            _undo.Push(c);
            // actual command application will be implemented later
        }
    }
}
