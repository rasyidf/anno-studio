using System.Windows.Input;

namespace AnnoDesigner.Controls.EditorCanvas.Interaction
{
    /// <summary>
    /// Skeleton input interaction service — converts raw UI events into higher level interactions.
    /// Implementation will include gesture recognition, drag state, hit-testing, etc.
    /// </summary>
    public class InputInteractionService : IInputHandler
    {
        private readonly Tooling.ToolManager _toolManager;
        private readonly HotkeyManager _hotkeyManager;

        public InputInteractionService(Tooling.ToolManager toolManager, HotkeyManager hotkeyManager)
        {
            _toolManager = toolManager;
            _hotkeyManager = hotkeyManager;
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (TryHandleHotkey(e)) return;
            _toolManager?.OnKeyDown(e);
        }

        public void OnKeyUp(KeyEventArgs e)
        {
            _toolManager?.OnKeyUp(e);
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (TryHandleHotkey(e)) return;
            _toolManager?.OnMouseDown(e);
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            _toolManager?.OnMouseMove(e);
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            _toolManager?.OnMouseUp(e);
        }

        private bool TryHandleHotkey(KeyEventArgs e)
        {
            if (_hotkeyManager == null || e == null) return false;
            var handled = _hotkeyManager.HandleKeyDown(e);
            if (handled) e.Handled = true;
            return handled;
        }

        private bool TryHandleHotkey(MouseButtonEventArgs e)
        {
            if (_hotkeyManager == null || e == null) return false;
            var handled = _hotkeyManager.HandleMouseDown(e);
            if (handled) e.Handled = true;
            return handled;
        }
    }
}
