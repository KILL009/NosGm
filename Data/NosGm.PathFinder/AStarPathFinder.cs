using System;
using System.Collections.Generic;

namespace NosGm.PathFinder
{
    public static class AStarPathFinder
    {
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

            bool[,] closed = new bool[maxX, maxY];
            double[,] gScore = new double[maxX, maxY];
            GridPos[,] parent = new GridPos[maxX, maxY];
            
            for(int i = 0; i < maxX; i++)
                for(int j = 0; j < maxY; j++)
                    gScore[i,j] = double.MaxValue;

            var openSet = new BinaryHeap<PathNode>(128);
            
            gScore[start.X, start.Y] = 0;
            openSet.Push(new PathNode(start.X, start.Y) { F = Heuristic.Octile(Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y)) });

            int expansions = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.Pop();
                
                if (closed[current.X, current.Y]) continue;
                closed[current.X, current.Y] = true;

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
                    if (closed[nx, ny] || !grid[nx][ny].IsWalkable()) continue;

                    // Corner cutting prevention
                    if (i % 2 != 0) 
                    {
                        if (!grid[current.X][ny].IsWalkable() || !grid[nx][current.Y].IsWalkable()) continue;
                    }

                    double tentativeG = gScore[current.X, current.Y] + (i % 2 == 0 ? 1 : 1.41421356);

                    if (tentativeG < gScore[nx, ny])
                    {
                        parent[nx, ny] = grid[current.X][current.Y];
                        gScore[nx, ny] = tentativeG;
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
