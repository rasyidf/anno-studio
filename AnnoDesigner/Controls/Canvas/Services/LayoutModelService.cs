using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class LayoutModelService : ILayoutModelService
    {
        private readonly ICoordinateHelper _coordinateHelper;
        private readonly StatisticsCalculationHelper _statisticsCalculationHelper;
        private readonly List<LayoutObject> _items = new();
        public QuadTree<LayoutObject> PlacedObjects { get; } = new QuadTree<LayoutObject>(new Rect(-10000, -10000, 20000, 20000));

        public LayoutModelService(ICoordinateHelper coordinateHelper, StatisticsCalculationHelper statisticsCalculationHelper)
        {
            _coordinateHelper = coordinateHelper ?? throw new System.ArgumentNullException(nameof(coordinateHelper));
            _statisticsCalculationHelper = statisticsCalculationHelper ?? throw new System.ArgumentNullException(nameof(statisticsCalculationHelper));
        }

        public IReadOnlyList<LayoutObject> Items => _items;

        public void AddItem(LayoutObject item)
        {
            _items.Add(item);
            PlacedObjects.Add(item);
        }

        public void RemoveItem(LayoutObject item)
        {
            _items.Remove(item);
            PlacedObjects.Remove(item);
        }

        public void AddRange(IEnumerable<LayoutObject> items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        public void RemoveRange(IEnumerable<LayoutObject> items)
        {
            foreach (var item in items)
            {
                RemoveItem(item);
            }
        }

        public void Clear()
        {
            _items.Clear();
            PlacedObjects.Clear();
        }

        /// <summary>
        /// Checks if there is a collision between given objects a and b.
        /// </summary>
        public static bool ObjectIntersectionExists(LayoutObject a, LayoutObject b)
        {
            return a.CollisionRect.IntersectsWith(b.CollisionRect);
        }

        /// <summary>
        /// Checks if there is a collision between a list of AnnoObjects a and object b.
        /// </summary>
        public static bool ObjectIntersectionExists(IEnumerable<LayoutObject> a, LayoutObject b)
        {
            return a.Any(_ => _.CollisionRect.IntersectsWith(b.CollisionRect));
        }

        /// <summary>
        /// Tries to place current objects on the grid.
        /// Returns the objects that can be placed without collision.
        /// </summary>
        /// <param name="currentObjects">The objects to place.</param>
        /// <param name="forcePlacement"><c>true</c> to force placement even with collisions</param>
        /// <returns>The objects that can be placed.</returns>
        public List<LayoutObject> GetObjectsToPlace(IEnumerable<LayoutObject> currentObjects, bool forcePlacement)
        {
            if (!currentObjects.Any())
            {
                return [];
            }

            var boundingRect = ComputeBoundingRect(currentObjects);
            var relevantPlacedObjects = PlacedObjects.GetItemsIntersecting(boundingRect);
            var intersectingCurrentObjects = currentObjects.Where(x => ObjectIntersectionExists(relevantPlacedObjects, x));

            if (forcePlacement || !intersectingCurrentObjects.Any())
            {
                return CloneLayoutObjects(currentObjects.Except(intersectingCurrentObjects), currentObjects.Count());
            }

            return [];
        }

        /// <summary>
        /// Retrieves the object at the given position given in screen coordinates.
        /// </summary>
        /// <param name="position">position given in screen coordinates</param>
        /// <param name="gridSize">The grid size.</param>
        /// <param name="viewport">The viewport.</param>
        /// <returns>object at the position, <see langword="null"/> if no object could be found</returns>
        public LayoutObject GetObjectAt(Point position, int gridSize, Viewport viewport)
        {
            if (PlacedObjects.Count == 0)
            {
                return null;
            }

            var gridPosition = _coordinateHelper.ScreenToFractionalGrid(position, gridSize);
            gridPosition = viewport.OriginToViewport(gridPosition);
            var possibleItems = PlacedObjects.GetItemsIntersecting(new Rect(gridPosition, new Size(1, 1)));
            foreach (var curItem in possibleItems)
            {
                if (curItem.GridRect.Contains(gridPosition))
                {
                    return curItem;
                }
            }

            return null;
        }

        /// <summary>
        /// Computes a <see cref="Rect"/> that encompasses the given objects.
        /// </summary>
        /// <param name="objects">The collection of <see cref="LayoutObject"/> to compute the bounding <see cref="Rect"/> for.</param>
        /// <returns>The <see cref="Rect"/> that encompasses all <paramref name="objects"/>.</returns>
        public Rect ComputeBoundingRect(IEnumerable<LayoutObject> objects)
        {
            //make sure to include ALL objects (e.g. roads and ignored objetcs)
            return (Rect)_statisticsCalculationHelper.CalculateStatistics(objects.Select(_ => _.WrappedAnnoObject), includeRoads: true, includeIgnoredObjects: true);
        }

        private List<LayoutObject> CloneLayoutObjects(IEnumerable<LayoutObject> list, int capacity)
        {
            return list.Select(x => new LayoutObject(new AnnoObject(x.WrappedAnnoObject), _coordinateHelper, x.BrushCache, x.PenCache)).ToListWithCapacity(capacity);
        }
    }
}
