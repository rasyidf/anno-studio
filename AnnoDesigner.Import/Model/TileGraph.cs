using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Gamedata;

namespace AnnoDesigner.Import.Model
{
    internal class TileGraph
    {
        public enum Direction
        {
            E  = 0,   // 0
            SE = 45,  // 45
            S  = 90,  // 90
            SW = 135, // 135
            W  = 180, // 180 or -180
            NW = 225, // 225 or -45
            N  = 270, // 270 or -90
            NE = 315  // 315 or -135
        }

        public class Edge
        {
            public int Length { get; private set; }
            public Tile Start { get; private set; }
            public Tile End { get; private set; }

            public IEnumerable<Tile> Tiles
            {
                get
                {
                    for (Tile tile = Start; tile != null; tile = tile.Next)
                    {
                        yield return tile;
                    }
                }
            }

            public void Add(Tile tile)
            {
                if (Start == null)
                {
                    this.Start = tile;
                    this.End = tile;
                }
                else
                {
                    this.End.Next = tile;
                    tile.Previous = End;
                    this.End = tile;
                }

                Length++;
            }
        }

        public interface TileResult
        {
            int Guid { get; }

#if DEBUG
            SerializableColor? Color { get; }
            string Label { get; }
#endif

            Point2D<float> Position { get; }
            byte Quadrants { get; }
            double Rotation { get; }
        }

        public class Tile : TileResult
        {
            public Tile(int guid, Edge edge, Point2D<float> position, double rotation)
            {
                this.RotationDegrees = (int)Math.Round(rotation * 180 / Math.PI);
                this.IsDiagonal = (RotationDegrees + 45) % 90 == 0;
                this.Edge = edge;

                this.Guid = guid;
                this.Position = position;
                this.Rotation = rotation;
                this.Quadrants = 0b1111;
            }

            public Edge Edge { get; }
            public Tile Previous { get; set; }
            public Tile Next { get; set; }

            public bool IsStart => Edge.Start == this;
            public bool IsEnd => Edge.End == this;

            public bool IsDiagonal { get; }
            public bool IsOrthogonal => !IsDiagonal;

#if DEBUG
            public SerializableColor? Color { get; set; }
            public string Label { get; set; }
#endif

            public int Guid { get; }
            public Point2D<float> Position { get; }
            public byte Quadrants { get; private set; }

            public double Rotation { get; }
            internal int RotationDegrees { get; }

            public Direction GetDirection()
            {
                int angle = GetPositiveAngle();
                return Enum.IsDefined(typeof(Direction), angle) ? (Direction)angle : throw new InvalidOperationException($"Angle {angle} does not correspond to a valid {nameof(Direction)}");
            }

            public int GetNormalizedAngle()
            {
                int result = this.RotationDegrees;
                if (!this.IsStart) result = (result + 180) % 360;
                if (result > 180) result -= 360;
                return result;
            }

            public int GetPositiveAngle()
            {
                int angle = GetNormalizedAngle();
                if (angle < 0) angle += 360;
                return angle;
            }

            public int GetRelativeAngle(Tile other)
            {
                int result = other.GetNormalizedAngle() - this.GetNormalizedAngle();
                if (result > 180) result -= 360;
                if (result < -180) result += 360;
                return result;
            }

            public void Merge(List<Tile> tiles)
            {
                Merge(CalculateQuadrants(tiles));

#if DEBUG
                this.Label = string.Join("+", tiles.Select(t => t.GetDirection())
                    .Select(direction => direction.ToString())
                    .Order());
#endif

                foreach (Tile other in tiles.Where(t => t != this))
                {
                    // merge any previous modifications into this and zero out others
                    if (other.Quadrants != 0b1111) Merge(other.Quadrants);
                    other.Quadrants = 0b0000;
                }

                ModifyAdjacent(tiles);
            }

            private void Merge(byte quadrants)
            {
                if (this.IsOrthogonal) this.Quadrants &= quadrants; // orthogonals might have previous modifications, so we need to merge quadrants
                else this.Quadrants = quadrants; // diagonals cannot have previous modifications, so we can just set quadrants
            }

            internal byte CalculateQuadrants(List<Tile> tiles)
            {
                if (tiles.Count < 2) throw new ArgumentException("At least two tiles required!", nameof(tiles));
                int countDiagonals = tiles.Count(t => t.IsDiagonal);
                
                if (countDiagonals == 0)
                {
                    // all orthogonal
                    return 0b1111;
                }
                
                if (this.IsDiagonal)
                {
                    List<Tile> others = tiles.Where(t => t != this).ToList();

                    if (this.HasOppositeDiagonal(others) || countDiagonals >= 3)
                    {
                        return 0b1111;
                    }
                    else if (this.HasPerpendicularDiagonal(others, out int relativeAngle))
                    {
                        return Special((relativeAngle, this.IsStart) switch
                        {
                            ( 90, false) => 0b1110,
                            (-90, false) => 0b0111,
                            ( 90, true)  => 0b1011,
                            (-90, true)  => 0b1101,
                            _ => throw new InvalidOperationException($"Unsupported perpendicular case! (Angle={relativeAngle}, IsStart={this.IsStart})")
                        });
                    }

                    // mixed orthogonal/diagonal
                    return this.IsStart ? Special(0b0011) : Special(0b1100);
                }

#if DEBUG
                this.Color = new SerializableColor(255, 255, 0, 0);
#endif

                // fallback (should not happen)
                return 0b1111;
            }

            internal Dictionary<Tile, byte> ModifyAdjacent(List<Tile> tiles)
            {
                Dictionary<Tile, byte> result = new Dictionary<Tile, byte>();
                List<Tile> orthogonalTiles = tiles.Where(t => t.IsOrthogonal).ToList();
                if (orthogonalTiles.Count == 0) return result; // pure diagonal intersection, no modifications

                List<Tile> diagonalTiles = tiles.Where(t => t.IsDiagonal).ToList();
                if (diagonalTiles.Count == 0) return result; // pure orthogonal intersection, no modifications

                // mixed intersection, orthogonals get modifications
                foreach (Tile orthogonal in orthogonalTiles)
                {
                    List<Tile> adjacentDiagonals = diagonalTiles.Where(diagonal =>
                    {
                        int relativeAngle = Math.Abs(orthogonal.GetRelativeAngle(diagonal));
                        return relativeAngle == 45 || relativeAngle == 315;
                    }).ToList();
                    
                    if (adjacentDiagonals.Count > 0)
                    {
                        if (adjacentDiagonals.Count > 2) throw new InvalidOperationException($"Unsupported adjacent diagonals! (Count={adjacentDiagonals.Count})");
                        Tile adjacentTile = orthogonal.IsStart ? orthogonal.Next : orthogonal.Previous;

                        if (adjacentTile != null)
                        {
                            int orthogonalAngle = orthogonal.GetPositiveAngle();
                            byte quadrants = 0b1111;

                            foreach (Tile diagonal in adjacentDiagonals)
                            {
                                int diagonalAngle = diagonal.GetPositiveAngle();

                                quadrants &= (orthogonalAngle, diagonalAngle) switch
                                {
                                    (  0, 315) => 0b0011, // [1] E + NE => Bottom-Left half (diagonal)
                                    (  0,  45) => 0b1001, // [1] E + SE => Top-Left half (diagonal)
                                                          // [2] E + NE + SE => 0b0011 & 0b1001 = 0b0001 => Left triangle

                                    ( 90,  45) => 0b0110, // [1] S + SE => Bottom-Right half (diagonal)
                                    ( 90, 135) => 0b0011, // [1] S + SW => Bottom-Left half (diagonal)
                                                          // [2] S + SE + SW => 0b0110 & 0b0011 = 0b0010 => Bottom triangle

                                    (180, 135) => 0b1100, // [1] W + SW => Top-Right half (diagonal)
                                    (180, 225) => 0b0110, // [1] W + NW => Bottom-Right half (diagonal)
                                                          // [2] W + SW + NW => 0b1100 & 0b0110 = 0b0100 => Right triangle

                                    (270, 315) => 0b1100, // [1] N + NE => Top-Right half (diagonal)
                                    (270, 225) => 0b1001, // [1] N + NW => Top-Left half (diagonal)
                                                          // [2] N + NE + NW => 0b1100 & 0b1001 = 0b1000 => Top triangle

                                    _ => throw new InvalidOperationException($"Unsupported orthogonal/diagonal pair! (Angle={orthogonalAngle}/{diagonalAngle})")
                                };
                            }

                            adjacentTile.Quadrants &= quadrants;
                            result[orthogonal] = quadrants;
                        }
                    }
                }

                return result;
            }

            #region Private Helper Methods

            private bool HasOppositeDiagonal(List<Tile> others)
            {
                if (!this.IsDiagonal)
                {
                    throw new InvalidOperationException("Operation not supported for non-diagonal tiles.");
                }

                foreach (Tile other in others.Where(t => t.IsDiagonal))
                {
                    int relativeAngle = this.GetRelativeAngle(other);
                    if (Math.Abs(relativeAngle) == 180) return true;
                }

                return false;
            }

            private bool HasPerpendicularDiagonal(List<Tile> others, out int relativeAngle)
            {
                if (!this.IsDiagonal)
                {
                    throw new InvalidOperationException("Operation not supported for non-diagonal tiles.");
                }

                foreach (Tile other in others.Where(t => t.IsDiagonal))
                {
                    relativeAngle = this.GetRelativeAngle(other);
                    if (Math.Abs(relativeAngle) == 90) return true;
                }

                relativeAngle = 0;
                return false;
            }

            #endregion
        }

        private readonly List<Edge> Edges;
        private readonly Dictionary<Point2D<float>, List<Tile>> TilesByPosition;

        public TileGraph()
        {
            this.Edges = new List<Edge>();
            this.TilesByPosition = new Dictionary<Point2D<float>, List<Tile>>();
        }

        public void AddEdge(int guid, Line2D<float> edge)
        {
            List<Point2D<float>> points = edge.Rasterize().ToList();
            AddEdge(guid, points, edge.Angle);
        }

        // Pass 1: Create Edges and group all Tiles by position
        private void AddEdge(int guid, List<Point2D<float>> points, double angle)
        {
            Edge edge = new Edge();

            for (int i = 0; i < points.Count; i++)
            {
                Point2D<float> point = new Point2D<float>(points[i].X, points[i].Y);
                Tile tile = new Tile(guid, edge, point, angle);

#if DEBUG
                tile.Label = point.ToString();
#endif

                edge.Add(tile);
                AddTile(point, tile);
            }

            Edges.Add(edge);
        }

        private void AddTile(Point2D<float> position, Tile tile)
        {
            if (!TilesByPosition.ContainsKey(position)) TilesByPosition[position] = new List<Tile>();
            TilesByPosition[position].Add(tile);
        }

        public IEnumerable<TileResult> Merge()
        {
            // Pass 2: Merge tiles at intersections
            foreach ((Point2D<float> position, List<Tile> tiles) in TilesByPosition)
            {
                if (tiles.Count > 1)
                {
#if DEBUG
                    var directions = tiles.Select(t => t.GetDirection()).Select(direction => direction.ToString()).Order();
                    Debug.WriteLine($"Intersection at ({position.X}, {position.Y}): {string.Join("+", directions)}");
#endif

                    Tile result = SelectTile(tiles);
                    result.Merge(tiles);
                }
            }

            // Pass 3: Yield return all non-zero tiles
            foreach (var edge in Edges)
            {
                foreach (var tile in edge.Tiles)
                {
                    if (tile.Quadrants == 0b0000) continue;
                    yield return tile;
                }
            }
        }

        internal List<Tile> GetTilesAtPosition(Point2D<float> position)
        {
            return TilesByPosition.TryGetValue(position, out var tiles) ? tiles : new List<Tile>();
        }

        internal static Tile SelectTile(List<Tile> tiles)
        {
            return tiles.All(t => t.IsOrthogonal) ? tiles.FirstOrDefault(t => t.IsOrthogonal) : tiles.FirstOrDefault(t => t.IsDiagonal);
        }

        internal static byte Special(byte quadrants)
        {
            return (byte)(0x80 | quadrants);
        }
    }
}
