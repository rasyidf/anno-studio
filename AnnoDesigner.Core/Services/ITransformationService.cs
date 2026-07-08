using System.Collections.Generic;

namespace AnnoDesigner.Core.Services
{
    /// <summary>
    /// Provides layout transformation operations that act on a selection or group of canvas items.
    /// Implementations should operate on model objects only and avoid UI concerns.
    /// </summary>
    public interface ITransformationService
    {
        /// <summary>
        /// Aligns the given items according to <paramref name="mode"/>.
        /// </summary>
        void Align(IEnumerable<object> items, Models.AlignmentMode mode);

        /// <summary>
        /// Distributes the given items horizontally or vertically.
        /// </summary>
        void Distribute(IEnumerable<object> items, Models.DistributionMode mode);


        /// <summary>
        /// Rotates the given items by a single step in the provided direction.
        /// </summary>
        void Rotate(IEnumerable<object> items, Models.RotationDirection direction);

        /// <summary>
        /// Flips the given items horizontally or vertically.
        /// </summary>
        void Flip(IEnumerable<object> items, Models.FlipDirection direction);


    }
}
