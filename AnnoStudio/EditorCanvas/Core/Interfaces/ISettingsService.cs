using System;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Manages application settings and preferences.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get settings of specific type.
    /// </summary>
    T GetSettings<T>() where T : class, new();

    /// <summary>
    /// Save settings of specific type.
    /// </summary>
    void SaveSettings<T>(T settings) where T : class;

    /// <summary>
    /// Reset settings to defaults.
    /// </summary>
    void ResetToDefaults<T>() where T : class, new();

    /// <summary>
    /// Watch for settings changes.
    /// </summary>
    IObservable<T> WatchSettings<T>() where T : class;

    /// <summary>
    /// Check if settings exist.
    /// </summary>
    bool HasSettings<T>() where T : class;

    /// <summary>
    /// Event raised when any settings change.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Event arguments for settings changes.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public Type SettingsType { get; init; } = typeof(object);
    public object? Settings { get; init; }
}
