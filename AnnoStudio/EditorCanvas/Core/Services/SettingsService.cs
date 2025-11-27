using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of settings management service.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly Dictionary<Type, object> _settings = new();
    private readonly Dictionary<Type, List<Action<object>>> _observers = new();
    private readonly string _settingsPath;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnnoStudio",
            "EditorCanvas",
            "settings.json"
        );

        EnsureDirectoryExists();
    }

    public T GetSettings<T>() where T : class, new()
    {
        var type = typeof(T);

        if (_settings.TryGetValue(type, out var cached))
        {
            return (T)cached;
        }

        // Try to load from disk
        var loaded = LoadFromDisk<T>();
        if (loaded != null)
        {
            _settings[type] = loaded;
            return loaded;
        }

        // Create new instance with defaults
        var instance = new T();
        _settings[type] = instance;
        SaveSettings(instance);
        return instance;
    }

    public void SaveSettings<T>(T settings) where T : class
    {
        var type = typeof(T);
        _settings[type] = settings;
        SaveToDisk(settings);

        // Notify observers
        if (_observers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(settings);
                }
                catch
                {
                    // Ignore observer exceptions
                }
            }
        }

        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
        {
            SettingsType = type,
            Settings = settings
        });
    }

    public void ResetToDefaults<T>() where T : class, new()
    {
        var type = typeof(T);
        var defaultSettings = new T();
        _settings[type] = defaultSettings;
        SaveToDisk(defaultSettings);

        if (_observers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(defaultSettings);
                }
                catch
                {
                    // Ignore observer exceptions
                }
            }
        }

        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
        {
            SettingsType = type,
            Settings = defaultSettings
        });
    }

    public IObservable<T> WatchSettings<T>() where T : class
    {
        var type = typeof(T);

        return new SettingsObservable<T>(observer =>
        {
            // Add observer to list
            if (!_observers.TryGetValue(type, out var handlers))
            {
                handlers = new List<Action<object>>();
                _observers[type] = handlers;
            }
            
            Action<object> handler = obj => observer.OnNext((T)obj);
            handlers.Add(handler);

            // Send current value immediately
            if (_settings.TryGetValue(type, out var current))
            {
                observer.OnNext((T)current);
            }

            // Return unsubscribe action
            return () => handlers.Remove(handler);
        });
    }

    public bool HasSettings<T>() where T : class
    {
        return _settings.ContainsKey(typeof(T)) || File.Exists(GetSettingsFilePath<T>());
    }

    private T? LoadFromDisk<T>() where T : class
    {
        var filePath = GetSettingsFilePath<T>();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });
        }
        catch
        {
            return null;
        }
    }

    private void SaveToDisk<T>(T settings) where T : class
    {
        var filePath = GetSettingsFilePath<T>();
        EnsureDirectoryExists();

        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Silently fail - settings will still be in memory
        }
    }

    private string GetSettingsFilePath<T>()
    {
        var fileName = $"{typeof(T).Name}.json";
        return Path.Combine(Path.GetDirectoryName(_settingsPath)!, fileName);
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    // Simple observable implementation
    private class SettingsObservable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, Action> _subscribe;

        public SettingsObservable(Func<IObserver<T>, Action> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var unsubscribe = _subscribe(observer);
            return new Unsubscriber(unsubscribe);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsubscribe;

            public Unsubscriber(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose() => _unsubscribe();
        }
    }
}
