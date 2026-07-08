using System;
using System.Windows;

namespace AnnoDesigner.Controls.EditorCanvas.Content.Models
{
    /// <summary>
    /// Extensible contract for objects managed by the EditorCanvas content layer.
    /// Implement this interface to introduce domain-specific canvas object types.
    /// </summary>
    public interface ICanvasObject
    {
        Guid Id { get; }

        Rect Bounds { get; set; }

        int ZIndex { get; set; }

        bool IsSelectable { get; set; }

        string Identifier { get; set; }

        double RotationDegrees { get; set; }

        /// <summary>
        /// Arbitrary domain-specific data attached to the canvas object.
        /// </summary>
        object? Tag { get; set; }
    }
}
