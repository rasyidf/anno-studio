using System;
using System.Collections.Generic;
using System.Linq;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class SelectionService : ISelectionService
    {
        private readonly LayoutModelService _layoutModelService;
        public HashSet<LayoutObject> SelectedObjects { get; } = new();

        public SelectionService(LayoutModelService layoutModelService)
        {
            _layoutModelService = layoutModelService ?? throw new ArgumentNullException(nameof(layoutModelService));
        }

        public IEnumerable<LayoutObject> SelectedItems => SelectedObjects;

        public void ClearSelection() => SelectedObjects.Clear();

        /// <summary>
        /// Add the objects to SelectedObjects, optionally also add all objects which match one of their identifiers.
        /// </summary>
        /// <param name="includeSameObjects"> 
        /// If <see langword="true"/> then apply to objects whose identifier matches one of those in <see href="objectsToAdd"/>.
        /// </param>
        public void AddSelectedObjects(IEnumerable<LayoutObject> objectsToAdd, bool includeSameObjects)
        {
            if (includeSameObjects)
            {
                // Add all placed objects whose identifier matches any of those in the objectsToAdd.
                SelectedObjects.UnionWith(_layoutModelService.PlacedObjects.Where(placed => objectsToAdd.Any(toAdd => toAdd.Identifier.Equals(placed.Identifier, StringComparison.OrdinalIgnoreCase))));
            }
            else
            {
                SelectedObjects.UnionWith(objectsToAdd);
            }
        }

        /// <summary>
        /// Remove the objects from SelectedObjects, optionally also remove all objects which match one of their identifiers.
        /// </summary>
        /// <param name="includeSameObjects"> 
        /// If <see langword="true"/> then apply to objects whose identifier matches one of those in <see href="objectsToRemove"/>.
        /// </param>
        public void RemoveSelectedObjects(IEnumerable<LayoutObject> objectsToRemove, bool includeSameObjects)
        {
            if (includeSameObjects)
            {
                // Exclude any selected objects whose identifier matches any of those in the objectsToRemove.
                _ = SelectedObjects.RemoveWhere(placed => objectsToRemove.Any(toRemove => toRemove.Identifier.Equals(placed.Identifier, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                SelectedObjects.ExceptWith(objectsToRemove);
            }
        }

        /// <summary>
        /// Remove the objects from SelectedObjects which match specified predicate.
        /// </summary>
        public void RemoveSelectedObjects(Predicate<LayoutObject> predicate)
        {
            _ = SelectedObjects.RemoveWhere(predicate);
        }

        /// <summary>
        /// Add a single object to SelectedObjects, optionally also add all objects with the same identifier.
        /// </summary>
        /// <param name="includeSameObjects"> 
        /// If <see langword="true"/> then apply to objects whose identifier match that of <see href="objectToAdd"/>.
        /// </param>
        public void AddSelectedObject(LayoutObject objectToAdd, bool includeSameObjects = false)
        {
            AddSelectedObjects([objectToAdd], includeSameObjects);
        }

        /// <summary>
        /// Remove a single object from SelectedObjects, optionally also remove all objects with the same identifier.
        /// </summary>
        /// <param name="includeSameObjects"> 
        /// If <see langword="true"/> then apply to objects whose identifier match that of <see href="objectToRemove"/>.
        /// </param>
        public void RemoveSelectedObject(LayoutObject objectToRemove, bool includeSameObjects = false)
        {
            RemoveSelectedObjects([objectToRemove], includeSameObjects);
        }
    }
}
