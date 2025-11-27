using SkiaSharp;
using System;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Represents a 2D transformation (position, rotation, scale).
/// </summary>
public struct Transform2D : IEquatable<Transform2D>
{
    /// <summary>
    /// Position offset.
    /// </summary>
    public SKPoint Position { get; set; }

    /// <summary>
    /// Rotation in degrees.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Scale factors.
    /// </summary>
    public SKPoint Scale { get; set; }

    /// <summary>
    /// Pivot point for rotation and scale (local coordinates).
    /// </summary>
    public SKPoint Pivot { get; set; }

    /// <summary>
    /// Creates a new identity transform.
    /// </summary>
    public static Transform2D Identity => new()
    {
        Position = SKPoint.Empty,
        Rotation = 0,
        Scale = new SKPoint(1, 1),
        Pivot = SKPoint.Empty
    };

    /// <summary>
    /// Convert to SKMatrix for rendering.
    /// </summary>
    public SKMatrix ToMatrix()
    {
        var matrix = SKMatrix.Identity;

        // Apply transformations in order: Scale -> Rotate -> Translate
        if (Pivot != SKPoint.Empty)
        {
            matrix = SKMatrix.CreateTranslation(-Pivot.X, -Pivot.Y);
        }

        if (Scale.X != 1 || Scale.Y != 1)
        {
            matrix = matrix.PostConcat(SKMatrix.CreateScale(Scale.X, Scale.Y));
        }

        if (Rotation != 0)
        {
            matrix = matrix.PostConcat(SKMatrix.CreateRotationDegrees(Rotation));
        }

        if (Pivot != SKPoint.Empty)
        {
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(Pivot.X, Pivot.Y));
        }

        if (Position != SKPoint.Empty)
        {
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(Position.X, Position.Y));
        }

        return matrix;
    }

    /// <summary>
    /// Get inverse transform.
    /// </summary>
    public Transform2D Inverse()
    {
        var matrix = ToMatrix();
        matrix.TryInvert(out var inverted);

        // Extract components from inverted matrix
        return new Transform2D
        {
            Position = new SKPoint(inverted.TransX, inverted.TransY),
            Rotation = -Rotation,
            Scale = new SKPoint(1 / Scale.X, 1 / Scale.Y),
            Pivot = Pivot
        };
    }

    /// <summary>
    /// Transform a point using this transformation.
    /// </summary>
    public SKPoint TransformPoint(SKPoint point)
    {
        var matrix = ToMatrix();
        return matrix.MapPoint(point);
    }

    /// <summary>
    /// Transform a rectangle using this transformation.
    /// </summary>
    public SKRect TransformRect(SKRect rect)
    {
        var matrix = ToMatrix();
        return matrix.MapRect(rect);
    }

    public bool Equals(Transform2D other)
    {
        return Position.Equals(other.Position) &&
               Rotation.Equals(other.Rotation) &&
               Scale.Equals(other.Scale) &&
               Pivot.Equals(other.Pivot);
    }

    public override bool Equals(object? obj)
    {
        return obj is Transform2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Rotation, Scale, Pivot);
    }

    public static bool operator ==(Transform2D left, Transform2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Transform2D left, Transform2D right)
    {
        return !left.Equals(right);
    }
}
