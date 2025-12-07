using System.Windows.Input;

namespace AnnoDesigner.Controls.EditorCanvas.Interaction
{
    /// <summary>
    /// Input handling contract for mouse/keyboard events from the UI layer.
    /// Implementations will translate UI events into higher-level interaction events.
    /// </summary>
    public interface IInputHandler
    {
        void OnMouseDown(MouseButtonEventArgs e);
        void OnMouseMove(MouseEventArgs e);
        void OnMouseUp(MouseButtonEventArgs e);
        void OnKeyDown(KeyEventArgs e);
        void OnKeyUp(KeyEventArgs e);
    }
}
