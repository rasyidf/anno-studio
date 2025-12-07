using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using CommunityToolkit.HighPerformance;

namespace AnnoDesigner.Controls.EditorCanvas.Content
{
    /// <summary>
    /// Grid-backed spatial index using a Span2D-backed bucket array for improved manipulation performance.
    /// This is a dynamic grid: it grows when objects fall outside current extents.
    /// </summary>
    public class ObjectManagerQuadTree : IObjectManager<CanvasObject>
    {
        private readonly List<CanvasObject> _items = new();

        // grid config
        private int _cellSize = 128; // cell size in device units
        private int _cols = 16;
        private int _rows = 16;
        private double _originX = 0;
        private double _originY = 0;

        // 2D backing array for buckets
        private List<CanvasObject>[,] _buckets2d;

        public ObjectManagerQuadTree()
        {
            _buckets2d = new List<CanvasObject>[_rows, _cols];
            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    _buckets2d[y, x] = new List<CanvasObject>();
                }
            }
        }

        public void Add(CanvasObject item)
        {
            if (item == null) return;
            _items.Add(item);
            EnsureBucketForItem(item);
            var (cx, cy) = GetCellForPoint(new Point(item.Bounds.X + item.Bounds.Width / 2, item.Bounds.Y + item.Bounds.Height / 2));
            if (cx >= 0 && cx < _cols && cy >= 0 && cy < _rows)
            {
                Span2D<List<CanvasObject>> grid = _buckets2d;
                grid[cy, cx].Add(item);
            }
        }

        public void Clear()
        {
            _items.Clear();
            Span2D<List<CanvasObject>> grid = _buckets2d;
            for (int y = 0; y < _rows; y++)
                for (int x = 0; x < _cols; x++)
                    grid[y, x].Clear();
        }

        public IEnumerable<CanvasObject> GetAll()
        {
            return _items;
        }

        public void Remove(CanvasObject item)
        {
            if (item == null) return;
            _items.Remove(item);
            // remove from its bucket if present
            var (cx, cy) = GetCellForPoint(new Point(item.Bounds.X + item.Bounds.Width / 2, item.Bounds.Y + item.Bounds.Height / 2));
            if (cx >= 0 && cx < _cols && cy >= 0 && cy < _rows)
            {
                Span2D<List<CanvasObject>> grid = _buckets2d;
                grid[cy, cx].Remove(item);
            }
        }

        public IEnumerable<CanvasObject> GetObjectsAt(Point point)
        {
            var hits = new List<CanvasObject>();

            var (cx, cy) = GetCellForPoint(point);
            if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows)
            {
                // outside grid — fall back to brute force
                foreach (var item in _items)
                {
                    if (item != null && item.IsSelectable && item.Bounds.Contains(point)) hits.Add(item);
                }
            }
            else
            {
                Span2D<List<CanvasObject>> grid = _buckets2d;
                // check bucket and its immediate neighbors for safety
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = cx + ox;
                        int ny = cy + oy;
                        if (nx < 0 || nx >= _cols || ny < 0 || ny >= _rows) continue;
                        var bucket = grid[ny, nx];
                        foreach (var item in bucket)
                        {
                            if (item != null && item.IsSelectable && item.Bounds.Contains(point)) hits.Add(item);
                        }
                    }
                }
            }

            // order by ZIndex descending (top-most first)
            return hits.OrderByDescending(h => h.ZIndex);
        }

        private void EnsureBucketForItem(CanvasObject item)
        {
            // Expand grid if item is outside current bounds
            var center = new Point(item.Bounds.X + item.Bounds.Width / 2, item.Bounds.Y + item.Bounds.Height / 2);
            var (cx, cy) = GetCellForPoint(center);
            if (cx >= 0 && cx < _cols && cy >= 0 && cy < _rows) return;

            // determine required bounds
            int newCols = _cols;
            int newRows = _rows;
            while (cx < 0 || cx >= newCols) newCols *= 2;
            while (cy < 0 || cy >= newRows) newRows *= 2;

            // create new 2D buckets
            var newBuckets2d = new List<CanvasObject>[newRows, newCols];
            for (int y = 0; y < newRows; y++)
            {
                for (int x = 0; x < newCols; x++)
                {
                    newBuckets2d[y, x] = new List<CanvasObject>();
                }
            }

            // place items into the new grid using a local Span2D for indexing
            Span2D<List<CanvasObject>> newGrid = newBuckets2d;
            foreach (var it in _items)
            {
                if (it == null) continue;
                var (tcx, tcy) = GetCellForPoint(new Point(it.Bounds.X + it.Bounds.Width / 2, it.Bounds.Y + it.Bounds.Height / 2));
                if (tcx >= 0 && tcx < newCols && tcy >= 0 && tcy < newRows)
                {
                    newGrid[tcy, tcx].Add(it);
                }
            }

            _cols = newCols;
            _rows = newRows;
            _buckets2d = newBuckets2d;
        }

        // No GetBucket helper — use the 2D array and Span2D locally.

        private (int cx, int cy) GetCellForPoint(Point pt)
        {
            int cx = (int)Math.Floor((pt.X - _originX) / _cellSize);
            int cy = (int)Math.Floor((pt.Y - _originY) / _cellSize);
            return (cx, cy);
        }
    }
}
