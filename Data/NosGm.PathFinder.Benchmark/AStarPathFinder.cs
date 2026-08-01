using System;
using System.Collections.Generic;

namespace NosGm.PathFinder
{
    public static class AStarPathFinder
    {
        private class PathfinderState
        {
            public int Generation;
            public int[,] NodeGen;
            public bool[,] Closed;
            public double[,] GScore;
            public GridPos[,] Parent;
            public int CapacityX;
            public int CapacityY;

            public PathfinderState(int capacityX, int capacityY)
            {
                CapacityX = capacityX;
                CapacityY = capacityY;
                NodeGen = new int[capacityX, capacityY];
                Closed = new bool[capacityX, capacityY];
                GScore = new double[capacityX, capacityY];
                Parent = new GridPos[capacityX, capacityY];
            }

            public void EnsureCapacity(int sizeX, int sizeY)
            {
                if (sizeX > CapacityX || sizeY > CapacityY)
                {
                    CapacityX = Math.Max(CapacityX * 2, sizeX);
                    CapacityY = Math.Max(CapacityY * 2, sizeY);
                    NodeGen = new int[CapacityX, CapacityY];
                    Closed = new bool[CapacityX, CapacityY];
                    GScore = new double[CapacityX, CapacityY];
                    Parent = new GridPos[CapacityX, CapacityY];
                    Generation = 0;
                }
            }

            public double GetGScore(int x, int y) => NodeGen[x, y] == Generation ? GScore[x, y] : double.MaxValue;
            public bool IsClosed(int x, int y) => NodeGen[x, y] == Generation && Closed[x, y];

            public void SetGScore(int x, int y, double score)
            {
                if (NodeGen[x, y] != Generation)
                {
                    NodeGen[x, y] = Generation;
                    Closed[x, y] = false;
                }
                GScore[x, y] = score;
            }

            public void SetClosed(int x, int y)
            {
                if (NodeGen[x, y] != Generation)
                {
                    NodeGen[x, y] = Generation;
                    GScore[x, y] = double.MaxValue;
                }
                Closed[x, y] = true;
            }
        }

        [ThreadStatic]
        private static PathfinderState _state;
        private static readonly short[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly short[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        public static List<GridPos> FindPath(GridPos start, GridPos end, GridPos[][] grid, short maxX = -1, short maxY = -1)
        {
            if (maxX == -1) maxX = (short)grid.Length;
            if (maxY == -1 && grid.Length > 0) maxY = (short)grid[0].Length;

            if (start == null || end == null || start.X < 0 || start.Y < 0 || start.X >= maxX || start.Y >= maxY || end.X < 0 || end.Y < 0 || end.X >= maxX || end.Y >= maxY)
                return new List<GridPos>();

            if (!grid[end.X][end.Y].IsWalkable())
                return new List<GridPos>();

            if (start.X == end.X && start.Y == end.Y)
                return new List<GridPos> { start };

            if (_state == null) _state = new PathfinderState(256, 256);
            _state.EnsureCapacity(maxX, maxY);

            _state.Generation++;
            if (_state.Generation == int.MaxValue)
            {
                _state.NodeGen = new int[_state.CapacityX, _state.CapacityY];
                _state.Generation = 1;
            }
            
            var parent = _state.Parent;

            var openSet = new BinaryHeap<PathNode>(128);
            
            _state.SetGScore(start.X, start.Y, 0);
            openSet.Push(new PathNode(start.X, start.Y) { F = Heuristic.Octile(Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y)) });

            int expansions = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.Pop();
                
                if (_state.IsClosed(current.X, current.Y)) continue;
                _state.SetClosed(current.X, current.Y);

                if (current.X == end.X && current.Y == end.Y)
                {
                    return Backtrace(parent, start, end, grid);
                }

                if (++expansions > 2500) break; // Limit search depth to prevent lag

                for (int i = 0; i < 8; i++)
                {
                    short nx = (short)(current.X + dx[i]);
                    short ny = (short)(current.Y + dy[i]);

                    if (nx < 0 || ny < 0 || nx >= maxX || ny >= maxY) continue;
                    if (_state.IsClosed(nx, ny) || !grid[nx][ny].IsWalkable()) continue;

                    // Corner cutting prevention
                    if (i % 2 != 0) 
                    {
                        if (!grid[current.X][ny].IsWalkable() || !grid[nx][current.Y].IsWalkable()) continue;
                    }

                    double tentativeG = _state.GetGScore(current.X, current.Y) + (i % 2 == 0 ? 1 : 1.41421356);

                    if (tentativeG < _state.GetGScore(nx, ny))
                    {
                        parent[nx, ny] = grid[current.X][current.Y];
                        _state.SetGScore(nx, ny, tentativeG);
                        double f = tentativeG + Heuristic.Octile(Math.Abs(nx - end.X), Math.Abs(ny - end.Y));
                        openSet.Push(new PathNode(nx, ny) { F = f });
                    }
                }
            }

            return new List<GridPos>();
        }

        private static List<GridPos> Backtrace(GridPos[,] parent, GridPos start, GridPos end, GridPos[][] grid)
        {
            var path = new List<GridPos>();
            var current = end;

            while (current.X != start.X || current.Y != start.Y)
            {
                path.Add(current);
                current = parent[current.X, current.Y];
            }
            
            path.Reverse();
            return path;
        }
    }
}
