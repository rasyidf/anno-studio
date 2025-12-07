using System;
using System.Windows;

namespace AnnoDesigner.Controls.EditorCanvas.Content.Models
{
    /// <summary>
    /// Minimal canvas object model for EditorCanvas scaffolding.
    /// Real layout objects (buildings, influences, icons) will extend this model.
    /// </summary>
    public class CanvasObject
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Rect Bounds { get; set; }

        public bool IsSelectable { get; set; } = true;

        public string Identifier { get; set; }

        /// <summary>
        /// Z-order index. Higher values are drawn on top and considered first in hit-tests.
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>Rotation in degrees applied around the object's center.</summary>
        public double RotationDegrees { get; set; } = 0;

        public CanvasObject Clone()
        {
            return new CanvasObject
            {
                Bounds = this.Bounds,
                IsSelectable = this.IsSelectable,
                Identifier = this.Identifier,
                ZIndex = this.ZIndex,
                RotationDegrees = this.RotationDegrees
            };
        }
    }
}
