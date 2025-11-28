using System;
using System.Collections.Generic;
using System.ComponentModel;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Represents an object that can be placed and manipulated on the canvas.
/// </summary>
public interface ICanvasObject : ISerializable, INotifyPropertyChanged
{
    /// <summary>
    /// Unique identifier for this object.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Type identifier for serialization/deserialization.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Display name of the object.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Bounding rectangle in canvas coordinates.
    /// </summary>
    SKRect Bounds { get; }

    /// <summary>
    /// 2D transformation (position, rotation, scale).
    /// </summary>
    Transform2D Transform { get; set; }

    /// <summary>
    /// Layer this object belongs to.
    /// </summary>
    string Layer { get; set; }

    /// <summary>
    /// Visibility flag.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Lock flag prevents modification.
    /// </summary>
    bool IsLocked { get; set; }

    /// <summary>
    /// Z-order within the layer.
    /// </summary>
    int ZOrder { get; set; }

    /// <summary>
    /// Test if a point intersects this object.
    /// </summary>
    bool HitTest(SKPoint point);

    /// <summary>
    /// Render the object to the canvas.
    /// </summary>
    void Render(SKCanvas canvas, RenderContext context);

    /// <summary>
    /// Create a deep copy of this object.
    /// </summary>
    ICanvasObject Clone();

    /// <summary>
    /// Get custom properties for serialization.
    /// </summary>
    Dictionary<string, object> GetProperties();

    /// <summary>
    /// Set custom properties from deserialization.
    /// </summary>
    void SetProperties(Dictionary<string, object> properties);
}
