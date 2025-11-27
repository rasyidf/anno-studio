using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Base;

/// <summary>
/// Base class for canvas objects with common functionality.
/// </summary>
public abstract class CanvasObjectBase : ICanvasObject
{
    private string _name = string.Empty;
    private Transform2D _transform = Transform2D.Identity;
    private string _layer = "Default";
    private bool _isVisible = true;
    private bool _isLocked = false;
    private int _zOrder = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public abstract string Type { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public abstract SKRect Bounds { get; }

    public Transform2D Transform
    {
        get => _transform;
        set => SetProperty(ref _transform, value);
    }

    public string Layer
    {
        get => _layer;
        set => SetProperty(ref _layer, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public int ZOrder
    {
        get => _zOrder;
        set => SetProperty(ref _zOrder, value);
    }

    public abstract bool HitTest(SKPoint point);

    public abstract void Render(SKCanvas canvas, RenderContext context);

    public abstract ICanvasObject Clone();

    public virtual Dictionary<string, object> GetProperties()
    {
        return new Dictionary<string, object>
        {
            ["Id"] = Id.ToString(),
            ["Type"] = Type,
            ["Name"] = Name,
            ["Layer"] = Layer,
            ["IsVisible"] = IsVisible,
            ["IsLocked"] = IsLocked,
            ["ZOrder"] = ZOrder,
            ["Transform"] = new Dictionary<string, object>
            {
                ["PositionX"] = Transform.Position.X,
                ["PositionY"] = Transform.Position.Y,
                ["Rotation"] = Transform.Rotation,
                ["ScaleX"] = Transform.Scale.X,
                ["ScaleY"] = Transform.Scale.Y,
                ["PivotX"] = Transform.Pivot.X,
                ["PivotY"] = Transform.Pivot.Y
            }
        };
    }

    public virtual void SetProperties(Dictionary<string, object> properties)
    {
        if (properties.TryGetValue("Id", out var id) && id is string idStr)
            Id = Guid.Parse(idStr);

        if (properties.TryGetValue("Name", out var name))
            Name = name?.ToString() ?? string.Empty;

        if (properties.TryGetValue("Layer", out var layer))
            Layer = layer?.ToString() ?? "Default";

        if (properties.TryGetValue("IsVisible", out var isVisible))
            IsVisible = Convert.ToBoolean(isVisible);

        if (properties.TryGetValue("IsLocked", out var isLocked))
            IsLocked = Convert.ToBoolean(isLocked);

        if (properties.TryGetValue("ZOrder", out var zOrder))
            ZOrder = Convert.ToInt32(zOrder);

        if (properties.TryGetValue("Transform", out var transformObj) && 
            transformObj is Dictionary<string, object> transform)
        {
            Transform = new Transform2D
            {
                Position = new SKPoint(
                    Convert.ToSingle(transform.GetValueOrDefault("PositionX", 0f)),
                    Convert.ToSingle(transform.GetValueOrDefault("PositionY", 0f))
                ),
                Rotation = Convert.ToSingle(transform.GetValueOrDefault("Rotation", 0f)),
                Scale = new SKPoint(
                    Convert.ToSingle(transform.GetValueOrDefault("ScaleX", 1f)),
                    Convert.ToSingle(transform.GetValueOrDefault("ScaleY", 1f))
                ),
                Pivot = new SKPoint(
                    Convert.ToSingle(transform.GetValueOrDefault("PivotX", 0f)),
                    Convert.ToSingle(transform.GetValueOrDefault("PivotY", 0f))
                )
            };
        }
    }

    public virtual JsonElement Serialize()
    {
        var properties = GetProperties();
        var json = JsonSerializer.Serialize(properties);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public virtual void Deserialize(JsonElement element)
    {
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
        if (properties != null)
        {
            SetProperties(properties);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
