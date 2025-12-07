using System;
using System.Collections.Generic;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Simple ToolManager to register and switch active tools.
    /// Will be extended with event routing and input forwarding.
    /// </summary>
    public class ToolManager
    {
        private readonly Dictionary<string, ITool> _tools = new();
        private ITool _activeTool;

        public IEnumerable<ITool> RegisteredTools => _tools.Values;

        public ITool ActiveTool => _activeTool;

        public event Action<ITool> ActiveToolChanged;

        public void RegisterTool(ITool tool)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            if (!_tools.ContainsKey(tool.Name)) _tools[tool.Name] = tool;
        }

        public bool Activate(string name)
        {
            if (!_tools.TryGetValue(name, out var tool)) return false;

            _activeTool?.Deactivate();
            _activeTool = tool;
            _activeTool.Activate();
            ActiveToolChanged?.Invoke(_activeTool);
            return true;
        }

        public void DeactivateActive()
        {
            if (_activeTool == null) return;
            _activeTool.Deactivate();
            _activeTool = null;
            ActiveToolChanged?.Invoke(null);
        }

        public void CancelActive()
        {
            _activeTool?.OnCancel();
        }

        // Forward input events to the active tool
        public void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            _activeTool?.OnMouseDown(e);
        }

        public void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            _activeTool?.OnMouseMove(e);
        }

        public void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            _activeTool?.OnMouseUp(e);
        }

        public void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            _activeTool?.OnKeyDown(e);
        }

        public void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            _activeTool?.OnKeyUp(e);
        }
    }
}
