using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Represents a keyboard shortcut and its associated action.
/// </summary>
public class ShortcutAction
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required Action Action { get; init; }
    public required KeyGesture Gesture { get; init; }

    public override string ToString() => $"{Name} ({Gesture})";
}

/// <summary>
/// Manages keyboard shortcuts and their associated actions.
/// </summary>
public class KeyboardShortcutManager
{
    private readonly Dictionary<KeyGesture, ShortcutAction> _shortcuts = new();
    private readonly Dictionary<string, ShortcutAction> _namedShortcuts = new();

    /// <summary>
    /// Registers a keyboard shortcut with an action.
    /// </summary>
    public void RegisterShortcut(KeyGesture gesture, Action action, string name, string description = "")
    {
        var shortcutAction = new ShortcutAction
        {
            Name = name,
            Description = description,
            Action = action,
            Gesture = gesture
        };

        _shortcuts[gesture] = shortcutAction;
        _namedShortcuts[name] = shortcutAction;
    }

    /// <summary>
    /// Registers a keyboard shortcut with an action.
    /// </summary>
    public void RegisterShortcut(Key key, Action action, string name, string description = "")
    {
        RegisterShortcut(new KeyGesture(key), action, name, description);
    }

    /// <summary>
    /// Registers a keyboard shortcut with modifiers.
    /// </summary>
    public void RegisterShortcut(Key key, KeyModifiers modifiers, Action action, string name, string description = "")
    {
        RegisterShortcut(new KeyGesture(key, modifiers), action, name, description);
    }

    /// <summary>
    /// Unregisters a shortcut by gesture.
    /// </summary>
    public void UnregisterShortcut(KeyGesture gesture)
    {
        if (_shortcuts.TryGetValue(gesture, out var action))
        {
            _shortcuts.Remove(gesture);
            _namedShortcuts.Remove(action.Name);
        }
    }

    /// <summary>
    /// Unregisters a shortcut by name.
    /// </summary>
    public void UnregisterShortcut(string name)
    {
        if (_namedShortcuts.TryGetValue(name, out var action))
        {
            _shortcuts.Remove(action.Gesture);
            _namedShortcuts.Remove(name);
        }
    }

    /// <summary>
    /// Handles a key down event and executes matching shortcut.
    /// </summary>
    /// <returns>True if a shortcut was executed.</returns>
    public bool HandleKeyDown(KeyEventArgs e)
    {
        var gesture = new KeyGesture(e.Key, e.KeyModifiers);

        if (_shortcuts.TryGetValue(gesture, out var action))
        {
            action.Action?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    public IReadOnlyList<ShortcutAction> GetAllShortcuts()
    {
        return _shortcuts.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a shortcut by name.
    /// </summary>
    public ShortcutAction? GetShortcut(string name)
    {
        return _namedShortcuts.TryGetValue(name, out var action) ? action : null;
    }

    /// <summary>
    /// Clears all registered shortcuts.
    /// </summary>
    public void Clear()
    {
        _shortcuts.Clear();
        _namedShortcuts.Clear();
    }
}
