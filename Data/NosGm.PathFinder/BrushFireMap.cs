using System;
using System.Collections.Generic;

namespace NosGm.PathFinder
{
    public static class BrushFireMap
    {
        private static readonly short[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly short[] dy = { -1, 0, 1, 0, -1, 1, 1, -1 };
        private static readonly byte[] cost = { 10, 10, 10, 10, 14, 14, 14, 14 };

        public static GridPos[][] Calculate(GridPos user, GridPos[][] grid, short maxDistance = 22)
        {
            short maxX = (short)grid.Length;
            short maxY = grid.Length > 0 ? (short)grid[0].Length : (short)0;

            GridPos[][] fire = new GridPos[maxX][];
            for (int i = 0; i < maxX; i++)
            {
                fire[i] = new GridPos[maxY];
                for (int j = 0; j < maxY; j++)
                {
                    fire[i][j] = new GridPos { X = (short)i, Y = (short)j, Value = 255 };
                }
            }

            if (user.X < 0 || user.Y < 0 || user.X >= maxX || user.Y >= maxY) return fire;

            var queue = new Queue<GridPos>();
            fire[user.X][user.Y].Value = 0;
            queue.Enqueue(fire[user.X][user.Y]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Value >= maxDistance * 10) continue;

                for (int i = 0; i < 8; i++)
                {
                    short nx = (short)(current.X + dx[i]);
                    short ny = (short)(current.Y + dy[i]);

                    if (nx >= 0 && ny >= 0 && nx < maxX && ny < maxY)
                    {
                        if (grid[nx][ny].IsWalkable())
                        {
                            byte newCost = (byte)(current.Value + cost[i]);
                            if (newCost < fire[nx][ny].Value)
                            {
                                fire[nx][ny].Value = newCost;
                                queue.Enqueue(fire[nx][ny]);
                            }
                        }
                    }
                }
            }

            return fire;
        }

        public static List<GridPos> TracePath(GridPos node, GridPos[][] fireGrid, GridPos[][] mapGrid)
        {
            var path = new List<GridPos>();
            short maxX = (short)fireGrid.Length;
            short maxY = fireGrid.Length > 0 ? (short)fireGrid[0].Length : (short)0;

            if (node.X < 0 || node.Y < 0 || node.X >= maxX || node.Y >= maxY) return path;

            GridPos current = fireGrid[node.X][node.Y];
            
            while (current.Value > 0 && current.Value != 255)
            {
                path.Add(mapGrid[current.X][current.Y]);

                GridPos bestNeighbor = null;
                byte bestValue = current.Value;

                for (int i = 0; i < 8; i++)
                {
                    short nx = (short)(current.X + dx[i]);
                    short ny = (short)(current.Y + dy[i]);

                    if (nx >= 0 && ny >= 0 && nx < maxX && ny < maxY)
                    {
                        var neighbor = fireGrid[nx][ny];
                        if (neighbor.Value < bestValue)
                        {
                            bestValue = neighbor.Value;
                            bestNeighbor = neighbor;
                        }
                    }
                }

                if (bestNeighbor == null) break;
                current = bestNeighbor;
            }

            return path;
        }
    }
}
