using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Manages context menus for canvas objects and the canvas itself.
/// </summary>
public class ContextMenuService
{
    private readonly List<ContextMenuAction> _objectActions = new();
    private readonly List<ContextMenuAction> _canvasActions = new();

    /// <summary>
    /// Registers a context menu action for canvas objects.
    /// </summary>
    public void RegisterObjectAction(string name, string header, Action<ICanvasObject, ICanvasContext> action, 
        Func<ICanvasObject, bool>? canExecute = null, string? icon = null)
    {
        _objectActions.Add(new ContextMenuAction
        {
            Name = name,
            Header = header,
            ObjectAction = action,
            CanExecuteForObject = canExecute,
            Icon = icon
        });
    }

    /// <summary>
    /// Registers a context menu action for the canvas.
    /// </summary>
    public void RegisterCanvasAction(string name, string header, Action<ICanvasContext> action,
        Func<ICanvasContext, bool>? canExecute = null, string? icon = null)
    {
        _canvasActions.Add(new ContextMenuAction
        {
            Name = name,
            Header = header,
            CanvasAction = action,
            CanExecuteForCanvas = canExecute,
            Icon = icon
        });
    }

    /// <summary>
    /// Unregisters an object action by name.
    /// </summary>
    public void UnregisterObjectAction(string name)
    {
        _objectActions.RemoveAll(a => a.Name == name);
    }

    /// <summary>
    /// Unregisters a canvas action by name.
    /// </summary>
    public void UnregisterCanvasAction(string name)
    {
        _canvasActions.RemoveAll(a => a.Name == name);
    }

    /// <summary>
    /// Builds a context menu for a canvas object.
    /// </summary>
    public ContextMenu BuildObjectContextMenu(ICanvasObject obj, ICanvasContext context)
    {
        var menu = new ContextMenu();
        var items = new List<object>();

        foreach (var action in _objectActions)
        {
            if (action.CanExecuteForObject?.Invoke(obj) ?? true)
            {
                var menuItem = new MenuItem
                {
                    Header = action.Header,
                    Command = new ActionCommand(() => action.ObjectAction?.Invoke(obj, context))
                };

                if (!string.IsNullOrEmpty(action.Icon))
                {
                    // TODO: Set icon from resource
                }

                items.Add(menuItem);
            }
        }

        menu.ItemsSource = items;
        return menu;
    }

    /// <summary>
    /// Builds a context menu for the canvas.
    /// </summary>
    public ContextMenu BuildCanvasContextMenu(ICanvasContext context)
    {
        var menu = new ContextMenu();
        var items = new List<object>();

        foreach (var action in _canvasActions)
        {
            if (action.CanExecuteForCanvas?.Invoke(context) ?? true)
            {
                var menuItem = new MenuItem
                {
                    Header = action.Header,
                    Command = new ActionCommand(() => action.CanvasAction?.Invoke(context))
                };

                if (!string.IsNullOrEmpty(action.Icon))
                {
                    // TODO: Set icon from resource
                }

                items.Add(menuItem);
            }
        }

        menu.ItemsSource = items;
        return menu;
    }

    /// <summary>
    /// Gets all registered object actions.
    /// </summary>
    public IReadOnlyList<ContextMenuAction> GetObjectActions() => _objectActions.AsReadOnly();

    /// <summary>
    /// Gets all registered canvas actions.
    /// </summary>
    public IReadOnlyList<ContextMenuAction> GetCanvasActions() => _canvasActions.AsReadOnly();

    /// <summary>
    /// Clears all registered actions.
    /// </summary>
    public void Clear()
    {
        _objectActions.Clear();
        _canvasActions.Clear();
    }
}

/// <summary>
/// Represents a context menu action.
/// </summary>
public class ContextMenuAction
{
    public required string Name { get; init; }
    public required string Header { get; init; }
    public string? Icon { get; init; }
    
    public Action<ICanvasObject, ICanvasContext>? ObjectAction { get; init; }
    public Func<ICanvasObject, bool>? CanExecuteForObject { get; init; }
    
    public Action<ICanvasContext>? CanvasAction { get; init; }
    public Func<ICanvasContext, bool>? CanExecuteForCanvas { get; init; }
}

/// <summary>
/// Simple command implementation for menu items.
/// </summary>
internal class ActionCommand : System.Windows.Input.ICommand
{
    private readonly Action _action;

    public ActionCommand(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _action();
}
