using System;
using System.Collections.Generic;
using System.Linq;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of tool registry service.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IEditorTool> _tools = new();
    private IEditorTool? _activeTool;
    private ICanvasContext? _context;

    public event EventHandler<ToolChangedEventArgs>? ActiveToolChanged;

    public IEditorTool? ActiveTool => _activeTool;

    public void RegisterTool<T>() where T : IEditorTool, new()
    {
        var tool = new T();
        RegisterTool(tool);
    }

    public void RegisterTool(IEditorTool tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new ArgumentException("Tool name cannot be empty.", nameof(tool));
        }

        _tools[tool.Name] = tool;
    }

    public void UnregisterTool(string toolName)
    {
        if (_tools.ContainsKey(toolName))
        {
            // Deactivate if this is the active tool
            if (_activeTool?.Name == toolName)
            {
                SetActiveTool(null);
            }

            _tools.Remove(toolName);
        }
    }

    public IEditorTool? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    public IEnumerable<IEditorTool> GetAllTools()
    {
        return _tools.Values.ToList();
    }

    public void SetActiveTool(string? name)
    {
        IEditorTool? newTool = null;

        if (!string.IsNullOrWhiteSpace(name))
        {
            newTool = GetTool(name);
            if (newTool == null)
            {
                throw new ArgumentException($"Tool '{name}' is not registered.", nameof(name));
            }
        }

        if (_activeTool == newTool)
        {
            return;
        }

        var oldTool = _activeTool;

        // Deactivate current tool
        if (_activeTool != null && _context != null)
        {
            _activeTool.OnDeactivated(_context);
        }

        _activeTool = newTool;

        // Activate new tool
        if (_activeTool != null && _context != null)
        {
            _activeTool.OnActivated(_context);
        }

        ActiveToolChanged?.Invoke(this, new ToolChangedEventArgs
        {
            OldTool = oldTool,
            NewTool = newTool
        });
    }

    /// <summary>
    /// Set canvas context for tool activation/deactivation.
    /// </summary>
    public void SetContext(ICanvasContext context)
    {
        _context = context;
    }
}
