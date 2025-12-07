namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Tool contract for EditorCanvas tooling layer.
    /// Tools encapsulate behavior like selection, drawing, transformation.
    /// </summary>
    public interface ITool
    {
        string Name { get; }
        void Activate();
        void Deactivate();
        /// <summary>
        /// Cancel any in-progress interaction for this tool (e.g., rubber bands, drags).
        /// </summary>
        void OnCancel();
        // Input event handlers forwarded from the UI
        void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e);
        void OnMouseMove(System.Windows.Input.MouseEventArgs e);
        void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e);
        void OnKeyDown(System.Windows.Input.KeyEventArgs e);
        void OnKeyUp(System.Windows.Input.KeyEventArgs e);
        // Render overlay visuals for this tool using the WPF DrawingContext
        void Render(System.Windows.Media.DrawingContext dc);
    }
}
