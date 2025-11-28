using System;
using System.Collections.Generic;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Manages tool registration and activation.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool type.
    /// </summary>
    void RegisterTool<T>() where T : IEditorTool, new();

    /// <summary>
    /// Register a tool instance.
    /// </summary>
    void RegisterTool(IEditorTool tool);

    /// <summary>
    /// Unregister a tool.
    /// </summary>
    void UnregisterTool(string toolName);

    /// <summary>
    /// Get tool by name.
    /// </summary>
    IEditorTool? GetTool(string name);

    /// <summary>
    /// Get all registered tools.
    /// </summary>
    IEnumerable<IEditorTool> GetAllTools();

    /// <summary>
    /// Currently active tool.
    /// </summary>
    IEditorTool? ActiveTool { get; }

    /// <summary>
    /// Set active tool by name.
    /// </summary>
    void SetActiveTool(string name);

    /// <summary>
    /// Event raised when active tool changes.
    /// </summary>
    event EventHandler<ToolChangedEventArgs>? ActiveToolChanged;
}

/// <summary>
/// Event arguments for tool change events.
/// </summary>
public class ToolChangedEventArgs : EventArgs
{
    public IEditorTool? OldTool { get; init; }
    public IEditorTool? NewTool { get; init; }
}
