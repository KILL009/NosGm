using System;
using System.Collections.Generic;

namespace NosGm.PathFinder
{
    public static class BestFirstSearch
    {
        // Replicating original method signatures for backward compatibility, 
        // but now redirecting to AStar engine internally or handling it better.
        // BrushFire is now its own optimized component.

        public static List<GridPos> FindPathJagged(GridPos start, GridPos end, GridPos[][] Grid, short maxX = -1, short maxY = -1)
        {
            return AStarPathFinder.FindPath(start, end, Grid, maxX, maxY);
        }

        public static GridPos[][] LoadBrushFireJagged(GridPos user, GridPos[][] Grid, short maxDistance = 22)
        {
            return BrushFireMap.Calculate(user, Grid, maxDistance);
        }

        public static List<GridPos> TracePathJagged(GridPos node, GridPos[][] Grid, GridPos[][] mapGrid)
        {
            return BrushFireMap.TracePath(node, Grid, mapGrid);
        }
    }
}
