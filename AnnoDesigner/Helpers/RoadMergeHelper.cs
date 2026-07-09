using System.Linq;
using System.Windows;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services.Undo.Operations;

namespace AnnoDesigner.Helper
{
    public static class RoadMergeHelper
    {
        public static void MergeRoads(IAnnoCanvas canvas, IAdjacentCellGrouper grouper, ICoordinateHelper coordinateHelper, IBrushCache brushCache, IPenCache penCache)
        {
            var roadColorGroups = canvas.PlacedObjects.Where(p => p.WrappedAnnoObject.Road)
                .GroupBy(p => (p.WrappedAnnoObject.Borderless, p.Color));
            foreach (var roadColorGroup in roadColorGroups)
            {
                if (roadColorGroup.Count() <= 1)
                {
                    continue;
                }

                var bounds =
                    (Rect)new StatisticsCalculationHelper().CalculateStatistics(
                        roadColorGroup.Select(p => p.WrappedAnnoObject));

                var cells = Enumerable.Range(0, (int)bounds.Width).Select(_ => new LayoutObject[(int)bounds.Height])
                    .ToArray();
                foreach (var item in roadColorGroup)
                {
                    for (var i = 0; i < item.Size.Width; i++)
                    {
                        for (var j = 0; j < item.Size.Height; j++)
                        {
                            cells[(int)(item.Position.X + i - bounds.Left)][(int)(item.Position.Y + j - bounds.Top)] =
                                item;
                        }
                    }
                }

                var groups = grouper.GroupAdjacentCells(cells).ToList();
                canvas.UndoManager.AsSingleUndoableOperation(() =>
                {
                    var oldObjects = groups.SelectMany(g => g.Items).ToList();
                    foreach (var item in oldObjects)
                    {
                        _ = canvas.PlacedObjects.Remove(item);
                    }

                    var newObjects = groups
                        .Select(g => new LayoutObject(
                            new AnnoObject(g.Items.First().WrappedAnnoObject)
                            {
                                Position = g.Bounds.TopLeft + (Vector)bounds.TopLeft,
                                Size = g.Bounds.Size
                            },
                            coordinateHelper,
                            brushCache,
                            penCache
                        ))
                        .ToList();
                    canvas.PlacedObjects.AddRange(newObjects);

                    canvas.UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
                    {
                        Objects = oldObjects,
                        Collection = canvas.PlacedObjects
                    });
                    canvas.UndoManager.RegisterOperation(new AddObjectsOperation<LayoutObject>()
                    {
                        Objects = newObjects,
                        Collection = canvas.PlacedObjects
                    });
                });
            }
        }
    }
}
