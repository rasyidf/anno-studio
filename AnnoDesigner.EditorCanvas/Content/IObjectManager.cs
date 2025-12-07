using System.Collections.Generic;

namespace AnnoDesigner.Controls.EditorCanvas.Content
{
    /// <summary>
    /// Generic object manager contract for EditorCanvas content layer.
    /// Implementations will manage storage, querying, and lifecycle of canvas objects.
    /// </summary>
    public interface IObjectManager<T>
    {
        /// <summary>
        /// Returns all objects that intersect the provided point (hit test).
        /// Implementations should return items ordered from top-most to bottom-most when relevant.
        /// </summary>
        System.Collections.Generic.IEnumerable<T> GetObjectsAt(System.Windows.Point point);

        IEnumerable<T> GetAll();
        void Add(T item);
        void Remove(T item);
        void Clear();
    }
}
