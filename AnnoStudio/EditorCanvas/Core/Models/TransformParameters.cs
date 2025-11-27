using SkiaSharp;
using System.Collections.Generic;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Parameters for transform operations.
/// </summary>
public class TransformParameters
{
    /// <summary>
    /// Position delta for move operations.
    /// </summary>
    public SKPoint? DeltaPosition { get; set; }

    /// <summary>
    /// Rotation delta in degrees.
    /// </summary>
    public float? DeltaRotation { get; set; }

    /// <summary>
    /// Scale delta.
    /// </summary>
    public SKPoint? DeltaScale { get; set; }

    /// <summary>
    /// Pivot point for rotation and scaling.
    /// </summary>
    public SKPoint? Pivot { get; set; }

    /// <summary>
    /// Whether to snap to grid.
    /// </summary>
    public bool SnapToGrid { get; set; }

    /// <summary>
    /// Custom parameters for specific transforms.
    /// </summary>
    public Dictionary<string, object> CustomParameters { get; set; } = new();

    /// <summary>
    /// Get custom parameter value.
    /// </summary>
    public T? GetParameter<T>(string key)
    {
        if (CustomParameters.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }
        return default;
    }

    /// <summary>
    /// Set custom parameter value.
    /// </summary>
    public void SetParameter<T>(string key, T value)
    {
        if (value != null)
        {
            CustomParameters[key] = value;
        }
    }
}
